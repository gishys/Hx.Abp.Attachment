# PostgreSQL 全文搜索与智能推荐解决方案

## 📚 文档导航

### 🎯 主要文档

-   **[📖 完整解决方案指南](./INTEGRATED_SOLUTION_GUIDE.md)** - 整合了全文搜索、智能推荐、OCR 处理等核心功能的完整解决方案

### 🔧 技术文档

-   **[🔄 EF Core 迁移指南](./EF_CORE_MIGRATION.md)** - SQL 查询迁移到 EF Core 的详细说明
-   **[🐛 SQL 查询修复](./SQL_QUERY_FIXES.md)** - 常见 SQL 查询问题的修复方案
-   **[📋 模板 ID 可空修复](./TEMPLATE_ID_NULLABLE_FIX.md)** - 模板 ID 字段可空性问题的解决方案
-   **[📊 模板使用统计](./template-usage-count-migration.sql)** - 模板使用次数统计的 SQL 脚本
-   **[🧠 动态搜索指南](./DYNAMIC_SEARCH_GUIDE.md)** - 基于数据库内容的动态智能匹配功能

### 🗄️ 数据库脚本

-   **[🏗️ 数据库迁移](./database-migration.sql)** - 完整的数据库结构迁移脚本
-   **[📁 目录模板迁移](./attach-catalogue-templates-migration.sql)** - 目录模板相关的数据库迁移
-   **[🔍 OCR 文本块迁移](./ocr-text-blocks-migration.sql)** - OCR 文本块相关的数据库迁移
-   **[🔍 模糊搜索调试](./fuzzy-search-debug.sql)** - 模糊搜索功能的调试脚本
-   **[🧠 动态搜索迁移](./dynamic-search-migration.sql)** - 动态搜索功能的数据库迁移脚本

### 📖 功能指南

-   **[📋 目录模板指南](./ATTACH_CATALOGUE_TEMPLATE_GUIDE.md)** - 目录模板的详细使用指南
-   **[🔍 增强 OCR 指南](./README-EnhancedOcr.md)** - 增强 OCR 功能的详细说明
-   **[📋 迁移指南](./MIGRATION_GUIDE.md)** - 系统迁移的详细步骤

## 🚀 快速开始

### 1. 查看完整解决方案

首先阅读 **[完整解决方案指南](./INTEGRATED_SOLUTION_GUIDE.md)**，了解系统的整体架构和核心功能。

### 2. 数据库配置

执行数据库迁移脚本：

```sql
-- 执行 database-migration.sql 文件中的所有SQL语句
-- 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 创建中文全文搜索配置
CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
ALTER TEXT SEARCH CONFIGURATION chinese_fts
    ALTER MAPPING FOR
        asciiword, asciihword, hword_asciipart,
        word, hword, hword_part
    WITH simple;
```

### 3. 依赖注入配置

在 `HxAbpAttachmentApplicationModule.cs` 中注册服务：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // 注册OCR服务
    context.Services.AddScoped<IOcrService, OcrService>();

    // 注册全文搜索仓储
    context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();

    // 注册语义匹配服务
    context.Services.AddScoped<ISemanticMatcher, DefaultSemanticMatcher>();
}
```

## 🔧 核心功能

### 📖 全文搜索

-   OCR 文本提取
-   全文内容存储
-   PostgreSQL 全文搜索
-   模糊搜索
-   组合搜索

### 🧠 智能推荐

-   语义匹配
-   模板推荐
-   关键字维护
-   批量处理

### 🔍 OCR 处理

-   多格式支持
-   批量处理
-   状态跟踪
-   错误处理

## 📊 性能优化

-   数据库索引优化
-   查询性能优化
-   批量处理优化
-   内存使用优化

## 🐛 故障排除

-   常见问题解决
-   调试方法
-   性能监控
-   日志分析

## 📈 扩展功能

-   真实 OCR 服务集成
-   语义搜索
-   搜索建议
-   向量数据库

## 🤝 贡献指南

1. 阅读相关技术文档
2. 遵循代码规范
3. 编写测试用例
4. 更新文档说明

## 📞 支持

如有问题，请查看：

1. [完整解决方案指南](./INTEGRATED_SOLUTION_GUIDE.md)
2. [故障排除章节](./INTEGRATED_SOLUTION_GUIDE.md#故障排除)
3. [中文文本搜索修复指南](./CHINESE_TEXT_SEARCH_FIX.md)
4. 相关技术文档

---

**注意**: 本文档集合涵盖了附件管理系统的完整解决方案，建议按照文档导航顺序进行学习和使用。
