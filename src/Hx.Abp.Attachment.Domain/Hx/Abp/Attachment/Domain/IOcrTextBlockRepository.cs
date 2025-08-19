using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// OCR文本块仓储接口
    /// </summary>
    public interface IOcrTextBlockRepository : IRepository<OcrTextBlock, Guid>
    {
        /// <summary>
        /// 根据文件ID获取文本块列表
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="minProbability">最小置信度</param>
        /// <returns>文本块列表</returns>
        Task<List<OcrTextBlock>> GetByFileIdAsync(Guid fileId, float minProbability = 0.0f);

        /// <summary>
        /// 获取OCR统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        Task<OcrStatistics> GetStatisticsAsync();

        /// <summary>
        /// 清理孤立的文本块
        /// </summary>
        /// <returns>删除的记录数</returns>
        Task<int> CleanupOrphanedBlocksAsync();
    }

    /// <summary>
    /// OCR统计信息
    /// </summary>
    public class OcrStatistics
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
    }
}
