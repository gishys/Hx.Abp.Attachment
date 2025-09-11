using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IAttachCatalogueTemplateRepository : IRepository<AttachCatalogueTemplate>
    {
        Task<List<AttachCatalogueTemplate>> FindByRuleMatchAsync(Dictionary<string, object> context, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> GetChildrenAsync(Guid parentId, int parentVersion, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> GetChildrenByParentAsync(Guid parentId, int parentVersion, bool onlyLatest = true);

        // 新增版本相关方法
        Task<AttachCatalogueTemplate?> GetLatestVersionAsync(Guid templateId, bool includeTreeStructure = false);
        Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(Guid templateId);
        Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId);
        Task<AttachCatalogueTemplate?> GetByVersionAsync(Guid templateId, int version);
        Task SetAsLatestVersionAsync(Guid templateId, int version);
        Task SetAllOtherVersionsAsNotLatestAsync(Guid templateId, int excludeVersion);
        
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
        Task<bool> ExistsByNameAsync(string templateName, Guid? parentId = null, int? parentVersion = null, Guid? excludeId = null);
        Task<List<AttachCatalogueTemplate>> FindSimilarTemplatesAsync(
            string semanticQuery,
            double similarityThreshold = 0.7,
            int maxResults = 10);

        Task<List<AttachCatalogueTemplate>> GetTemplatesByVectorDimensionAsync(
            int minDimension,
            int maxDimension,
            bool onlyLatest = true);

        // 新增统计方法
        Task<TemplateStatistics> GetTemplateStatisticsAsync();
        Task<AttachCatalogueTemplate?> GetAsync(Guid templateId, bool includeTreeStructure = false, bool returnRoot = false);

        #region 配置维护方法

        /// <summary>
        /// 更新模板的工作流配置
        /// </summary>
        Task UpdateWorkflowConfigAsync(Guid templateId, string workflowConfig);

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
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号，null表示查询所有版本</param>
        Task<int> GetTemplateUsageCountAsync(Guid templateId, int? templateVersion = null);

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号，null表示查询所有版本</param>
        Task<TemplateUsageStats> GetTemplateUsageStatsAsync(Guid templateId, int? templateVersion = null);

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="daysBack">查询天数</param>
        /// <param name="templateVersion">模板版本号，null表示查询所有版本</param>
        Task<List<TemplateUsageTrend>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30, int? templateVersion = null);

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
            double semanticWeight = 0.6,
            bool onlyLatest = true);

        /// <summary>
        /// 增强版混合检索模板（支持真正的向量相似度计算）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> SearchTemplatesHybridAdvancedAsync(
            string? keyword = null,
            string? semanticQuery = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            double similarityThreshold = 0.7,
            double textWeight = 0.4,
            double semanticWeight = 0.6,
            bool enableVectorSearch = true,
            bool enableFullTextSearch = true,
            bool onlyLatest = true);

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
            bool onlyLatest = true,
            string? fulltextQuery = null);

        /// <summary>
        /// 递归获取模板的完整子树
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetTemplateSubtreeAsync(
            Guid rootId,
            bool onlyLatest = true,
            int maxDepth = 10);

        #endregion

        #region 模板路径相关查询方法

        /// <summary>
        /// 根据模板路径获取模板
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetTemplatesByPathAsync(string? templatePath, bool includeChildren = false);

        /// <summary>
        /// 根据路径深度获取模板
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetTemplatesByPathDepthAsync(int depth, bool onlyLatest = true);

        /// <summary>
        /// 根据路径范围获取模板
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetTemplatesByPathRangeAsync(string? startPath, string? endPath, bool onlyLatest = true);

        /// <summary>
        /// 获取指定路径下的所有子模板
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetChildTemplatesByPathAsync(string parentPath, bool onlyLatest = true);

        /// <summary>
        /// 检查路径是否存在
        /// </summary>
        Task<bool> ExistsByPathAsync(string templatePath);

        /// <summary>
        /// 获取路径统计信息
        /// </summary>
        Task<Dictionary<int, int>> GetPathDepthStatisticsAsync(bool onlyLatest = true);

        /// <summary>
        /// 获取指定路径的直接子节点（不包含孙子节点）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetDirectChildrenByPathAsync(string parentPath, bool onlyLatest = true);

        /// <summary>
        /// 获取指定路径的所有后代节点（包含所有层级的子节点）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetAllDescendantsByPathAsync(string parentPath, bool onlyLatest = true, int? maxDepth = null);

        /// <summary>
        /// 获取指定路径的祖先节点（从根节点到指定路径的所有父节点）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetAncestorsByPathAsync(string templatePath, bool onlyLatest = true);

        /// <summary>
        /// 获取指定路径的兄弟节点（同一父节点下的其他节点）
        /// </summary>
        Task<List<AttachCatalogueTemplate>> GetSiblingsByPathAsync(string templatePath, bool onlyLatest = true);

        /// <summary>
        /// 检查两个路径是否为祖先-后代关系
        /// </summary>
        Task<bool> IsAncestorDescendantAsync(string ancestorPath, string descendantPath);

        /// <summary>
        /// 获取同级模板中的最大路径（用于自动生成下一个路径）
        /// </summary>
        /// <param name="parentPath">父路径，null表示根级别</param>
        /// <param name="onlyLatest">是否只查询最新版本</param>
        /// <returns>同级中的最大路径，如果没有同级则返回null</returns>
        Task<string?> GetMaxTemplatePathAtSameLevelAsync(string? parentPath, bool onlyLatest = true);

        #endregion
    }
}
