# OCR 功能完整解决方案

## 概述

本项目提供了基于阿里云 OCR 的增强全文搜索解决方案，包括：

1. **PDF 转图片** - 使用 PdfPig + ImageSharp 跨平台将 PDF 转换为高质量图片
2. **阿里云 OCR** - 使用真实的阿里云 OCR 服务进行文本识别
3. **全文内容存储** - 在目录级别存储所有文件的 OCR 内容
4. **全文搜索** - 基于 PostgreSQL 的全文搜索功能
5. **批量处理** - 支持批量 OCR 处理和并发控制

## 功能特性

### 1. PDF 转图片

-   使用 PdfPig + ImageSharp 进行跨平台高质量 PDF 渲染
-   支持多种图片格式（JPG、PNG 等）
-   可配置 DPI 分辨率
-   自动清理临时文件
-   完全跨平台支持（Windows、Linux、macOS）

### 2. 阿里云 OCR 集成

-   使用真实的阿里云 OCR 服务
-   支持中文文本识别
-   智能文本组合和排序
-   错误处理和重试机制

### 3. 全文搜索

-   基于 PostgreSQL 的`to_tsvector`和`plainto_tsquery`
-   中文文本支持
-   相关性排序
-   高性能索引

### 4. 批量处理

-   并发控制（最多 3 个并发）
-   进度跟踪和状态管理
-   错误处理和日志记录
-   支持重新处理

## 技术架构

### 核心组件

```
OcrService (OCR服务)
├── CrossPlatformPdfToImageConverter (跨平台PDF转图片)
├── UniversalTextRecognitionHelper (阿里云OCR)
├── OcrComposer (文本组合)
└── FullTextSearchRepository (全文搜索)
```

### 数据流

```
PDF文件 → PDF转图片 → 阿里云OCR → 文本组合 → 存储到数据库
图片文件 → 阿里云OCR → 文本组合 → 存储到数据库
```

## 安装和配置

### 1. NuGet 包依赖

```xml
<!-- 跨平台PDF转图片相关包 -->
<PackageReference Include="PdfPig" Version="0.1.8" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
<PackageReference Include="SixLabors.ImageSharp.Drawing" Version="2.1.0" />

<!-- 阿里云OCR SDK -->
<PackageReference Include="AlibabaCloud.SDK.Ocr20191230" Version="2.0.24" />
```

### 2. 环境变量配置

```bash
# 阿里云OCR配置
ALIBABA_CLOUD_ACCESS_KEY_ID=your_access_key_id
ALIBABA_CLOUD_ACCESS_KEY_SECRET=your_access_key_secret
```

### 3. 应用配置

```json
{
    "FileServer": {
        "BaseUrl": "http://localhost:5000",
        "BasePath": "D:/files",
        "AttachmentPath": "host/attachment"
    },
    "Ocr": {
        "PdfToImage": {
            "ImageFormat": "jpg",
            "Dpi": 300,
            "MaxConcurrency": 3
        },
        "AliyunOcr": {
            "MinConfidence": 0.8,
            "TimeoutSeconds": 30
        }
    }
}
```

### 4. 依赖注入配置

```csharp
// 在 HxAbpAttachmentApplicationModule.cs 中
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // 注册增强OCR服务
    context.Services.AddScoped<IOcrService, OcrService>();

    // 注册跨平台PDF转图片工具
    context.Services.AddScoped<CrossPlatformPdfToImageConverter>();

    // 注册全文搜索仓储
    context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();
}
```

## API 接口

### OCR 控制器

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

#### 4. 检查文件支持状态

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

## 使用示例

### 1. 处理 PDF 文件 OCR

```csharp
// 注入服务
private readonly IOcrService _ocrService;

// 处理PDF文件
public async Task<OcrResult> ProcessPdfFile(Guid fileId)
{
    var result = await _ocrService.ProcessFileAsync(fileId);
    return result;
}
```

### 2. 批量处理文件 OCR

```csharp
public async Task<List<OcrResult>> ProcessMultipleFiles(List<Guid> fileIds)
{
    var results = await _ocrService.ProcessFilesAsync(fileIds);
    return results;
}
```

### 3. 处理目录 OCR 并更新全文内容

```csharp
public async Task<CatalogueOcrResult> ProcessCatalogueOcr(Guid catalogueId)
{
    var result = await _ocrService.ProcessCatalogueAsync(catalogueId);
    return result;
}
```

### 4. 获取文件 OCR 状态

```csharp
public async Task<FileOcrStatusDto> GetFileOcrStatus(Guid fileId)
{
    var status = await _ocrService.GetFileOcrStatusAsync(fileId);
    return status;
}
```

### 5. 获取文件 OCR 内容

```csharp
public async Task<FileOcrContentDto> GetFileOcrContent(Guid fileId)
{
    var content = await _ocrService.GetFileOcrContentAsync(fileId);
    return content;
}
```

### 6. 获取目录全文内容

```csharp
public async Task<CatalogueFullTextDto> GetCatalogueFullText(Guid catalogueId)
{
    var fullText = await _ocrService.GetCatalogueFullTextAsync(catalogueId);
    return fullText;
}
```

### 4. 控制器使用示例

