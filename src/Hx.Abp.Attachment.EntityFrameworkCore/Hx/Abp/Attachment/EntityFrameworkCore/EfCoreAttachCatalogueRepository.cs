using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Linq;
using System.Linq.Dynamic.Core;
using Volo.Abp;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.ObjectMapping;

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
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                if (string.IsNullOrWhiteSpace(input.CatalogueName))
                {
                    predicate = predicate?.Or(d =>
                    d.ParentId == null &&
                    d.Reference == input.Reference &&
                    d.ReferenceType == input.ReferenceType);
                }
                else
                {
                    predicate = predicate?.Or(d =>
                    d.ParentId == null &&
                    d.Reference == input.Reference &&
                    d.ReferenceType == input.ReferenceType &&
                    d.CatalogueName == input.CatalogueName);
                }
            }
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

        public async Task<bool> AnyByNameAsync(Guid? parentId, string catalogueName, string reference, int referenceType)
        {
            return await (await GetDbSetAsync()).AnyAsync(p =>
            p.ParentId == parentId &&
            p.CatalogueName == catalogueName &&
            p.Reference == reference &&
            p.ReferenceType == referenceType);
        }
        public async Task<AttachCatalogue?> GetAsync(Guid? parentId, string catalogueName, string reference, int referenceType)
        {
            return await (await GetDbSetAsync()).Where(p =>
            p.ParentId == parentId &&
            p.CatalogueName == catalogueName &&
            p.Reference == reference &&
            p.ReferenceType == referenceType).FirstOrDefaultAsync();
        }
        public async Task<List<AttachCatalogue>> AnyByNameAsync(List<GetCatalogueInput> inputs, bool details = true)
        {
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.ParentId == input.ParentId &&
                d.Reference == input.Reference &&
                d.ReferenceType == input.ReferenceType &&
                d.CatalogueName == input.CatalogueName);
            }
            if (predicate == null)
            {
                throw new UserFriendlyException("输入条件不能为空！");
            }
            return await (await GetDbSetAsync()).IncludeDetails(details).Where(predicate).OrderBy(d => d.SequenceNumber).ToListAsync();
        }
        public async Task DeleteRootCatalogueAsync(List<GetCatalogueInput> inputs)
        {
            var queryable = (await GetDbSetAsync()).AsQueryable();
            var predicate = PredicateBuilder.New<AttachCatalogue>(true);
            foreach (var input in inputs)
            {
                predicate = predicate?.Or(d =>
                d.ParentId == input.ParentId &&
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
        public async Task<int> GetMaxSequenceNumberByReferenceAsync(Guid? parentId, string reference, int referenceType)
        {
            return await (await GetDbSetAsync())
                .Where(d => d.ParentId == parentId && d.Reference == reference && d.ReferenceType == referenceType)
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
        public async Task<int> ByReferenceMaxSequenceAsync(
            string reference,
            int referenceType,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(u => u.Reference == reference && u.ReferenceType == referenceType)
                .Select(d => d.SequenceNumber)
                .DefaultIfEmpty()
                .MaxAsync(GetCancellationToken(cancellationToken));
        }
        public async Task<AttachCatalogue?> GetByFileIdAsync(Guid fileId, CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .FirstOrDefaultAsync(u => u.AttachFiles.Any(d => d.Id == fileId));
        }

        public async Task<List<AttachCatalogue>> SearchByFullTextAsync(
            string searchText,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            // 使用原生SQL查询以支持中文全文搜索和更好的性能
            var sql = @"
                SELECT c.*, ts_rank(to_tsvector('chinese', c.""CATALOGUENAME""), plainto_tsquery('chinese', @searchText)) as rank
                FROM ""ATTACH_CATALOGUES"" c
                WHERE to_tsvector('chinese', c.""CATALOGUENAME"") @@ plainto_tsquery('chinese', @searchText)";

            var parameters = new List<object> { new NpgsqlParameter("@searchText", searchText) };

            // 添加业务引用过滤
            if (!string.IsNullOrEmpty(reference))
            {
                sql += " AND c.\"REFERENCE\" = @reference";
                parameters.Add(new NpgsqlParameter("@reference", reference));
            }

            if (referenceType.HasValue)
            {
                sql += " AND c.\"REFERENCETYPE\" = @referenceType";
                parameters.Add(new NpgsqlParameter("@referenceType", referenceType.Value));
            }

            sql += " ORDER BY rank DESC LIMIT @limit";
            parameters.Add(new NpgsqlParameter("@limit", limit));

            // 执行原生SQL查询
            var dbContext = await GetDbContextAsync();
            var results = await dbContext.Set<AttachCatalogue>()
                .FromSqlRaw(sql, parameters.ToArray())
                .IncludeDetails()
                .ToListAsync(cancellationToken);

            return results;
        }

        public async Task<List<AttachCatalogue>> SearchBySemanticAsync(
            float[] queryEmbedding,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default)
        {
            // 使用原生SQL查询实现真正的向量相似度搜索
            var sql = @"
                SELECT c.*, 1 - (c.embedding <=> @queryEmbedding) as similarity_score
                FROM ""ATTACH_CATALOGUES"" c
                WHERE c.embedding IS NOT NULL";

            var parameters = new List<object>();

            // 添加业务引用过滤
            if (!string.IsNullOrEmpty(reference))
            {
                sql += " AND c.\"REFERENCE\" = @reference";
                parameters.Add(new NpgsqlParameter("@reference", reference));
            }

            if (referenceType.HasValue)
            {
                sql += " AND c.\"REFERENCETYPE\" = @referenceType";
                parameters.Add(new NpgsqlParameter("@referenceType", referenceType.Value));
            }

            // 添加相似度过滤 - 使用余弦相似度
            sql += " AND 1 - (c.embedding <=> @queryEmbedding) >= @similarityThreshold";

            // 按相似度排序并限制结果
            sql += " ORDER BY c.embedding <=> @queryEmbedding LIMIT @limit";

            // 添加参数
            parameters.Add(new NpgsqlParameter("@queryEmbedding", queryEmbedding));
            parameters.Add(new NpgsqlParameter("@similarityThreshold", similarityThreshold));
            parameters.Add(new NpgsqlParameter("@limit", limit));

            // 执行原生SQL查询
            var dbContext = await GetDbContextAsync();
            var results = await dbContext.Set<AttachCatalogue>()
                .FromSqlRaw(sql, parameters.ToArray())
                .IncludeDetails()
                .ToListAsync(cancellationToken);

            return results;
        }
    }
}