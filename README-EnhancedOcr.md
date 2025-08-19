# OCR 功能完整解决方案

## 概述

本项目提供了基于阿里云 OCR 的增强全文搜索解决方案，包括：

1. **PDF 转图片** - 使用 PdfPig + ImageSharp 跨平台将 PDF 转换为高质量图片
2. **阿里云 OCR** - 使用真实的阿里云 OCR 服务进行文本识别
3. **URL 访问验证** - 通过 HTTP 请求验证文件 URL 可访问性
4. **全文内容存储** - 在目录级别存储所有文件的 OCR 内容
5. **全文搜索** - 基于 PostgreSQL 的全文搜索功能
6. **批量处理** - 支持批量 OCR 处理和并发控制

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

### 3. URL 访问验证

-   通过 HTTP HEAD 请求验证文件 URL 可访问性
-   设置超时控制（默认 10 秒）
-   详细的错误日志记录
-   支持文件服务器架构

### 4. 全文搜索

-   基于 PostgreSQL 的`to_tsvector`和`plainto_tsquery`
-   中文文本支持
-   相关性排序
-   高性能索引

### 5. 批量处理

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
├── IsUrlAccessibleAsync (URL访问验证)
└── FullTextSearchRepository (全文搜索)
```

### 数据流

```
PDF文件 → PDF转图片 → 阿里云OCR → 文本组合 → 存储到数据库
图片文件 → URL构建 → URL验证 → 阿里云OCR → 文本组合 → 存储到数据库
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

