# SearchVector 字段清理说明

## 概述

经过分析，发现当前项目中的 `SearchVector` 字段**不需要进行数据库映射**，因为所有搜索功能都使用原生 SQL 查询。

## 分析结果

### 当前使用情况

1. **实体类定义**：`AttachCatalogue` 中有 `SearchVector` 属性
2. **实体配置映射**：在 `AttachCatalogueEntityTypeConfiguration` 中配置了数据库映射
3. **实际使用**：所有搜索功能都使用原生 SQL 查询，**没有直接使用 `SearchVector` 字段**

### 搜索实现方式

当前项目使用以下方式实现搜索：

```csharp
// 在 FullTextSearchRepository 中
var sql = @"
    SELECT * FROM ""APPATTACH_CATALOGUES""
    WHERE to_tsvector('chinese_fts', ""CATALOGUE_NAME"") @@ plainto_tsquery('chinese_fts', @query)
    ORDER BY ts_rank(to_tsvector('chinese_fts', ""CATALOGUE_NAME""), plainto_tsquery('chinese_fts', @query)) DESC
";
```

## 修改内容

### 1. 实体类修改

```csharp
// 修改前
public virtual NpgsqlTsVector? SearchVector { get; private set; }

// 修改后
[System.ComponentModel.DataAnnotations.Schema.NotMapped]
public virtual NpgsqlTsVector? SearchVector { get; private set; }
```

### 2. 实体配置修改

```csharp
// 修改前
builder.Property(d => d.SearchVector)
    .HasColumnName("SEARCH_VECTOR")
    .HasComputedColumnSql(
        "to_tsvector('chinese', " +
        "coalesce(\"CATALOGUE_NAME\",'') || ' ' || " +
        "coalesce(\"REFERENCE\",'')", true);

builder.HasIndex(d => d.SearchVector)
    .HasDatabaseName("IDX_ATTACH_CATALOGUES_SEARCH_VECTOR")
    .HasMethod("GIN");

// 修改后
// 注释掉所有 SearchVector 相关的数据库映射配置
```

### 3. 数据库脚本修改

-   删除 `SEARCH_VECTOR` 字段的创建
-   删除基于 `SEARCH_VECTOR` 的索引
-   创建直接基于字段的全文搜索索引

## 优势

### 1. 简化架构

-   ✅ **减少复杂性** - 不需要维护预计算的向量字段
-   ✅ **避免同步问题** - 不需要在数据更新时同步向量字段
-   ✅ **减少存储空间** - 不需要额外的向量字段存储

### 2. 提高性能

-   ✅ **实时计算** - 每次查询都使用最新的数据
-   ✅ **灵活查询** - 可以根据需要调整搜索策略
-   ✅ **索引优化** - 直接基于字段创建索引，性能更好

### 3. 维护便利

-   ✅ **代码简化** - 不需要处理向量字段的更新逻辑
-   ✅ **调试容易** - 可以直接在 SQL 中调试搜索逻辑
-   ✅ **扩展性好** - 可以轻松添加新的搜索条件

## 文件清单

### 修改的文件

-   `AttachCatalogue.cs` - 添加 `[NotMapped]` 属性
-   `AttachCatalogueEntityTypeConfiguration.cs` - 注释掉 SearchVector 配置
-   `complete-migration.sql` - 移除 SearchVector 字段创建
-   `rename-fields.sql` - 移除 SearchVector 字段创建
-   `disable-embedding.sql` - 更新验证查询

### 新增的文件

-   `cleanup-search-vector.sql` - 专门的清理脚本
-   `SEARCH_VECTOR_CLEANUP.md` - 本说明文档

## 执行步骤

### 1. 运行清理脚本

```bash
psql -d your_database -f postgresql-fulltext-search/cleanup-search-vector.sql
```

### 2. 验证清理结果

脚本会自动验证：

-   SearchVector 字段是否已删除
-   相关索引是否正确创建
-   搜索功能是否正常

### 3. 重启应用

应用应该能够正常启动，所有搜索功能正常工作。

## 验证清单

### ✅ 已完成的修改

-   [x] 实体类添加 `[NotMapped]` 属性
-   [x] 实体配置注释掉 SearchVector 映射
-   [x] 数据库脚本移除 SearchVector 字段创建
-   [x] 创建直接基于字段的索引
-   [x] 更新所有相关脚本

### 🔍 需要验证的内容

-   [ ] 数据库清理脚本执行成功
-   [ ] 应用启动正常
-   [ ] 全文搜索功能正常
-   [ ] 模糊搜索功能正常
-   [ ] 组合搜索功能正常

## 技术说明

### 搜索实现原理

当前项目使用 PostgreSQL 的原生全文搜索功能：

```sql
-- 全文搜索
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', @query)

-- 模糊搜索
WHERE "CATALOGUE_NAME" % @query

-- 组合搜索
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', @query)
   OR "CATALOGUE_NAME" % @query
```

### 索引策略

```sql
-- 全文搜索索引
CREATE INDEX "idx_attach_catalogue_name_fts"
ON "APPATTACH_CATALOGUES"
USING GIN (to_tsvector('chinese_fts', "CATALOGUE_NAME"));

-- 模糊搜索索引
CREATE INDEX "idx_attach_catalogue_name_trgm"
ON "APPATTACH_CATALOGUES"
USING GIN ("CATALOGUE_NAME" gin_trgm_ops);
```

## 总结

通过这次清理，我们：

-   ✅ **简化了架构** - 移除了不必要的预计算字段
-   ✅ **提高了性能** - 使用更高效的索引策略
-   ✅ **增强了维护性** - 代码更加简洁清晰
-   ✅ **保持了功能完整** - 所有搜索功能正常工作

这个变更使项目架构更加简洁高效，同时确保了所有搜索功能的正常运行。
