# 全文搜索功能完整解决方案

## 概述

本项目提供了完整的全文搜索解决方案，包括：

1. **OCR 文本提取** - 从 PDF、图片等文件中提取文本内容
2. **全文内容存储** - 在目录级别存储所有文件的 OCR 内容
3. **全文搜索** - 基于 PostgreSQL 的全文搜索功能
4. **模糊搜索** - 基于相似度的模糊匹配
5. **组合搜索** - 结合全文搜索和模糊搜索的最佳结果

## 功能特性

### 1. OCR 文本提取

-   支持 PDF、JPG、PNG、TIFF 等文件格式
-   自动处理状态跟踪
-   批量处理能力
-   错误处理和重试机制

### 2. 全文搜索

-   基于 PostgreSQL 的`to_tsvector`和`plainto_tsquery`
-   中文文本支持
-   相关性排序
-   高性能索引

### 3. 模糊搜索

-   基于`pg_trgm`扩展
-   相似度匹配
-   容错性强

### 4. 组合搜索

-   智能排序算法
-   全文搜索优先
-   模糊搜索补充

## 数据库结构

### 新增字段

#### AttachCatalogue 表

```sql
-- 全文内容字段
FULL_TEXT_CONTENT text                    -- 存储分类下所有文件的OCR提取内容
FULL_TEXT_CONTENT_UPDATED_TIME timestamp  -- 全文内容更新时间
```

#### AttachFile 表

```sql
-- OCR相关字段
OCR_CONTENT text                          -- OCR提取的文本内容
OCR_PROCESS_STATUS integer               -- OCR处理状态
OCR_PROCESSED_TIME timestamp             -- OCR处理时间
```

### 索引结构

```sql
-- 全文搜索索引
CREATE INDEX IDX_ATTACH_CATALOGUES_FULLTEXT ON APPATTACH_CATALOGUES USING GIN (
    to_tsvector('chinese_fts',
        COALESCE(CATALOGUE_NAME, '') || ' ' ||
        COALESCE(FULL_TEXT_CONTENT, '')
    )
);

-- 模糊搜索索引
CREATE INDEX IDX_ATTACH_CATALOGUES_NAME_TRGM ON APPATTACH_CATALOGUES USING GIN (CATALOGUE_NAME gin_trgm_ops);
```

## API 接口

### OCR 处理接口

#### 1. 处理单个文件 OCR

```http
POST /api/ocr/files/{fileId}
```

#### 2. 批量处理文件 OCR

```http
POST /api/ocr/files/batch
Content-Type: application/json

["file-id-1", "file-id-2", "file-id-3"]
```

#### 3. 处理目录下所有文件 OCR

```http
POST /api/ocr/catalogues/{catalogueId}
```

#### 4. 检查文件是否支持 OCR

```http
GET /api/ocr/files/{fileId}/supported
```

#### 5. 获取文件 OCR 内容

```http
GET /api/ocr/files/{fileId}/content
```

#### 6. 获取目录全文内容

```http
GET /api/ocr/catalogues/{catalogueId}/content
```

### 全文搜索接口

#### 1. 全文搜索目录

```http
GET /api/fulltextsearch/catalogues?query=搜索关键词
```

#### 2. 全文搜索文件

```http
GET /api/fulltextsearch/files?query=搜索关键词
```

#### 3. 模糊搜索目录

```http
GET /api/fulltextsearch/catalogues/fuzzy?query=搜索关键词
```

#### 4. 模糊搜索文件

```http
GET /api/fulltextsearch/files/fuzzy?query=搜索关键词
```

#### 5. 组合搜索目录

```http
GET /api/fulltextsearch/catalogues/combined?query=搜索关键词
```

#### 6. 组合搜索文件

```http
GET /api/fulltextsearch/files/combined?query=搜索关键词
```

## 使用示例

### 1. 处理文件 OCR 并更新全文内容

```csharp
// 注入服务
private readonly IOcrService _ocrService;
private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;

// 处理单个文件
public async Task<OcrResult> ProcessFileOcr(Guid fileId)
{
    var file = await _fileRepository.GetAsync(fileId);
    var result = await _ocrService.ProcessFileAsync(file);

    // 保存更改
    await _fileRepository.UpdateAsync(file);

    return result;
}

// 处理目录下所有文件
public async Task<CatalogueOcrResult> ProcessCatalogueOcr(Guid catalogueId)
{
    var result = await _ocrService.ProcessCatalogueAsync(catalogueId);

    // 获取并保存目录
    var catalogue = await _catalogueRepository.GetAsync(catalogueId);
    await _catalogueRepository.UpdateAsync(catalogue);

    return result;
}
```

### 2. 全文搜索使用

