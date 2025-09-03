using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IAttachCatalogueTemplateRepository : IRepository<AttachCatalogueTemplate, Guid>
    {
        Task<List<AttachCatalogueTemplate>> FindBySemanticMatchAsync(string query, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> FindByRuleMatchAsync(Dictionary<string, object> context, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> GetChildrenAsync(Guid parentId, bool onlyLatest = true);

        // 新增版本相关方法
        Task<AttachCatalogueTemplate?> GetLatestVersionAsync(string templateName);
        Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(string templateName);
        Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId);
        Task SetAsLatestVersionAsync(Guid templateId);
        Task SetAllOtherVersionsAsNotLatestAsync(string templateName, Guid excludeId);
        
        // 新增智能推荐方法
        Task<List<AttachCatalogueTemplate>> GetIntelligentRecommendationsAsync(
            string query, 
            double threshold = 0.3, 
            int topN = 10, 
            bool onlyLatest = true,
            bool includeHistory = false);
            
        Task<List<AttachCatalogueTemplate>> GetRecommendationsByBusinessAsync(
            string businessDescription,
            List<string> fileTypes,
            int expectedLevels = 3,
            bool onlyLatest = true);

        // 新增模板标识查询方法
        Task<List<AttachCatalogueTemplate>> GetTemplatesByIdentifierAsync(
            int? facetType = null,
            int? templatePurpose = null,
            bool onlyLatest = true);

        // 新增向量相关查询方法
        Task<List<AttachCatalogueTemplate>> FindSimilarTemplatesAsync(
            string semanticQuery,
            double similarityThreshold = 0.7,
            int maxResults = 10);

        Task<List<AttachCatalogueTemplate>> GetTemplatesByVectorDimensionAsync(
            int minDimension,
            int maxDimension,
            bool onlyLatest = true);

        // 新增统计方法
        Task<object> GetTemplateStatisticsAsync();

        #region 配置维护方法

        /// <summary>
        /// 更新模板的规则表达式
        /// </summary>
        Task UpdateRuleExpressionAsync(Guid templateId, string ruleExpression);

        /// <summary>
        /// 智能更新模板配置（基于使用数据）
        /// </summary>
        Task UpdateTemplateConfigurationIntelligentlyAsync(Guid templateId);

        /// <summary>
        /// 智能更新模板关键字（基于使用数据）
        /// </summary>
        Task UpdateTemplateKeywordsIntelligentlyAsync(Guid templateId);

        #endregion

        #region 使用统计方法

        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        Task<int> GetTemplateUsageCountAsync(Guid templateId);

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        Task<TemplateUsageStats> GetTemplateUsageStatsAsync(Guid templateId);

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        Task<List<TemplateUsageTrend>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30);

        /// <summary>
        /// 批量获取模板使用统计
        /// </summary>
        Task<List<BatchTemplateUsageStats>> GetBatchTemplateUsageStatsAsync(List<Guid> templateIds, int daysBack = 30);

        /// <summary>
        /// 获取热门模板
        /// </summary>
        Task<List<HotTemplate>> GetHotTemplatesAsync(int topN = 10, int daysBack = 30);

        /// <summary>
        /// 获取模板使用统计概览
        /// </summary>
        Task<TemplateUsageOverview> GetTemplateUsageOverviewAsync();

        #endregion
    }
}
