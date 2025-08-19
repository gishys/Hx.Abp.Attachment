namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// OCR统计信息DTO
    /// </summary>
    public class OcrStatisticsDto
    {
        /// <summary>
        /// 总文本块数
        /// </summary>
        public long TotalTextBlocks { get; set; }

        /// <summary>
        /// 有OCR的文件数
        /// </summary>
        public long TotalFilesWithOcr { get; set; }

        /// <summary>
        /// 平均置信度
        /// </summary>
        public decimal AverageProbability { get; set; }

        /// <summary>
        /// 总文本长度
        /// </summary>
        public long TotalTextLength { get; set; }

        /// <summary>
        /// 统计时间
        /// </summary>
        public DateTime StatisticsTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 清理结果DTO
    /// </summary>
    public class CleanupResultDto
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 删除的记录数
        /// </summary>
        public int DeletedCount { get; set; }

        /// <summary>
        /// 清理时间
        /// </summary>
        public DateTime CleanupTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
