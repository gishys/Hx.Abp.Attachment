# 附件管理系统数据库迁移脚本

## 概述

本项目包含附件管理系统的完整数据库迁移脚本，支持 PostgreSQL 数据库的全文搜索、权限管理、模板管理等功能。

## 迁移脚本

### 1. 统一模板迁移脚本

-   **文件**: `unified-attach-catalogue-template-migration.sql`
-   **说明**: 完整的 `AttachCatalogueTemplate` 表迁移脚本，包含表创建、字段添加、权限字段、索引创建、约束添加等所有功能
-   **功能**:
    -   表结构和字段管理
    -   权限字段（JSONB 格式）
    -   完整的索引体系
    -   约束和验证
    -   示例数据插入

### 2. 附件分类增强迁移脚本

-   **文件**: `attach-catalogue-enhancement-migration.sql`
-   **说明**: 为 `AttachCatalogue` 表添加增强功能的迁移脚本
-   **功能**:
    -   模板类型和用途字段
    -   文本向量和向量维度
    -   权限集合字段
    -   相关索引和约束

### 3. 权限系统迁移脚本

-   **文件**: `permission-system-migration.sql`
-   **说明**: 完整的权限系统数据库结构迁移脚本
-   **功能**:
    -   权限表结构
    -   角色和用户权限关联
    -   权限验证和检查

### 4. 模板使用统计迁移脚本

-   **文件**: `template-usage-count-migration.sql`
-   **说明**: 为模板使用统计功能添加数据库支持的迁移脚本
-   **功能**:
    -   模板 ID 关联字段
    -   统计视图和函数
    -   使用趋势分析

### 5. 模板权限字段迁移脚本

-   **文件**: `attach-catalogue-template-permissions-migration.sql`
-   **说明**: 专门为模板表添加权限字段的迁移脚本
-   **功能**:
    -   权限字段添加和配置
    -   数据清理和标准化
    -   约束和索引创建

## 测试脚本

### 1. 附件分类增强测试脚本

-   **文件**: `attach-catalogue-enhancement-test.sql`
-   **说明**: 验证附件分类增强功能的测试脚本
-   **功能**:
    -   新增字段功能测试
    -   索引性能测试
    -   约束验证测试
    -   权限功能测试

### 2. 权限系统测试脚本

-   **文件**: `permission-system-test.sql`
-   **说明**: 验证权限系统功能的测试脚本
-   **功能**:
    -   用户权限查询测试
    -   角色权限验证测试
    -   权限继承测试
    -   性能测试

## 使用说明

### 执行顺序

1. 先执行 `permission-system-migration.sql`（权限系统基础）
2. 再执行 `unified-attach-catalogue-template-migration.sql`（模板表结构）
3. 然后执行 `attach-catalogue-enhancement-migration.sql`（分类表增强）
4. 最后执行 `template-usage-count-migration.sql`（统计功能）

### 执行方法

```sql
-- 在PostgreSQL中执行
\i script-name.sql
```

### 注意事项

-   执行前请备份数据库
-   建议在测试环境先验证
-   需要数据库管理员权限
-   生产环境谨慎操作

## 技术特性

-   **PostgreSQL 12+** 支持
-   **JSONB** 数据类型支持
-   **全文搜索** 索引
-   **向量搜索** 支持
-   **权限管理** 系统
-   **模板管理** 功能
-   **统计分析** 能力

## 文档

-   `UNIFIED_MIGRATION_README.md` - 统一迁移脚本详细说明
-   `TEMPLATE_USAGE_STATS_API_GUIDE.md` - 模板使用统计 API 指南
-   `INTEGRATED_SOLUTION_GUIDE.md` - 集成解决方案指南

## 联系信息

如有问题或建议，请联系开发团队。
