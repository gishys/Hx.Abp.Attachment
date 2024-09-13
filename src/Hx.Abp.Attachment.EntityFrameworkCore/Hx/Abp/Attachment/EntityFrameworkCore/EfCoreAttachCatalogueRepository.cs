using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
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
        public async Task<CreateAttachFileCatalogueInfo?> ByIdMaxSequenceAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(u => u.Id == id)
                .Select(d => new CreateAttachFileCatalogueInfo()
                {
                    SequenceNumber = d.AttachFiles.Count > 0 ? d.AttachFiles.Max(f => f.SequenceNumber) : 0,
                    Reference = d.Reference,
                    Id = d.Id,
                }).FirstOrDefaultAsync(cancellationToken);
        }
        public async Task<List<AttachCatalogue>> FindByReferenceAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .IncludeDetials(includeDetails)
                .Where(p => p.ParentId == null && inputs.Any(i => i.Reference == p.Reference && i.ReferenceType == p.ReferenceType))
                .OrderBy(d => d.SequenceNumber)
                .ToListAsync(cancellationToken);
        }
        public override async Task<IQueryable<AttachCatalogue>> WithDetailsAsync()
        {
            var queryable = await GetQueryableAsync();
            return queryable.IncludeDetials();
        }

        public async Task<bool> AnyByNameAsync(string catalogueName, string reference)
        {
            return await (await GetDbSetAsync()).AnyAsync(p => p.CatalogueName == catalogueName && p.Reference == reference);
        }
        public async Task<int> GetMaxSequenceNumberByReferenceAsync(string reference)
        {
            return await (await GetDbSetAsync())
                .Where(d => d.CatalogueName == reference)
                .OrderByDescending(d => d.SequenceNumber)
                .Select(d => d.SequenceNumber).FirstOrDefaultAsync();
        }
    }
}