using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueAppService : IApplicationService
    {
        Task<AttachCatalogueDto?> CreateAsync(AttachCatalogueCreateDto create, CatalogueCreateMode? createMode);
        Task<List<AttachCatalogueDto>> FindByReferenceAsync(List<GetAttachListInput> inputs);
        Task<AttachCatalogueDto> UpdateAsync(Guid id, AttachCatalogueUpdateDto input);
        Task DeleteAsync(Guid id);
        Task DeleteSingleFileAsync(Guid attachFileId);
        Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input);
        Task<List<AttachFileDto>> QueryFilesAsync(Guid catalogueId);
        Task<AttachFileDto> QueryFileAsync(Guid attachFileId);
        Task<List<AttachFileDto>> CreateFilesAsync(Guid? id, List<AttachFileCreateDto> inputs, string? prefix = null);
        Task<FileVerifyResultDto> VerifyUploadAsync(List<GetAttachListInput> inputs, bool details = false);
        Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode createMode);
        Task DeleteByReferenceAsync(List<AttachCatalogueCreateDto> inputs);
        Task<AttachCatalogueDto?> GetAttachCatalogueByFileIdAsync(Guid fileId);

        /// <summary>
        /// 全文检索分类
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> SearchByFullTextAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10);

        /// <summary>
        /// 语义检索分类
        /// </summary>
        /// <param name="queryEmbedding">查询向量</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <returns>匹配的分类列表</returns>
        Task<List<AttachCatalogueDto>> SearchBySemanticAsync(float[] queryEmbedding, string? reference = null, int? referenceType = null, int limit = 10, float similarityThreshold = 0.7f);

        /// <summary>
        /// 更新分类的语义向量
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="embedding">语义向量</param>
        /// <returns>更新后的分类</returns>
        Task<AttachCatalogueDto> UpdateEmbeddingAsync(Guid catalogueId, float[] embedding);
    }
}
