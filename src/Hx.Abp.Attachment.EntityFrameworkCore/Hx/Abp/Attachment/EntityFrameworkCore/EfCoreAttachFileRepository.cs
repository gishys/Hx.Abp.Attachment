using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class EfCoreAttachFileRepository(
        IDbContextProvider<AttachmentDbContext> provider)
                : EfCoreRepository<AttachmentDbContext, AttachFile, Guid>(provider),
        IEfCoreAttachFileRepository
    {
        public async Task<int> DeleteByCatalogueAsync(Guid catalogueId)
        {
            return await (await GetDbSetAsync()).Where(d => d.AttachCatalogueId == catalogueId).ExecuteDeleteAsync();
        }

        public async Task<List<AttachFile>> GetListByIdsAsync(List<Guid> ids)
        {
            return await (await GetDbSetAsync()).Where(d => ids.Any(id => id == d.Id)).ToListAsync();
        }

        public async Task<List<AttachFile>> GetListByCatalogueIdAsync(Guid catalogueId, CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(f => f.AttachCatalogueId == catalogueId)
                .OrderBy(f => f.SequenceNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetMaxSequenceNumberByCatalogueIdAsync(Guid catalogueId, CancellationToken cancellationToken = default)
        {
            var maxSequence = await (await GetDbSetAsync())
                .Where(f => f.AttachCatalogueId == catalogueId)
                .MaxAsync(f => (int?)f.SequenceNumber, cancellationToken);

            return maxSequence ?? 0;
        }

        public async Task<List<AttachFile>> GetListByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose, CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(f => f.Reference == reference &&
                           f.TemplatePurpose == templatePurpose &&
                           f.IsCategorized == false) // 未归档的文件
                .OrderBy(f => f.SequenceNumber)
                .ThenBy(f => f.CreationTime)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<AttachFile>> GetListByCatalogueIdsAsync(List<Guid> catalogueIds, CancellationToken cancellationToken = default)
        {
            if (catalogueIds == null || catalogueIds.Count == 0)
            {
                return [];
            }

            return await (await GetDbSetAsync())
                .Where(f => f.AttachCatalogueId.HasValue && catalogueIds.Contains(f.AttachCatalogueId.Value))
                .OrderBy(f => f.AttachCatalogueId)
                .ThenBy(f => f.SequenceNumber)
                .ToListAsync(cancellationToken);
        }
    }
}
