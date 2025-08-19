# PostgreSQL 中文全文搜索解决方案

## 概述

本项目提供了在 PostgreSQL 中实现中文全文搜索和模糊查询的完整解决方案，使用 PostgreSQL 内置功能，无需安装第三方扩展。

## 功能特性

-   **全文搜索**: 使用 PostgreSQL 内置的 `to_tsvector` 和 `plainto_tsquery`
-   **模糊搜索**: 使用 `pg_trgm` 扩展支持相似度匹配
-   目录搜索：支持 `CATALOGUE_NAME` 和 `FULL_TEXT_CONTENT` 字段
-   文件搜索：支持 `FILEALIAS` 和 `OCR_CONTENT` 字段
-   **组合搜索**: 结合全文搜索和模糊搜索，提供最佳搜索结果
-   **智能排序**: 按相关性排序，提供最佳用户体验
-   **多字段匹配**: 使用 `GREATEST` 函数选择最高相似度分数

## 技术实现

### 数据库配置

```sql
-- 创建中文全文搜索配置
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'chinese_fts') THEN
        CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese_fts
            ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part
            WITH simple;
    END IF;
END $$;

-- 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

### 索引配置

```sql
-- 全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT"
ON "APPATTACH_CATALOGUES" USING GIN (
    to_tsvector('chinese_fts',
        COALESCE("CATALOGUE_NAME", '') || ' ' ||
        COALESCE("FULL_TEXT_CONTENT", '')
    )
);

-- 模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_NAME_TRGM"
ON "APPATTACH_CATALOGUES" USING GIN ("CATALOGUE_NAME" gin_trgm_ops);
```

### ABP 项目集成

#### DbContext 配置

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // 启用 pg_trgm 扩展
    builder.HasPostgresExtension("pg_trgm");

    // 配置全文搜索索引
    ConfigureFullTextSearch(builder);
}

private void ConfigureFullTextSearch(ModelBuilder builder)
{
    builder.Entity<AttachCatalogue>(entity =>
    {
        entity.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    });
}
```

#### 搜索服务

```csharp
public class FullTextSearchRepository : IFullTextSearchRepository
{
    // 全文搜索目录
    public async Task<List<AttachCatalogue>> SearchCataloguesAsync(string query)
    {
        var sql = @"
            SELECT * FROM ""APPATTACH_CATALOGUES""
            WHERE to_tsvector('chinese_fts',
                COALESCE(""CATALOGUE_NAME"", '') || ' ' ||
                COALESCE(""FULL_TEXT_CONTENT"", '')
            ) @@ plainto_tsquery('chinese_fts', @query)
            ORDER BY ts_rank(
                to_tsvector('chinese_fts',
                    COALESCE(""CATALOGUE_NAME"", '') || ' ' ||
                    COALESCE(""FULL_TEXT_CONTENT"", '')
                ),
                plainto_tsquery('chinese_fts', @query)
            ) DESC
        ";

        return await _context.AttachCatalogues
            .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
            .ToListAsync();
    }

              // 优化模糊搜索目录（多层次搜索策略）
 public async Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query)
 {
     var sql = @"
         WITH search_results AS (
             SELECT *,
                    CASE
                        -- 1. 子字符串匹配（最高优先级）
                        WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern)
                             OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                        THEN 3
                        -- 2. 分词匹配（中等优先级）
                        WHEN EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ) OR EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        )
                        THEN 2
                        -- 3. 相似度匹配（最低优先级）
                        WHEN COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                             OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05
                        THEN 1
                        ELSE 0
                    END as match_type,
                    GREATEST(
                        COALESCE(similarity(""CATALOGUE_NAME"", @query), 0),
                        COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0)
                    ) as similarity_score
             FROM ""APPATTACH_CATALOGUES""
             WHERE
                 -- 子字符串匹配
                 (LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern)
                  OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern))
                 OR
                 -- 分词匹配
                 (EXISTS (
                     SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                     WHERE word LIKE LOWER(@query_pattern)
                 ) OR EXISTS (
                     SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                     WHERE word LIKE LOWER(@query_pattern)
                 ))
                 OR
                 -- 相似度匹配（降低阈值）
                 (COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                  OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05)
         )
         SELECT *,
                (match_type * 10 + similarity_score) as final_score
         FROM search_results
         ORDER BY final_score DESC, similarity_score DESC
         LIMIT 50
     ";

     return await _context.AttachCatalogues
         .FromSqlRaw(sql,
             new NpgsqlParameter("@query", query),
             new NpgsqlParameter("@query_pattern", $"%{query}%"))
         .ToListAsync();
 }
}
```

