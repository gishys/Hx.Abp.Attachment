# TEMPLATE_ID 字段可空性修复总结

## 问题描述

在智能推荐功能中，`APPATTACH_CATALOGUES` 表的 `TEMPLATE_ID` 字段被定义为可空字段，但代码中没有正确处理可空性，导致查询失败。

## 修复内容

### 1. 数据库迁移脚本修复

**文件**: `postgresql-fulltext-search/template-usage-count-migration.sql`

**修改**:

```sql
-- 修改前
ALTER TABLE "APPATTACH_CATALOGUES"
ADD COLUMN "TEMPLATE_ID" uuid;

-- 修改后
ALTER TABLE "APPATTACH_CATALOGUES"
ADD COLUMN "TEMPLATE_ID" uuid NULL;
```

### 2. Entity Framework 配置修复

**文件**: `src/Hx.Abp.Attachment.EntityFrameworkCore/Hx/Abp/Attachment/EntityFrameworkCore/AttachCatalogueEntityTypeConfiguration.cs`

**修改**:

```csharp
// 修改前
builder.Property(d => d.TemplateId).HasColumnName("TEMPLATE_ID");

// 修改后
builder.Property(d => d.TemplateId).HasColumnName("TEMPLATE_ID").IsRequired(false);
```

### 3. 仓储方法修复

**文件**: `src/Hx.Abp.Attachment.EntityFrameworkCore/Hx/Abp/Attachment/EntityFrameworkCore/AttachCatalogueTemplateRepository.cs`

#### 3.1 GetTemplateUsageCountAsync 方法

**修改内容**:

-   在 SQL 查询中添加 `IS NOT NULL` 条件
-   改进错误处理

```csharp
// 正确处理可空字段的查询
var sql = @"
    SELECT COUNT(*) as usage_count
    FROM ""APPATTACH_CATALOGUES"" ac
    WHERE ac.""TEMPLATE_ID"" IS NOT NULL
      AND ac.""TEMPLATE_ID"" = @templateId
      AND ac.""IS_DELETED"" = false";
```

#### 3.2 ExtractNamePatternFromFilesAsync 方法

**修改内容**:

-   在 SQL 查询中添加 `IS NOT NULL` 条件
-   改进错误处理

```csharp
// 正确处理可空字段的查询
var sql = @"
    SELECT DISTINCT
        CASE
            WHEN af.""FILE_NAME"" LIKE '%{ProjectName}%' THEN '项目_{ProjectName}_{Date}_{Version}'
            WHEN af.""FILE_NAME"" LIKE '%{Date}%' THEN '{Type}_{Date}_{Version}'
            WHEN af.""FILE_NAME"" LIKE '%{Version}%' THEN '{Type}_{ProjectName}_{Version}'
            ELSE '{Type}_{ProjectName}_{Date}'
        END as name_pattern
    FROM ""APPATTACH_FILES"" af
    INNER JOIN ""APPATTACH_CATALOGUES"" ac ON af.""CATALOGUE_ID"" = ac.""ID""
    WHERE ac.""TEMPLATE_ID"" IS NOT NULL
      AND ac.""TEMPLATE_ID"" = @templateId
    LIMIT 1";
```

## 实体定义确认

**文件**: `src/Hx.Abp.Attachment.Domain/Hx/Abp/Attachment/Domain/AttachCatalogue.cs`

实体中的 `TemplateId` 属性已正确定义为可空类型：

```csharp
/// <summary>
/// 关联的模板ID
/// </summary>
public virtual Guid? TemplateId { get; private set; }
```

## 修复效果

1. **数据库层面**: 明确指定 `TEMPLATE_ID` 字段为可空
2. **EF Core 配置**: 明确指定字段为可空
3. **查询层面**: 正确处理可空字段，避免 NULL 值导致的查询问题
4. **错误处理**: 改进异常处理，提高系统健壮性
5. **向后兼容**: 支持字段不存在的情况，提供默认值

## 执行步骤

1. 执行数据库迁移脚本：

    ```sql
    ALTER TABLE "APPATTACH_CATALOGUES"
    ADD COLUMN "TEMPLATE_ID" uuid NULL;
    ```

2. 重新编译并部署应用程序

3. 验证智能推荐功能是否正常工作

## 注意事项

-   如果数据库中已经存在 `TEMPLATE_ID` 字段，需要先删除再重新添加
-   确保所有相关的 SQL 查询都正确处理了可空字段
-   在生产环境中执行迁移前，建议先在测试环境验证
