# 模板路径字段迁移脚本说明（简化版）

## 修改说明

根据用户反馈，已移除数据库函数创建部分，因为相关逻辑已在 C#代码中实现。

## 脚本内容

### 1. 表结构修改

-   添加 `TEMPLATE_PATH` 字段（VARCHAR(200)）
-   添加字段注释
-   添加格式验证约束

### 2. 视图创建

-   统计和树形结构视图已在 C#代码中实现，无需在数据库中创建

### 3. 触发器

-   `maintain_template_path()`：自动维护模板路径格式验证
-   使用内联正则表达式验证

### 4. 索引创建（并发）

-   模板路径索引
-   路径和最新版本复合索引
-   路径前缀索引（用于子路径查询）

## 执行方式

### 开发环境

直接执行完整的 `AddTemplatePathField.sql` 脚本

### 生产环境

1. 先执行表结构修改部分（步骤 1-8）
2. 再执行并发索引创建部分（步骤 9-11）

## 验证查询

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

-- 检查约束是否添加成功
SELECT constraint_name, check_clause
FROM information_schema.check_constraints
WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH_FORMAT';
```

## 注意事项

1. **函数逻辑**：模板路径的计算、验证等逻辑由 C#应用层处理
2. **数据库职责**：数据库仅负责存储、索引和基础格式验证
3. **性能优化**：使用并发索引创建避免锁表
4. **数据完整性**：通过约束确保路径格式正确
