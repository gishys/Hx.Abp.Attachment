# AttachCatalogue 增强功能实现说明

## 概述

成功为 `AttachCatalogue` 实体添加了与 `AttachCatalogueTemplate` 相同的功能，包括分类类型标识、向量维度支持和权限管理，实现了功能的一致性和完整性。

## 新增功能特性

### 1. 分类类型标识系统

-   **CatalogueType**: 标识分类的层级和用途
    -   项目级分类 (1)
    -   阶段级分类 (2)
    -   业务分类 (3)
    -   专业领域分类 (4)
    -   通用分类 (99)
-   **CataloguePurpose**: 标识分类的具体用途
    -   分类管理 (1)
    -   文档管理 (2)
    -   流程管理 (3)
    -   权限管理 (4)
    -   其他用途 (99)

### 2. 向量维度支持

-   **TextVector**: 文本向量（64-2048 维），非必填
-   **VectorDimension**: 向量维度，自动计算
-   支持向量相似度计算和搜索
-   自动验证向量维度范围

### 3. 权限管理

-   **Permissions**: 权限集合（JSONB 格式）
-   支持 RBAC、ABAC、PBAC 混合模型
-   权限继承与覆盖机制
-   运行时权限检查

## 技术实现

### 1. 领域层 (Domain)

#### AttachCatalogue 实体

-   添加新字段：`CatalogueType`、`CataloguePurpose`、`TextVector`、`VectorDimension`、`Permissions`
-   新增业务方法：
    -   `SetCatalogueIdentifiers()` - 设置分类标识
    -   `SetTextVector()` - 设置文本向量
    -   `ValidateConfiguration()` - 验证配置
    -   `GetCatalogueIdentifierDescription()` - 获取分类标识描述
    -   `MatchesCatalogueIdentifier()` - 检查是否匹配分类标识
    -   权限管理方法：`AddPermission()`、`RemovePermission()`、`HasPermission()`等

### 2. 应用层 (Application)

#### AttachCatalogueAppService

-   新增方法：
    -   `UpdateAsync()` - 更新分类信息
    -   `SetPermissionsAsync()` - 设置分类权限
    -   `GetPermissionsAsync()` - 获取分类权限
    -   `HasPermissionAsync()` - 检查用户权限
    -   `GetCatalogueIdentifierDescriptionAsync()` - 获取分类标识描述
    -   `GetByCatalogueIdentifierAsync()` - 根据分类标识查询
    -   `GetByVectorDimensionAsync()` - 根据向量维度查询

#### IAttachCatalogueAppService 接口

-   扩展接口定义，包含所有新方法

### 3. 数据传输层 (DTOs)

#### AttachCatalogueDto

-   添加新字段：`CatalogueType`、`CataloguePurpose`、`TextVector`、`VectorDimension`、`Permissions`
-   新增计算属性：`CatalogueIdentifierDescription`

#### AttachCatalogueCreateDto

-   添加新字段：`CatalogueType`、`CataloguePurpose`、`TextVector`
-   包含数据验证注解

### 4. 基础设施层 (EntityFrameworkCore)

#### AttachCatalogueEntityTypeConfiguration

-   配置新字段的数据库映射
-   添加约束和索引
-   配置权限集合的 JSONB 存储
-   创建复合索引优化查询性能

### 5. API 层 (HttpApi)

#### AttachmentCatalogueController

-   新增 API 端点：
    -   `PUT /api/app/attachment/update` - 更新分类
    -   `PUT /api/app/attachment/permissions/set` - 设置权限
    -   `GET /api/app/attachment/permissions/get` - 获取权限
    -   `GET /api/app/attachment/permissions/check` - 检查权限
    -   `GET /api/app/attachment/identifier/description` - 获取标识描述
    -   `GET /api/app/attachment/search/by-identifier` - 按标识查询
    -   `GET /api/app/attachment/search/by-vector-dimension` - 按向量维度查询

## 数据库结构

### 1. 新增字段

```sql
-- 分类类型
"CATALOGUE_TYPE" integer NOT NULL DEFAULT 99

-- 分类用途
"CATALOGUE_PURPOSE" integer NOT NULL DEFAULT 1

-- 文本向量
"TEXT_VECTOR" double precision[]

-- 向量维度
"VECTOR_DIMENSION" integer NOT NULL DEFAULT 0

-- 权限集合
"PERMISSIONS" jsonb
```

### 2. 约束

```sql
-- 向量维度约束
CHECK ("VECTOR_DIMENSION" >= 0 AND "VECTOR_DIMENSION" <= 2048)

-- 分类类型约束
CHECK ("CATALOGUE_TYPE" IN (1, 2, 3, 4, 99))

-- 分类用途约束
CHECK ("CATALOGUE_PURPOSE" IN (1, 2, 3, 4, 99))
```

### 3. 索引

