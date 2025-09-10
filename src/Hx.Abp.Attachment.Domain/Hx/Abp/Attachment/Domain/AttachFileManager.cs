using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件文件管理领域服务实现
    /// 负责文件OCR处理、文本块管理等核心业务逻辑
    /// </summary>
    public class AttachFileManager(
        IRepository<AttachFile, Guid> fileRepository,
        IRepository<OcrTextBlock, Guid> textBlockRepository,
        ILogger<AttachFileManager> logger) : DomainService, IAttachFileManager
    {
        private readonly IRepository<AttachFile, Guid> _fileRepository = fileRepository;
        private readonly IRepository<OcrTextBlock, Guid> _textBlockRepository = textBlockRepository;
        private readonly ILogger<AttachFileManager> _logger = logger;

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
        /// 检查文件是否支持OCR处理
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否支持OCR</returns>
        public bool IsSupportedFileType(string fileType)
        {
            if (string.IsNullOrWhiteSpace(fileType))
                return false;

            var lowerFileType = fileType.ToLowerInvariant();
            return SupportedImageTypes.Contains(lowerFileType) || SupportedPdfTypes.Contains(lowerFileType);
        }

        /// <summary>
        /// 检查文件是否已完成OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <returns>是否已完成OCR处理</returns>
        public bool IsOcrProcessed(AttachFile attachFile)
        {
            Check.NotNull(attachFile, nameof(attachFile));
            
            return !string.IsNullOrWhiteSpace(attachFile.OcrContent) && 
                   attachFile.OcrProcessStatus == OcrProcessStatus.Completed;
        }

        /// <summary>
        /// 开始OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        public void StartOcrProcessing(AttachFile attachFile)
        {
            Check.NotNull(attachFile, nameof(attachFile));
            
            if (!IsSupportedFileType(attachFile.FileType))
            {
                throw new BusinessException("ATTACH_FILE_TYPE_NOT_SUPPORTED")
                    .WithData("fileType", attachFile.FileType)
                    .WithData("fileName", attachFile.FileName);
            }

            attachFile.SetOcrProcessStatus(OcrProcessStatus.Processing);
            
            _logger.LogInformation("开始OCR处理文件: {FileName} (ID: {FileId})", 
                attachFile.FileName, attachFile.Id);
        }

        /// <summary>
        /// 完成OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="extractedText">提取的文本内容</param>
        /// <param name="textBlocks">文本块集合</param>
        public void CompleteOcrProcessing(AttachFile attachFile, string extractedText, List<OcrTextBlock>? textBlocks = null)
        {
            Check.NotNull(attachFile, nameof(attachFile));
            Check.NotNullOrWhiteSpace(extractedText, nameof(extractedText));

            // 设置OCR内容
            attachFile.SetOcrContent(extractedText);

            // 处理文本块
            if (textBlocks != null && textBlocks.Count > 0)
            {
                // 清除旧的文本块
                attachFile.ClearOcrTextBlocks();
                
                // 添加新的文本块
                attachFile.AddOcrTextBlocks(textBlocks);
                
                _logger.LogInformation("文件 {FileName} OCR处理成功，提取文本长度: {TextLength}，文本块数量: {BlockCount}", 
                    attachFile.FileName, extractedText.Length, textBlocks.Count);
            }
            else
            {
                _logger.LogInformation("文件 {FileName} OCR处理成功，提取文本长度: {TextLength}", 
                    attachFile.FileName, extractedText.Length);
            }
        }

        /// <summary>
        /// 标记OCR处理失败
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="reason">失败原因</param>
        public void MarkOcrProcessingFailed(AttachFile attachFile, string? reason = null)
        {
            Check.NotNull(attachFile, nameof(attachFile));

            attachFile.SetOcrProcessStatus(OcrProcessStatus.Failed);
            
            _logger.LogWarning("文件 {FileName} OCR处理失败: {Reason}", 
                attachFile.FileName, reason ?? "未知原因");
        }

        /// <summary>
        /// 跳过OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="reason">跳过原因</param>
        public void SkipOcrProcessing(AttachFile attachFile, string? reason = null)
        {
            Check.NotNull(attachFile, nameof(attachFile));

            attachFile.SetOcrProcessStatus(OcrProcessStatus.Skipped);
            
            _logger.LogInformation("跳过文件 {FileName} OCR处理: {Reason}", 
                attachFile.FileName, reason ?? "不支持的文件类型");
        }

        /// <summary>
        /// 清除文件的OCR内容
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        public void ClearOcrContent(AttachFile attachFile)
        {
            Check.NotNull(attachFile, nameof(attachFile));

            attachFile.ClearOcrContent();
            
            _logger.LogInformation("清除文件 {FileName} OCR内容", attachFile.FileName);
        }

        /// <summary>
        /// 重新处理OCR
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        public void ResetOcrProcessing(AttachFile attachFile)
        {
            Check.NotNull(attachFile, nameof(attachFile));

            // 清除现有OCR内容
            ClearOcrContent(attachFile);
            
            // 重新开始处理
            StartOcrProcessing(attachFile);
            
            _logger.LogInformation("重置文件 {FileName} OCR处理状态", attachFile.FileName);
        }

        /// <summary>
        /// 更新分类的全文内容
        /// </summary>
        /// <param name="catalogue">附件分类</param>
        public void UpdateCatalogueFullTextContent(AttachCatalogue catalogue)
        {
            Check.NotNull(catalogue, nameof(catalogue));

            catalogue.RegenerateFullTextContent();
            
            _logger.LogInformation("更新分类 {CatalogueName} 全文内容", catalogue.CatalogueName);
        }

        /// <summary>
        /// 批量保存OCR文本块到数据库
        /// </summary>
        /// <param name="textBlocks">文本块集合</param>
        /// <returns>保存任务</returns>
        public async Task SaveOcrTextBlocksAsync(IEnumerable<OcrTextBlock> textBlocks)
        {
            if (textBlocks == null)
                return;

            var textBlockList = textBlocks.ToList();
            if (textBlockList.Count == 0)
                return;

            foreach (var textBlock in textBlockList)
            {
                await _textBlockRepository.InsertAsync(textBlock);
            }

            _logger.LogInformation("批量保存 {Count} 个OCR文本块", textBlockList.Count);
        }

        /// <summary>
        /// 获取支持的文件类型列表
        /// </summary>
        /// <returns>支持的文件类型集合</returns>
        public static HashSet<string> GetSupportedFileTypes()
        {
            return new HashSet<string>(SupportedImageTypes.Concat(SupportedPdfTypes), StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取支持的图片类型列表
        /// </summary>
        /// <returns>支持的图片类型集合</returns>
        public static HashSet<string> GetSupportedImageTypes()
        {
            return new HashSet<string>(SupportedImageTypes, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取支持的PDF类型列表
        /// </summary>
        /// <returns>支持的PDF类型集合</returns>
        public static HashSet<string> GetSupportedPdfTypes()
        {
            return new HashSet<string>(SupportedPdfTypes, StringComparer.OrdinalIgnoreCase);
        }
    }
}
