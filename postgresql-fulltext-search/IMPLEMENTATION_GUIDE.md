# PostgreSQL 中文全文搜索 - 实现指南

## 概述

本指南详细介绍了如何在 PostgreSQL 中实现中文全文搜索和模糊查询功能，使用 PostgreSQL 内置功能，无需安装第三方扩展。

## 技术方案

### 使用 PostgreSQL 内置功能

我们采用 PostgreSQL 内置的全文搜索功能，配合 `pg_trgm` 扩展来实现中文搜索，避免了第三方扩展的兼容性问题。

## 技术实现

### 1. 数据库配置

```sql
-- 创建中文全文搜索配置
CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
ALTER TEXT SEARCH CONFIGURATION chinese_fts ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part WITH simple;

-- 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

### 2. ABP 项目修改

#### DbContext 配置

-   移除了第三方扩展依赖
-   添加了 `pg_trgm` 扩展支持
-   配置了全文搜索索引

#### 搜索服务

-   实现了全文搜索功能
-   实现了模糊搜索功能
-   实现了组合搜索功能

#### API 控制器

-   提供了多种搜索接口
-   支持测试功能

## 功能特性

### 1. 全文搜索

-   使用 PostgreSQL 内置的 `to_tsvector` 和 `plainto_tsquery`
-   支持中文文本搜索
-   按相关性排序

### 2. 模糊搜索

-   使用 `pg_trgm` 扩展
-   支持相似度匹配
-   容错性强

### 3. 组合搜索

-   结合全文搜索和模糊搜索
-   智能排序算法
-   提供最佳搜索结果

## 优势

✅ **无兼容性问题** - 使用 PostgreSQL 内置功能
✅ **性能优秀** - 支持索引，查询速度快
✅ **配置简单** - 无需额外扩展安装
✅ **维护成本低** - 标准 PostgreSQL 功能
✅ **功能完整** - 支持多种搜索模式

## 使用方式

### 1. 启动应用

应用启动时会自动：

-   创建中文全文搜索配置
-   创建必要的索引
-   初始化搜索功能

### 2. API 接口

```bash
# 测试搜索功能
GET /api/FullTextSearch/test?text=测试中文搜索

# 全文搜索目录
GET /api/FullTextSearch/catalogues?query=搜索关键词

# 模糊搜索目录
GET /api/FullTextSearch/catalogues/fuzzy?query=搜索关键词

# 组合搜索目录
GET /api/FullTextSearch/catalogues/combined?query=搜索关键词

# 搜索文件（类似接口）
GET /api/FullTextSearch/files?query=搜索关键词
```

### 3. 数据库测试

```sql
-- 测试全文搜索
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能');

-- 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索');

-- 实际搜索示例
SELECT * FROM "AttachCatalogues"
WHERE to_tsvector('chinese_fts', "Name") @@ plainto_tsquery('chinese_fts', '测试')
ORDER BY ts_rank(to_tsvector('chinese_fts', "Name"), plainto_tsquery('chinese_fts', '测试')) DESC;
```

## 文件说明

### 核心文件

-   `README.md` - 项目说明文档
-   `IMPLEMENTATION_GUIDE.md` - 本实现指南
-   `database-test.sql` - 数据库测试脚本

### 项目集成文件

-   `AttachmentDbContext.cs` - 更新数据库配置
-   `Program.cs` - 更新初始化逻辑
-   `FullTextSearchService.cs` - 搜索服务实现
-   `FullTextSearchController.cs` - API 控制器

## 测试建议

1. **启动应用** - 检查控制台输出，确认全文搜索配置初始化成功
2. **测试 API** - 使用 `/api/FullTextSearch/test` 接口测试基本功能
3. **数据库测试** - 运行 `database-test.sql` 脚本验证数据库功能
4. **实际搜索** - 添加测试数据，使用搜索接口进行实际测试

## 总结

使用 PostgreSQL 内置功能实现中文全文搜索和模糊查询，不仅解决了兼容性问题，还提供了更稳定、更易维护的搜索功能。

这个解决方案可以立即投入使用，无需担心扩展兼容性问题，同时提供了强大的搜索功能。