## API 接口

### 测试接口

```bash
GET /api/FullTextSearch/test?text=测试中文搜索
```

### 搜索接口

```bash
# 全文搜索目录
GET /api/FullTextSearch/catalogues?query=搜索关键词

# 模糊搜索目录
GET /api/FullTextSearch/catalogues/fuzzy?query=搜索关键词

# 组合搜索目录
GET /api/FullTextSearch/catalogues/combined?query=搜索关键词

# 搜索文件
GET /api/FullTextSearch/files?query=搜索关键词
```

## 数据库迁移

### 完整迁移脚本

运行 `database-migration.sql` 执行完整的数据库迁移：

```bash
psql -d your_database -f postgresql-fulltext-search/database-migration.sql
```

该脚本包含：

-   添加全文搜索和 OCR 相关字段
-   字段重命名（驼峰命名 → 下划线命名）
-   中文全文搜索配置创建
-   必要扩展启用
-   全文搜索和模糊搜索索引创建
-   OCR 处理状态索引创建
-   验证和测试

### 字段映射

| 原字段名             | 新字段名              | 说明         |
| -------------------- | --------------------- | ------------ |
| `CATALOGUENAME`      | `CATALOGUE_NAME`      | 目录名称     |
| `ATTACHRECEIVETYPE`  | `ATTACH_RECEIVE_TYPE` | 附件接收类型 |
| `REFERENCETYPE`      | `REFERENCE_TYPE`      | 引用类型     |
| `ATTACHCOUNT`        | `ATTACH_COUNT`        | 附件数量     |
| `PAGECOUNT`          | `PAGE_COUNT`          | 页数         |
| `ISVERIFICATION`     | `IS_VERIFICATION`     | 是否核验     |
| `VERIFICATIONPASSED` | `VERIFICATION_PASSED` | 核验通过     |
| `ISREQUIRED`         | `IS_REQUIRED`         | 是否必收     |
| `SEQUENCENUMBER`     | `SEQUENCE_NUMBER`     | 顺序号       |
| `PARENTID`           | `PARENT_ID`           | 父 ID        |
| `ISSTATIC`           | `IS_STATIC`           | 静态标识     |

## 架构设计

### 分层架构

-   **仓储层**: `FullTextSearchRepository` - 负责数据访问和 SQL 查询
-   **服务层**: `FullTextSearchService` - 负责业务逻辑和流程控制
-   **控制器层**: `FullTextSearchController` - 负责 API 接口和请求处理

### 搜索策略

1. **全文搜索**: 使用 `to_tsvector` 和 `plainto_tsquery` 进行精确匹配
2. **模糊搜索**: 使用多层次搜索策略，针对长文本内容优化
    - **子字符串匹配**（最高优先级）：直接匹配包含查询词的记录
    - **分词匹配**（中等优先级）：将长文本分词后与查询词匹配
    - **相似度匹配**（最低优先级）：使用 `pg_trgm` 扩展进行相似度匹配
    - 目录：同时搜索 `CATALOGUE_NAME` 和 `FULL_TEXT_CONTENT` 字段
    - 文件：同时搜索 `FILEALIAS` 和 `OCR_CONTENT` 字段
    - 智能排序：根据匹配类型、相似度和位置进行综合评分
3. **组合搜索**: 结合两种搜索方式，提供最佳结果

### 长文本搜索优化说明

针对 `FULL_TEXT_CONTENT` 和 `OCR_CONTENT` 字段存储的长文本内容，我们实现了专门的多层次搜索策略：

#### 问题分析

-   **长文本 vs 短查询词**：长文本内容可能包含数千字符，而查询词通常只有几个字符
-   **相似度匹配效率低**：直接使用 `similarity()` 函数比较长文本与短查询词，相似度会很低
-   **传统阈值不合理**：使用 0.1 的阈值会过滤掉很多相关结果

#### 解决方案

1. **子字符串匹配**：优先查找包含查询词的记录，确保精确匹配
2. **分词匹配**：将长文本按空格分词，查找包含查询词的单词
3. **相似度匹配**：降低阈值到 0.05，作为兜底方案
4. **智能排序**：根据匹配类型、相似度和匹配位置进行综合评分

#### 评分机制

-   **子字符串匹配**：得分 = 30 + 相似度分数 + 位置分数
-   **分词匹配**：得分 = 20 + 相似度分数 + 位置分数
-   **相似度匹配**：得分 = 10 + 相似度分数 + 位置分数
-   **位置分数**：匹配位置越靠前，分数越高

