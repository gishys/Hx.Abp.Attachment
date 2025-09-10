using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Application.Utils;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OcrTextComposer;
using System.Text.Json;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// OCR服务实现
    /// </summary>
    public class OcrService(
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        IRepository<AttachFile, Guid> fileRepository,
        IOcrTextBlockRepository textBlockRepository,
        ILogger<OcrService> logger,
        IConfiguration configuration,
        CrossPlatformPdfToImageConverter pdfConverter) : DomainService, IOcrService
    {
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository = catalogueRepository;
        private readonly IRepository<AttachFile, Guid> _fileRepository = fileRepository;
        private readonly IOcrTextBlockRepository _textBlockRepository = textBlockRepository;
        private readonly ILogger<OcrService> _logger = logger;
        private readonly IConfiguration _configuration = configuration;
        private readonly CrossPlatformPdfToImageConverter _pdfConverter = pdfConverter;

        // 支持OCR的文件类型
        private static readonly HashSet<string> SupportedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif"
        };

        private static readonly HashSet<string> SupportedPdfTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf"
        };

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

                // 检查文件是否已经有提取的文本，如果有则直接返回
                if (!string.IsNullOrWhiteSpace(attachFile.OcrContent) && 
                    attachFile.OcrProcessStatus == OcrProcessStatus.Completed)
                {
                    result.IsSuccess = true;
                    result.ExtractedText = attachFile.OcrContent;
                    result.ProcessingTime = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("文件 {FileName} 已有OCR提取文本，直接返回，文本长度: {TextLength}", 
                        attachFile.FileName, attachFile.OcrContent.Length);
                    
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
                List<OcrTextBlock> textBlocks = [];
                
                if (SupportedPdfTypes.Contains(attachFile.FileType.ToLowerInvariant()))
                {
                    // 处理PDF文件
                    var (ExtractedText, TextBlocks) = await ProcessPdfFileWithTextBlocksAsync(attachFile);
                    extractedText = ExtractedText;
                    textBlocks = TextBlocks;
                }
                else
                {
                    // 处理图片文件
                    var (ExtractedText, TextBlocks) = await ProcessImageFileWithTextBlocksAsync(attachFile);
                    extractedText = ExtractedText;
                    textBlocks = TextBlocks;
                }

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    attachFile.SetOcrContent(extractedText);
                    
                    // 存储文本块
                    if (textBlocks.Count > 0)
                    {
                        // 清除旧的文本块
                        attachFile.ClearOcrTextBlocks();
                        
                        // 添加新的文本块
                        attachFile.AddOcrTextBlocks(textBlocks);
                        
                        // 保存文本块到数据库
                        foreach (var textBlock in textBlocks)
                        {
                            await _textBlockRepository.InsertAsync(textBlock);
                        }
                        
                        _logger.LogInformation("文件 {FileName} OCR处理成功，提取文本长度: {TextLength}，文本块数量: {BlockCount}", 
                            attachFile.FileName, extractedText.Length, textBlocks.Count);
                    }
                    
                    result.IsSuccess = true;
                    result.ExtractedText = extractedText;
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
        /// 处理PDF文件并返回文本块
        /// </summary>
        private async Task<(string? ExtractedText, List<OcrTextBlock> TextBlocks)> ProcessPdfFileWithTextBlocksAsync(AttachFile attachFile)
        {
            try
            {
                // 在使用Path.Combine前，确保fileServerBasePath不为null，否则抛出异常
                var fileServerBasePath = _configuration[AppGlobalProperties.FileServerBasePath]
                    ?? throw new InvalidOperationException($"配置项 {AppGlobalProperties.FileServerBasePath} 不能为空");
                var fullFilePath = Path.Combine(fileServerBasePath, "host", "attachment", attachFile.FilePath);

                if (!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException($"PDF文件不存在: {fullFilePath}");
                }

                // 创建临时目录
                var tempDir = Path.Combine(Path.GetTempPath(), "pdf_ocr", GuidGenerator.Create().ToString());
                Directory.CreateDirectory(tempDir);
                List<string> imagePaths = [];

                try
                {
                    // 将PDF转换为图片
                    imagePaths = await _pdfConverter.ConvertPdfToImagesAsync(fullFilePath, tempDir, "jpg", 300);
                    
                    if (imagePaths.Count == 0)
                    {
                        _logger.LogWarning("PDF文件 {FileName} 转换图片失败", attachFile.FileName);
                        // 将
                        // return null;
                        // 替换为
                        return (null, new List<OcrTextBlock>());
                    }

                    // 对每个图片进行OCR处理
                    var allTexts = new List<string>();
                    var allTextBlocks = new List<OcrTextBlock>();
                    var blockOrder = 0;
                    
                    for (int pageIndex = 0; pageIndex < imagePaths.Count; pageIndex++)
                    {
                        var imagePath = imagePaths[pageIndex];
                        var imageUrl = ConvertLocalPathToUrl(imagePath);
                        
                        // 获取详细的OCR结果
                        var ocrResult = await ProcessImageWithAliyunOcrDetailedAsync(imageUrl);
                        if (ocrResult?.Results?.Count > 0)
                        {
                            var imageText = OcrComposer.Compose(ocrResult);
                            if (!string.IsNullOrWhiteSpace(imageText))
                            {
                                allTexts.Add(imageText);
                            }
                            
                            // 处理文本块
                            foreach (var result in ocrResult.Results)
                            {
                                if (!string.IsNullOrWhiteSpace(result.Text) && result.TextRectangles != null)
                                {
                                    var textBlock = new OcrTextBlock(
                                        GuidGenerator.Create(),
                                        attachFile.Id,
                                        result.Text,
                                        result.Probability ?? 0.0f,
                                        pageIndex,
                                        JsonSerializer.Serialize(new TextPosition
                                        {
                                            Angle = result.TextRectangles.Angle,
                                            Height = result.TextRectangles.Height,
                                            Left = result.TextRectangles.Left,
                                            Top = result.TextRectangles.Top,
                                            Width = result.TextRectangles.Width
                                        }),
                                        blockOrder++
                                    );
                                    allTextBlocks.Add(textBlock);
                                }
                            }
                        }
                    }

                    // 合并所有页面的文本
                    var combinedText = string.Join("\n\n--- 页面分隔 ---\n\n", allTexts);
                    
                    _logger.LogInformation("PDF文件 {FileName} 处理完成，共 {PageCount} 页，提取文本长度: {TextLength}，文本块数量: {BlockCount}", 
                        attachFile.FileName, imagePaths.Count, combinedText.Length, allTextBlocks.Count);

                    return (combinedText, allTextBlocks);
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

        /// <summary>
        /// 处理图片文件并返回文本块
        /// </summary>
        private async Task<(string? ExtractedText, List<OcrTextBlock> TextBlocks)> ProcessImageFileWithTextBlocksAsync(AttachFile attachFile)
        {
            try
            {
                // 构建文件URL
                var fileServerBaseUrl = _configuration[AppGlobalProperties.FileServerBasePath] 
                    ?? throw new InvalidOperationException("配置项 FileServer:BaseUrl 不能为空");
                var imageUrl = $"{fileServerBaseUrl.TrimEnd('/')}/host/attachment/{attachFile.FilePath.Replace('\\', '/')}";

                // 验证URL是否可访问
                if (!await IsUrlAccessibleAsync(imageUrl))
                {
                    throw new FileNotFoundException($"图片文件URL不可访问: {imageUrl}");
                }

                // 使用阿里云OCR处理图片
                var ocrResult = await ProcessImageWithAliyunOcrDetailedAsync(imageUrl);
                var extractedText = string.Empty;
                var textBlocks = new List<OcrTextBlock>();
                
                if (ocrResult?.Results?.Count > 0)
                {
                    extractedText = OcrComposer.Compose(ocrResult);
                    
                    // 处理文本块
                    var blockOrder = 0;
                    foreach (var result in ocrResult.Results)
                    {
                        if (!string.IsNullOrWhiteSpace(result.Text) && result.TextRectangles != null)
                        {
                            var textBlock = new OcrTextBlock(
                                GuidGenerator.Create(),
                                attachFile.Id,
                                result.Text,
                                result.Probability ?? 0.0f,
                                0, // 单页图片
                                JsonSerializer.Serialize(new TextPosition
                                {
                                    Angle = result.TextRectangles.Angle,
                                    Height = result.TextRectangles.Height,
                                    Left = result.TextRectangles.Left,
                                    Top = result.TextRectangles.Top,
                                    Width = result.TextRectangles.Width
                                }),
                                blockOrder++
                            );
                            textBlocks.Add(textBlock);
                        }
                    }
                }
                
                _logger.LogInformation("图片文件 {FileName} 处理完成，提取文本长度: {TextLength}，文本块数量: {BlockCount}", 
                    attachFile.FileName, extractedText.Length, textBlocks.Count);

                return (extractedText, textBlocks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图片文件 {FileName} 处理失败", attachFile.FileName);
                throw;
            }
        }

        /// <summary>
        /// 使用阿里云OCR处理图片并返回详细结果
        /// </summary>
        private async Task<RecognizeCharacterDto?> ProcessImageWithAliyunOcrDetailedAsync(string imageUrl)
        {
            try
            {
                // 获取阿里云OCR配置
                var accessKeyId = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID")
                    ?? throw new InvalidOperationException("缺少环境变量 ALIBABA_CLOUD_ACCESS_KEY_ID");
                var accessKeySecret = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET")
                    ?? throw new InvalidOperationException("缺少环境变量 ALIBABA_CLOUD_ACCESS_KEY_SECRET");

                // 调用阿里云OCR
                var ocrResult = await UniversalTextRecognitionHelper.JpgUniversalTextRecognition(
                    accessKeyId, accessKeySecret, imageUrl);

                return ocrResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云OCR处理失败: {ImageUrl}", imageUrl);
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
                var relativePath = localPath[fileServerBasePath.Length..].Replace('\\', '/');
                return $"{fileServerBaseUrl.TrimEnd('/')}{relativePath}";
            }

            // 如果无法转换，返回原始路径（假设已经是URL）
            return localPath;
        }

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

                var files = catalogue.AttachFiles?.ToList() ?? [];
                result.ProcessedFilesCount = files.Count;

                if (files.Count == 0)
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

                if (successfulTexts.Count != 0)
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
            var attachFile = await _fileRepository.GetAsync(fileId) ?? throw new InvalidOperationException($"文件不存在: {fileId}");
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
            var attachFile = await _fileRepository.GetAsync(fileId) ?? throw new InvalidOperationException($"文件不存在: {fileId}");
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
            var catalogue = await _catalogueRepository.GetAsync(catalogueId) ?? throw new InvalidOperationException($"目录不存在: {catalogueId}");
            return new CatalogueFullTextDto
            {
                CatalogueId = catalogue.Id,
                CatalogueName = catalogue.CatalogueName,
                FullTextContent = catalogue.FullTextContent,
                FullTextContentUpdatedTime = catalogue.FullTextContentUpdatedTime,
                AttachCount = catalogue.AttachCount
            };
        }

        /// <summary>
        /// 获取文件的OCR内容（包含文本块信息）
        /// </summary>
        public async Task<FileOcrContentWithBlocksDto> GetFileOcrContentWithBlocksAsync(Guid fileId)
        {
            var attachFile = await _fileRepository.GetAsync(fileId) ?? throw new InvalidOperationException($"文件不存在: {fileId}");
            
            // 获取文本块
            var textBlocks = await GetFileTextBlocksAsync(fileId);
            
            return new FileOcrContentWithBlocksDto
            {
                FileId = attachFile.Id,
                FileName = attachFile.FileName,
                OcrContent = attachFile.OcrContent,
                OcrProcessStatus = attachFile.OcrProcessStatus,
                OcrProcessedTime = attachFile.OcrProcessedTime,
                TextBlocks = textBlocks
            };
        }

        /// <summary>
        /// 获取目录的OCR内容（包含文本块信息）
        /// </summary>
        public async Task<CatalogueOcrContentWithBlocksDto> GetCatalogueOcrContentWithBlocksAsync(Guid catalogueId)
        {
            var catalogue = await _catalogueRepository.GetAsync(catalogueId) ?? throw new InvalidOperationException($"目录不存在: {catalogueId}");
            
            var fileOcrContents = new List<FileOcrContentWithBlocksDto>();
            
            if (catalogue.AttachFiles != null)
            {
                foreach (var file in catalogue.AttachFiles)
                {
                    var fileOcrContent = await GetFileOcrContentWithBlocksAsync(file.Id);
                    fileOcrContents.Add(fileOcrContent);
                }
            }
            
            return new CatalogueOcrContentWithBlocksDto
            {
                CatalogueId = catalogue.Id,
                CatalogueName = catalogue.CatalogueName,
                FullTextContent = catalogue.FullTextContent,
                FullTextContentUpdatedTime = catalogue.FullTextContentUpdatedTime,
                AttachCount = catalogue.AttachCount,
                FileOcrContents = fileOcrContents
            };
        }

        /// <summary>
        /// 获取文件的文本块列表
        /// </summary>
        public async Task<List<OcrTextBlockDto>> GetFileTextBlocksAsync(Guid fileId)
        {
            // 通过仓储获取 IQueryable
            var queryable = await _textBlockRepository.GetQueryableAsync();
            var textBlocks = queryable
                .Where(tb => tb.AttachFileId == fileId)
                .OrderBy(tb => tb.PageIndex)
                .ThenBy(tb => tb.BlockOrder)
                .ToList();

            return [.. textBlocks.Select(MapToOcrTextBlockDto)];
        }

        /// <summary>
        /// 获取文本块详情
        /// </summary>
        public async Task<OcrTextBlockDto?> GetTextBlockAsync(Guid textBlockId)
        {
            var textBlock = await _textBlockRepository.GetAsync(textBlockId);
            return textBlock != null ? MapToOcrTextBlockDto(textBlock) : null;
        }

        /// <summary>
        /// 映射到OcrTextBlockDto
        /// </summary>
        private static OcrTextBlockDto MapToOcrTextBlockDto(OcrTextBlock textBlock)
        {
            return new OcrTextBlockDto
            {
                Id = textBlock.Id,
                AttachFileId = textBlock.AttachFileId,
                Text = textBlock.Text,
                Probability = textBlock.Probability,
                PageIndex = textBlock.PageIndex,
                Position = MapToTextPositionDto(textBlock.GetPosition()),
                BlockOrder = textBlock.BlockOrder,
                CreationTime = textBlock.CreationTime
            };
        }

        /// <summary>
        /// 映射到TextPositionDto
        /// </summary>
        private static TextPositionDto? MapToTextPositionDto(TextPosition? position)
        {
            if (position == null) return null;

            return new TextPositionDto
            {
                Angle = position.Angle,
                Height = position.Height,
                Left = position.Left,
                Top = position.Top,
                Width = position.Width
            };
        }

        /// <summary>
        /// 获取OCR统计信息
        /// </summary>
        public async Task<OcrStatisticsDto> GetOcrStatisticsAsync()
        {
            try
            {
                var statistics = await _textBlockRepository.GetStatisticsAsync();
                
                return new OcrStatisticsDto
                {
                    TotalTextBlocks = statistics.TotalTextBlocks,
                    TotalFilesWithOcr = statistics.TotalFilesWithOcr,
                    AverageProbability = statistics.AverageProbability,
                    TotalTextLength = statistics.TotalTextLength,
                    StatisticsTime = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取OCR统计信息失败");
                throw;
            }
        }

        /// <summary>
        /// 清理孤立的文本块
        /// </summary>
        public async Task<CleanupResultDto> CleanupOrphanedTextBlocksAsync()
        {
            var result = new CleanupResultDto();
            
            try
            {
                var deletedCount = await _textBlockRepository.CleanupOrphanedBlocksAsync();
                
                result.IsSuccess = true;
                result.DeletedCount = deletedCount;
                result.CleanupTime = DateTime.UtcNow;
                
                _logger.LogInformation("清理孤立文本块完成，删除了 {DeletedCount} 条记录", deletedCount);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "清理孤立文本块失败");
            }
            
            return result;
        }
    }
}
