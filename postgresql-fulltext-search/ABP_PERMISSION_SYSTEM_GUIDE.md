# 基于 ABP vNext 的权限系统使用指南

## 概述

本权限系统基于 ABP vNext 内置的权限管理功能，简化了实现复杂度，同时保持了 RBAC + ABAC + PBAC 混合模型的灵活性。权限集合直接通过实体关联存储，PostgreSQL 原生支持 JSONB 集合。

## 系统架构

### 1. 权限模型

-   **Role (基于角色)**: 利用 ABP 内置的角色管理
-   **User (基于用户)**: 直接用户权限分配
-   **Policy (基于策略)**: 支持属性条件的策略权限

### 2. 核心组件

-   **AttachCatalogueTemplatePermission**: 权限值对象
-   **AttachmentPermissionDefinitionProvider**: ABP 权限定义提供者
-   **PermissionService**: 权限服务（基于 ABP 权限检查器）
-   **PermissionController**: 权限管理控制器

### 3. 存储方式

-   **实体关联**: 权限集合直接作为 `AttachCatalogueTemplate` 实体的属性
-   **PostgreSQL JSONB**: 原生支持复杂对象集合的存储和查询
-   **无需额外字段**: 不需要 `PermissionsJson` 等持久化字段

## 功能特性

### 1. 权限继承与覆盖

-   利用 ABP 内置的权限层次结构
-   支持模板特定的权限配置
-   权限优先级管理

### 2. 运行时权限检查

-   一行代码即可检查权限
-   支持批量权限检查
-   结合 ABP 内置权限和模板特定权限

### 3. 权限缓存

-   利用 ABP 内置的权限缓存机制
-   支持热更新
-   自动缓存管理

### 4. 存储优势

-   **零序列化成本**: PostgreSQL 直接存储对象集合
-   **原生查询支持**: 支持 JSONB 的 GIN 索引和查询
-   **类型安全**: 强类型的权限值对象
-   **维护简单**: 无需手动维护 JSON 字符串

## 使用方法

### 1. 基本权限检查

```csharp
// 检查用户是否具有指定模板的查看权限
var hasPermission = await _permissionService.HasPermissionAsync(userId, templateId, PermissionAction.View);
```

### 2. 批量权限检查

```csharp
// 批量检查多个模板的权限
var templateIds = new List<Guid> { templateId1, templateId2, templateId3 };
var results = await _permissionService.HasPermissionsAsync(userId, templateIds, PermissionAction.Edit);
```

### 3. 权限管理

```csharp
// 获取模板权限配置
var permissions = await _permissionService.GetTemplatePermissionsAsync(templateId);

// 设置模板权限
await _permissionService.SetTemplatePermissionsAsync(templateId, permissions);

// 添加单个权限
await _permissionService.AddTemplatePermissionAsync(templateId, permission);

// 移除单个权限
await _permissionService.RemoveTemplatePermissionAsync(templateId, permission);
```

## API 接口

### 1. 权限检查接口

-   `GET /api/permissions/check` - 检查单个权限
-   `POST /api/permissions/check-batch` - 批量检查权限
-   `GET /api/permissions/simple-check` - 一行代码权限检查示例

### 2. 权限管理接口

-   `GET /api/permissions/template/{templateId}` - 获取模板权限配置
-   `PUT /api/permissions/template/{templateId}` - 设置模板权限
-   `POST /api/permissions/template/{templateId}` - 添加模板权限
-   `DELETE /api/permissions/template/{templateId}` - 移除模板权限

### 3. 权限查询接口

-   `GET /api/permissions/template/{templateId}/summary` - 获取模板权限摘要
-   `GET /api/permissions/template/{templateId}/conflicts` - 检查权限冲突

## 权限配置示例

### 1. 角色权限配置

```json
{
    "permissionType": "Role",
    "permissionTarget": "Admin",
    "action": "View",
    "effect": "Allow",
    "description": "管理员可以查看模板"
}
```

### 2. 用户权限配置

```json
{
    "permissionType": "User",
    "permissionTarget": "11111111-1111-1111-1111-111111111111",
    "action": "Create",
    "effect": "Allow",
    "description": "特定用户可以创建模板"
}
```

