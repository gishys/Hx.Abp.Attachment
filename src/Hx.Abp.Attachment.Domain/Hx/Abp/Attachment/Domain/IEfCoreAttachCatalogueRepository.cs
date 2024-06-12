using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IEfCoreAttachCatalogueRepository : IBasicRepository<AttachCatalogue, Guid>
    {
        Task<List<AttachCatalogue>> FindByReferenceAsync(
            string reference,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task<int> ByParentIdFindMaxSequenceAsync(
            Guid parentId,
            CancellationToken cancellationToken = default);
        Task<bool> AnyByNameAsync(string catalogueName, string reference);
        Task<int> GetMaxSequenceNumberByReferenceAsync(string reference);
        Task<CreateAttachFileCatalogueInfo?> ByIdMaxSequenceAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
