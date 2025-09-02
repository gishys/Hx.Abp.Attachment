# 权限系统文件说明

## 概述

本目录包含了完整的权限系统实现，基于 ABP vNext 框架，实现了 RBAC + ABAC + PBAC 混合模型的权限管理。

## 文件结构

### 1. 核心文档

-   **`PERMISSION_SYSTEM_GUIDE.md`** - 权限系统使用指南
    -   系统架构说明
    -   功能特性介绍
    -   使用方法示例
    -   API 接口文档
    -   最佳实践建议

### 2. 数据库脚本

-   **`permission-system-migration.sql`** - 权限系统数据库迁移脚本

    -   创建权限相关表结构
    -   创建索引和视图
    -   创建函数和存储过程
    -   创建审计触发器

-   **`permission-system-test.sql`** - 权限系统测试脚本
    -   测试数据准备
    -   功能验证测试
    -   视图和函数测试
    -   测试数据清理

## 系统特性

### 1. 权限模型

-   **RBAC (Role-Based Access Control)**: 基于角色的权限控制
-   **ABAC (Attribute-Based Access Control)**: 基于属性的权限控制
-   **PBAC (Policy-Based Access Control)**: 基于策略的权限控制

### 2. 核心功能

-   权限继承与覆盖
-   运行时权限检查
-   权限缓存管理
-   审计日志记录
-   权限冲突检测

### 3. 技术架构

-   基于 ABP vNext 框架
-   PostgreSQL 数据库支持
-   JSONB 字段存储
-   GIN 索引优化
-   触发器审计

## 使用方法

### 1. 数据库部署

```sql
-- 执行迁移脚本
\i permission-system-migration.sql
```

### 2. 功能测试

```sql
-- 执行测试脚本
\i permission-system-test.sql
```

### 3. 应用集成

参考 `PERMISSION_SYSTEM_GUIDE.md` 中的使用说明和最佳实践。

## 维护说明

### 1. 文件合并历史

-   原文件已合并，删除了冗余和重复内容
-   保留了最终实现结果
-   简化了维护复杂度

### 2. 更新建议

-   修改功能时优先更新核心文档
-   数据库变更时同步更新迁移脚本
-   测试用例变更时同步更新测试脚本

### 3. 版本管理

-   主要版本变更时创建新的迁移脚本
-   保持向后兼容性
-   记录重要的架构变更

## 注意事项

1. 执行迁移脚本前请备份数据库
2. 测试脚本会创建和删除测试数据
3. 生产环境部署前请充分测试
4. 定期检查权限配置和审计日志

## 技术支持

如有问题，请参考：

1. `PERMISSION_SYSTEM_GUIDE.md` 中的故障排除部分
2. ABP vNext 官方文档
3. PostgreSQL 官方文档