```csharp
[ApiController]
[Route("api/[controller]")]
public class DocumentController : AbpController
{
    private readonly IOcrService _ocrService;
    private readonly IFullTextSearchRepository _searchRepository;

    public DocumentController(
        IOcrService ocrService,
        IFullTextSearchRepository searchRepository)
    {
        _ocrService = ocrService;
        _searchRepository = searchRepository;
    }

    [HttpPost("ocr/{fileId}")]
    public async Task<IActionResult> ProcessOcr(Guid fileId)
    {
        var result = await _ocrService.ProcessFileAsync(fileId);
        return Ok(result);
    }

    [HttpPost("ocr/batch")]
    public async Task<IActionResult> ProcessBatchOcr([FromBody] List<Guid> fileIds)
    {
        var results = await _ocrService.ProcessFilesAsync(fileIds);
        return Ok(results);
    }

    [HttpGet("ocr/{fileId}/status")]
    public async Task<IActionResult> GetOcrStatus(Guid fileId)
    {
        var status = await _ocrService.GetFileOcrStatusAsync(fileId);
        return Ok(status);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("搜索关键词不能为空");

        var results = await _searchRepository.SearchCataloguesAsync(query);
        return Ok(results);
    }
}
```

## 性能优化

### 1. 并发控制

-   使用信号量限制并发数量（默认 3 个）
-   避免过度占用系统资源
-   可配置并发数量

### 2. 临时文件管理

-   自动创建和清理临时目录
-   使用 GUID 避免文件名冲突
-   异常情况下的资源清理

### 3. 错误处理

-   详细的错误日志记录
-   优雅的异常处理
-   状态跟踪和恢复

### 4. 缓存策略

-   OCR 结果缓存
-   避免重复处理
-   支持强制重新处理

## 监控和日志

### 1. 日志记录

```csharp
// 在 appsettings.json 中配置
{
  "Logging": {
    "LogLevel": {
      "Hx.Abp.Attachment.Application": "Information",
      "Hx.Abp.Attachment.Application.OcrService": "Debug"
    }
  }
}
```

### 2. 性能监控

-   OCR 处理时间统计
-   文件大小和处理时间关系
-   成功率统计

### 3. 错误监控

-   OCR 失败原因分析
-   文件格式支持统计
-   网络连接问题监控

## 故障排除

### 1. 常见问题

#### PDF 转图片失败

-   检查 PdfPig 和 ImageSharp 是否正确安装
-   确认 PDF 文件是否损坏
-   检查临时目录权限
-   确认系统字体是否可用

#### 阿里云 OCR 调用失败

-   检查 AccessKey 配置
-   确认网络连接
-   验证文件 URL 可访问性

#### 内存不足

-   减少并发数量
-   降低图片 DPI
-   增加系统内存

### 2. 调试方法

#### 启用详细日志

```csharp
// 在代码中启用详细日志
_logger.LogDebug("处理文件: {FileName}, 类型: {FileType}", file.FileName, file.FileType);
```

#### 检查临时文件

```bash
# 检查临时目录
ls -la /tmp/pdf_ocr/
```

#### 测试阿里云 OCR

```csharp
// 测试OCR服务连接
var testResult = await UniversalTextRecognitionHelper.JpgUniversalTextRecognition(
    accessKeyId, accessKeySecret, testImageUrl);
```

## 扩展功能

### 1. 支持更多 OCR 服务

-   腾讯云 OCR
-   百度 OCR
-   Azure Computer Vision
-   Google Cloud Vision

### 2. 图片预处理

-   图片压缩
-   图片增强
-   去噪处理

### 3. 文本后处理

-   文本纠错
-   格式标准化
-   关键词提取

### 4. 异步处理

-   后台任务队列
-   进度通知
-   结果回调

## 部署指南

### 1. 生产环境配置

```json
{
    "FileServer": {
        "BaseUrl": "https://your-domain.com",
        "BasePath": "/app/files",
        "AttachmentPath": "host/attachment"
    },
    "Ocr": {
        "PdfToImage": {
            "ImageFormat": "jpg",
            "Dpi": 300,
            "MaxConcurrency": 2,
            "KeepTempFiles": false
        },
        "AliyunOcr": {
            "MinConfidence": 0.8,
            "TimeoutSeconds": 60
        }
    }
}
```

### 2. 环境变量

```bash
# 生产环境环境变量
export ALIBABA_CLOUD_ACCESS_KEY_ID=your_production_key_id
export ALIBABA_CLOUD_ACCESS_KEY_SECRET=your_production_key_secret
```

### 3. 系统要求

-   .NET 8.0
-   PostgreSQL 12+
-   足够的内存（建议 4GB+）
-   网络连接到阿里云

## 总结

本增强 OCR 解决方案提供了：

1. **完整的 PDF 处理流程** - 从 PDF 到图片到文本
2. **真实的 OCR 服务** - 基于阿里云 OCR 的高质量识别
3. **高效的批量处理** - 并发控制和资源管理
4. **完善的错误处理** - 详细的日志和异常处理
5. **灵活的配置** - 支持多种配置选项
6. **丰富的 API 接口** - 完整的 RESTful API

通过这个解决方案，您可以轻松实现企业级的文档 OCR 处理和全文搜索功能，提高文档管理效率和用户体验。
