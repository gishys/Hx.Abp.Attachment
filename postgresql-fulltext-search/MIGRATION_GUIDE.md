# 数据库迁移指南

## 概述

本指南提供了完整的数据库迁移步骤，包括字段重命名、全文搜索配置、OCR 字段添加、索引创建等。

## 迁移步骤

### 完整迁移（推荐）

运行完整的数据库迁移脚本：

```bash
psql -d your_database -f postgresql-fulltext-search/database-migration.sql
```

该脚本包含：

-   添加全文搜索和 OCR 相关字段
-   字段重命名（驼峰命名 → 下划线命名）
-   中文全文搜索配置创建
-   pg_trgm 扩展启用
-   全文搜索和模糊搜索索引创建
-   OCR 处理状态索引创建
-   验证和测试

## 迁移内容

### 新增字段

#### 附件目录表 (APPATTACH_CATALOGUES)

-   `FULL_TEXT_CONTENT` - 全文内容，存储分类下所有文件的 OCR 提取内容
-   `FULL_TEXT_CONTENT_UPDATED_TIME` - 全文内容更新时间

#### 附件文件表 (APPATTACHFILE)

-   `OCR_CONTENT` - OCR 提取的文本内容
-   `OCR_PROCESS_STATUS` - OCR 处理状态：0-未处理，1-处理中，2-完成，3-失败，4-跳过
-   `OCR_PROCESSED_TIME` - OCR 处理时间

### 字段重命名

| 原字段名             | 新字段名              | 说明         |
| -------------------- | --------------------- | ------------ |
| `CATALOGUENAME`      | `CATALOGUE_NAME`      | 目录名称     |
| `ATTACHRECEIVETYPE`  | `ATTACH_RECEIVE_TYPE` | 附件接收类型 |
| `REFERENCETYPE`      | `REFERENCE_TYPE`      | 引用类型     |
| `ATTACHCOUNT`        | `ATTACH_COUNT`        | 附件数量     |
| `PAGECOUNT`          | `PAGE_COUNT`          | 页数         |
| `ISVERIFICATION`     | `IS_VERIFICATION`     | 是否核验     |
| `VERIFICATIONPASSED` | `VERIFICATION_PASSED` | 核验通过     |
| `ISREQUIRED`         | `IS_REQUIRED`         | 是否必收     |
| `SEQUENCENUMBER`     | `SEQUENCE_NUMBER`     | 顺序号       |
| `PARENTID`           | `PARENT_ID`           | 父 ID        |
| `ISSTATIC`           | `IS_STATIC`           | 静态标识     |

### 全文搜索配置

-   创建 `chinese_fts` 全文搜索配置
-   启用 `pg_trgm` 扩展
-   创建全文搜索和模糊搜索索引

### 索引创建

#### 全文搜索索引

-   `IDX_ATTACH_CATALOGUES_FULLTEXT` - 目录表全文搜索索引
-   `IDX_ATTACH_FILES_FULLTEXT` - 文件表全文搜索索引

#### 模糊搜索索引

-   `IDX_ATTACH_CATALOGUES_NAME_TRGM` - 目录名称模糊搜索索引
-   `IDX_ATTACH_FILES_NAME_TRGM` - 文件名模糊搜索索引

#### OCR 相关索引

-   `IDX_ATTACH_FILES_OCR_STATUS` - OCR 处理状态索引
-   `IDX_ATTACH_FILES_OCR_TIME` - OCR 处理时间索引
-   `IDX_ATTACH_CATALOGUES_FULLTEXT_TIME` - 全文内容更新时间索引

## 验证清单

### ✅ 迁移后验证

-   [ ] 新增字段创建成功
-   [ ] 字段重命名成功
-   [ ] 全文搜索配置创建成功
-   [ ] 所有索引创建成功
-   [ ] 应用启动正常
-   [ ] 搜索功能正常
-   [ ] OCR 功能正常

### 🔍 测试命令

```sql
-- 验证新增字段
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'APPATTACH_CATALOGUES'
AND column_name IN ('FULL_TEXT_CONTENT', 'FULL_TEXT_CONTENT_UPDATED_TIME');

-- 验证字段重命名
SELECT column_name FROM information_schema.columns
WHERE table_name = 'APPATTACH_CATALOGUES'
AND column_name LIKE '%NAME%';

-- 验证全文搜索配置
SELECT cfgname FROM pg_ts_config WHERE cfgname = 'chinese_fts';

-- 验证索引
SELECT indexname FROM pg_indexes
WHERE tablename IN ('APPATTACH_CATALOGUES', 'APPATTACHFILE')
AND (indexname LIKE '%FULLTEXT%' OR indexname LIKE '%TRGM%' OR indexname LIKE '%OCR%');

-- 测试搜索功能
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能');
```

## 注意事项

1. **备份数据** - 执行迁移前请备份数据库
2. **测试环境** - 建议先在测试环境执行
3. **应用重启** - 迁移完成后需要重启应用
4. **功能验证** - 确保所有搜索和 OCR 功能正常工作
5. **索引创建** - 使用 `CONCURRENTLY` 创建索引，避免锁表

## 故障排除

### 常见问题

1. **字段不存在错误**

    - 检查表名是否正确
    - 确认字段名是否已存在

2. **权限错误**

    - 确保数据库用户有足够权限
    - 检查扩展安装权限

3. **索引创建失败**

    - 检查表是否有数据
    - 确认字段类型是否正确
    - 检查是否有足够的磁盘空间

4. **OCR 字段问题**
    - 确认 OCR 相关字段已正确创建
    - 检查 OCR 处理状态字段的默认值

### 回滚方案

如果需要回滚，可以：

1. 恢复数据库备份
2. 或者手动删除新增字段和索引
3. 重命名字段回原名称

## 性能优化建议

1. **索引优化** - 定期分析索引使用情况
2. **数据清理** - 定期清理无用的 OCR 内容
3. **查询优化** - 使用适当的搜索策略
4. **监控** - 监控搜索性能和处理时间
