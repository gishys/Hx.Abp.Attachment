# Reference 字段修改总结

## 概述

根据用户要求，将 `AttachCatalogue` 实体中的 `Reference` 字段从可空修改为不可为空，确保数据完整性。

## 修改内容

### 1. 实体类修改

**文件**: `src/Hx.Abp.Attachment.Domain/Hx/Abp/Attachment/Domain/AttachCatalogue.cs`

#### 属性修改

```csharp
// 修改前
public virtual string? Reference { get; private set; }

// 修改后
public virtual string Reference { get; private set; }
```

#### 构造函数修改

```csharp
// 修改前
public AttachCatalogue(
    Guid id,
    AttachReceiveType attachReceiveType,
    string catologueName,
    int sequenceNumber,
    string? reference,  // 可空参数
    int referenceType,
    // ... 其他参数
)

// 修改后
public AttachCatalogue(
    Guid id,
    AttachReceiveType attachReceiveType,
    string catologueName,
    int sequenceNumber,
    string reference,   // 不可空参数
    int referenceType,
    // ... 其他参数
)
```

### 2. 实体配置修改

**文件**: `src/Hx.Abp.Attachment.EntityFrameworkCore/Hx/Abp/Attachment/EntityFrameworkCore/AttachCatalogueEntityTypeConfiguration.cs`

```csharp
// 修改前
builder.Property(d => d.Reference).HasColumnName("REFERENCE").HasMaxLength(100);

// 修改后
builder.Property(d => d.Reference).HasColumnName("REFERENCE").HasMaxLength(100).IsRequired();
```

## 影响分析

### 1. 数据库层面

-   `REFERENCE` 字段将设置为 `NOT NULL`
-   现有数据中如果有 `NULL` 值，需要先清理
-   新插入的数据必须提供 `Reference` 值

### 2. 应用层面

-   创建 `AttachCatalogue` 时必须提供 `Reference` 值
-   DTO 类中的 `Reference` 已经是 `required`，无需修改
-   搜索方法中的可选 `Reference` 参数保持不变（用于过滤）

### 3. 兼容性

-   现有的搜索方法（如 `SearchByFullTextAsync`）中的可选 `Reference` 参数保持不变
-   这些方法用于过滤条件，`Reference` 为 `null` 表示不过滤该字段

## 验证清单

### ✅ 已完成的修改

-   [x] 实体类 `Reference` 属性改为不可空
-   [x] 构造函数参数改为不可空
-   [x] 实体配置添加 `IsRequired()`
-   [x] DTO 类中的 `Reference` 已经是 `required`
-   [x] CreateDto 和 UpdateDto 中的 `Reference` 已经是 `required`

### 🔍 需要验证的内容

-   [ ] 现有数据库中是否有 `NULL` 的 `Reference` 值
-   [ ] 应用启动时是否需要数据迁移
-   [ ] 所有创建 `AttachCatalogue` 的地方是否都提供了 `Reference` 值

## 数据迁移建议

如果现有数据中有 `NULL` 的 `Reference` 值，建议执行以下步骤：

### 1. 检查现有数据

```sql
SELECT COUNT(*) FROM "APPATTACH_CATALOGUES" WHERE "REFERENCE" IS NULL;
```

### 2. 清理数据（如果需要）

```sql
-- 为NULL的Reference设置默认值（根据业务需求）
UPDATE "APPATTACH_CATALOGUES"
SET "REFERENCE" = 'DEFAULT_REFERENCE'
WHERE "REFERENCE" IS NULL;
```

### 3. 添加 NOT NULL 约束

```sql
ALTER TABLE "APPATTACH_CATALOGUES"
ALTER COLUMN "REFERENCE" SET NOT NULL;
```

## 总结

通过将 `Reference` 字段修改为不可为空，我们：

-   ✅ **提高了数据完整性** - 确保每个目录都有业务引用
-   ✅ **增强了业务逻辑** - 强制要求提供业务关联信息
-   ✅ **保持了向后兼容** - 搜索方法中的可选参数保持不变
-   ✅ **符合业务需求** - 满足用户对数据完整性的要求

这个修改确保了 `AttachCatalogue` 实体的数据质量，同时保持了现有功能的正常运行。
