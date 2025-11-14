using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 预设元数据内容应用服务接口
    /// </summary>
    public interface IMetaFieldPresetAppService : IApplicationService
    {
        /// <summary>
        /// 创建预设
        /// </summary>
        Task<MetaFieldPresetDto> CreateAsync(CreateUpdateMetaFieldPresetDto input);

        /// <summary>
        /// 更新预设
        /// </summary>
        Task<MetaFieldPresetDto> UpdateAsync(Guid id, CreateUpdateMetaFieldPresetDto input);

        /// <summary>
        /// 删除预设
        /// </summary>
        Task DeleteAsync(Guid id);

        /// <summary>
        /// 根据ID获取预设
        /// </summary>
        Task<MetaFieldPresetDto> GetAsync(Guid id);

        /// <summary>
        /// 获取所有预设
        /// </summary>
        Task<List<MetaFieldPresetDto>> GetListAsync();

        /// <summary>
        /// 搜索预设
        /// </summary>
        Task<List<MetaFieldPresetDto>> SearchAsync(PresetSearchRequestDto input);

        /// <summary>
        /// 获取推荐预设
        /// </summary>
        Task<List<PresetRecommendationDto>> GetRecommendationsAsync(PresetRecommendationRequestDto input);

        /// <summary>
        /// 获取热门预设
        /// </summary>
        Task<List<MetaFieldPresetDto>> GetPopularPresetsAsync(int topN = 10);

        /// <summary>
        /// 启用预设
        /// </summary>
        Task EnableAsync(Guid id);

        /// <summary>
        /// 禁用预设
        /// </summary>
        Task DisableAsync(Guid id);

        /// <summary>
        /// 应用预设到模板（将预设的元数据字段应用到模板）
        /// </summary>
        Task<List<MetaFieldDto>> ApplyPresetToTemplateAsync(Guid presetId, Guid templateId);

        /// <summary>
        /// 批量应用预设到模板（将多个预设的元数据字段应用到模板）
        /// </summary>
        Task<ApplyPresetsToTemplateResponseDto> ApplyPresetsToTemplateAsync(Guid templateId, ApplyPresetsToTemplateRequestDto input);

        /// <summary>
        /// 记录预设使用（当模板使用预设时调用）
        /// </summary>
        Task RecordUsageAsync(Guid presetId, Guid? templateId = null);

        /// <summary>
        /// 获取统计信息
        /// </summary>
        Task<PresetStatisticsDto> GetStatisticsAsync();

        /// <summary>
        /// 批量更新推荐权重（用于自我进化）
        /// </summary>
        Task BatchUpdateRecommendationWeightsAsync(CancellationToken cancellationToken = default);
    }
}

