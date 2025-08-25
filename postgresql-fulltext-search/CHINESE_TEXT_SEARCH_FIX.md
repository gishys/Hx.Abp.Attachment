# 中文文本搜索配置修复指南

## 问题描述

在使用智能推荐功能时，出现以下错误：

```
Npgsql.PostgresException (0x80004005): 42704: 文本搜寻配置 "chinese" 不存在
```

这是因为 PostgreSQL 默认不包含中文全文搜索配置。

## 解决方案

### 方案 1：创建中文文本搜索配置（推荐）

执行以下 SQL 脚本来创建必要的文本搜索配置：

```sql
-- 创建 chinese 文本搜索配置
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'chinese'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
    END IF;
END $$;
```

### 方案 2：使用 EF Core LINQ 查询（已实现）

我们已经修改了代码，将 PostgreSQL 特定的全文搜索查询替换为 EF Core LINQ 查询，这样就不需要依赖 PostgreSQL 的文本搜索配置了。

## 修改内容

### 1. Repository 层修改

在 `AttachCatalogueTemplateRepository.cs` 中：

-   移除了使用 `to_tsvector('chinese', ...)` 的 SQL 查询
-   实现了 `CalculateBusinessMatchScore` 方法来计算业务匹配分数
-   使用 EF Core LINQ 查询和内存中的业务逻辑评分

### 2. 业务逻辑评分算法

新的评分算法包括：

1. **业务关键词匹配**：每个关键词匹配增加 0.3 分
2. **文件类型关键词匹配**：每个文件类型匹配增加 0.2 分
3. **完全匹配加分**：完全匹配额外加 0.5 分
4. **长度匹配**：较长的匹配文本得分更高

### 3. 权重分配

-   模板名称匹配：权重 1.2（最高）
-   语义模型匹配：权重 1.1（中等）
-   规则表达式匹配：权重 0.8（较低）

## 执行步骤

### 1. 应用数据库修复

```bash
# 连接到PostgreSQL数据库
psql -U your_username -d your_database

# 执行修复脚本
\i postgresql-fulltext-search/fix-chinese-text-search.sql
```

### 2. 验证修复

```sql
-- 检查配置是否存在
SELECT cfgname, cfgparser
FROM pg_ts_config
WHERE cfgname IN ('chinese', 'chinese_fts');

-- 测试文本搜索功能
SELECT to_tsvector('chinese', '工程文档管理系统') as test_vector;
```

### 3. 重新部署应用

```bash
# 重新编译项目
dotnet build

# 重启应用
dotnet run
```

## 优势

### 使用 EF Core LINQ 的优势

1. **数据库无关性**：不依赖 PostgreSQL 特定的文本搜索功能
2. **更好的可移植性**：可以轻松迁移到其他数据库
3. **类型安全**：编译时类型检查
4. **更好的调试**：可以在 C#代码中设置断点调试
5. **性能可控**：可以优化内存中的算法

### 业务逻辑评分的优势

1. **更精确的匹配**：可以根据业务需求定制评分算法
2. **更好的可扩展性**：容易添加新的评分规则
3. **更好的可维护性**：业务逻辑集中在 C#代码中

## 注意事项

1. **性能考虑**：对于大量数据，内存中的评分可能影响性能
2. **缓存策略**：建议对频繁查询的结果进行缓存
3. **分页处理**：对于大量结果，建议实现分页机制

## 相关文件

-   `fix-chinese-text-search.sql`：数据库修复脚本
-   `AttachCatalogueTemplateRepository.cs`：修改后的 Repository 实现
-   `IntelligentRecommendationAppService.cs`：智能推荐应用服务
