using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Dynamic.Core;
using Volo.Abp;
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
                .IncludeDetails(includeDetails)
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
            var queryable = (await GetDbSetAsync()).IncludeDetails(includeDetails);
            var predicate = GetCataloguesByReference(inputs);
            queryable = queryable.Where(predicate);
            return await queryable.OrderBy(d => d.Reference)
             .ThenBy(d => d.SequenceNumber)
             .ToListAsync(cancellationToken);
        }
        public async Task DeleteByReferenceAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            var queryable = (await GetDbSetAsync()).IncludeDetails(includeDetails);
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.Reference == input.Reference &&
                d.ReferenceType == input.ReferenceType);
            }
            if (predicate == null)
            {
                throw new UserFriendlyException("输入条件不能为空！");
            }
            queryable = queryable.Where(predicate);
            var ids = await queryable.Select(d => d.Id)
            .ToListAsync(cancellationToken);
            await DeleteManyAsync(ids);
        }
        public async Task<List<AttachCatalogue>> VerifyUploadAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            var queryable = (await GetDbSetAsync()).IncludeDetails(includeDetails);
            var predicate = GetCataloguesByReference(inputs);
            queryable = queryable.Where(predicate);
            return await queryable.OrderBy(d => d.Reference)
             .ThenBy(d => d.SequenceNumber)
             .ToListAsync(cancellationToken);
        }
        public override async Task<IQueryable<AttachCatalogue>> WithDetailsAsync()
        {
            var queryable = await GetQueryableAsync();
            return queryable.IncludeDetails();
        }

        public async Task<bool> AnyByNameAsync(string catalogueName, string reference, int referenceType)
        {
            return await (await GetDbSetAsync()).AnyAsync(p =>
            p.CatalogueName == catalogueName &&
            p.Reference == reference &&
            p.ReferenceType == referenceType);
        }
        public async Task<List<AttachCatalogue>> AnyByNameAsync(List<GetCatalogueInput> inputs)
        {
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.Reference == input.Reference &&
                d.ReferenceType == input.ReferenceType &&
                d.CatalogueName == input.CatalogueName);
            }
            if (predicate == null)
            {
                throw new UserFriendlyException("输入条件不能为空！");
            }
            return await (await GetDbSetAsync()).Where(predicate).ToListAsync();
        }
        public async Task DeleteRootCatalogueAsync(List<GetCatalogueInput> inputs)
        {
            var queryable = (await GetDbSetAsync()).AsQueryable();
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.Reference == input.Reference &&
                d.ReferenceType == input.ReferenceType &&
                d.CatalogueName == input.CatalogueName);
            }
            if (predicate == null)
            {
                throw new UserFriendlyException("输入条件不能为空！");
            }
            queryable = queryable.Where(predicate);
            var ids = await queryable.Select(d => d.Id).ToListAsync();
            await DeleteManyAsync(ids);
        }
        public async Task<int> GetMaxSequenceNumberByReferenceAsync(string reference)
        {
            return await (await GetDbSetAsync())
                .Where(d => d.CatalogueName == reference)
                .OrderByDescending(d => d.SequenceNumber)
                .Select(d => d.SequenceNumber).FirstOrDefaultAsync();
        }
        private ExpressionStarter<AttachCatalogue> GetCataloguesByReference(List<GetAttachListInput> inputs)
        {
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.ParentId == null &&
                d.Reference == input.Reference &&
                d.ReferenceType == input.ReferenceType);
            }
            if (predicate == null)
            {
                throw new UserFriendlyException("输入条件不能为空！");
            }
            return predicate;
        }
    }
}