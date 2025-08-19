using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// OCR文本块仓储实现
    /// </summary>
    public class OcrTextBlockRepository(IDbContextProvider<AttachmentDbContext> dbContextProvider) : EfCoreRepository<AttachmentDbContext, OcrTextBlock, Guid>(dbContextProvider), IOcrTextBlockRepository
    {

        /// <summary>
        /// 根据文件ID获取文本块列表
        /// </summary>
        public async Task<List<OcrTextBlock>> GetByFileIdAsync(Guid fileId, float minProbability = 0.0f)
        {
            var dbContext = await GetDbContextAsync();
            
            return await dbContext.OcrTextBlocks
                .Where(tb => tb.AttachFileId == fileId && tb.Probability >= minProbability)
                .OrderBy(tb => tb.PageIndex)
                .ThenBy(tb => tb.BlockOrder)
                .ToListAsync();
        }

        /// <summary>
        /// 获取OCR统计信息
        /// </summary>
        public async Task<OcrStatistics> GetStatisticsAsync()
        {
            var dbContext = await GetDbContextAsync();

            var stats = await dbContext.OcrTextBlocks
                .GroupBy(tb => 1)
                .Select(g => new
                {
                    TotalTextBlocks = g.Count(),
                    TotalFilesWithOcr = g.Select(tb => tb.AttachFileId).Distinct().Count(),
                    AverageProbability = g.Average(tb => tb.Probability),
                    TotalTextLength = g.Sum(tb => tb.Text.Length)
                })
                .FirstOrDefaultAsync();

            return new OcrStatistics
            {
                TotalTextBlocks = stats?.TotalTextBlocks ?? 0,
                TotalFilesWithOcr = stats?.TotalFilesWithOcr ?? 0,
                AverageProbability = stats != null ? (decimal)stats.AverageProbability : 0,
                TotalTextLength = stats?.TotalTextLength ?? 0
            };
        }

        /// <summary>
        /// 清理孤立的文本块
        /// </summary>
        public async Task<int> CleanupOrphanedBlocksAsync()
        {
            var dbContext = await GetDbContextAsync();
            
            // 查找孤立的文本块（关联的文件不存在）
            var orphanedBlocks = await dbContext.OcrTextBlocks
                .Where(tb => !dbContext.AttachFiles.Any(af => af.Id == tb.AttachFileId))
                .ToListAsync();

            if (orphanedBlocks.Count != 0)
            {
                dbContext.OcrTextBlocks.RemoveRange(orphanedBlocks);
                await dbContext.SaveChangesAsync();
            }

            return orphanedBlocks.Count;
        }
    }
}
