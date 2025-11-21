using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IEfCoreAttachCatalogueRepository : IBasicRepository<AttachCatalogue, Guid>
    {
        Task<List<AttachCatalogue>> FindByReferenceAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task<int> ByParentIdFindMaxSequenceAsync(
            Guid parentId,
            CancellationToken cancellationToken = default);
        Task<bool> AnyByNameAsync(Guid? parentId, string catalogueName, string reference, int referenceType);
        Task<int> GetMaxSequenceNumberByReferenceAsync(Guid? parentId, string reference, int referenceType);
        Task<CreateAttachFileCatalogueInfo?> ByIdMaxSequenceAsync(
            Guid id,
            CancellationToken cancellationToken = default);
        Task<List<AttachCatalogue>> VerifyUploadAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task DeleteByReferenceAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task DeleteRootCatalogueAsync(List<GetCatalogueInput> inputs);
        Task<List<AttachCatalogue>> AnyByNameAsync(List<GetCatalogueInput> inputs, bool details = true);
        Task<int> ByReferenceMaxSequenceAsync(
            string reference,
            int referenceType,
            CancellationToken cancellationToken = default);
        Task<AttachCatalogue?> GetAsync(Guid? parentId, string catalogueName, string reference, int referenceType);
        Task<AttachCatalogue?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据引用和名称查找分类
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="catalogueName">分类名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类</returns>
        Task<AttachCatalogue?> FindByReferenceAndNameAsync(
            string reference,
            int referenceType,
            string catalogueName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 全文检索分类
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> SearchByFullTextAsync(
                    string searchText,
                    string? reference = null,
                    int? referenceType = null,
                    int limit = 10,
                    CancellationToken cancellationToken = default);

        /// <summary>
        /// 混合搜索：结合全文检索和文本向量检索
        /// </summary>
        /// <param name="searchText">搜索文本（可选）</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> SearchByHybridAsync(
            string? searchText = null,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            string? queryTextVector = null,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据路径查找分类
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类</returns>
        Task<AttachCatalogue?> FindByPathAsync(
            string path,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据路径前缀查找子分类
        /// </summary>
        /// <param name="pathPrefix">路径前缀</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> FindByPathPrefixAsync(
            string pathPrefix,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据路径深度查找分类
        /// </summary>
        /// <param name="depth">路径深度</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> FindByPathDepthAsync(
            int depth,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查找根分类
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>根分类列表</returns>
        Task<AttachCatalogue?> FindRootCataloguesAsync(
            string reference,
            TemplatePurpose purpose,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 查找叶子分类
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>叶子分类列表</returns>
        Task<List<AttachCatalogue>> FindLeafCataloguesAsync(
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据父路径查找直接子分类
        /// </summary>
        /// <param name="parentPath">父路径</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>直接子分类列表</returns>
        Task<List<AttachCatalogue>> FindDirectChildrenByPathAsync(
            string parentPath,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取同级最大路径
        /// </summary>
        /// <param name="parentPath">父路径，null表示根级别</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>同级最大路径，如果没有则返回null</returns>
        Task<string?> GetMaxPathAtSameLevelAsync(
            string? parentPath = null,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据模板ID和版本查找分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> FindByTemplateAsync(
            Guid templateId,
            int? templateVersion = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据模板ID查找所有版本的分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> FindByTemplateIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据分类ID及其所有子分类（基于Path路径查询）
        /// 一次性查询出指定分类及其所有层级的子分类，性能优化版本
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分类及其所有子分类列表</returns>
        Task<List<AttachCatalogue>> GetCatalogueWithAllChildrenAsync(Guid catalogueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分类树形结构（用于树状展示）
        /// 基于路径优化，提供高性能的树形查询
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践
        /// </summary>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <param name="catalogueFacetType">分类分面类型过滤</param>
        /// <param name="cataloguePurpose">分类用途过滤</param>
        /// <param name="includeChildren">是否包含子节点</param>
        /// <param name="includeFiles">是否包含附件文件</param>
        /// <param name="fulltextQuery">全文搜索查询</param>
        /// <param name="templateId">模板ID过滤</param>
        /// <param name="templateVersion">模板版本过滤</param>
        /// <param name="creatorId">创建者用户ID过滤（可选，用于查询指定用户创建的分类）</param>
        /// <param name="targetDate">目标日期过滤（UTC时间，可选，用于查询指定日期创建的分类，null表示不过滤日期）</param>
        /// <param name="skipCount">跳过数量，用于分页</param>
        /// <param name="maxResultCount">最大返回数量，用于分页</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分类树形结构列表</returns>
        Task<List<AttachCatalogue>> GetCataloguesTreeAsync(
            string? reference = null,
            int? referenceType = null,
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null,
            bool includeChildren = true,
            bool includeFiles = false,
            string? fulltextQuery = null,
            Guid? templateId = null,
            int? templateVersion = null,
            Guid? creatorId = null,
            DateTime? targetDate = null,
            int skipCount = 0,
            int maxResultCount = int.MaxValue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取分类树形结构的根节点总数（用于分页）
        /// </summary>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <param name="catalogueFacetType">分类分面类型过滤</param>
        /// <param name="cataloguePurpose">分类用途过滤</param>
        /// <param name="fulltextQuery">全文搜索查询</param>
        /// <param name="templateId">模板ID过滤</param>
        /// <param name="templateVersion">模板版本过滤</param>
        /// <param name="creatorId">创建者用户ID过滤（可选，用于查询指定用户创建的分类）</param>
        /// <param name="targetDate">目标日期过滤（UTC时间，可选，用于查询指定日期创建的分类，null表示不过滤日期）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>根节点总数</returns>
        Task<long> GetCataloguesTreeCountAsync(
            string? reference = null,
            int? referenceType = null,
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null,
            string? fulltextQuery = null,
            Guid? templateId = null,
            int? templateVersion = null,
            Guid? creatorId = null,
            DateTime? targetDate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据归档状态查询分类
        /// </summary>
        /// <param name="isArchived">归档状态</param>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> GetByArchivedStatusAsync(
            bool isArchived,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 批量设置归档状态
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="isArchived">归档状态</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新的记录数</returns>
        Task<int> SetArchivedStatusAsync(
            List<Guid> catalogueIds,
            bool isArchived,
            CancellationToken cancellationToken = default);
    }
}
