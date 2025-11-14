using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 预设元数据内容仓储接口
    /// </summary>
    public interface IMetaFieldPresetRepository : IRepository<MetaFieldPreset, Guid>
    {
        /// <summary>
        /// 根据名称查找预设
        /// </summary>
        Task<MetaFieldPreset?> FindByNameAsync(string presetName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据业务场景查询预设
        /// </summary>
        Task<List<MetaFieldPreset>> FindByBusinessScenarioAsync(string businessScenario, bool onlyEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据分面类型查询预设
        /// </summary>
        Task<List<MetaFieldPreset>> FindByFacetTypeAsync(FacetType facetType, bool onlyEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据模板用途查询预设
        /// </summary>
        Task<List<MetaFieldPreset>> FindByTemplatePurposeAsync(TemplatePurpose templatePurpose, bool onlyEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 搜索预设（支持名称、描述、标签等）
        /// </summary>
        Task<List<MetaFieldPreset>> SearchAsync(
            string? keyword = null,
            List<string>? tags = null,
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool onlyEnabled = true,
            int maxResults = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取热门预设（按使用次数排序）
        /// </summary>
        Task<List<MetaFieldPreset>> GetPopularPresetsAsync(
            int topN = 10,
            bool onlyEnabled = true,
            DateTime? since = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取推荐预设（基于推荐权重和使用频率）
        /// </summary>
        Task<List<MetaFieldPreset>> GetRecommendedPresetsAsync(
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int topN = 10,
            double minWeight = 0.3,
            bool onlyEnabled = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据标签查询预设
        /// </summary>
        Task<List<MetaFieldPreset>> FindByTagsAsync(List<string> tags, bool matchAll = false, bool onlyEnabled = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取所有启用的预设
        /// </summary>
        Task<List<MetaFieldPreset>> GetAllEnabledAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 检查名称是否存在
        /// </summary>
        Task<bool> ExistsByNameAsync(string presetName, Guid? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量更新推荐权重
        /// </summary>
        Task BatchUpdateRecommendationWeightsAsync(Dictionary<Guid, double> weights, CancellationToken cancellationToken = default);
    }
}

