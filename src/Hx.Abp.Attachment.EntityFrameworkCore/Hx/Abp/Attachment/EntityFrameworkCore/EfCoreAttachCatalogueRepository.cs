using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
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
            queryable = queryable.Where(predicate!);
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
            queryable = queryable.Where(predicate!);
            var ids = await queryable.Select(d => d.Id)
            .ToListAsync(cancellationToken);
            await DeleteManyAsync(ids, cancellationToken: cancellationToken);
        }
        public async Task<List<AttachCatalogue>> VerifyUploadAsync(
            List<GetAttachListInput> inputs,
            bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            var queryable = (await GetDbSetAsync()).IncludeDetails(includeDetails);
            var predicate = GetCataloguesByReference(inputs);
            queryable = queryable.Where(predicate!);
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
            return await (await GetDbSetAsync())
                .IncludeDetails()
                .FirstOrDefaultAsync(u => u.ParentId == parentId &&
                u.CatalogueName == catalogueName &&
                u.Reference == reference &&
                u.ReferenceType == referenceType);
        }

        /// <summary>
        /// 根据引用和名称查找分类
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="catalogueName">分类名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类</returns>
        public async Task<AttachCatalogue?> FindByReferenceAndNameAsync(
            string reference,
            int referenceType,
            string catalogueName,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .IncludeDetails()
                .FirstOrDefaultAsync(u => u.Reference == reference &&
                u.ReferenceType == referenceType &&
                u.CatalogueName == catalogueName,
                GetCancellationToken(cancellationToken));
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
            return await (await GetDbSetAsync()).IncludeDetails(details).Where(predicate!).OrderBy(d => d.SequenceNumber).ToListAsync();
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
            queryable = queryable.Where(predicate!);
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
        private static ExpressionStarter<AttachCatalogue> GetCataloguesByReference(List<GetAttachListInput> inputs)
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
                .FirstOrDefaultAsync(u => u.AttachFiles.Any(d => d.Id == fileId), cancellationToken: cancellationToken);
        }

        // 当前已有的全文搜索方法保持不变
        public async Task<List<AttachCatalogue>> SearchByFullTextAsync(
            string searchText,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            // 保持现有实现不变
            try
            {
                // 基础SQL，仅包含全文搜索
                var sql = @"
                    WITH RankedResults AS (
                        SELECT c.*, 
                            ts_rank_cd(
                                setweight(to_tsvector('chinese', coalesce(c.""CATALOGUE_NAME"",'')), 'A') || 
                                setweight(to_tsvector('chinese', coalesce(c.""REFERENCE"",'')), 'B'),
                                to_tsquery('chinese', @searchQuery)
                            ) as rank
                        FROM ""ATTACH_CATALOGUES"" c
                        WHERE (
                            setweight(to_tsvector('chinese', coalesce(c.""CATALOGUE_NAME"",'')), 'A') ||
                            setweight(to_tsvector('chinese', coalesce(c.""REFERENCE"",'')), 'B')
                        ) @@ to_tsquery('chinese', @searchQuery)
                    )
                    SELECT * FROM RankedResults 
                    WHERE rank > 0";

                // 转换搜索文本为tsquery格式
                var searchQuery = string.Join(" & ", 
                    searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(term => term + ":*")); // 使用前缀匹配

                var parameters = new List<object> { 
                    new NpgsqlParameter("@searchQuery", searchQuery)
                };

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

                // 按全文搜索分数降序排列并限制结果数量
                sql += @" 
                    ORDER BY rank DESC
                    LIMIT @limit";
                
                parameters.Add(new NpgsqlParameter("@limit", limit));

                var dbContext = await GetDbContextAsync();
                var results = await dbContext.Set<AttachCatalogue>()
                    .FromSqlRaw(sql, [.. parameters])
                    .IncludeDetails()
                    .ToListAsync(cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("全文搜索执行失败", ex.Message);
            }
        }

        // 当前已有的语义搜索方法保持不变
        public async Task<List<AttachCatalogue>> SearchBySemanticAsync(
            float[] queryEmbedding,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default)
        {
            // 保持现有实现不变
            var sql = @"
                SELECT c.*, 1 - (c.embedding <=> @queryEmbedding) as similarity_score
                FROM ""ATTACH_CATALOGUES"" c
                WHERE c.embedding IS NOT NULL";

            var parameters = new List<object> {
                new NpgsqlParameter("@queryEmbedding", queryEmbedding) 
                { NpgsqlDbType = NpgsqlDbType.Real | NpgsqlDbType.Array }
            };

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

            // 添加相似度过滤和排序
            sql += @" 
                AND 1 - (c.embedding <=> @queryEmbedding) >= @similarityThreshold
                ORDER BY c.embedding <=> @queryEmbedding
                LIMIT @limit";

            parameters.Add(new NpgsqlParameter("@similarityThreshold", similarityThreshold));
            parameters.Add(new NpgsqlParameter("@limit", limit));

            var dbContext = await GetDbContextAsync();
            var results = await dbContext.Set<AttachCatalogue>()
                .FromSqlRaw(sql, [.. parameters])
                .IncludeDetails()
                .ToListAsync(cancellationToken);

            return results;
        }

        /// <summary>
        /// 混合搜索：结合全文检索和语义检索
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryEmbedding">语义向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public async Task<List<AttachCatalogue>> SearchByHybridAsync(
            string searchText,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            float[]? queryEmbedding = null,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 基础SQL，包含全文搜索和向量相似度计算
                var sql = @"
                    WITH RankedResults AS (
                        SELECT c.*, 
                            CASE 
                                WHEN @hasEmbedding::boolean = true THEN
                                    (1 - (c.embedding <=> @queryEmbedding::real[])) * 0.6 + -- 向量相似度权重60%
                                    ts_rank_cd(
                                        setweight(to_tsvector('chinese', coalesce(c.""CATALOGUE_NAME"",'')), 'A') || 
                                        setweight(to_tsvector('chinese', coalesce(c.""REFERENCE"",'')), 'B'),
                                        to_tsquery('chinese', @searchQuery)
                                    ) * 0.4 -- 全文搜索权重40%
                                ELSE
                                    ts_rank_cd(
                                        setweight(to_tsvector('chinese', coalesce(c.""CATALOGUE_NAME"",'')), 'A') || 
                                        setweight(to_tsvector('chinese', coalesce(c.""REFERENCE"",'')), 'B'),
                                        to_tsquery('chinese', @searchQuery)
                                    )
                            END as rank
                        FROM ""ATTACH_CATALOGUES"" c
                        WHERE (
                            setweight(to_tsvector('chinese', coalesce(c.""CATALOGUE_NAME"",'')), 'A') ||
                            setweight(to_tsvector('chinese', coalesce(c.""REFERENCE"",'')), 'B')
                        ) @@ to_tsquery('chinese', @searchQuery)
                        AND (@hasEmbedding::boolean = false OR 
                             (c.embedding IS NOT NULL AND 1 - (c.embedding <=> @queryEmbedding::real[]) >= @similarityThreshold))
                    )
                    SELECT * FROM RankedResults 
                    WHERE rank > 0";

                // 转换搜索文本为tsquery格式
                var searchQuery = string.Join(" & ", 
                    searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(term => term + ":*")); // 使用前缀匹配

                var parameters = new List<object> { 
                    new NpgsqlParameter("@searchQuery", searchQuery),
                    new NpgsqlParameter("@hasEmbedding", queryEmbedding != null),
                    new NpgsqlParameter("@queryEmbedding", 
                        queryEmbedding ?? []) { NpgsqlDbType = NpgsqlDbType.Real | NpgsqlDbType.Array },
                    new NpgsqlParameter("@similarityThreshold", similarityThreshold)
                };

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

                // 按综合排序分数降序排列并限制结果数量
                sql += @" 
                    ORDER BY rank DESC
                    LIMIT @limit";
                
                parameters.Add(new NpgsqlParameter("@limit", limit));

                var dbContext = await GetDbContextAsync();
                var results = await dbContext.Set<AttachCatalogue>()
                    .FromSqlRaw(sql, [.. parameters])
                    .IncludeDetails()
                    .ToListAsync(cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException("混合搜索执行失败", ex.Message);
            }
        }
    }
}

//-- 需要添加向量运算符支持
//CREATE EXTENSION IF NOT EXISTS vector;

//-- 创建混合索引
//CREATE INDEX idx_attach_catalogues_hybrid ON "ATTACH_CATALOGUES" USING gin(
//    (
//        setweight(to_tsvector('chinese', coalesce("CATALOGUE_NAME",'')), 'A') ||
//        setweight(to_tsvector('chinese', coalesce("REFERENCE",'')), 'B')
//    )
//);
//CREATE INDEX idx_attach_catalogues_embedding ON "ATTACH_CATALOGUES" USING ivfflat (embedding vector_cosine_ops);
