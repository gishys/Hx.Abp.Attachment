using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Application.Utils;
using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using OcrTextComposer;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// OCR服务实现
    /// </summary>
    public class OcrService : DomainService, IOcrService
    {
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
        private readonly IRepository<AttachFile, Guid> _fileRepository;
        private readonly ILogger<OcrService> _logger;
        private readonly IConfiguration _configuration;
        private readonly CrossPlatformPdfToImageConverter _pdfConverter;

        // 支持OCR的文件类型
        private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif"
        };

        private static readonly HashSet<string> SupportedPdfTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf"
        };

        public OcrService(
            IRepository<AttachCatalogue, Guid> catalogueRepository,
            IRepository<AttachFile, Guid> fileRepository,
            ILogger<OcrService> logger,
            IConfiguration configuration,
            CrossPlatformPdfToImageConverter pdfConverter)
        {
            _catalogueRepository = catalogueRepository;
            _fileRepository = fileRepository;
            _logger = logger;
            _configuration = configuration;
            _pdfConverter = pdfConverter;
        }

        /// <summary>
        /// 检查文件是否支持OCR
        /// </summary>
        public bool IsSupportedFileType(string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                return false;

            var lowerFileType = fileType.ToLowerInvariant();
            return SupportedImageTypes.Contains(lowerFileType) || SupportedPdfTypes.Contains(lowerFileType);
        }

        /// <summary>
        /// 处理单个文件的OCR
        /// </summary>
        public async Task<OcrResult> ProcessFileAsync(Guid fileId)
        {
            var startTime = DateTime.UtcNow;
            var result = new OcrResult { FileId = fileId };

            try
            {
                var attachFile = await _fileRepository.GetAsync(fileId);
                if (attachFile == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"文件不存在: {fileId}";
                    return result;
                }

                if (!IsSupportedFileType(attachFile.FileType))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"不支持的文件类型: {attachFile.FileType}";
                    attachFile.SetOcrProcessStatus(OcrProcessStatus.Skipped);
                    await _fileRepository.UpdateAsync(attachFile);
                    return result;
                }

                // 设置处理中状态
                attachFile.SetOcrProcessStatus(OcrProcessStatus.Processing);
                await _fileRepository.UpdateAsync(attachFile);

                string? extractedText;
                if (SupportedPdfTypes.Contains(attachFile.FileType.ToLowerInvariant()))
                {
                    // 处理PDF文件
                    extractedText = await ProcessPdfFileAsync(attachFile);
                }
                else
                {
                    // 处理图片文件
                    extractedText = await ProcessImageFileAsync(attachFile);
                }

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    attachFile.SetOcrContent(extractedText);
                    result.IsSuccess = true;
                    result.ExtractedText = extractedText;
                    _logger.LogInformation("文件 {FileName} OCR处理成功，提取文本长度: {TextLength}", 
                        attachFile.FileName, extractedText.Length);
                }
                else
                {
                    attachFile.SetOcrProcessStatus(OcrProcessStatus.Failed);
                    result.IsSuccess = false;
                    result.ErrorMessage = "OCR处理未提取到文本内容";
                }

                await _fileRepository.UpdateAsync(attachFile);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "文件 {FileId} OCR处理失败", fileId);
            }

            result.ProcessingTime = DateTime.UtcNow - startTime;
            return result;
        }

        /// <summary>
        /// 处理PDF文件
        /// </summary>
        private async Task<string?> ProcessPdfFileAsync(AttachFile attachFile)
        {
            try
            {
                // 获取文件完整路径
                var fileServerBasePath = _configuration[AppGlobalProperties.FileServerBasePath];
                var fullFilePath = Path.Combine(fileServerBasePath, "host", "attachment", attachFile.FilePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException($"PDF文件不存在: {fullFilePath}");
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), "pdf_ocr", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);
                List<string> imagePaths = new();

                try
                {
                    // 将PDF转换为图片
                    imagePaths = await _pdfConverter.ConvertPdfToImagesAsync(fullFilePath, tempDir, "jpg", 300);
                    
                    if (!imagePaths.Any())
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
                    if (imagePaths.Any())
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

        /// <summary>
        /// 处理图片文件
        /// </summary>
        private async Task<string?> ProcessImageFileAsync(AttachFile attachFile)
        {
            try
            {
                // 获取文件完整路径
                var fileServerBasePath = _configuration[AppGlobalProperties.FileServerBasePath];
                var fullFilePath = Path.Combine(fileServerBasePath, "host", "attachment", attachFile.FilePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException($"图片文件不存在: {fullFilePath}");
                }

                // 使用阿里云OCR处理图片
                var extractedText = await ProcessImageWithAliyunOcrAsync(fullFilePath);
                
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

        /// <summary>
        /// 使用阿里云OCR处理图片
        /// </summary>
        private async Task<string?> ProcessImageWithAliyunOcrAsync(string imagePath)
        {
            try
            {
                // 获取阿里云OCR配置
                var accessKeyId = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID") 
                    ?? throw new InvalidOperationException("缺少环境变量 ALIBABA_CLOUD_ACCESS_KEY_ID");
                var accessKeySecret = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET") 
                    ?? throw new InvalidOperationException("缺少环境变量 ALIBABA_CLOUD_ACCESS_KEY_SECRET");

                // 将本地文件路径转换为URL（这里假设有文件服务器）
                var imageUrl = ConvertLocalPathToUrl(imagePath);

                // 调用阿里云OCR
                var ocrResult = await UniversalTextRecognitionHelper.JpgUniversalTextRecognition(
                    accessKeyId, accessKeySecret, imageUrl);

                if (ocrResult?.Results?.Any() == true)
                {
                    // 使用OcrComposer组合文本
                    var composedText = OcrComposer.Compose(ocrResult);
                    return composedText;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云OCR处理失败: {ImagePath}", imagePath);
                throw;
            }
        }

        /// <summary>
        /// 将本地文件路径转换为URL
        /// </summary>
        private string ConvertLocalPathToUrl(string localPath)
        {
            // 这里需要根据实际的文件服务器配置来实现
            // 假设文件服务器的基础URL配置在配置文件中
            var fileServerBaseUrl = _configuration["FileServer:BaseUrl"] ?? "http://localhost:5000";
            
            // 提取相对路径
            var fileServerBasePath = _configuration[AppGlobalProperties.FileServerBasePath];
            if (!string.IsNullOrEmpty(fileServerBasePath) && localPath.StartsWith(fileServerBasePath))
            {
                var relativePath = localPath.Substring(fileServerBasePath.Length).Replace('\\', '/');
                return $"{fileServerBaseUrl.TrimEnd('/')}{relativePath}";
            }

            // 如果无法转换，返回原始路径（假设已经是URL）
            return localPath;
        }

        /// <summary>
        /// 批量处理文件的OCR
        /// </summary>
        public async Task<List<OcrResult>> ProcessFilesAsync(List<Guid> fileIds)
        {
            var results = new List<OcrResult>();
            
            // 使用信号量限制并发数量，避免过度占用资源
            var semaphore = new SemaphoreSlim(3, 3); // 最多3个并发
            var tasks = fileIds.Select(async fileId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ProcessFileAsync(fileId);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var processedResults = await Task.WhenAll(tasks);
            results.AddRange(processedResults);

            return results;
        }

        /// <summary>
        /// 处理目录下所有文件的OCR并更新目录的全文内容
        /// </summary>
        public async Task<CatalogueOcrResult> ProcessCatalogueAsync(Guid catalogueId)
        {
            var startTime = DateTime.UtcNow;
            var result = new CatalogueOcrResult { CatalogueId = catalogueId };

            try
            {
                // 获取目录及其文件
                var catalogue = await _catalogueRepository.GetAsync(catalogueId);
                if (catalogue == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"目录不存在: {catalogueId}";
                    return result;
                }

                var files = catalogue.AttachFiles?.ToList() ?? new List<AttachFile>();
                result.ProcessedFilesCount = files.Count;

                if (!files.Any())
                {
                    result.IsSuccess = true;
                    result.CombinedFullText = null;
                    catalogue.SetFullTextContent(null);
                    await _catalogueRepository.UpdateAsync(catalogue);
                    return result;
                }

                // 处理所有文件的OCR
                var fileIds = files.Select(f => f.Id).ToList();
                var ocrResults = await ProcessFilesAsync(fileIds);

                // 统计结果
                result.SuccessFilesCount = ocrResults.Count(r => r.IsSuccess);
                result.FailedFilesCount = ocrResults.Count(r => !r.IsSuccess && !r.ErrorMessage?.Contains("不支持") == true);
                result.SkippedFilesCount = ocrResults.Count(r => r.ErrorMessage?.Contains("不支持") == true);

                // 合并成功的OCR文本
                var successfulTexts = ocrResults
                    .Where(r => r.IsSuccess && !string.IsNullOrWhiteSpace(r.ExtractedText))
                    .Select(r => r.ExtractedText)
                    .ToList();

                if (successfulTexts.Any())
                {
                    result.CombinedFullText = string.Join("\n\n--- 文件分隔 ---\n\n", successfulTexts);
                    catalogue.SetFullTextContent(result.CombinedFullText);
                    result.IsSuccess = true;
                    
                    _logger.LogInformation("目录 {CatalogueName} OCR处理完成，成功处理 {SuccessCount}/{TotalCount} 个文件", 
                        catalogue.CatalogueName, result.SuccessFilesCount, result.ProcessedFilesCount);
                }
                else
                {
                    catalogue.SetFullTextContent(null);
                    result.IsSuccess = false;
                    result.ErrorMessage = "没有成功提取到任何文本内容";
                }

                await _catalogueRepository.UpdateAsync(catalogue);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "目录 {CatalogueId} OCR处理失败", catalogueId);
            }

            result.ProcessingTime = DateTime.UtcNow - startTime;
            return result;
        }

        /// <summary>
        /// 获取文件OCR状态
        /// </summary>
        public async Task<FileOcrStatusDto> GetFileOcrStatusAsync(Guid fileId)
        {
            var attachFile = await _fileRepository.GetAsync(fileId);
            if (attachFile == null)
            {
                throw new InvalidOperationException($"文件不存在: {fileId}");
            }

            return new FileOcrStatusDto
            {
                FileId = attachFile.Id,
                FileName = attachFile.FileName,
                FileType = attachFile.FileType,
                IsSupported = IsSupportedFileType(attachFile.FileType),
                OcrProcessStatus = attachFile.OcrProcessStatus,
                OcrProcessedTime = attachFile.OcrProcessedTime
            };
        }

        /// <summary>
        /// 获取文件OCR内容
        /// </summary>
        public async Task<FileOcrContentDto> GetFileOcrContentAsync(Guid fileId)
        {
            var attachFile = await _fileRepository.GetAsync(fileId);
            if (attachFile == null)
            {
                throw new InvalidOperationException($"文件不存在: {fileId}");
            }

            return new FileOcrContentDto
            {
                FileId = attachFile.Id,
                FileName = attachFile.FileName,
                OcrContent = attachFile.OcrContent,
                OcrProcessStatus = attachFile.OcrProcessStatus,
                OcrProcessedTime = attachFile.OcrProcessedTime
            };
        }

        /// <summary>
        /// 获取目录全文内容
        /// </summary>
        public async Task<CatalogueFullTextDto> GetCatalogueFullTextAsync(Guid catalogueId)
        {
            var catalogue = await _catalogueRepository.GetAsync(catalogueId);
            if (catalogue == null)
            {
                throw new InvalidOperationException($"目录不存在: {catalogueId}");
            }

            return new CatalogueFullTextDto
            {
                CatalogueId = catalogue.Id,
                CatalogueName = catalogue.CatalogueName,
                FullTextContent = catalogue.FullTextContent,
                FullTextContentUpdatedTime = catalogue.FullTextContentUpdatedTime,
                AttachCount = catalogue.AttachCount
            };
        }
    }
}
