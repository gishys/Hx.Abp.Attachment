using Volo.Abp.Domain.Repositories;
using Hx.Abp.Attachment.Domain.Shared;

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

        #region 混合检索方法

        /// <summary>
        /// 混合检索模板（字面 + 语义）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> SearchTemplatesHybridAsync(
            string? keyword = null,
            string? semanticQuery = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            double similarityThreshold = 0.7,
            double textWeight = 0.4,
            double semanticWeight = 0.6);

        /// <summary>
        /// 全文检索模板（基于倒排索引）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> SearchTemplatesByTextAsync(
            string keyword,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            bool enableFuzzy = true,
            bool enablePrefix = true);

        /// <summary>
        /// 标签检索模板
        /// </summary>
        Task<List<AttachCatalogueTemplate>> SearchTemplatesByTagsAsync(
            List<string> tags,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            int maxResults = 20,
            bool matchAll = false);

        /// <summary>
        /// 语义检索模板（基于向量相似度）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> SearchTemplatesBySemanticAsync(
            string semanticQuery,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            double similarityThreshold = 0.7,
            int maxResults = 20);

        /// <summary>
        /// 获取热门标签
        /// </summary>
        Task<List<string>> GetPopularTagsAsync(int topN = 20);

        /// <summary>
        /// 获取标签统计
        /// </summary>
        Task<Dictionary<string, int>> GetTagStatisticsAsync();

        #endregion

        #region 树状结构查询方法

        /// <summary>
        /// 获取根节点模板（用于树状展示）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetRootTemplatesAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool includeChildren = true,
            bool onlyLatest = true);

        /// <summary>
        /// 递归获取模板的完整子树
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetTemplateSubtreeAsync(
            Guid rootId,
            bool onlyLatest = true,
            int maxDepth = 10);

        #endregion
    }
}
