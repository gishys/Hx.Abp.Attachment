﻿using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IEfCoreAttachCatalogueRepository : IBasicRepository<AttachCatalogue, Guid>
    {
        Task<List<AttachCatalogue>> FindByReferenceAsync(
            string reference,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task<AttachFile?> FindSingleAttachFileAsync(
            Guid catalogueId,
            Guid attachFileId);
        Task<List<AttachCatalogue>> FindByParentIdAsync(
            Guid parentId,
            bool includeDetails = true,
            CancellationToken cancellationToken = default);
        Task<int> ByParentIdFindMaxSequenceAsync(
            Guid parentId,
            CancellationToken cancellationToken = default);
        Task<bool> AnyByNameAsync(string catalogueName);
        Task<int> DeleteByNameAsync(string catalogueName);
        Task<CreateAttachFileCatalogueInfo?> ByIdMaxSequenceAsync(
            Guid id,
            CancellationToken cancellationToken = default);
    }
}
