# 权限系统使用指南

## 概述

本权限系统实现了 RBAC + ABAC + PBAC 混合模型，支持权限继承、覆盖、运行时权限检查等功能，为附件目录模板提供完整的权限管理能力。

## 系统架构

### 1. 权限模型

-   **RBAC (Role-Based Access Control)**: 基于角色的权限控制
-   **ABAC (Attribute-Based Access Control)**: 基于属性的权限控制
-   **PBAC (Policy-Based Access Control)**: 基于策略的权限控制

### 2. 核心组件

-   **AttachCatalogueTemplatePermission**: 权限实体
-   **PermissionRule**: 权限规则值对象
-   **RuntimePermissionContext**: 运行时权限上下文
-   **PermissionService**: 权限服务
-   **PermissionController**: 权限控制器

## 功能特性

### 1. 权限继承与覆盖

-   子节点自动继承父节点权限
-   支持权限优先级设置
-   本地权限可覆盖继承权限

### 2. 运行时权限检查

-   一行代码即可检查权限
-   支持批量权限检查
-   实时权限计算

### 3. 权限缓存

-   运行时权限缓存
-   支持热更新
-   一键刷新缓存

### 4. 审计日志

-   完整的权限变更记录
-   支持时间范围查询
-   详细的变更历史

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

### 3. 运行时权限计算

```csharp
// 计算模板的运行时权限（继承+覆盖）
var runtimePermissions = await _permissionService.CalculateRuntimePermissionsAsync(templateId, userId);
```

### 4. 权限摘要查询

```csharp
// 获取模板权限摘要
var summary = await _permissionService.GetTemplatePermissionSummaryAsync(templateId);

// 获取用户权限摘要
var userSummaries = await _permissionService.GetUserPermissionSummaryAsync(userId);
```

## API 接口

### 1. 权限检查接口

-   `GET /api/permissions/check` - 检查单个权限
-   `POST /api/permissions/check-batch` - 批量检查权限
-   `GET /api/permissions/simple-check` - 一行代码权限检查示例

### 2. 权限管理接口

-   `POST /api/permissions/calculate-runtime` - 计算运行时权限
-   `GET /api/permissions/template-summary/{templateId}` - 获取模板权限摘要
-   `GET /api/permissions/user-summary/{userId}` - 获取用户权限摘要

### 3. 权限维护接口

-   `GET /api/permissions/check-conflicts/{templateId}` - 检查权限冲突
-   `POST /api/permissions/refresh-cache/{templateId}` - 刷新权限缓存
-   `GET /api/permissions/audit-logs/{templateId}` - 获取权限审计日志

## 数据库结构

### 1. 主要表

-   `APPATTACH_ATTACH_CATALOGUE_TEMPLATE_PERMISSIONS` - 模板权限表
-   `APPATTACH_PERMISSION_RULES` - 权限规则表
-   `APPATTACH_PERMISSION_RULE_RELATIONS` - 权限规则关联表
-   `APPATTACH_PERMISSION_AUDIT_LOGS` - 权限审计日志表

### 2. 索引优化

-   基础字段索引
-   JSONB 字段 GIN 索引
-   复合索引
-   条件索引

### 3. 视图和函数

-   `V_APPATTACH_PERMISSION_SUMMARY` - 权限摘要视图
-   `V_APPATTACH_PERMISSION_STATISTICS` - 权限统计视图
-   `FN_CHECK_USER_PERMISSION` - 权限检查函数
-   `SP_CALCULATE_INHERITED_PERMISSIONS` - 权限继承计算存储过程

## 权限配置示例

### 1. RBAC 权限配置

```json
{
    "permissionType": "RBAC",
    "roleName": "Admin",
    "action": "ManagePermissions",
    "effect": "Allow",
    "description": "管理员可以管理所有权限"
}
```

### 2. ABAC 权限配置

```json
{
    "permissionType": "ABAC",
    "attributeConditions": {
        "department": "IT",
        "securityLevel": "High",
        "location": "Headquarters"
    },
    "action": "View",
    "effect": "Allow"
}
```

### 3. PBAC 权限配置

```json
{
    "permissionType": "PBAC",
    "policyRules": {
        "timeWindow": "09:00-18:00",
        "ipRange": "192.168.1.0/24",
        "riskLevel": "Low"
    },
    "action": "Export",
    "effect": "Allow"
}
```

## 最佳实践

### 1. 权限设计原则

-   最小权限原则
-   职责分离原则
-   权限继承层次不宜过深

### 2. 性能优化

-   合理使用权限缓存
-   定期清理过期权限
-   优化数据库索引

### 3. 安全考虑

-   定期审计权限配置
-   及时撤销无效权限
-   监控异常权限访问

## 故障排除

### 1. 常见问题

-   权限检查返回 false：检查权限配置、时间范围、用户角色
-   权限缓存不更新：手动刷新缓存或检查缓存配置
-   权限继承异常：检查父节点权限配置

### 2. 调试方法

-   查看权限审计日志
-   检查运行时权限缓存
-   验证权限配置有效性

## 扩展功能

### 1. 自定义权限规则

-   支持自定义规则引擎
-   可扩展的权限计算逻辑
-   灵活的规则表达式

### 2. 权限模板

-   预定义权限模板
-   快速权限配置
-   标准化权限管理

### 3. 权限分析

-   权限使用统计
-   权限风险分析
-   权限优化建议

## 总结

本权限系统提供了完整的权限管理解决方案，支持复杂的权限场景，具有良好的扩展性和维护性。通过合理配置和使用，可以有效保障系统的安全性和可用性。
