using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 模板使用统计应用服务接口
    /// </summary>
    public interface ITemplateUsageStatsAppService : IApplicationService
    {
        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>使用次数</returns>
        Task<int> GetTemplateUsageCountAsync(Guid templateId);

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        /// <param name="input">查询输入</param>
        /// <returns>使用统计</returns>
        Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync(TemplateUsageStatsInputDto input);

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="daysBack">查询天数</param>
        /// <returns>使用趋势</returns>
        Task<List<TemplateUsageTrendDto>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30);

        /// <summary>
        /// 批量获取模板使用统计
        /// </summary>
        /// <param name="input">批量查询输入</param>
        /// <returns>批量使用统计</returns>
        Task<List<BatchTemplateUsageStatsDto>> GetBatchTemplateUsageStatsAsync(BatchTemplateUsageStatsInputDto input);

        /// <summary>
        /// 获取热门模板
        /// </summary>
        /// <param name="input">查询输入</param>
        /// <returns>热门模板列表</returns>
        Task<ListResultDto<HotTemplateDto>> GetHotTemplatesAsync(HotTemplatesInputDto input);

        /// <summary>
        /// 获取模板使用统计概览
        /// </summary>
        /// <returns>统计概览</returns>
        Task<object> GetTemplateUsageOverviewAsync();

        /// <summary>
        /// 更新模板使用统计（内部使用）
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>是否成功</returns>
        Task<bool> UpdateTemplateUsageStatsAsync(Guid templateId);
    }
}
