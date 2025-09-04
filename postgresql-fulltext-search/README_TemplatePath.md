# 模板路径功能数据库迁移说明

## 概述

本文档描述了为 `AttachCatalogueTemplate` 表添加 `TemplatePath` 字段的数据库迁移过程。该字段用于实现模板的层级路径管理，支持快速查询和层级展示。

## 迁移文件

-   `AddTemplatePathField.sql` - 添加模板路径字段的迁移脚本
-   `RollbackTemplatePathField.sql` - 回滚迁移的脚本
-   `README_TemplatePath.md` - 本说明文档

## 字段说明

### TemplatePath 字段

-   **字段名**: `TEMPLATE_PATH`
-   **数据类型**: `VARCHAR(200)`
-   **是否可空**: 是（根节点可以为空）
-   **格式**: `00001.00002.00003`（5 位数字，用点分隔）
-   **用途**: 用于快速查询层级关系和路径导航

### 字段格式规则

-   每个路径段都是 5 位数字（00001-99999）
-   路径段之间用点号（.）分隔
-   根节点路径为空或 null
-   示例：
    -   根节点：`null` 或 `""`
    -   第一层：`"00001"`, `"00002"`, `"00003"`
    -   第二层：`"00001.00001"`, `"00001.00002"`
    -   第三层：`"00001.00001.00001"`

## 新增对象

### 索引

1. `IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH` - 模板路径索引
2. `IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_LATEST` - 路径和最新版本复合索引
3. `IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_PREFIX` - 路径前缀索引（用于子路径查询）

### 函数

1. `get_template_path_depth(template_path TEXT)` - 获取路径深度
2. `is_valid_template_path(template_path TEXT)` - 验证路径格式
3. `get_parent_template_path(template_path TEXT)` - 获取父路径
4. `get_last_unit_template_path_code(template_path TEXT)` - 获取最后一个单元代码
5. `calculate_next_template_path(current_path TEXT)` - 计算下一个路径

### 视图

1. `V_TEMPLATE_PATH_STATISTICS` - 模板路径统计视图
2. `V_TEMPLATE_PATH_TREE` - 模板路径树形结构视图

### 触发器

1. `TRG_MAINTAIN_TEMPLATE_PATH` - 自动维护模板路径的触发器

## 执行迁移

### 1. 执行迁移脚本

```sql
-- 在数据库中执行
\i AddTemplatePathField.sql
```

### 2. 验证迁移结果

```sql
-- 检查字段是否添加成功
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES'
AND column_name = 'TEMPLATE_PATH';

-- 检查索引是否创建成功
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES'
AND indexname LIKE '%TEMPLATE_PATH%';

-- 检查函数是否创建成功
SELECT proname, prosrc
FROM pg_proc
WHERE proname LIKE '%template_path%';
```

## 回滚迁移

如果需要回滚迁移，执行以下命令：

```sql
-- 在数据库中执行
\i RollbackTemplatePathField.sql
```

## 使用示例

### 1. 查询指定深度的模板

```sql
SELECT * FROM "APPATTACH_CATALOGUE_TEMPLATES"
WHERE get_template_path_depth("TEMPLATE_PATH") = 2
AND "IS_DELETED" = false;
```

### 2. 查询指定路径下的所有子模板

```sql
SELECT * FROM "APPATTACH_CATALOGUE_TEMPLATES"
WHERE "TEMPLATE_PATH" LIKE '00001.%'
AND "IS_DELETED" = false;
```

### 3. 获取模板路径统计

```sql
SELECT * FROM "V_TEMPLATE_PATH_STATISTICS";
```

### 4. 获取模板路径树形结构

```sql
SELECT * FROM "V_TEMPLATE_PATH_TREE"
WHERE "IS_LATEST" = true
ORDER BY "TEMPLATE_PATH";
```

### 5. 计算下一个路径

```sql
SELECT calculate_next_template_path('00001.00002');
-- 结果: 00001.00003
```

## 性能优化

### 索引使用建议

1. 使用 `TEMPLATE_PATH` 索引进行精确路径查询
2. 使用 `PATH_LATEST` 复合索引进行路径和版本过滤
3. 使用 `PATH_PREFIX` 索引进行子路径查询

### 查询优化

1. 优先使用路径查询而不是递归查询
2. 利用路径前缀进行范围查询
3. 使用视图进行复杂统计查询

## 注意事项

1. **数据一致性**: 确保模板路径与父子关系保持一致
2. **并发安全**: 路径生成需要考虑并发情况
3. **性能监控**: 定期监控索引使用情况和查询性能
4. **备份策略**: 执行迁移前请备份数据库

## 故障排除

### 常见问题

1. **路径格式错误**

    - 检查路径是否符合 `00001.00002.00003` 格式
    - 使用 `is_valid_template_path()` 函数验证

2. **索引性能问题**

    - 检查索引是否被正确使用
    - 考虑重建索引

3. **触发器问题**
    - 检查触发器函数是否正确执行
    - 查看数据库日志

### 联系支持

如果遇到问题，请联系开发团队或查看相关文档。

## 版本历史

-   v1.0 (2024-12-19) - 初始版本，添加模板路径功能
