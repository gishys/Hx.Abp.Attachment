# Embedding 字段修复说明

## 问题描述

在修改 `Reference` 字段后，应用启动时出现以下错误：

```
The 'float[]' property 'AttachCatalogue.Embedding' could not be mapped to the database type 'vector(384)' because the database provider does not support mapping 'float[]' properties to 'vector(384)' columns.
```

## 问题原因

1. **pgvector 扩展配置问题** - Entity Framework Core 无法正确映射 `float[]` 类型到 PostgreSQL 的 `vector(384)` 类型
2. **缺少必要的配置** - 需要特殊的 pgvector 配置来支持向量类型映射
3. **数据库字段缺失** - 数据库中可能缺少 `EMBEDDING` 字段或相关索引

## 解决方案

### 方案一：临时禁用 Embedding 字段（推荐）

为了避免 pgvector 配置问题，我们暂时禁用了 Embedding 字段：

#### 1. 实体类修改

```csharp
/// <summary>
/// 语义检索向量
/// </summary>
[System.ComponentModel.DataAnnotations.Schema.NotMapped]
public virtual float[]? Embedding { get; private set; }
```

#### 2. 实体配置修改

```csharp
// 语义检索配置 - 暂时忽略 Embedding 字段以避免 pgvector 配置问题
// builder.Property(d => d.Embedding)
//     .HasColumnName("EMBEDDING")
//     .HasColumnType("vector(384)")
//     .HasVectorDimensions(384);

// 向量索引 - 暂时注释掉
// builder.HasIndex(d => d.Embedding)
//     .HasDatabaseName("IDX_ATTACH_CATALOGUES_EMBEDDING")
//     .HasMethod("ivfflat")
//     .HasOperators("vector_cosine_ops");
```

#### 3. 数据库清理脚本

运行 `disable-embedding.sql` 脚本来清理数据库中的相关字段和索引。

### 方案二：完整配置 pgvector（可选）

如果需要使用语义搜索功能，可以按照以下步骤配置：

#### 1. 确保 pgvector 扩展已安装

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

#### 2. 添加 Embedding 字段

```sql
ALTER TABLE "APPATTACH_CATALOGUES"
ADD COLUMN "EMBEDDING" vector(384);
```

#### 3. 创建向量索引

```sql
CREATE INDEX "IDX_ATTACH_CATALOGUES_EMBEDDING"
ON "APPATTACH_CATALOGUES"
USING ivfflat ("EMBEDDING" vector_cosine_ops);
```

#### 4. 修改实体配置

取消注释 Embedding 相关的配置代码。

## 文件清单

### 修改的文件

-   `AttachCatalogue.cs` - 添加 `[NotMapped]` 属性
-   `AttachCatalogueEntityTypeConfiguration.cs` - 注释掉 Embedding 配置

### 新增的文件

-   `disable-embedding.sql` - 清理数据库脚本
-   `fix-embedding-field.sql` - 完整配置脚本
-   `EMBEDDING_FIELD_FIX.md` - 本说明文档

## 验证步骤

### 1. 运行清理脚本

```bash
psql -d your_database -f postgresql-fulltext-search/disable-embedding.sql
```

### 2. 重启应用

应用应该能够正常启动，不再出现 Embedding 字段映射错误。

### 3. 验证功能

-   全文搜索功能应该正常工作
-   模糊搜索功能应该正常工作
-   组合搜索功能应该正常工作

## 后续计划

1. **短期** - 使用方案一，确保应用正常运行
2. **中期** - 研究 pgvector 的正确配置方法
3. **长期** - 如果需要语义搜索功能，重新启用 Embedding 字段

## 注意事项

-   禁用 Embedding 字段后，语义搜索功能将不可用
-   全文搜索和模糊搜索功能不受影响
-   如果将来需要语义搜索，可以参考 `fix-embedding-field.sql` 脚本重新配置
