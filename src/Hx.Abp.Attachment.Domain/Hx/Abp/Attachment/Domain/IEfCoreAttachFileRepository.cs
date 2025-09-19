using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IEfCoreAttachFileRepository : IBasicRepository<AttachFile, Guid>
    {
        Task<int> DeleteByCatalogueAsync(Guid catalogueId);
        Task<List<AttachFile>> GetListByIdsAsync(List<Guid> ids);
        
        /// <summary>
        /// 根据分类ID获取文件列表
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件列表</returns>
        Task<List<AttachFile>> GetListByCatalogueIdAsync(Guid catalogueId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 根据分类ID获取文件的最大序号
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>最大序号，如果没有文件则返回0</returns>
        Task<int> GetMaxSequenceNumberByCatalogueIdAsync(Guid catalogueId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件列表</returns>
        Task<List<AttachFile>> GetListByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据多个分类ID获取文件列表
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>文件列表</returns>
        Task<List<AttachFile>> GetListByCatalogueIdsAsync(List<Guid> catalogueIds, CancellationToken cancellationToken = default);
    }
}
