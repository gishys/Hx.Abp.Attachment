using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 模板使用统计控制器
    /// </summary>
    [Route("api/app/template-usage-stats")]
    public class TemplateUsageStatsController(ITemplateUsageStatsAppService templateUsageStatsAppService) : AbpControllerBase, ITemplateUsageStatsAppService
    {
        private readonly ITemplateUsageStatsAppService _templateUsageStatsAppService = templateUsageStatsAppService;

        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>使用次数</returns>
        [HttpGet("usage-count/{templateId}")]
        public async Task<int> GetTemplateUsageCountAsync(Guid templateId)
        {
            return await _templateUsageStatsAppService.GetTemplateUsageCountAsync(templateId);
        }

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        /// <param name="input">查询输入</param>
        /// <returns>使用统计</returns>
        [HttpPost("stats")]
        public async Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync([FromBody] TemplateUsageStatsInputDto input)
        {
            return await _templateUsageStatsAppService.GetTemplateUsageStatsAsync(input);
        }

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="daysBack">查询天数</param>
        /// <returns>使用趋势</returns>
        [HttpGet("trend/{templateId}")]
        public async Task<List<TemplateUsageTrendDto>> GetTemplateUsageTrendAsync(Guid templateId, [FromQuery] int daysBack = 30)
        {
            return await _templateUsageStatsAppService.GetTemplateUsageTrendAsync(templateId, daysBack);
        }

        /// <summary>
        /// 批量获取模板使用统计
        /// </summary>
        /// <param name="input">批量查询输入</param>
        /// <returns>批量使用统计</returns>
        [HttpPost("batch-stats")]
        public async Task<List<BatchTemplateUsageStatsDto>> GetBatchTemplateUsageStatsAsync([FromBody] BatchTemplateUsageStatsInputDto input)
        {
            return await _templateUsageStatsAppService.GetBatchTemplateUsageStatsAsync(input);
        }

        /// <summary>
        /// 获取热门模板
        /// </summary>
        /// <param name="input">查询输入</param>
        /// <returns>热门模板列表</returns>
        [HttpPost("hot-templates")]
        public async Task<ListResultDto<HotTemplateDto>> GetHotTemplatesAsync([FromBody] HotTemplatesInputDto input)
        {
            return await _templateUsageStatsAppService.GetHotTemplatesAsync(input);
        }

        /// <summary>
        /// 获取模板使用统计概览
        /// </summary>
        /// <returns>统计概览</returns>
        [HttpGet("overview")]
        public async Task<object> GetTemplateUsageOverviewAsync()
        {
            return await _templateUsageStatsAppService.GetTemplateUsageOverviewAsync();
        }

        /// <summary>
        /// 更新模板使用统计（内部使用）
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>是否成功</returns>
        [HttpPost("update/{templateId}")]
        public async Task<bool> UpdateTemplateUsageStatsAsync(Guid templateId)
        {
            return await _templateUsageStatsAppService.UpdateTemplateUsageStatsAsync(templateId);
        }
    }
}