### 3. 策略权限配置

```json
{
    "permissionType": "Policy",
    "permissionTarget": "TimeBased",
    "action": "Edit",
    "effect": "Allow",
    "attributeConditions": "{\"timeWindow\": \"09:00-18:00\"}",
    "description": "工作时间内可以编辑模板"
}
```

## 数据库结构

### 1. 存储方式

-   **实体关联**: 权限集合直接作为实体属性存储
-   **PostgreSQL JSONB**: 原生支持复杂对象集合
-   **无需额外字段**: 不需要 `PERMISSIONS_JSON` 等字段

### 2. 索引优化

-   实体关联的索引优化
-   权限操作和类型的复合索引
-   权限目标的优化索引

### 3. 视图和函数

-   **V_APPATTACH_PERMISSION_SUMMARY**: 权限摘要视图（通过应用层计算）
-   **V_APPATTACH_PERMISSION_STATISTICS**: 权限统计视图（通过应用层计算）
-   **V_APPATTACH_PERMISSION_ANALYSIS**: 权限分析视图
-   **FN_CHECK_TEMPLATE_PERMISSION_SIMPLE**: 简化权限检查函数
-   **FN_GET_TEMPLATE_PERMISSION_COUNT**: 获取权限数量函数

## 部署说明

### 1. 数据库迁移

执行 `abp-permission-system-migration.sql` 脚本创建权限系统所需的视图、函数等。注意：不需要添加额外的字段，因为权限集合直接通过实体关联存储。

### 2. 应用配置

确保在应用模块中注册了权限服务：

```csharp
services.AddTransient<IPermissionService, PermissionService>();
```

### 3. 权限定义

在 `AttachmentPermissionDefinitionProvider` 中定义所需的权限：

```csharp
public override void Define(IPermissionDefinitionContext context)
{
    var attachmentGroup = context.AddGroup(AttachmentPermissions.GroupName, L("Permission:Attachment"));

    var templatePermission = attachmentGroup.AddPermission(AttachmentPermissions.Templates.Default, L("Permission:Templates"));
    templatePermission.AddChild(AttachmentPermissions.Templates.View, L("Permission:Templates.View"));
    // ... 其他权限
}
```

## 最佳实践

### 1. 权限设计原则

-   优先使用 ABP 内置的角色权限
-   模板特定权限作为补充
-   策略权限用于复杂条件判断

### 2. 性能优化

-   合理使用权限缓存
-   利用 PostgreSQL 的 JSONB 查询优化
-   批量权限检查

### 3. 存储优势

-   **零序列化成本**: 无需手动序列化/反序列化
-   **原生查询支持**: 支持复杂的 JSONB 查询
-   **类型安全**: 强类型的权限值对象
-   **维护简单**: 无需维护额外的持久化字段

### 4. 安全考虑

-   定期审查权限配置
-   监控权限使用情况
-   及时撤销无效权限

## 故障排除

### 1. 常见问题

-   权限检查返回 false：检查 ABP 权限配置和模板特定权限
-   权限缓存不更新：检查 ABP 权限缓存配置
-   权限继承异常：检查 ABP 权限层次结构

### 2. 调试方法

-   查看 ABP 权限日志
-   检查模板权限配置
-   验证权限定义提供者

## 扩展功能

### 1. 自定义权限提供者

-   继承 `IPermissionValueProvider`
-   实现自定义权限逻辑
-   注册到 ABP 权限系统

### 2. 权限策略引擎

-   扩展策略评估逻辑
-   支持复杂条件表达式
-   动态策略加载

### 3. 权限分析

-   权限使用统计
-   权限风险分析
-   权限优化建议

## 总结

基于 ABP vNext 的权限系统提供了简洁而强大的权限管理解决方案，充分利用了 ABP 框架的内置功能和 PostgreSQL 的原生 JSONB 支持。通过实体关联存储权限集合，避免了额外的序列化/反序列化成本，同时保持了系统的灵活性和扩展性。通过合理配置和使用，可以有效保障系统的安全性和可用性。