```sql
-- 基础索引
CREATE INDEX "IDX_ATTACH_CATALOGUES_CATALOGUE_TYPE" ON "APPATTACH_CATALOGUES" ("CATALOGUE_TYPE");
CREATE INDEX "IDX_ATTACH_CATALOGUES_CATALOGUE_PURPOSE" ON "APPATTACH_CATALOGUES" ("CATALOGUE_PURPOSE");
CREATE INDEX "IDX_ATTACH_CATALOGUES_VECTOR_DIMENSION" ON "APPATTACH_CATALOGUES" ("VECTOR_DIMENSION");

-- 复合索引
CREATE INDEX "IDX_ATTACH_CATALOGUES_TYPE_PURPOSE" ON "APPATTACH_CATALOGUES" ("CATALOGUE_TYPE", "CATALOGUE_PURPOSE");
CREATE INDEX "IDX_ATTACH_CATALOGUES_PARENT_TYPE" ON "APPATTACH_CATALOGUES" ("PARENT_ID", "CATALOGUE_TYPE");

-- JSONB索引
CREATE INDEX "IX_ATTACH_CATALOGUES_PERMISSIONS_GIN" ON "APPATTACH_CATALOGUES" USING GIN ("PERMISSIONS" jsonb_path_ops);
```

### 4. 视图

```sql
-- 分类标识统计视图
CREATE VIEW "V_ATTACH_CATALOGUES_BY_IDENTIFIER" AS
SELECT "CATALOGUE_TYPE", "CATALOGUE_PURPOSE", COUNT(*) AS "CATALOGUE_COUNT", ...

-- 向量维度统计视图
CREATE VIEW "V_ATTACH_CATALOGUES_BY_VECTOR_DIMENSION" AS
SELECT CASE WHEN "VECTOR_DIMENSION" = 0 THEN '无向量' WHEN "VECTOR_DIMENSION" BETWEEN 64 AND 128 THEN '64-128维' ...
```

### 5. 函数

```sql
-- 分类标识描述函数
CREATE FUNCTION "FN_GET_CATALOGUE_IDENTIFIER_DESCRIPTION"(p_catalogue_type integer, p_catalogue_purpose integer)
RETURNS text

-- 权限检查函数
CREATE FUNCTION "FN_CHECK_CATALOGUE_PERMISSION"(p_catalogue_id uuid, p_user_id uuid, p_action integer)
RETURNS boolean
```

## 使用方法

### 1. 创建分类

```csharp
var catalogue = new AttachCatalogue(
    Guid.NewGuid(),
    AttachReceiveType.Copy,
    "项目文档",
    1,
    "PROJ001",
    1,
    catalogueType: TemplateType.Project,
    cataloguePurpose: TemplatePurpose.Document,
    textVector: new List<double> { 0.1, 0.2, 0.3, ... }
);
```

### 2. 设置分类标识

```csharp
catalogue.SetCatalogueIdentifiers(TemplateType.Project, TemplatePurpose.Document);
```

### 3. 设置文本向量

```csharp
catalogue.SetTextVector(new List<double> { 0.1, 0.2, 0.3, ... });
```

### 4. 添加权限

```csharp
var permission = new AttachCatalogueTemplatePermission(
    "Role", "Admin", PermissionAction.View, PermissionEffect.Allow
);
catalogue.AddPermission(permission);
```

### 5. 检查权限

```csharp
bool hasPermission = catalogue.HasPermission(userId, PermissionAction.View);
```

### 6. API 调用示例

```http
# 按分类标识查询
GET /api/app/attachment/search/by-identifier?catalogueType=1&cataloguePurpose=1

# 按向量维度查询
GET /api/app/attachment/search/by-vector-dimension?minDimension=64&maxDimension=128

# 设置权限
PUT /api/app/attachment/permissions/set/{id}
Content-Type: application/json

[
  {
    "permissionType": "Role",
    "permissionTarget": "Admin",
    "action": 1,
    "effect": 1,
    "isEnabled": true,
    "description": "管理员可以查看"
  }
]
```

## 迁移部署

### 1. 执行迁移脚本

```sql
\i attach-catalogue-enhancement-migration.sql
```

### 2. 验证迁移结果

```sql
\i attach-catalogue-enhancement-test.sql
```

### 3. 检查新增字段

```sql
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'APPATTACH_CATALOGUES'
  AND column_name IN ('CATALOGUE_TYPE', 'CATALOGUE_PURPOSE', 'TEXT_VECTOR', 'VECTOR_DIMENSION', 'PERMISSIONS');
```

## 最佳实践

### 1. 分类标识设计

-   根据业务需求合理设置分类类型和用途
-   避免创建过多的分类层级
-   保持分类标识的一致性和可维护性

### 2. 向量管理

-   合理设置向量维度范围（64-2048）
-   定期清理无效的向量数据
-   使用向量索引优化查询性能

### 3. 权限配置

-   遵循最小权限原则
-   合理设置权限的有效期
-   定期审计权限配置

### 4. 性能优化

-   使用复合索引优化多字段查询
-   合理使用 JSONB 索引
-   避免在权限 JSONB 字段上进行复杂查询

## 注意事项

1. 执行迁移脚本前请备份数据库
2. 新字段有默认值，不会影响现有数据
3. 权限字段使用 JSONB 格式，支持复杂的权限规则
4. 向量字段支持 64-2048 维，超出范围会抛出异常
5. 建议在生产环境部署前充分测试

## 总结

通过为 `AttachCatalogue` 实体添加与 `AttachCatalogueTemplate` 相同的功能，实现了：

-   **功能一致性**: 两个实体具有相同的核心功能
-   **代码复用**: 最大程度复用了现有的权限和向量管理逻辑
-   **维护性**: 统一的代码结构和 API 设计
-   **扩展性**: 支持未来的功能扩展和优化

这些增强功能为附件分类管理提供了更强大的分类标识、向量支持和权限管理能力，同时保持了代码的简洁性和可维护性。
