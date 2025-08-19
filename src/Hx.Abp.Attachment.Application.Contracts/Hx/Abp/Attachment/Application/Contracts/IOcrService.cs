namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// OCR服务接口
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// 处理单个文件的OCR
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>OCR处理结果</returns>
        Task<OcrResult> ProcessFileAsync(Guid fileId);

        /// <summary>
        /// 批量处理文件的OCR
        /// </summary>
        /// <param name="fileIds">文件ID列表</param>
        /// <returns>OCR处理结果列表</returns>
        Task<List<OcrResult>> ProcessFilesAsync(List<Guid> fileIds);

        /// <summary>
        /// 处理目录下所有文件的OCR并更新目录的全文内容
        /// </summary>
        /// <param name="catalogueId">目录ID</param>
        /// <returns>处理结果</returns>
        Task<CatalogueOcrResult> ProcessCatalogueAsync(Guid catalogueId);

        /// <summary>
        /// 检查文件是否支持OCR
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件OCR状态</returns>
        Task<FileOcrStatusDto> GetFileOcrStatusAsync(Guid fileId);

        /// <summary>
        /// 获取文件的OCR内容
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件OCR内容</returns>
        Task<FileOcrContentDto> GetFileOcrContentAsync(Guid fileId);

        /// <summary>
        /// 获取目录的全文内容
        /// </summary>
        /// <param name="catalogueId">目录ID</param>
        /// <returns>目录全文内容</returns>
        Task<CatalogueFullTextDto> GetCatalogueFullTextAsync(Guid catalogueId);

        /// <summary>
        /// 获取文件的OCR内容（包含文本块信息）
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文件OCR内容（包含文本块）</returns>
        Task<FileOcrContentWithBlocksDto> GetFileOcrContentWithBlocksAsync(Guid fileId);

        /// <summary>
        /// 获取目录的OCR内容（包含文本块信息）
        /// </summary>
        /// <param name="catalogueId">目录ID</param>
        /// <returns>目录OCR内容（包含文本块）</returns>
        Task<CatalogueOcrContentWithBlocksDto> GetCatalogueOcrContentWithBlocksAsync(Guid catalogueId);

        /// <summary>
        /// 获取文件的文本块列表
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <returns>文本块列表</returns>
        Task<List<OcrTextBlockDto>> GetFileTextBlocksAsync(Guid fileId);

        /// <summary>
        /// 获取文本块详情
        /// </summary>
        /// <param name="textBlockId">文本块ID</param>
        /// <returns>文本块详情</returns>
        Task<OcrTextBlockDto?> GetTextBlockAsync(Guid textBlockId);

        /// <summary>
        /// 检查文件类型是否支持OCR
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否支持</returns>
        bool IsSupportedFileType(string fileType);

        /// <summary>
        /// 获取OCR统计信息
        /// </summary>
        /// <returns>OCR统计信息</returns>
        Task<OcrStatisticsDto> GetOcrStatisticsAsync();

        /// <summary>
        /// 清理孤立的文本块
        /// </summary>
        /// <returns>清理结果</returns>
        Task<CleanupResultDto> CleanupOrphanedTextBlocksAsync();
    }

    /// <summary>
    /// OCR处理结果
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 提取的文本内容
        /// </summary>
        public string? ExtractedText { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }

    /// <summary>
    /// 目录OCR处理结果
    /// </summary>
    public class CatalogueOcrResult
    {
        /// <summary>
        /// 目录ID
        /// </summary>
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 处理的文件数量
        /// </summary>
        public int ProcessedFilesCount { get; set; }

        /// <summary>
        /// 成功的文件数量
        /// </summary>
        public int SuccessFilesCount { get; set; }

        /// <summary>
        /// 失败的文件数量
        /// </summary>
        public int FailedFilesCount { get; set; }

        /// <summary>
        /// 跳过的文件数量
        /// </summary>
        public int SkippedFilesCount { get; set; }

        /// <summary>
        /// 合并的全文内容
        /// </summary>
        public string? CombinedFullText { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
    }
}
