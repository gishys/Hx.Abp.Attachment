using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class EfCoreAttachCatalogueRepository(
        IDbContextProvider<AttachmentDbContext> provider)
                : EfCoreRepository<AttachmentDbContext, AttachCatalogue, Guid>(provider),
        IEfCoreAttachCatalogueRepository
    {
        public override async Task<AttachCatalogue?> FindAsync(
            Guid id,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .IncludeDetials(includeDetails)
                .FirstOrDefaultAsync(u => u.Id == id,
                GetCancellationToken(cancellationToken));
        }
        public async Task<int> ByParentIdFindMaxSequenceAsync(
            Guid parentId,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(u => u.ParentId == parentId)
                .Select(d => d.SequenceNumber)
                .DefaultIfEmpty()
                .MaxAsync(GetCancellationToken(cancellationToken));
        }
        public async Task<List<AttachCatalogue>> FindByReferenceAsync(
            string reference,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .IncludeDetials(includeDetails)
                .Where(p => p.Reference == reference && p.ParentId == null)
                .OrderBy(d => d.SequenceNumber)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<AttachCatalogue>> FindByParentIdAsync(
            Guid parentId,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .IncludeDetials(includeDetails)
                .Where(p => p.ParentId == parentId)
                .OrderBy(d => d.SequenceNumber)
                .ToListAsync(cancellationToken);
        }
        public async Task<AttachFile?> FindSingleAttachFileAsync(
            Guid catalogueId,
            Guid attachFileId)
        {
            var entity = (await GetDbSetAsync())
                .IncludeDetials(true)
                .FirstOrDefault(d => d.Id == catalogueId);
            return entity?
                .AttachFiles?
                .FirstOrDefault(d => d.Id == attachFileId);
        }
        public override async Task<IQueryable<AttachCatalogue>> WithDetailsAsync()
        {
            var queryable = await GetQueryableAsync();
            return queryable.IncludeDetials();
        }

        public async Task<bool> AnyByNameAsync(string catalogueName)
        {
            return await (await GetDbSetAsync()).AnyAsync(p => p.CatalogueName == catalogueName);
        }
        public async Task<int> DeleteByNameAsync(string catalogueName)
        {
            return await (await GetDbSetAsync()).Where(d => d.CatalogueName == catalogueName).ExecuteDeleteAsync();
        }
    }
}