using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 预设元数据内容API控制器
    /// </summary>
    [ApiController]
    [Route("api/app/meta-field-preset")]
    public class MetaFieldPresetController(IMetaFieldPresetAppService appService) : AbpControllerBase
    {
        protected IMetaFieldPresetAppService AppService { get; } = appService;

        /// <summary>
        /// 创建预设
        /// </summary>
        [HttpPost]
        public virtual Task<MetaFieldPresetDto> CreateAsync([FromBody] CreateUpdateMetaFieldPresetDto input)
        {
            return AppService.CreateAsync(input);
        }

        /// <summary>
        /// 更新预设
        /// </summary>
        [HttpPut("{id}")]
        public virtual Task<MetaFieldPresetDto> UpdateAsync(Guid id, [FromBody] CreateUpdateMetaFieldPresetDto input)
        {
            return AppService.UpdateAsync(id, input);
        }

        /// <summary>
        /// 删除预设
        /// </summary>
        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return AppService.DeleteAsync(id);
        }

        /// <summary>
        /// 根据ID获取预设
        /// </summary>
        [HttpGet("{id}")]
        public virtual Task<MetaFieldPresetDto> GetAsync(Guid id)
        {
            return AppService.GetAsync(id);
        }

        /// <summary>
        /// 获取所有预设
        /// </summary>
        [HttpGet]
        public virtual Task<List<MetaFieldPresetDto>> GetListAsync()
        {
            return AppService.GetListAsync();
        }

        /// <summary>
        /// 搜索预设
        /// </summary>
        [HttpPost("search")]
        public virtual Task<List<MetaFieldPresetDto>> SearchAsync([FromBody] PresetSearchRequestDto input)
        {
            return AppService.SearchAsync(input);
        }

        /// <summary>
        /// 获取推荐预设
        /// </summary>
        [HttpPost("recommendations")]
        public virtual Task<List<PresetRecommendationDto>> GetRecommendationsAsync([FromBody] PresetRecommendationRequestDto input)
        {
            return AppService.GetRecommendationsAsync(input);
        }

        /// <summary>
        /// 获取热门预设
        /// </summary>
        [HttpGet("popular")]
        public virtual Task<List<MetaFieldPresetDto>> GetPopularPresetsAsync([FromQuery] int topN = 10)
        {
            return AppService.GetPopularPresetsAsync(topN);
        }

        /// <summary>
        /// 启用预设
        /// </summary>
        [HttpPost("{id}/enable")]
        public virtual Task EnableAsync(Guid id)
        {
            return AppService.EnableAsync(id);
        }

        /// <summary>
        /// 禁用预设
        /// </summary>
        [HttpPost("{id}/disable")]
        public virtual Task DisableAsync(Guid id)
        {
            return AppService.DisableAsync(id);
        }

        /// <summary>
        /// 应用预设到模板
        /// </summary>
        [HttpPost("{presetId}/apply-to-template/{templateId}")]
        public virtual Task<List<MetaFieldDto>> ApplyPresetToTemplateAsync(Guid presetId, Guid templateId)
        {
            return AppService.ApplyPresetToTemplateAsync(presetId, templateId);
        }

        /// <summary>
        /// 批量应用预设到模板
        /// </summary>
        [HttpPost("{templateId}/apply-presets")]
        public virtual Task<ApplyPresetsToTemplateResponseDto> ApplyPresetsToTemplateAsync(Guid templateId, [FromBody] ApplyPresetsToTemplateRequestDto input)
        {
            return AppService.ApplyPresetsToTemplateAsync(templateId, input);
        }

        /// <summary>
        /// 记录预设使用
        /// </summary>
        [HttpPost("{presetId}/record-usage")]
        public virtual Task RecordUsageAsync(Guid presetId, [FromQuery] Guid? templateId = null)
        {
            return AppService.RecordUsageAsync(presetId, templateId);
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        [HttpGet("statistics")]
        public virtual Task<PresetStatisticsDto> GetStatisticsAsync()
        {
            return AppService.GetStatisticsAsync();
        }

        /// <summary>
        /// 批量更新推荐权重（用于自我进化）
        /// </summary>
        [HttpPost("batch-update-weights")]
        public virtual Task BatchUpdateRecommendationWeightsAsync()
        {
            return AppService.BatchUpdateRecommendationWeightsAsync();
        }
    }
}

