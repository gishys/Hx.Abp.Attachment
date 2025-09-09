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
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> SearchByHybridAsync(
            string searchText,
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
        Task<List<AttachCatalogue>> FindRootCataloguesAsync(
            string? reference = null,
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
    }
}
