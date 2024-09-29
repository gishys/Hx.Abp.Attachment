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
        Task<bool> AnyByNameAsync(string catalogueName, string reference, int referenceType);
        Task<int> GetMaxSequenceNumberByReferenceAsync(string reference);
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
    }
}