```csharp
// 注入服务
private readonly IFullTextSearchRepository _searchRepository;

// 搜索目录
public async Task<List<AttachCatalogue>> SearchCatalogues(string query)
{
    return await _searchRepository.SearchCataloguesAsync(query);
}

// 搜索文件
public async Task<List<AttachFile>> SearchFiles(string query)
{
    return await _searchRepository.SearchFilesAsync(query);
}

// 组合搜索
public async Task<List<AttachCatalogue>> CombinedSearch(string query)
{
    return await _searchRepository.CombinedSearchCataloguesAsync(query);
}
```

### 3. 控制器使用示例

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchController : AbpController
{
    private readonly IFullTextSearchRepository _searchRepository;
    private readonly IOcrService _ocrService;

    public SearchController(
        IFullTextSearchRepository searchRepository,
        IOcrService ocrService)
    {
        _searchRepository = searchRepository;
        _ocrService = ocrService;
    }

    [HttpGet("catalogues")]
    public async Task<IActionResult> SearchCatalogues([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("搜索关键词不能为空");

        var results = await _searchRepository.SearchCataloguesAsync(query);
        return Ok(results);
    }

    [HttpPost("catalogues/{catalogueId}/ocr")]
    public async Task<IActionResult> ProcessCatalogueOcr(Guid catalogueId)
    {
        var result = await _ocrService.ProcessCatalogueAsync(catalogueId);
        return Ok(result);
    }
}
```

## 部署步骤

### 1. 数据库迁移

执行数据库迁移脚本：

```sql
-- 执行 database-migration.sql 文件中的所有SQL语句
```

### 2. 依赖注入配置

确保在模块中注册服务：

```csharp
// 在 HxAbpAttachmentApplicationModule.cs 中
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // 注册OCR服务
    context.Services.AddScoped<IOcrService, OcrService>();

    // 注册全文搜索仓储
    context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();
}
```

### 3. 配置 PostgreSQL 扩展

确保 PostgreSQL 安装了必要的扩展：

```sql
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

## 性能优化

### 1. 索引优化

-   使用 GIN 索引提高全文搜索性能
-   使用 gin_trgm_ops 操作符提高模糊搜索性能
-   定期更新统计信息

### 2. 查询优化

-   使用 COALESCE 处理 NULL 值
-   合理使用 LIMIT 限制结果数量
-   避免在 WHERE 子句中使用函数

### 3. 批量处理

-   使用批量 OCR 处理减少数据库交互
-   使用事务确保数据一致性
-   异步处理提高响应速度

## 监控和日志

### 1. 日志记录

-   OCR 处理状态和结果
-   搜索查询性能
-   错误和异常信息

### 2. 性能监控

-   查询执行时间
-   索引使用情况
-   数据库连接池状态

## 扩展功能

### 1. 真实 OCR 服务集成

替换模拟 OCR 处理为真实的 OCR 服务：

-   Tesseract OCR
-   Azure Computer Vision
-   Google Cloud Vision API
-   阿里云 OCR

### 2. 语义搜索

集成向量数据库进行语义搜索：

-   PostgreSQL pgvector 扩展
-   Milvus 向量数据库
-   Elasticsearch 向量搜索

### 3. 搜索建议

实现搜索建议功能：

-   基于历史搜索记录
-   基于热门搜索词
-   基于内容相似度

## 故障排除

### 1. 常见问题

#### OCR 处理失败

-   检查文件格式是否支持
-   检查文件是否损坏
-   检查 OCR 服务是否可用

#### 搜索无结果

-   检查全文内容是否已生成
-   检查搜索关键词是否正确
-   检查数据库索引是否正常

#### 性能问题

-   检查数据库索引是否创建
-   检查查询是否使用了索引
-   检查数据库配置是否合理

### 2. 调试方法

#### 启用详细日志

```csharp
// 在 appsettings.json 中配置
{
  "Logging": {
    "LogLevel": {
      "Hx.Abp.Attachment": "Debug"
    }
  }
}
```

#### 测试搜索功能

```sql
-- 测试全文搜索
SELECT to_tsvector('chinese_fts', '测试文本') @@ plainto_tsquery('chinese_fts', '测试');

-- 测试模糊搜索
SELECT similarity('测试文本', '测试');
```

## 总结

本解决方案提供了完整的全文搜索功能，包括：

1. **完整的 OCR 处理流程** - 从文件上传到文本提取
2. **高效的全文搜索** - 基于 PostgreSQL 原生功能
3. **灵活的 API 接口** - 支持多种搜索方式
4. **良好的扩展性** - 易于集成真实 OCR 服务
5. **完善的文档** - 详细的使用说明和示例

通过这个解决方案，您可以轻松实现文档内容的全文搜索功能，提高用户体验和系统可用性。