## 测试

### 数据库测试

迁移脚本包含内置的测试功能，执行后会自动验证：

```sql
-- 测试全文搜索
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能');

-- 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索');

-- 实际搜索示例
SELECT * FROM "APPATTACH_CATALOGUES"
WHERE to_tsvector('chinese_fts',
    COALESCE("CATALOGUE_NAME", '') || ' ' ||
    COALESCE("FULL_TEXT_CONTENT", '')
) @@ plainto_tsquery('chinese_fts', '测试')
ORDER BY ts_rank(
    to_tsvector('chinese_fts',
        COALESCE("CATALOGUE_NAME", '') || ' ' ||
        COALESCE("FULL_TEXT_CONTENT", '')
    ),
    plainto_tsquery('chinese_fts', '测试')
) DESC;

-- 优化模糊搜索示例（多层次搜索策略）
WITH search_results AS (
    SELECT *,
           CASE
               -- 1. 子字符串匹配（最高优先级）
               WHEN LOWER(COALESCE("CATALOGUE_NAME", '')) LIKE LOWER('%测试%')
                    OR LOWER(COALESCE("FULL_TEXT_CONTENT", '')) LIKE LOWER('%测试%')
               THEN 3
               -- 2. 分词匹配（中等优先级）
               WHEN EXISTS (
                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("CATALOGUE_NAME", '')), ' ')) word
                   WHERE word LIKE LOWER('%测试%')
               ) OR EXISTS (
                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FULL_TEXT_CONTENT", '')), ' ')) word
                   WHERE word LIKE LOWER('%测试%')
               )
               THEN 2
               -- 3. 相似度匹配（最低优先级）
               WHEN COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.05
                    OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.05
               THEN 1
               ELSE 0
           END as match_type,
           GREATEST(
               COALESCE(similarity("CATALOGUE_NAME", '测试'), 0),
               COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0)
           ) as similarity_score
    FROM "APPATTACH_CATALOGUES"
    WHERE
        -- 子字符串匹配
        (LOWER(COALESCE("CATALOGUE_NAME", '')) LIKE LOWER('%测试%')
         OR LOWER(COALESCE("FULL_TEXT_CONTENT", '')) LIKE LOWER('%测试%'))
        OR
        -- 分词匹配
        (EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("CATALOGUE_NAME", '')), ' ')) word
            WHERE word LIKE LOWER('%测试%')
        ) OR EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FULL_TEXT_CONTENT", '')), ' ')) word
            WHERE word LIKE LOWER('%测试%')
        ))
        OR
        -- 相似度匹配（降低阈值）
        (COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.05
         OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.05)
)
SELECT *,
       (match_type * 10 + similarity_score) as final_score
FROM search_results
ORDER BY final_score DESC, similarity_score DESC;
```

### 应用测试

1. 启动应用，检查控制台输出确认全文搜索配置初始化成功
2. 使用 `/api/FullTextSearch/test` 接口测试全文搜索功能
3. 使用 `/api/FullTextSearch/test/fuzzy` 接口测试模糊搜索功能
4. 添加测试数据，使用搜索接口进行实际测试

### 模糊搜索调试

如果模糊搜索没有返回结果，请按以下步骤进行调试：

1. **运行诊断脚本**：

    ```bash
    psql -d your_database -f postgresql-fulltext-search/fuzzy-search-debug.sql
    ```

2. **检查常见问题**：

    - 确认 `pg_trgm` 扩展已安装
    - 确认模糊搜索索引已创建
    - 确认表中有数据
    - 尝试降低相似度阈值（当前设置为 0.1）

