# FacetType 字段重命名迁移说明

## 概述

本次迁移将 `TEMPLATE_TYPE` 字段重命名为 `FACET_TYPE`，并更新相应的枚举类型、约束、索引和默认值。迁移涉及两个主要表：

-   `APPATTACH_CATALOGUE_TEMPLATES` 表
-   `APPATTACH_CATALOGUES` 表

## 迁移内容

### 1. 数据库层变更

#### 字段重命名

-   `TEMPLATE_TYPE` → `FACET_TYPE` (在 APPATTACH_CATALOGUE_TEMPLATES 表中)
-   `CATALOGUE_TYPE` → `CATALOGUE_FACET_TYPE` (在 APPATTACH_CATALOGUES 表中)

#### 枚举值映射

| 旧值 | 新值 | 说明                           |
| ---- | ---- | ------------------------------ |
| 99   | 0    | General (通用分面)             |
| 1    | 1    | Organization (组织维度)        |
| 2    | 2    | ProjectType (项目类型)         |
| 3    | 3    | Phase (阶段分面)               |
| 4    | 4    | Discipline (专业领域)          |
| -    | 5    | DocumentType (文档类型) - 新增 |
| -    | 6    | TimeSlice (时间切片) - 新增    |
| 99   | 99   | Custom (业务自定义) - 保持     |

#### 约束更新

-   删除旧约束：`CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE`
-   添加新约束：`CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE`
-   约束值范围：`(0, 1, 2, 3, 4, 5, 6, 99)`

#### 索引更新

-   删除旧索引：`IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE`
-   创建新索引：`IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE`
-   更新复合索引：`IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE`

### 2. 应用层变更

#### 枚举类型

-   删除：`TemplateType` 枚举
-   新增：`FacetType` 枚举

#### 实体类

-   `AttachCatalogueTemplate.FacetType` 属性
-   `AttachCatalogue.CatalogueFacetType` 属性

#### DTO 类

-   `AttachCatalogueTemplateDto.FacetType`
-   `CreateUpdateAttachCatalogueTemplateDto.FacetType`
-   `GetAttachCatalogueTemplateListDto.FacetTypeCounts`
-   `AttachCatalogueDto.CatalogueFacetType`
-   `AttachCatalogueCreateDto.CatalogueFacetType`

#### 服务接口

-   `IAttachCatalogueTemplateAppService.GetTemplatesByIdentifierAsync(facetType)`
-   `IAttachCatalogueAppService.GetByCatalogueIdentifierAsync(catalogueFacetType)`

#### 仓储接口

-   `IAttachCatalogueTemplateRepository.GetTemplatesByIdentifierAsync(facetType)`

## 迁移步骤

### 执行顺序

1. **备份数据**（可选但推荐）
2. **删除旧约束**
3. **重命名字段**
4. **更新字段注释**
5. **更新数据值**
6. **添加新约束**
7. **更新索引**
8. **验证迁移结果**

### 执行命令

```bash
# 执行迁移脚本
psql -d your_database -f facet-type-migration.sql

# 如果出错，执行回滚脚本
psql -d your_database -f facet-type-rollback.sql
```

## 注意事项

### 数据兼容性

-   迁移过程中数据不会丢失
-   枚举值 99 会映射为 0
-   其他枚举值保持不变

### 应用兼容性

-   需要同时更新应用程序代码
-   确保所有引用都已更新
-   测试所有相关功能

### 性能影响

-   索引重建可能需要时间
-   建议在低峰期执行
-   使用 `CONCURRENTLY` 创建索引减少锁表时间

### 回滚准备

-   保留回滚脚本
-   备份重要数据
-   记录迁移时间点

## 测试建议

### 迁移前测试

1. 在测试环境执行迁移脚本
2. 验证字段重命名成功
3. 检查约束和索引是否正确
4. 验证数据完整性

### 迁移后测试

1. 测试所有相关 API 接口
2. 验证枚举值映射正确
3. 检查业务逻辑是否正常
4. 性能测试确认索引有效

## 监控和维护

### 迁移后监控

-   检查数据库性能
-   监控错误日志
-   验证业务功能正常

### 长期维护

-   更新相关文档
-   培训开发团队
-   建立变更记录

## 联系信息

如有问题，请联系：

-   数据库管理员
-   开发团队负责人
-   系统架构师

---

**重要提醒**：在生产环境执行前，请务必在测试环境完整测试迁移流程！