<!-- HTTP客户端支持 -->
<PackageReference Include="System.Net.Http" />
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
        "BasePath": "D:/files"
    },
    "AppGlobalProperties": {
        "FileServerBasePath": "D:/files"
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
        },
        "UrlValidation": {
            "TimeoutSeconds": 10
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

### 1. 处理图片文件 OCR（URL 方式）

```csharp
// 注入服务
private readonly IOcrService _ocrService;

// 处理图片文件（使用URL访问）
public async Task<OcrResult> ProcessImageFile(Guid fileId)
{
    var result = await _ocrService.ProcessFileAsync(fileId);
    return result;
}
```

### 2. 处理 PDF 文件 OCR

```csharp
// 处理PDF文件（本地文件路径）
public async Task<OcrResult> ProcessPdfFile(Guid fileId)
{
    var result = await _ocrService.ProcessFileAsync(fileId);
    return result;
}
```

### 3. 批量处理文件 OCR

```csharp
public async Task<List<OcrResult>> ProcessMultipleFiles(List<Guid> fileIds)
{
    var results = await _ocrService.ProcessFilesAsync(fileIds);
    return results;
}
```

### 4. 处理目录 OCR 并更新全文内容

```csharp
public async Task<CatalogueOcrResult> ProcessCatalogueOcr(Guid catalogueId)
{
    var result = await _ocrService.ProcessCatalogueAsync(catalogueId);
    return result;
}
```

### 5. 获取文件 OCR 状态

```csharp
public async Task<FileOcrStatusDto> GetFileOcrStatus(Guid fileId)
{
    var status = await _ocrService.GetFileOcrStatusAsync(fileId);
    return status;
}
```

### 6. 获取文件 OCR 内容

```csharp
public async Task<FileOcrContentDto> GetFileOcrContent(Guid fileId)
{
    var content = await _ocrService.GetFileOcrContentAsync(fileId);
    return content;
}
```

### 7. 获取目录全文内容

```csharp
public async Task<CatalogueFullTextDto> GetCatalogueFullText(Guid catalogueId)
{
    var fullText = await _ocrService.GetCatalogueFullTextAsync(catalogueId);
    return fullText;
}
```

### 8. 控制器使用示例

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

## 核心实现细节

### 1. URL 访问验证

```csharp
/// <summary>
/// 验证URL是否可访问
/// </summary>
private async Task<bool> IsUrlAccessibleAsync(string url)
{
    try
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10); // 设置10秒超时
        
        var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));
        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "URL访问检查失败: {Url}", url);
        return false;
    }
}
```

### 2. 图片文件处理（URL 方式）

```csharp
/// <summary>
/// 处理图片文件
/// </summary>
private async Task<string?> ProcessImageFileAsync(AttachFile attachFile)
{
    try
    {
        // 构建文件URL
        var fileServerBaseUrl = _configuration["FileServer:BaseUrl"] 
            ?? throw new InvalidOperationException("配置项 FileServer:BaseUrl 不能为空");
        var imageUrl = $"{fileServerBaseUrl.TrimEnd('/')}/host/attachment/{attachFile.FilePath.Replace('\\', '/')}";

        // 验证URL是否可访问
        if (!await IsUrlAccessibleAsync(imageUrl))
        {
            throw new FileNotFoundException($"图片文件URL不可访问: {imageUrl}");
        }

        // 使用阿里云OCR处理图片
        var extractedText = await ProcessImageWithAliyunOcrAsync(imageUrl);
        
        _logger.LogInformation("图片文件 {FileName} 处理完成，提取文本长度: {TextLength}", 
            attachFile.FileName, extractedText?.Length ?? 0);

        return extractedText;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "图片文件 {FileName} 处理失败", attachFile.FileName);
        throw;
    }
}
```

### 3. PDF 文件处理（本地路径）

```csharp
/// <summary>
/// 处理PDF文件
/// </summary>
private async Task<string?> ProcessPdfFileAsync(AttachFile attachFile)
{
    try
    {
        // 使用本地文件路径
        var fileServerBasePath = _configuration[AppGlobalProperties.FileServerBasePath]
            ?? throw new InvalidOperationException($"配置项 {AppGlobalProperties.FileServerBasePath} 不能为空");
        var fullFilePath = Path.Combine(fileServerBasePath, "host", "attachment", attachFile.FilePath);

        if (!File.Exists(fullFilePath))
        {
            throw new FileNotFoundException($"PDF文件不存在: {fullFilePath}");
        }

        // 创建临时目录
        var tempDir = Path.Combine(Path.GetTempPath(), "pdf_ocr", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        List<string> imagePaths = [];

        try
        {
            // 将PDF转换为图片
            imagePaths = await _pdfConverter.ConvertPdfToImagesAsync(fullFilePath, tempDir, "jpg", 300);
            
            if (imagePaths.Count == 0)
            {
                _logger.LogWarning("PDF文件 {FileName} 转换图片失败", attachFile.FileName);
                return null;
            }

            // 对每个图片进行OCR处理
            var allTexts = new List<string>();
            foreach (var imagePath in imagePaths)
            {
                var imageText = await ProcessImageWithAliyunOcrAsync(imagePath);
                if (!string.IsNullOrWhiteSpace(imageText))
                {
                    allTexts.Add(imageText);
                }
            }

            // 合并所有页面的文本
            var combinedText = string.Join("\n\n--- 页面分隔 ---\n\n", allTexts);
            
            _logger.LogInformation("PDF文件 {FileName} 处理完成，共 {PageCount} 页，提取文本长度: {TextLength}", 
                attachFile.FileName, imagePaths.Count, combinedText.Length);

            return combinedText;
        }
        finally
        {
            // 清理临时文件
            if (imagePaths.Count != 0)
            {
                await _pdfConverter.CleanupTempImagesAsync(imagePaths);
            }
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "清理临时目录失败: {TempDir}", tempDir);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "PDF文件 {FileName} 处理失败", attachFile.FileName);
        throw;
    }
}
```

## 性能优化

### 1. 并发控制

-   使用信号量限制并发数量（默认 3 个）
-   避免过度占用系统资源
-   可配置并发数量

### 2. URL 访问优化

-   使用 HEAD 请求只获取响应头，不下载文件内容
-   设置合理的超时时间（10 秒）
-   详细的错误日志记录

### 3. 临时文件管理

-   自动创建和清理临时目录
-   使用 GUID 避免文件名冲突
-   异常情况下的资源清理

### 4. 错误处理

-   详细的错误日志记录
-   优雅的异常处理
-   状态跟踪和恢复

### 5. 缓存策略

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
-   URL 访问成功率监控

### 3. 错误监控

-   OCR 失败原因分析
-   文件格式支持统计
-   网络连接问题监控
-   URL 访问失败统计

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

#### URL 访问验证失败

-   检查 FileServer:BaseUrl 配置
-   确认文件服务器是否正常运行
-   验证网络连接和防火墙设置
-   检查文件路径是否正确

#### 内存不足

-   减少并发数量
-   降低图片 DPI
-   增加系统内存

### 2. 调试方法

#### 启用详细日志

```csharp
// 在代码中启用详细日志
_logger.LogDebug("处理文件: {FileName}, 类型: {FileType}", file.FileName, file.FileType);
_logger.LogDebug("构建URL: {Url}", imageUrl);
```

#### 检查 URL 访问

```csharp
// 测试URL访问
var isAccessible = await IsUrlAccessibleAsync(testUrl);
_logger.LogInformation("URL访问测试: {Url} -> {Result}", testUrl, isAccessible);
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

### 5. 高级 URL 验证

-   支持多种认证方式
-   自定义请求头
-   重试机制

## 部署指南

### 1. 生产环境配置

```json
{
    "FileServer": {
        "BaseUrl": "https://your-domain.com",
        "BasePath": "/app/files"
    },
    "AppGlobalProperties": {
        "FileServerBasePath": "/app/files"
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
        },
        "UrlValidation": {
            "TimeoutSeconds": 15
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
-   文件服务器可访问

### 4. 网络配置

-   确保应用服务器可以访问文件服务器
-   配置适当的防火墙规则
-   设置合理的超时时间

## 总结

本增强 OCR 解决方案提供了：

1. **完整的 PDF 处理流程** - 从 PDF 到图片到文本
2. **真实的 OCR 服务** - 基于阿里云 OCR 的高质量识别
3. **URL 访问验证** - 通过 HTTP 请求验证文件可访问性
4. **高效的批量处理** - 并发控制和资源管理
5. **完善的错误处理** - 详细的日志和异常处理
6. **灵活的配置** - 支持多种配置选项
7. **丰富的 API 接口** - 完整的 RESTful API

通过这个解决方案，您可以轻松实现企业级的文档 OCR 处理和全文搜索功能，提高文档管理效率和用户体验。URL 访问验证确保了在分布式架构中文件访问的可靠性。