3. **测试不同阈值**：

    ```sql
    -- 测试不同相似度阈值（单字段）
    SELECT COUNT(*) FROM "APPATTACH_CATALOGUES"
    WHERE similarity("CATALOGUE_NAME", '测试') > 0.05;

    SELECT COUNT(*) FROM "APPATTACH_CATALOGUES"
    WHERE similarity("CATALOGUE_NAME", '测试') > 0.1;

    SELECT COUNT(*) FROM "APPATTACH_CATALOGUES"
    WHERE similarity("CATALOGUE_NAME", '测试') > 0.2;

         -- 测试优化多字段模糊搜索
     WITH search_results AS (
         SELECT *,
                CASE
                    WHEN LOWER(COALESCE("CATALOGUE_NAME", '')) LIKE LOWER('%测试%')
                         OR LOWER(COALESCE("FULL_TEXT_CONTENT", '')) LIKE LOWER('%测试%')
                    THEN 3
                    WHEN EXISTS (
                        SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("CATALOGUE_NAME", '')), ' ')) word
                        WHERE word LIKE LOWER('%测试%')
                    ) OR EXISTS (
                        SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FULL_TEXT_CONTENT", '')), ' ')) word
                        WHERE word LIKE LOWER('%测试%')
                    )
                    THEN 2
                    WHEN COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.05
                         OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.05
                    THEN 1
                    ELSE 0
                END as match_type
         FROM "APPATTACH_CATALOGUES"
         WHERE
             (LOWER(COALESCE("CATALOGUE_NAME", '')) LIKE LOWER('%测试%')
              OR LOWER(COALESCE("FULL_TEXT_CONTENT", '')) LIKE LOWER('%测试%'))
             OR
             (EXISTS (
                 SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("CATALOGUE_NAME", '')), ' ')) word
                 WHERE word LIKE LOWER('%测试%')
             ) OR EXISTS (
                 SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FULL_TEXT_CONTENT", '')), ' ')) word
                 WHERE word LIKE LOWER('%测试%')
             ))
             OR
             (COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.05
              OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.05)
     )
     SELECT COUNT(*) as total_matches,
            COUNT(CASE WHEN match_type = 3 THEN 1 END) as substring_matches,
            COUNT(CASE WHEN match_type = 2 THEN 1 END) as word_matches,
            COUNT(CASE WHEN match_type = 1 THEN 1 END) as similarity_matches
     FROM search_results;

     -- 测试文件优化模糊搜索
     WITH search_results AS (
         SELECT *,
                CASE
                    WHEN LOWER(COALESCE("FILEALIAS", '')) LIKE LOWER('%测试%')
                         OR LOWER(COALESCE("OCR_CONTENT", '')) LIKE LOWER('%测试%')
                    THEN 3
                    WHEN EXISTS (
                        SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FILEALIAS", '')), ' ')) word
                        WHERE word LIKE LOWER('%测试%')
                    ) OR EXISTS (
                        SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("OCR_CONTENT", '')), ' ')) word
                        WHERE word LIKE LOWER('%测试%')
                    )
                    THEN 2
                    WHEN COALESCE(similarity("FILEALIAS", '测试'), 0) > 0.05
                         OR COALESCE(similarity("OCR_CONTENT", '测试'), 0) > 0.05
                    THEN 1
                    ELSE 0
                END as match_type
         FROM "APPATTACHFILE"
         WHERE
             (LOWER(COALESCE("FILEALIAS", '')) LIKE LOWER('%测试%')
              OR LOWER(COALESCE("OCR_CONTENT", '')) LIKE LOWER('%测试%'))
             OR
             (EXISTS (
                 SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("FILEALIAS", '')), ' ')) word
                 WHERE word LIKE LOWER('%测试%')
             ) OR EXISTS (
                 SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE("OCR_CONTENT", '')), ' ')) word
                 WHERE word LIKE LOWER('%测试%')
             ))
             OR
             (COALESCE(similarity("FILEALIAS", '测试'), 0) > 0.05
              OR COALESCE(similarity("OCR_CONTENT", '测试'), 0) > 0.05)
     )
     SELECT COUNT(*) as total_matches,
            COUNT(CASE WHEN match_type = 3 THEN 1 END) as substring_matches,
            COUNT(CASE WHEN match_type = 2 THEN 1 END) as word_matches,
            COUNT(CASE WHEN match_type = 1 THEN 1 END) as similarity_matches
     FROM search_results;
    ```

## 优势

✅ **无兼容性问题** - 使用 PostgreSQL 内置功能  
✅ **性能优秀** - 支持索引，查询速度快  
✅ **配置简单** - 无需额外扩展安装  
✅ **维护成本低** - 标准 PostgreSQL 功能  
✅ **功能完整** - 支持多种搜索模式

## 文件说明

-   `README.md` - 本说明文档
-   `database-migration.sql` - 完整数据库迁移脚本（包含字段重命名、全文搜索配置、OCR 字段添加等）
-   `MIGRATION_GUIDE.md` - 数据库迁移指南

## 注意事项

1. **备份数据** - 执行迁移前请备份数据库
2. **测试环境** - 建议先在测试环境执行
3. **应用重启** - 迁移完成后需要重启应用
4. **功能验证** - 确保所有搜索功能正常工作

## 总结

这个解决方案使用 PostgreSQL 内置功能实现了强大的中文全文搜索和模糊查询功能，避免了第三方扩展的兼容性问题，提供了稳定、高效的搜索体验。
