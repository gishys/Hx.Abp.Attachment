# PostgreSQL 中文全文搜索解决方案

## 概述

本项目提供了在 PostgreSQL 中实现中文全文搜索和模糊查询的完整解决方案，使用 PostgreSQL 内置功能，无需安装第三方扩展。

## 功能特性

### 1. 全文搜索

-   使用 PostgreSQL 内置的 `to_tsvector` 和 `plainto_tsquery`
-   支持中文文本搜索
-   按相关性排序

### 2. 模糊搜索

-   使用 `pg_trgm` 扩展
-   支持相似度匹配
-   容错性强

### 3. 组合搜索

-   结合全文搜索和模糊搜索
-   智能排序算法
-   提供最佳搜索结果

## 技术实现

### 数据库配置

```sql
-- 创建中文全文搜索配置
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'chinese_fts'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese_fts
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
    END IF;
END $$;

-- 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;
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
            WHERE to_tsvector('chinese_fts', ""CATALOGUE_NAME"") @@ plainto_tsquery('chinese_fts', @query)
            ORDER BY ts_rank(to_tsvector('chinese_fts', ""CATALOGUE_NAME""), plainto_tsquery('chinese_fts', @query)) DESC
        ";

        return await _context.AttachCatalogues
            .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
            .ToListAsync();
    }

    // 模糊搜索目录
    public async Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query)
    {
        var sql = @"
            SELECT * FROM ""APPATTACH_CATALOGUES""
            WHERE ""CATALOGUE_NAME"" % @query
            ORDER BY similarity(""CATALOGUE_NAME"", @query) DESC
        ";

        return await _context.AttachCatalogues
            .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
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

# 搜索文件（类似接口）
GET /api/FullTextSearch/files?query=搜索关键词
```

## 优势

✅ **无兼容性问题** - 使用 PostgreSQL 内置功能
✅ **性能优秀** - 支持索引，查询速度快
✅ **配置简单** - 无需额外扩展安装
✅ **维护成本低** - 标准 PostgreSQL 功能
✅ **功能完整** - 支持多种搜索模式

## 文件说明

-   `README.md` - 本说明文档
-   `IMPLEMENTATION_GUIDE.md` - 解决方案详细说明
-   `database-test.sql` - 数据库测试脚本

## 使用步骤

1. **启动应用** - 应用会自动初始化搜索配置
2. **测试功能** - 使用 `/api/FullTextSearch/test` 接口
3. **执行搜索** - 使用各种搜索接口进行实际搜索

## 测试

运行 `database-test.sql` 脚本可以测试数据库功能：

```sql
-- 测试全文搜索
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能');

-- 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索');

-- 实际搜索示例
SELECT * FROM "APPATTACH_CATALOGUES"
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试')
ORDER BY ts_rank(to_tsvector('chinese_fts', "CATALOGUE_NAME"), plainto_tsquery('chinese_fts', '测试')) DESC;
```

## 总结

这个解决方案使用 PostgreSQL 内置功能实现了强大的中文全文搜索和模糊查询功能，避免了第三方扩展的兼容性问题，提供了稳定、高效的搜索体验。
