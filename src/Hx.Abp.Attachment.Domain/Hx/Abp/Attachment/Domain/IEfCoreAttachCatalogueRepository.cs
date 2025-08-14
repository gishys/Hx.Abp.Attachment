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
        /// 语义检索分类
        /// </summary>
        /// <param name="queryEmbedding">查询向量</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogue>> SearchBySemanticAsync(
            float[] queryEmbedding,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default);
    }
}
