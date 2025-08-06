using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IEfCoreAttachFileRepository : IBasicRepository<AttachFile, Guid>
    {
        Task<int> DeleteByCatalogueAsync(Guid catalogueId);
        Task<List<AttachFile>> GetListByIdsAsync(List<Guid> ids);
    }
}
