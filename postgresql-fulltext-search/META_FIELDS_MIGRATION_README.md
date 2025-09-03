# 元数据字段迁移执行指南

## 概述

本文档描述了如何为 `ATTACH_CATALOGUE_TEMPLATES` 表添加元数据字段（`META_FIELDS`）的迁移过程。元数据字段用于存储模板的元数据信息，支持命名实体识别(NER)、前端展示和业务场景配置。

## 迁移内容

### 1. 新增字段

-   **字段名**: `META_FIELDS`
-   **数据类型**: `jsonb`
-   **默认值**: `'[]'::jsonb`
-   **用途**: 存储元数据字段集合

### 2. 新增索引

-   GIN 索引：用于 JSON 查询和全文检索
-   路径索引：用于特定字段的快速查询
-   复合索引：用于多条件查询优化
-   全文检索索引：用于混合检索

### 3. 新增触发器

-   自动更新全文检索向量
-   实时维护索引一致性

## 执行步骤

### 第一步：执行主迁移脚本

```sql
-- 在PostgreSQL中执行
\i postgresql-fulltext-search/add-meta-fields-migration.sql
```

**注意事项**：

-   确保数据库连接正常
-   确保有足够的权限执行 DDL 操作
-   建议在业务低峰期执行

### 第二步：创建索引（可选，但推荐）

```sql
-- 在迁移脚本执行完成后，单独执行
\i postgresql-fulltext-search/create-meta-fields-indexes.sql
```

**注意事项**：

-   `CREATE INDEX CONCURRENTLY` 不能在事务块中运行
-   索引创建可能需要较长时间，建议在业务低峰期执行
-   可以根据实际查询需求选择性创建索引

## 验证迁移结果

### 1. 检查字段是否添加成功

```sql
SELECT
    column_name,
    data_type,
    column_default,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES'
    AND column_name = 'META_FIELDS';
```

### 2. 检查索引是否创建成功

```sql
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES'
    AND indexname LIKE '%META_FIELDS%'
ORDER BY indexname;
```

### 3. 检查触发器是否创建成功

```sql
SELECT
    trigger_name,
    event_manipulation,
    action_statement
FROM information_schema.triggers
WHERE event_object_table = 'APPATTACH_CATALOGUE_TEMPLATES'
    AND trigger_name LIKE '%META_FIELDS%';
```

## 回滚方案

如果需要回滚迁移，可以执行以下操作：

### 1. 删除触发器

```sql
DROP TRIGGER IF EXISTS trigger_update_meta_fields_full_text_vector ON "APPATTACH_CATALOGUE_TEMPLATES";
DROP FUNCTION IF EXISTS update_meta_fields_full_text_vector();
```

### 2. 删除索引

```sql
-- 删除所有相关的元数据字段索引
DROP INDEX CONCURRENTLY IF EXISTS "IX_ATTACH_CATALOGUE_TEMPLATES_META_FIELDS";
DROP INDEX CONCURRENTLY IF EXISTS "IX_ATTACH_CATALOGUE_TEMPLATES_META_FIELDS_ENTITY_TYPE";
-- ... 其他索引
```

### 3. 删除字段

```sql
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP COLUMN IF EXISTS "META_FIELDS";
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP COLUMN IF EXISTS "META_FIELDS_FULL_TEXT_VECTOR";
```

## 性能影响

### 1. 存储影响

-   每个元数据字段约占用 200-500 字节
-   建议控制单个模板的元数据字段数量（建议不超过 100 个）

### 2. 查询影响

-   JSON 查询性能：GIN 索引提供高效的 JSON 查询支持
-   全文检索性能：tsvector 索引提供快速的全文检索
-   插入/更新性能：触发器会略微影响性能，但提供数据一致性

### 3. 索引维护

-   索引创建时间：根据数据量，可能需要几分钟到几小时
-   索引大小：GIN 索引通常比 B-tree 索引大 2-3 倍

## 最佳实践

### 1. 数据设计

-   合理设计元数据字段结构
-   避免过深的 JSON 嵌套
-   使用有意义的字段键名

### 2. 查询优化

-   优先使用索引字段进行查询
-   避免在 WHERE 子句中使用复杂的 JSON 表达式
-   合理使用复合索引

### 3. 维护建议

-   定期清理无用的元数据字段
-   监控索引使用情况
-   根据查询模式调整索引策略

## 故障排除

### 1. 常见错误

-   **权限不足**: 确保数据库用户有 DDL 权限
-   **表不存在**: 检查表名是否正确
-   **字段已存在**: 迁移脚本会自动跳过已存在的字段

### 2. 性能问题

-   **索引创建慢**: 使用 `CONCURRENTLY` 选项，避免锁表
-   **查询慢**: 检查是否使用了合适的索引
-   **触发器性能**: 监控触发器执行时间

### 3. 数据一致性问题

-   **全文检索向量不更新**: 检查触发器是否正常工作
-   **索引不一致**: 重建相关索引

## 联系支持

如果在迁移过程中遇到问题，请联系技术支持团队，并提供以下信息：

-   错误信息和堆栈跟踪
-   数据库版本和配置
-   执行的具体 SQL 语句
-   相关的日志文件
