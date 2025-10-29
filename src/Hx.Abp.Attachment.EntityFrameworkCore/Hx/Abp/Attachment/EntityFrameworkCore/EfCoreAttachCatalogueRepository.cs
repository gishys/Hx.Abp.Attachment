using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using System.Linq.Dynamic.Core;
using System.Text;
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

        /// <summary>
        /// 全文检索：基于倒排索引的高性能全文搜索
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public async Task<List<AttachCatalogue>> SearchByFullTextAsync(
            string searchText,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    throw new UserFriendlyException("搜索文本不能为空");
                }

                var dbContext = await GetDbContextAsync();

                // 构建全文搜索查询
                var searchQuery = BuildFullTextQuery(searchText, true); // 默认启用前缀匹配
                var searchPattern = $"%{searchText}%";

                // 优化的全文搜索SQL
                var sql = @"
                    WITH fulltext_results AS (
                        -- 全文搜索匹配
                        SELECT 
                            c.*,
                            ts_rank_cd(
                                setweight(to_tsvector('chinese_fts', coalesce(c.""CATALOGUE_NAME"",'')), 'A') || 
                                setweight(to_tsvector('chinese_fts', coalesce(c.""REFERENCE"",'')), 'B') ||
                                setweight(to_tsvector('chinese_fts', coalesce(c.""FULL_TEXT_CONTENT"",'')), 'C'),
                                to_tsquery('chinese_fts', @searchQuery)
                            ) as fulltext_rank
                        FROM ""APPATTACH_CATALOGUES"" c
                        WHERE c.""IS_DELETED"" = false
                          AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                          AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                          AND (
                            setweight(to_tsvector('chinese_fts', coalesce(c.""CATALOGUE_NAME"",'')), 'A') ||
                              setweight(to_tsvector('chinese_fts', coalesce(c.""REFERENCE"",'')), 'B') ||
                              setweight(to_tsvector('chinese_fts', coalesce(c.""FULL_TEXT_CONTENT"",'')), 'C')
                        ) @@ to_tsquery('chinese_fts', @searchQuery)
                    ),
                    fuzzy_results AS (
                        -- 模糊搜索匹配（如果启用）
                        SELECT 
                            c.*,
                            GREATEST(
                                COALESCE(similarity(c.""CATALOGUE_NAME"", @searchText), 0) * 1.0,
                                COALESCE(similarity(c.""REFERENCE"", @searchText), 0) * 0.8
                            ) as fuzzy_score
                        FROM ""APPATTACH_CATALOGUES"" c
                        WHERE c.""IS_DELETED"" = false
                          AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                          AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                          AND (
                              c.""CATALOGUE_NAME"" ILIKE @searchPattern
                              OR c.""REFERENCE"" ILIKE @searchPattern
                              OR c.""FULL_TEXT_CONTENT"" ILIKE @searchPattern
                          )
                    ),
                    combined_results AS (
                        -- 合并全文搜索和模糊搜索结果
                        SELECT 
                            COALESCE(ft.""Id"", fz.""Id"") as ""Id"",
                            COALESCE(ft.""CATALOGUE_NAME"", fz.""CATALOGUE_NAME"") as ""CATALOGUE_NAME"",
                            COALESCE(ft.""REFERENCE"", fz.""REFERENCE"") as ""REFERENCE"",
                            COALESCE(ft.""REFERENCE_TYPE"", fz.""REFERENCE_TYPE"") as ""REFERENCE_TYPE"",
                            COALESCE(ft.""PARENT_ID"", fz.""PARENT_ID"") as ""PARENT_ID"",
                            COALESCE(ft.""SEQUENCE_NUMBER"", fz.""SEQUENCE_NUMBER"") as ""SEQUENCE_NUMBER"",
                            COALESCE(ft.""IS_DELETED"", fz.""IS_DELETED"") as ""IS_DELETED"",
                            COALESCE(ft.""CREATION_TIME"", fz.""CREATION_TIME"") as ""CREATION_TIME"",
                            COALESCE(ft.""CREATOR_ID"", fz.""CREATOR_ID"") as ""CREATOR_ID"",
                            COALESCE(ft.""LAST_MODIFICATION_TIME"", fz.""LAST_MODIFICATION_TIME"") as ""LAST_MODIFICATION_TIME"",
                            COALESCE(ft.""LAST_MODIFIER_ID"", fz.""LAST_MODIFIER_ID"") as ""LAST_MODIFIER_ID"",
                            COALESCE(ft.""TEXT_VECTOR"", fz.""TEXT_VECTOR"") as ""TEXT_VECTOR"",
                            COALESCE(ft.""VECTOR_DIMENSION"", fz.""VECTOR_DIMENSION"") as ""VECTOR_DIMENSION"",
                            COALESCE(ft.""FULL_TEXT_CONTENT"", fz.""FULL_TEXT_CONTENT"") as ""FULL_TEXT_CONTENT"",
                            COALESCE(ft.""FULL_TEXT_CONTENT_UPDATED_TIME"", fz.""FULL_TEXT_CONTENT_UPDATED_TIME"") as ""FULL_TEXT_CONTENT_UPDATED_TIME"",
                            COALESCE(ft.""ATTACH_COUNT"", fz.""ATTACH_COUNT"") as ""ATTACH_COUNT"",
                            COALESCE(ft.""ATTACH_RECEIVE_TYPE"", fz.""ATTACH_RECEIVE_TYPE"") as ""ATTACH_RECEIVE_TYPE"",
                            COALESCE(ft.""PAGE_COUNT"", fz.""PAGE_COUNT"") as ""PAGE_COUNT"",
                            COALESCE(ft.""IS_VERIFICATION"", fz.""IS_VERIFICATION"") as ""IS_VERIFICATION"",
                            COALESCE(ft.""VERIFICATION_PASSED"", fz.""VERIFICATION_PASSED"") as ""VERIFICATION_PASSED"",
                            COALESCE(ft.""IS_REQUIRED"", fz.""IS_REQUIRED"") as ""IS_REQUIRED"",
                            COALESCE(ft.""IS_STATIC"", fz.""IS_STATIC"") as ""IS_STATIC"",
                            COALESCE(ft.""TEMPLATE_ID"", fz.""TEMPLATE_ID"") as ""TEMPLATE_ID"",
                            COALESCE(ft.""CATALOGUE_FACET_TYPE"", fz.""CATALOGUE_FACET_TYPE"") as ""CATALOGUE_FACET_TYPE"",
                            COALESCE(ft.""CATALOGUE_PURPOSE"", fz.""CATALOGUE_PURPOSE"") as ""CATALOGUE_PURPOSE"",
                            COALESCE(ft.""TAGS"", fz.""TAGS"") as ""TAGS"",
                            COALESCE(ft.""PERMISSIONS"", fz.""PERMISSIONS"") as ""PERMISSIONS"",
                            COALESCE(ft.""META_FIELDS"", fz.""META_FIELDS"") as ""META_FIELDS"",
                            COALESCE(ft.""EXTRA_PROPERTIES"", fz.""EXTRA_PROPERTIES"") as ""EXTRA_PROPERTIES"",
                            COALESCE(ft.""CONCURRENCY_STAMP"", fz.""CONCURRENCY_STAMP"") as ""CONCURRENCY_STAMP"",
                            COALESCE(ft.""DELETER_ID"", fz.""DELETER_ID"") as ""DELETER_ID"",
                            COALESCE(ft.""DELETION_TIME"", fz.""DELETION_TIME"") as ""DELETION_TIME"",
                            COALESCE(ft.""IS_ARCHIVED"", fz.""IS_ARCHIVED"") as ""IS_ARCHIVED"",
                            COALESCE(ft.""PATH"", fz.""PATH"") as ""PATH"",
                            COALESCE(ft.""TEMPLATE_VERSION"", fz.""TEMPLATE_VERSION"") as ""TEMPLATE_VERSION"",
                            COALESCE(ft.""SUMMARY"", fz.""SUMMARY"") as ""SUMMARY"",
                            COALESCE(ft.""TEMPLATE_ROLE"", fz.""TEMPLATE_ROLE"") as ""TEMPLATE_ROLE"",
                            -- 综合评分：全文搜索分数 + 模糊搜索分数
                            COALESCE(ft.fulltext_rank, 0) + COALESCE(fz.fuzzy_score, 0) as final_score
                        FROM fulltext_results ft
                        FULL OUTER JOIN fuzzy_results fz ON ft.""Id"" = fz.""Id""
                    )
                    SELECT * FROM combined_results
                    WHERE final_score > 0
                    ORDER BY final_score DESC
                    LIMIT @limit";

                var parameters = new List<object>
                {
                    new NpgsqlParameter("@searchText", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchText },
                    new NpgsqlParameter("@searchQuery", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchQuery },
                    new NpgsqlParameter("@searchPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchPattern },
                    new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = limit }
                };

                // 添加业务引用过滤
                if (!string.IsNullOrEmpty(reference))
                {
                    parameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = reference });
                }
                else
                {
                    parameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
                }

                if (referenceType.HasValue)
                {
                    parameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = referenceType.Value });
                }
                else
                {
                    parameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = DBNull.Value });
                }

                var results = await dbContext.Set<AttachCatalogue>()
                    .FromSqlRaw(sql, [.. parameters])
                    .ToListAsync(cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(message: $"全文检索执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 构建全文搜索查询字符串
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="enablePrefix">是否启用前缀匹配</param>
        /// <returns>格式化的查询字符串</returns>
        private static string BuildFullTextQuery(string searchText, bool enablePrefix)
        {
            var terms = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (enablePrefix)
            {
                // 使用前缀匹配
                return string.Join(" & ", terms.Select(term => term + ":*"));
            }
            else
            {
                // 使用精确匹配
                return string.Join(" & ", terms);
            }
        }


        /// <summary>
        /// 混合检索：基于行业最佳实践的向量召回 + 全文检索加权过滤 + 分数融合
        /// </summary>
        /// <param name="searchText">搜索文本（可选）</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public async Task<List<AttachCatalogue>> SearchByHybridAsync(
            string? searchText = null,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            string? queryTextVector = null,
            float similarityThreshold = 0.7f,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dbContext = await GetDbContextAsync();

                // 如果没有提供搜索文本，根据其他条件进行查询
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // 检查是否有向量查询条件
                    var hasVectorQuery = !string.IsNullOrEmpty(queryTextVector);
                    // 检查是否有其他筛选条件
                    var hasOtherConditions = !string.IsNullOrEmpty(reference) || referenceType.HasValue;
                    
                    if (hasVectorQuery)
                    {
                        // 如果有向量查询，使用向量相似度搜索（即使没有文本查询）
                        // 这里需要执行向量相似度查询，但searchText为空
                        // 我们可以使用一个通用的搜索词或者直接进行向量匹配
                        var vectorSearchText = "vector_search"; // 使用占位符文本
                        var vectorSearchQuery = string.Join(" & ",
                            vectorSearchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(term => term + ":*"));

                        // 计算向量召回数量（通常为最终结果的2-3倍）
                        var vectorTopNForVector = Math.Max(limit * 2, 50);

                        // 混合检索架构：向量召回 + 全文检索加权过滤 + 分数融合
                        var vectorSql = @"
                            WITH vector_recall AS (
                                -- 第一阶段：向量召回 Top-N（语义检索）
                                SELECT 
                                    c.*,
                                    -- 向量相似度计算（如果有向量数据）
                                    CASE 
                                        WHEN c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true
                                        THEN COALESCE(
                                            -- 使用余弦相似度计算
                                            (
                                                SELECT SUM(a.val * b.val) / 
                                                       (SQRT(SUM(a.val * a.val)) * SQRT(SUM(b.val * b.val)))
                                                FROM (
                                                    SELECT unnest(c.""TEXT_VECTOR"") as val, 
                                                           generate_subscripts(c.""TEXT_VECTOR"", 1) as idx
                                                ) a
                                                JOIN (
                                                    SELECT unnest(@queryTextVector::double precision[]) as val,
                                                           generate_subscripts(@queryTextVector::double precision[], 1) as idx
                                                ) b ON a.idx = b.idx
                                            ), 0
                                        ) * @semanticWeight
                                        ELSE 0
                                    END as vector_score
                                FROM ""APPATTACH_CATALOGUES"" c
                                WHERE c.""IS_DELETED"" = false
                                  AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                                  AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                                  AND c.""TEXT_VECTOR"" IS NOT NULL
                                ORDER BY vector_score DESC
                                LIMIT @vectorTopN
                            ),
                            fulltext_scoring AS (
                                -- 第二阶段：全文检索加权过滤和重排
                                SELECT 
                                    vr.*,
                                    -- 全文检索分数计算（多字段加权评分）
                                    COALESCE(
                                        GREATEST(
                                            -- 分类名称匹配（权重最高）
                                            CASE WHEN vr.""CATALOGUE_NAME"" ILIKE @searchPattern 
                                                 THEN COALESCE(similarity(vr.""CATALOGUE_NAME"", @searchText), 0) * 1.0
                                                 ELSE 0 END,
                                            
                                            -- 引用字段匹配（权重较高）
                                            CASE WHEN vr.""REFERENCE"" IS NOT NULL AND vr.""REFERENCE"" ILIKE @searchPattern 
                                                 THEN COALESCE(similarity(vr.""REFERENCE"", @searchText), 0) * 0.8
                                                 ELSE 0 END,
                                            
                                            -- 全文搜索分数（权重中等）
                                            CASE WHEN (
                                                setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') ||
                                                setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B')
                                            ) @@ to_tsquery('chinese_fts', @searchQuery)
                                            THEN ts_rank_cd(
                                                setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') || 
                                                setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B'),
                                                to_tsquery('chinese_fts', @searchQuery)
                                            ) * 0.6
                                            ELSE 0 END
                                        ), 0
                                    ) as fulltext_score,
                                    
                                    -- 使用频率权重（基于文件数量）
                                    COALESCE(vr.""ATTACH_COUNT"", 0) * 0.05 as usage_score,
                                    
                                    -- 时间衰减权重（最近修改时间）
                                    CASE 
                                        WHEN vr.""LAST_MODIFICATION_TIME"" IS NOT NULL 
                                        THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""LAST_MODIFICATION_TIME"")) / (365 * 24 * 3600))
                                        ELSE 0 
                                    END as time_score
                                FROM vector_recall vr
                                WHERE (@hasTextVector::boolean = false OR vr.vector_score > @similarityThreshold)
                            ),
                            final_scoring AS (
                                -- 第三阶段：分数融合和最终排序
                                SELECT 
                                    fs.*,
                                    -- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
                                    (fs.vector_score + 
                                     fs.fulltext_score * @textWeight + 
                                     fs.usage_score + 
                                     fs.time_score) as final_score
                                FROM fulltext_scoring fs
                            )
                            SELECT 
                                c.*,
                                final_score, vector_score, fulltext_score, usage_score, time_score
                            FROM final_scoring fs
                            JOIN ""APPATTACH_CATALOGUES"" c ON c.""Id"" = fs.""Id""
                            ORDER BY final_score DESC
                            LIMIT @limit";

                        var vectorSearchPattern = $"%{vectorSearchText}%";
                        var vectorParameters = new List<object>
                        {
                            new NpgsqlParameter("@searchText", NpgsqlTypes.NpgsqlDbType.Text) { Value = vectorSearchText },
                            new NpgsqlParameter("@searchQuery", NpgsqlTypes.NpgsqlDbType.Text) { Value = vectorSearchQuery },
                            new NpgsqlParameter("@searchPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = vectorSearchPattern },
                            new NpgsqlParameter("@hasTextVector", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = !string.IsNullOrEmpty(queryTextVector) },
                            new NpgsqlParameter("@queryTextVector", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTextVector ?? "" },
                            new NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Real) { Value = similarityThreshold },
                            new NpgsqlParameter("@textWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = 0.4 }, // 默认文本权重
                            new NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = 0.6 }, // 默认语义权重
                            new NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = vectorTopNForVector },
                            new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = limit }
                        };

                        // 添加业务引用过滤
                        if (!string.IsNullOrEmpty(reference))
                        {
                            vectorParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = reference });
                        }
                        else
                        {
                            vectorParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
                        }

                        if (referenceType.HasValue)
                        {
                            vectorParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = referenceType.Value });
                        }
                        else
                        {
                            vectorParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = DBNull.Value });
                        }

                        var vectorResults = await dbContext.Set<AttachCatalogue>()
                            .FromSqlRaw(vectorSql, [.. vectorParameters])
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return vectorResults;
                    }
                    else if (hasOtherConditions)
                    {
                        // 使用其他条件进行筛选查询
                        var query = dbContext.Set<AttachCatalogue>()
                            .Where(c => c.IsDeleted == false);

                        if (!string.IsNullOrEmpty(reference))
                        {
                            query = query.Where(c => c.Reference == reference);
                        }

                        if (referenceType.HasValue)
                        {
                            query = query.Where(c => c.ReferenceType == referenceType.Value);
                        }

                        var noQueryTextResults = await query
                            .OrderByDescending(c => c.CreationTime)
                            .Take(limit)
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return noQueryTextResults;
                    }
                    else
                    {
                        // 如果所有条件都不存在，返回最新创建的limit行数据
                        var noQueryTextResults = await dbContext.Set<AttachCatalogue>()
                            .Where(c => c.IsDeleted == false)
                            .OrderByDescending(c => c.CreationTime)
                            .Take(limit)
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return noQueryTextResults;
                    }
                }

                // 计算向量召回数量（通常为最终结果的2-3倍）
                var vectorTopN = Math.Max(limit * 2, 50);

                // 转换搜索文本为tsquery格式
                var searchQuery = string.Join(" & ",
                    searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(term => term + ":*"));

                // 混合检索架构：向量召回 + 全文检索加权过滤 + 分数融合
                var sql = @"
                    WITH vector_recall AS (
                        -- 第一阶段：向量召回 Top-N（语义检索）
                        SELECT 
                            c.*,
                            -- 向量相似度计算（如果有向量数据）
                            CASE 
                                WHEN c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true
                                THEN COALESCE(
                                    -- 使用余弦相似度计算
                                    (
                                        SELECT SUM(a.val * b.val) / 
                                               (SQRT(SUM(a.val * a.val)) * SQRT(SUM(b.val * b.val)))
                                        FROM (
                                            SELECT unnest(c.""TEXT_VECTOR"") as val, 
                                                   generate_subscripts(c.""TEXT_VECTOR"", 1) as idx
                                        ) a
                                        JOIN (
                                            SELECT unnest(@queryTextVector::double precision[]) as val,
                                                   generate_subscripts(@queryTextVector::double precision[], 1) as idx
                                        ) b ON a.idx = b.idx
                                    ), 0
                                ) * @semanticWeight
                                ELSE 0
                            END as vector_score
                        FROM ""APPATTACH_CATALOGUES"" c
                        WHERE c.""IS_DELETED"" = false
                          AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                          AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                          AND (
                              -- 向量过滤条件：有向量数据或文本匹配
                              (c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true)
                              OR (
                            setweight(to_tsvector('chinese_fts', coalesce(c.""CATALOGUE_NAME"",'')), 'A') ||
                            setweight(to_tsvector('chinese_fts', coalesce(c.""REFERENCE"",'')), 'B')
                        ) @@ to_tsquery('chinese_fts', @searchQuery)
                              OR c.""CATALOGUE_NAME"" ILIKE @searchPattern
                              OR c.""REFERENCE"" ILIKE @searchPattern
                          )
                        ORDER BY vector_score DESC
                        LIMIT @vectorTopN
                    ),
                    fulltext_scoring AS (
                        -- 第二阶段：全文检索加权过滤和重排
                        SELECT 
                            vr.*,
                            -- 全文检索分数计算（多字段加权评分）
                            COALESCE(
                                GREATEST(
                                    -- 分类名称匹配（权重最高）
                                    CASE WHEN vr.""CATALOGUE_NAME"" ILIKE @searchPattern 
                                         THEN COALESCE(similarity(vr.""CATALOGUE_NAME"", @searchText), 0) * 1.0
                                         ELSE 0 END,
                                    
                                    -- 引用字段匹配（权重较高）
                                    CASE WHEN vr.""REFERENCE"" IS NOT NULL AND vr.""REFERENCE"" ILIKE @searchPattern 
                                         THEN COALESCE(similarity(vr.""REFERENCE"", @searchText), 0) * 0.8
                                         ELSE 0 END,
                                    
                                    -- 全文搜索分数（权重中等）
                                    CASE WHEN (
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') ||
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B')
                                    ) @@ to_tsquery('chinese_fts', @searchQuery)
                                    THEN ts_rank_cd(
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') || 
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B'),
                                        to_tsquery('chinese_fts', @searchQuery)
                                    ) * 0.6
                                    ELSE 0 END
                                ), 0
                            ) as fulltext_score,
                            
                            -- 使用频率权重（基于文件数量）
                            COALESCE(vr.""ATTACH_COUNT"", 0) * 0.05 as usage_score,
                            
                            -- 时间衰减权重（最近修改时间）
                            CASE 
                                WHEN vr.""LAST_MODIFICATION_TIME"" IS NOT NULL 
                                THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""LAST_MODIFICATION_TIME"")) / (365 * 24 * 3600))
                                ELSE 0 
                            END as time_score
                        FROM vector_recall vr
                        WHERE (@hasTextVector::boolean = false OR vr.vector_score > @similarityThreshold)
                    ),
                    final_scoring AS (
                        -- 第三阶段：分数融合和最终排序
                        SELECT 
                            fs.*,
                            -- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
                            (fs.vector_score + 
                             fs.fulltext_score * @textWeight + 
                             fs.usage_score + 
                             fs.time_score) as final_score
                        FROM fulltext_scoring fs
                    )
                    SELECT 
                        c.*,
                        final_score, vector_score, fulltext_score, usage_score, time_score
                    FROM final_scoring fs
                    JOIN ""APPATTACH_CATALOGUES"" c ON c.""Id"" = fs.""Id""
                    ORDER BY final_score DESC
                    LIMIT @limit";

                var searchPattern = $"%{searchText}%";
                var parameters = new List<object>
                {
                    new NpgsqlParameter("@searchText", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchText },
                    new NpgsqlParameter("@searchQuery", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchQuery },
                    new NpgsqlParameter("@searchPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchPattern },
                    new NpgsqlParameter("@hasTextVector", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = !string.IsNullOrEmpty(queryTextVector) },
                    new NpgsqlParameter("@queryTextVector", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTextVector ?? "" },
                    new NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Real) { Value = similarityThreshold },
                    new NpgsqlParameter("@textWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = 0.4 }, // 默认文本权重
                    new NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = 0.6 }, // 默认语义权重
                    new NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = vectorTopN },
                    new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = limit }
                };

                // 添加业务引用过滤
                if (!string.IsNullOrEmpty(reference))
                {
                    parameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = reference });
                }
                else
                {
                    parameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
                }

                if (referenceType.HasValue)
                {
                    parameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = referenceType.Value });
                }
                else
                {
                    parameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = DBNull.Value });
                }

                var results = await dbContext.Set<AttachCatalogue>()
                    .FromSqlRaw(sql, [.. parameters])
                    .IncludeDetails()
                    .ToListAsync(cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(message: $"混合检索执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 增强版混合检索：支持更灵活的配置和高级功能
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="textWeight">文本权重</param>
        /// <param name="semanticWeight">语义权重</param>
        /// <param name="enableVectorSearch">是否启用向量搜索</param>
        /// <param name="enablePrefixMatch">是否启用前缀匹配</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public async Task<List<AttachCatalogue>> SearchByHybridAdvancedAsync(
            string? searchText = null,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            string? queryTextVector = null,
            float similarityThreshold = 0.7f,
            double textWeight = 0.4,
            double semanticWeight = 0.6,
            bool enableVectorSearch = true,
            bool enablePrefixMatch = true,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dbContext = await GetDbContextAsync();

                // 如果没有提供搜索文本，根据其他条件进行查询
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // 检查是否有向量查询条件
                    var hasVectorQuery = !string.IsNullOrEmpty(queryTextVector);
                    // 检查是否有其他筛选条件
                    var hasOtherConditions = !string.IsNullOrEmpty(reference) || referenceType.HasValue;
                    
                    if (hasVectorQuery)
                    {
                        // 如果有向量查询，使用向量相似度搜索（即使没有文本查询）
                        // 这里需要执行向量相似度查询，但searchText为空
                        // 我们可以使用一个通用的搜索词或者直接进行向量匹配
                        var vectorSearchText = "vector_search"; // 使用占位符文本
                        var advancedSearchQuery = BuildFullTextQuery(vectorSearchText, enablePrefixMatch);
                        var advancedSearchPattern = $"%{vectorSearchText}%";
                        
                        // 计算向量召回数量
                        var advancedVectorTopN = Math.Max(limit * 2, 50);
                        
                        // 构建动态SQL查询进行向量搜索
                        var advancedSqlBuilder = new StringBuilder();
                        
                        // 第一阶段：向量召回（如果启用）
                        if (enableVectorSearch)
                        {
                            advancedSqlBuilder.AppendLine(@"
                                WITH vector_recall AS (
                                    -- 第一阶段：向量召回 Top-N（语义检索）
                                    SELECT 
                                        c.*,
                                        -- 向量相似度计算（如果有向量数据）
                                        CASE 
                                            WHEN c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true
                                            THEN COALESCE(
                                                -- 使用余弦相似度计算
                                                (
                                                    SELECT SUM(a.val * b.val) / 
                                                           (SQRT(SUM(a.val * a.val)) * SQRT(SUM(b.val * b.val)))
                                                    FROM (
                                                        SELECT unnest(c.""TEXT_VECTOR"") as val, 
                                                               generate_subscripts(c.""TEXT_VECTOR"", 1) as idx
                                                    ) a
                                                    JOIN (
                                                        SELECT unnest(@queryTextVector::double precision[]) as val,
                                                               generate_subscripts(@queryTextVector::double precision[], 1) as idx
                                                    ) b ON a.idx = b.idx
                                                ), 0
                                            ) * @semanticWeight
                                            ELSE 0
                                        END as vector_score
                                    FROM ""APPATTACH_CATALOGUES"" c
                                    WHERE c.""IS_DELETED"" = false
                                      AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                                      AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                                      AND c.""TEXT_VECTOR"" IS NOT NULL
                                    ORDER BY vector_score DESC
                                    LIMIT @vectorTopN
                                ),
                                final_scoring AS (
                                    -- 第二阶段：分数融合和最终排序
                                    SELECT 
                                        fs.*,
                                        -- 线性加权融合：向量分数 + 使用频率 + 时间衰减
                                        (fs.vector_score + 
                                         fs.usage_score + 
                                         fs.time_score) as final_score
                                    FROM (
                                        SELECT 
                                            vr.*,
                                            -- 使用频率权重（基于文件数量）
                                            COALESCE(vr.""ATTACH_COUNT"", 0) * 0.05 as usage_score,
                                            
                                            -- 时间衰减权重（最近修改时间）
                                            CASE 
                                                WHEN vr.""LAST_MODIFICATION_TIME"" IS NOT NULL 
                                                THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""LAST_MODIFICATION_TIME"")) / (365 * 24 * 3600))
                                                ELSE 0 
                                            END as time_score
                                        FROM vector_recall vr
                                        WHERE vr.vector_score > @similarityThreshold
                                    ) fs
                                )
                                SELECT 
                                    c.*,
                                    final_score, vector_score, usage_score, time_score
                                FROM final_scoring fs
                                JOIN ""APPATTACH_CATALOGUES"" c ON c.""Id"" = fs.""Id""
                                ORDER BY final_score DESC
                                LIMIT @limit");
                        }
                        else
                        {
                            // 如果不启用向量搜索，回退到普通查询
                            advancedSqlBuilder.AppendLine(@"
                                SELECT 
                                    c.*,
                                    0 as final_score, 0 as vector_score, 0 as usage_score, 0 as time_score
                                FROM ""APPATTACH_CATALOGUES"" c
                                WHERE c.""IS_DELETED"" = false
                                  AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                                  AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                                ORDER BY c.""CREATION_TIME"" DESC
                                LIMIT @limit");
                        }

                        var advancedSql = advancedSqlBuilder.ToString();
                        var advancedParameters = new List<object>
                        {
                            new NpgsqlParameter("@hasTextVector", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = !string.IsNullOrEmpty(queryTextVector) },
                            new NpgsqlParameter("@queryTextVector", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTextVector ?? "" },
                            new NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Real) { Value = similarityThreshold },
                            new NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = semanticWeight },
                            new NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = advancedVectorTopN },
                            new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = limit }
                        };

                        // 添加业务引用过滤
                        if (!string.IsNullOrEmpty(reference))
                        {
                            advancedParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = reference });
                        }
                        else
                        {
                            advancedParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
                        }

                        if (referenceType.HasValue)
                        {
                            advancedParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = referenceType.Value });
                        }
                        else
                        {
                            advancedParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = DBNull.Value });
                        }

                        var vectorResults = await dbContext.Set<AttachCatalogue>()
                            .FromSqlRaw(advancedSql, [.. advancedParameters])
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return vectorResults;
                    }
                    else if (hasOtherConditions)
                    {
                        // 使用其他条件进行筛选查询
                        var query = dbContext.Set<AttachCatalogue>()
                            .Where(c => c.IsDeleted == false);

                        if (!string.IsNullOrEmpty(reference))
                        {
                            query = query.Where(c => c.Reference == reference);
                        }

                        if (referenceType.HasValue)
                        {
                            query = query.Where(c => c.ReferenceType == referenceType.Value);
                        }

                        var noQueryTextResults = await query
                            .OrderByDescending(c => c.CreationTime)
                            .Take(limit)
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return noQueryTextResults;
                    }
                    else
                    {
                        // 如果所有条件都不存在，返回最新创建的limit行数据
                        var noQueryTextResults = await dbContext.Set<AttachCatalogue>()
                            .Where(c => c.IsDeleted == false)
                            .OrderByDescending(c => c.CreationTime)
                            .Take(limit)
                            .IncludeDetails()
                            .ToListAsync(cancellationToken);

                        return noQueryTextResults;
                    }
                }
                // 计算向量召回数量
                var vectorTopN = Math.Max(limit * 2, 50);

                // 构建搜索查询
                var normalSearchQuery = BuildFullTextQuery(searchText, enablePrefixMatch);
                var normalSearchPattern = $"%{searchText}%";

                // 构建动态SQL查询
                var normalSqlBuilder = new StringBuilder();

                // 第一阶段：向量召回（如果启用）
                if (enableVectorSearch)
                {
                    normalSqlBuilder.AppendLine(@"
                        WITH vector_recall AS (
                            -- 第一阶段：向量召回 Top-N（语义检索）
                            SELECT 
                                c.*,
                                -- 向量相似度计算（如果有向量数据）
                                CASE 
                                    WHEN c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true
                                    THEN COALESCE(
                                        -- 使用余弦相似度计算
                                        (
                                            SELECT SUM(a.val * b.val) / 
                                                   (SQRT(SUM(a.val * a.val)) * SQRT(SUM(b.val * b.val)))
                                            FROM (
                                                SELECT unnest(c.""TEXT_VECTOR"") as val, 
                                                       generate_subscripts(c.""TEXT_VECTOR"", 1) as idx
                                            ) a
                                            JOIN (
                                                SELECT unnest(@queryTextVector::double precision[]) as val,
                                                       generate_subscripts(@queryTextVector::double precision[], 1) as idx
                                            ) b ON a.idx = b.idx
                                        ), 0
                                    ) * @semanticWeight
                                    ELSE 0
                                END as vector_score
                            FROM ""APPATTACH_CATALOGUES"" c
                            WHERE c.""IS_DELETED"" = false
                              AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                              AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                              AND (
                                  -- 向量过滤条件：有向量数据或文本匹配
                                  (c.""TEXT_VECTOR"" IS NOT NULL AND @hasTextVector::boolean = true)
                                  OR (
                                      setweight(to_tsvector('chinese_fts', coalesce(c.""CATALOGUE_NAME"",'')), 'A') ||
                                      setweight(to_tsvector('chinese_fts', coalesce(c.""REFERENCE"",'')), 'B')
                                  ) @@ to_tsquery('chinese_fts', @searchQuery)
                                  OR c.""CATALOGUE_NAME"" ILIKE @searchPattern
                                  OR c.""REFERENCE"" ILIKE @searchPattern
                              )
                            ORDER BY vector_score DESC
                            LIMIT @vectorTopN
                        ),");
                }
                else
                {
                    // 如果不启用向量搜索，直接使用基础过滤
                    normalSqlBuilder.AppendLine(@"
                        WITH vector_recall AS (
                            SELECT 
                                c.*,
                                0 as vector_score
                            FROM ""APPATTACH_CATALOGUES"" c
                            WHERE c.""IS_DELETED"" = false
                              AND (@reference IS NULL OR c.""REFERENCE"" = @reference)
                              AND (@referenceType IS NULL OR c.""REFERENCE_TYPE"" = @referenceType)
                              AND (
                                  c.""CATALOGUE_NAME"" ILIKE @searchPattern
                                  OR c.""REFERENCE"" ILIKE @searchPattern
                                  OR c.""FULL_TEXT_CONTENT"" ILIKE @searchPattern
                              )
                            LIMIT @vectorTopN
                        ),");
                }

                // 第二阶段：全文检索加权过滤和重排
                normalSqlBuilder.AppendLine(@"
                    fulltext_scoring AS (
                        -- 第二阶段：全文检索加权过滤和重排
                        SELECT 
                            vr.*,
                            -- 全文检索分数计算（多字段加权评分）
                            COALESCE(
                                GREATEST(
                                    -- 分类名称匹配（权重最高）
                                    CASE WHEN vr.""CATALOGUE_NAME"" ILIKE @searchPattern 
                                         THEN COALESCE(similarity(vr.""CATALOGUE_NAME"", @searchText), 0) * 1.0
                                         ELSE 0 END,
                                    
                                    -- 引用字段匹配（权重较高）
                                    CASE WHEN vr.""REFERENCE"" IS NOT NULL AND vr.""REFERENCE"" ILIKE @searchPattern 
                                         THEN COALESCE(similarity(vr.""REFERENCE"", @searchText), 0) * 0.8
                                         ELSE 0 END,
                                    
                                    -- 全文搜索分数（权重中等）
                                    CASE WHEN (
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') ||
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B')
                                    ) @@ to_tsquery('chinese_fts', @searchQuery)
                                    THEN ts_rank_cd(
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""CATALOGUE_NAME"",'')), 'A') || 
                                        setweight(to_tsvector('chinese_fts', coalesce(vr.""REFERENCE"",'')), 'B'),
                                        to_tsquery('chinese_fts', @searchQuery)
                                    ) * 0.6
                                    ELSE 0 END
                                ), 0
                            ) as fulltext_score,
                            
                            -- 使用频率权重（基于文件数量）
                            COALESCE(vr.""ATTACH_COUNT"", 0) * 0.05 as usage_score,
                            
                            -- 时间衰减权重（最近修改时间）
                            CASE 
                                WHEN vr.""LAST_MODIFICATION_TIME"" IS NOT NULL 
                                THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""LAST_MODIFICATION_TIME"")) / (365 * 24 * 3600))
                                ELSE 0 
                            END as time_score
                        FROM vector_recall vr");

                // 添加相似度阈值过滤
                if (enableVectorSearch)
                {
                    normalSqlBuilder.AppendLine("                        WHERE (@hasTextVector::boolean = false OR vr.vector_score > @similarityThreshold)");
                }

                normalSqlBuilder.AppendLine(@"
                    ),
                    final_scoring AS (
                        -- 第三阶段：分数融合和最终排序
                        SELECT 
                            fs.*,
                            -- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
                            (fs.vector_score + 
                             fs.fulltext_score * @textWeight + 
                             fs.usage_score + 
                             fs.time_score) as final_score
                        FROM fulltext_scoring fs
                    )
                    SELECT 
                        c.*,
                        final_score, vector_score, fulltext_score, usage_score, time_score
                    FROM final_scoring fs
                    JOIN ""APPATTACH_CATALOGUES"" c ON c.""Id"" = fs.""Id""
                    ORDER BY final_score DESC
                    LIMIT @limit");

                var normalSql = normalSqlBuilder.ToString();
                var normalParameters = new List<object>
                {
                    new NpgsqlParameter("@searchText", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchText },
                    new NpgsqlParameter("@searchQuery", NpgsqlTypes.NpgsqlDbType.Text) { Value = normalSearchQuery },
                    new NpgsqlParameter("@searchPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = normalSearchPattern },
                    new NpgsqlParameter("@hasTextVector", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = !string.IsNullOrEmpty(queryTextVector) },
                    new NpgsqlParameter("@queryTextVector", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTextVector ?? "" },
                    new NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Real) { Value = similarityThreshold },
                    new NpgsqlParameter("@textWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = textWeight },
                    new NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = semanticWeight },
                    new NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = vectorTopN },
                    new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = limit }
                };

                // 添加业务引用过滤
                if (!string.IsNullOrEmpty(reference))
                {
                    normalParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = reference });
                }
                else
                {
                    normalParameters.Add(new NpgsqlParameter("@reference", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
                }

                if (referenceType.HasValue)
                {
                    normalParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = referenceType.Value });
                }
                else
                {
                    normalParameters.Add(new NpgsqlParameter("@referenceType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = DBNull.Value });
                }

                var results = await dbContext.Set<AttachCatalogue>()
                    .FromSqlRaw(normalSql, [.. normalParameters])
                    .IncludeDetails()
                    .ToListAsync(cancellationToken);

                return results;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(message: $"增强版混合检索执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据路径查找分类
        /// </summary>
        public virtual async Task<AttachCatalogue?> FindByPathAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            return await (await GetQueryableAsync())
                .Where(c => c.Path == path && !c.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 根据路径前缀查找子分类
        /// </summary>
        public virtual async Task<List<AttachCatalogue>> FindByPathPrefixAsync(
            string pathPrefix,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(pathPrefix))
            {
                query = query.Where(c => c.Path != null && c.Path.StartsWith(pathPrefix + "."));
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Path)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据路径深度查找分类
        /// </summary>
        public virtual async Task<List<AttachCatalogue>> FindByPathDepthAsync(
            int depth,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted);

            if (depth == 0)
            {
                // 根节点：路径为空或深度为1（使用EF.Functions.Like来避免Contains翻译问题）
                query = query.Where(c => c.Path == null || c.Path == "" || !EF.Functions.Like(c.Path, "%.%"));
            }
            else
            {
                // 对于深度大于0的情况，我们需要使用客户端评估来计算路径深度
                // 先获取所有可能的记录，然后在客户端过滤
                var allRecords = await query
                    .Where(c => c.Path != null && c.Path != "")
                    .ToListAsync(cancellationToken);

                // 在客户端计算路径深度并过滤
                var filteredRecords = allRecords
                    .Where(c => c.Path != null && c.Path.Split('.').Length == depth)
                    .AsQueryable();

                query = filteredRecords.AsQueryable();
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Path)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 查找根分类
        /// </summary>
        public virtual async Task<AttachCatalogue?> FindRootCataloguesAsync(
            string reference,
            TemplatePurpose purpose,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted)
                .Where(c => !string.IsNullOrEmpty(c.Path) && !EF.Functions.Like(c.Path, "%.%"));

            if (string.IsNullOrWhiteSpace(reference))
            {
                return null;
            }
            query = query.Where(c => c.Reference == reference);
            query = query.Where(c => c.CataloguePurpose == purpose);
            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Path)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// 查找叶子分类
        /// </summary>
        public virtual async Task<List<AttachCatalogue>> FindLeafCataloguesAsync(
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted)
                .Where(c => !c.Children.Any(child => !child.IsDeleted));

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Path)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据父路径查找直接子分类
        /// </summary>
        public virtual async Task<List<AttachCatalogue>> FindDirectChildrenByPathAsync(
            string parentPath,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted && c.Path != null && c.Path != "");

            if (string.IsNullOrWhiteSpace(parentPath))
            {
                // 查找根节点的直接子节点（使用EF.Functions.Like来避免Contains翻译问题）
                query = query.Where(c => !EF.Functions.Like(c.Path, "%.%"));
            }
            else
            {
                // 查找指定父路径的直接子节点
                var childPathPattern = parentPath + ".";
                query = query.Where(c => EF.Functions.Like(c.Path, childPathPattern + "%") && 
                    !EF.Functions.Like(c.Path, childPathPattern + "%.%"));
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Path)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取同级最大路径
        /// </summary>
        public virtual async Task<string?> GetMaxPathAtSameLevelAsync(
            string? parentPath = null,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted && c.Path != null && c.Path != "");

            if (string.IsNullOrEmpty(parentPath))
            {
                // 根级别：查找所有根节点（使用EF.Functions.Like来避免Contains翻译问题）
                query = query.Where(c => !EF.Functions.Like(c.Path, "%.%"));
            }
            else
            {
                // 子级别：查找指定父路径下的直接子节点
                var childPathPattern = parentPath + ".";
                query = query.Where(c => EF.Functions.Like(c.Path, childPathPattern + "%") && 
                    !EF.Functions.Like(c.Path, childPathPattern + "%.%"));
            }

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            // 获取最大路径
            var maxPath = await query
                .Select(c => c.Path)
                .OrderByDescending(path => path)
                .FirstOrDefaultAsync(cancellationToken);

            return maxPath;
        }

        /// <summary>
        /// 根据模板ID和版本查找分类
        /// </summary>
        public async Task<List<AttachCatalogue>> FindByTemplateAsync(
            Guid templateId,
            int? templateVersion = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetDbSetAsync())
                .Where(c => c.TemplateId == templateId);

            if (templateVersion.HasValue)
            {
                query = query.Where(c => c.TemplateVersion == templateVersion.Value);
            }

            return await query
                .OrderBy(c => c.CreationTime)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 根据模板ID查找所有版本的分类
        /// </summary>
        public async Task<List<AttachCatalogue>> FindByTemplateIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            return await (await GetDbSetAsync())
                .Where(c => c.TemplateId == templateId)
                .OrderBy(c => c.TemplateVersion)
                .ThenBy(c => c.CreationTime)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取分类树形结构（用于树状展示）
        /// 基于路径优化，提供高性能的树形查询
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践
        /// </summary>
        public async Task<List<AttachCatalogue>> GetCataloguesTreeAsync(
            string? reference = null,
            int? referenceType = null,
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null,
            bool includeChildren = true,
            bool includeFiles = false,
            string? fulltextQuery = null,
            Guid? templateId = null,
            int? templateVersion = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var dbSet = (await GetDbSetAsync()).IncludeDetails(includeFiles);
                
                // 基础过滤条件
                var baseFilter = dbSet.Where(c => !c.IsDeleted);
                
                // 业务引用过滤
                if (!string.IsNullOrEmpty(reference))
                    baseFilter = baseFilter.Where(c => c.Reference == reference);
                
                // 业务类型过滤
                if (referenceType.HasValue)
                    baseFilter = baseFilter.Where(c => c.ReferenceType == referenceType.Value);
                
                // 分类分面类型过滤
                if (catalogueFacetType.HasValue)
                    baseFilter = baseFilter.Where(c => c.CatalogueFacetType == catalogueFacetType.Value);
                
                // 分类用途过滤
                if (cataloguePurpose.HasValue)
                    baseFilter = baseFilter.Where(c => c.CataloguePurpose == cataloguePurpose.Value);

                // 模板ID过滤
                if (templateId.HasValue)
                    baseFilter = baseFilter.Where(c => c.TemplateId == templateId.Value);

                // 模板版本过滤
                if (templateVersion.HasValue)
                    baseFilter = baseFilter.Where(c => c.TemplateVersion == templateVersion.Value);

                // 全文检索过滤条件
                if (!string.IsNullOrWhiteSpace(fulltextQuery))
                {
                    // 使用原生SQL进行JSONB模糊匹配
                    var dbContext = await GetDbContextAsync();
                    var escapedQuery = fulltextQuery.Replace("'", "''"); // 防止SQL注入

                    baseFilter = baseFilter.Where(c =>
                        c.CatalogueName.Contains(fulltextQuery) ||
                        (c.FullTextContent != null && c.FullTextContent.Contains(fulltextQuery)) ||
                        (c.Summary != null && c.Summary.Contains(fulltextQuery)) ||
                        // Tags字段：使用JSONB的精确匹配
                        (c.Tags != null && EF.Functions.JsonContains(c.Tags, $"[{JsonConvert.SerializeObject(fulltextQuery)}]")) ||
                        // MetaFields字段：使用PostgreSQL原生JSONB操作符进行模糊匹配
                        (c.MetaFields != null && (
                            // 使用 @> 操作符进行精确匹配（保持向后兼容）
                            EF.Functions.JsonContains(c.MetaFields, $"[{{\"FieldName\":{JsonConvert.SerializeObject(fulltextQuery)}}}]") ||
                            EF.Functions.JsonContains(c.MetaFields, $"[{{\"DefaultValue\":{JsonConvert.SerializeObject(fulltextQuery)}}}]") ||
                            EF.Functions.JsonContains(c.MetaFields, $"[{{\"Tags\":{JsonConvert.SerializeObject(fulltextQuery)}}}]") ||
                            // 或者使用JSON路径表达式进行匹配
                            EF.Functions.JsonContains(c.MetaFields, $"[{JsonConvert.SerializeObject(fulltextQuery)}]")
                        ))
                    );
                }

                if (includeChildren)
                {
                    // 使用Path进行高效查询，避免递归Include
                    // 查询所有匹配条件的分类，然后通过路径构建树形结构
                    var matchedCatalogues = await baseFilter
                        .OrderBy(c => c.Path)
                        .ThenBy(c => c.SequenceNumber)
                        .ThenBy(c => c.CreationTime)
                        .ToListAsync(cancellationToken);

                    // 如果有全文检索条件，需要获取所有相关的父节点和子节点
                    var allCatalogues = matchedCatalogues;
                    if (!string.IsNullOrWhiteSpace(fulltextQuery))
                    {
                        // 优化方案：使用单次查询获取所有相关节点
                        allCatalogues = await GetAllNodesFromPathsAsync(matchedCatalogues, cancellationToken);
                    }

                    // 通过路径构建树形结构
                    return BuildTreeFromPath(allCatalogues);
                }
                else
                {
                    // 只查询根节点（Path为空或null，或者ParentId为null）
                    var rootCatalogues = await baseFilter
                        .Where(c => c.ParentId == null || c.Path == null || c.Path == "")
                        .OrderBy(c => c.SequenceNumber)
                        .ThenBy(c => c.CreationTime)
                        .ToListAsync(cancellationToken);

                    return rootCatalogues;
                }
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException($"获取分类树形结构失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过路径构建分类树形结构
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践
        /// </summary>
        private static List<AttachCatalogue> BuildTreeFromPath(List<AttachCatalogue> allCatalogues)
        {
            if (allCatalogues.Count == 0)
                return [];

            // 创建分类字典，便于快速查找
            var catalogueDict = allCatalogues.ToDictionary(c => c.Id, c => c);
            
            // 分离根节点和子节点
            var rootCatalogues = allCatalogues
                .Where(c => c.ParentId == null || string.IsNullOrEmpty(c.Path))
                .OrderBy(c => c.SequenceNumber)
                .ThenBy(c => c.CreationTime)
                .ToList();

            // 为每个分类构建子节点集合
            foreach (var catalogue in allCatalogues)
            {
                catalogue.Children?.Clear();
            }

            // 构建父子关系
            foreach (var catalogue in allCatalogues)
            {
                if (catalogue.ParentId.HasValue && catalogueDict.TryGetValue(catalogue.ParentId.Value, out var parent))
                {
                    // 如果父节点的Children为null，跳过添加（这种情况应该很少见）
                    parent.Children?.Add(catalogue);
                }
            }

            // 对每个节点的子节点进行排序
            foreach (var catalogue in allCatalogues)
            {
                if (catalogue.Children?.Count > 0)
                {
                    var sortedChildren = catalogue.Children.OrderBy(c => c.SequenceNumber).ThenBy(c => c.CreationTime).ToList();
                    catalogue.Children.Clear();
                    foreach (var child in sortedChildren)
                    {
                        catalogue.Children.Add(child);
                    }
                }
            }

            return rootCatalogues;
        }

        /// <summary>
        /// 从路径获取所有相关节点
        /// 当有全文检索条件时，需要获取所有相关的父节点和子节点
        /// </summary>
        private async Task<List<AttachCatalogue>> GetAllNodesFromPathsAsync(
            List<AttachCatalogue> matchedCatalogues, 
            CancellationToken cancellationToken)
        {
            if (matchedCatalogues.Count == 0)
                return [];

            var dbSet = await GetDbSetAsync();
            var allRelatedCatalogues = new List<AttachCatalogue>();
            var addedIds = new HashSet<Guid>(); // 使用HashSet进行O(1)查找

            // 收集所有匹配节点
            foreach (var catalogue in matchedCatalogues)
            {
                if (addedIds.Add(catalogue.Id)) // HashSet.Add返回true表示成功添加（不重复）
                {
                    allRelatedCatalogues.Add(catalogue);
                }
            }

            // 为每个匹配的节点，获取其所有父节点和子节点
            foreach (var catalogue in matchedCatalogues)
            {
                if (!string.IsNullOrEmpty(catalogue.Path))
                {
                    // 获取根节点：路径按'.'截取的第一部分
                    var rootPath = catalogue.Path.Split('.')[0];
                    
                    // 直接获取所有以根节点开头的分类（包括父节点和子节点）
                    var relatedCatalogues = await dbSet
                        .Where(c => c.Path != null && c.Path.StartsWith(rootPath) && !c.IsDeleted)
                        .ToListAsync(cancellationToken);
                    
                    // 高效去重：使用HashSet进行O(1)查找
                    foreach (var relatedCatalogue in relatedCatalogues)
                    {
                        if (addedIds.Add(relatedCatalogue.Id)) // O(1)时间复杂度
                        {
                            allRelatedCatalogues.Add(relatedCatalogue);
                        }
                    }
                }
            }

            // 按路径排序，确保层级关系正确
            return allRelatedCatalogues
                .OrderBy(c => c.Path)
                .ThenBy(c => c.SequenceNumber)
                .ThenBy(c => c.CreationTime)
                .ToList();
        }

        /// <summary>
        /// 根据分类ID及其所有子分类（基于Path路径查询）
        /// 一次性查询出指定分类及其所有层级的子分类，性能优化版本
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>分类及其所有子分类列表</returns>
        public virtual async Task<List<AttachCatalogue>> GetCatalogueWithAllChildrenAsync(Guid catalogueId, CancellationToken cancellationToken = default)
        {
            // 1. 首先获取指定分类
            var targetCatalogue = await GetAsync(catalogueId, cancellationToken: cancellationToken);
            if (targetCatalogue == null)
            {
                return [];
            }

            // 2. 基于Path路径一次性查询所有子分类
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted);

            if (string.IsNullOrEmpty(targetCatalogue.Path))
            {
                // 如果目标分类是根节点（Path为空），查询所有分类
                // 这种情况比较少见，但需要处理
                query = query.Where(c => c.Id == catalogueId || c.Path != null);
            }
            else
            {
                // 获取根节点：路径按'.'截取的第一部分
                var rootPath = targetCatalogue.Path.Split('.')[0];
                
                // 查询目标分类本身以及所有以根节点开头的分类
                // 这样可以获取整个分类树，包括所有分支
                query = query.Where(c =>
                    c.Id == catalogueId ||
                    (c.Path != null && c.Path.StartsWith(rootPath)));
            }

            // 3. 按路径排序，确保层级关系正确
            var allCatalogues = await query
                .OrderBy(c => c.Path)
                .ThenBy(c => c.SequenceNumber)
                .ThenBy(c => c.CreationTime)
                .ToListAsync(cancellationToken);

            return allCatalogues;
        }

        /// <summary>
        /// 根据归档状态查询分类
        /// </summary>
        /// <param name="isArchived">归档状态</param>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogue>> GetByArchivedStatusAsync(
            bool isArchived,
            string? reference = null,
            int? referenceType = null,
            CancellationToken cancellationToken = default)
        {
            var query = (await GetQueryableAsync())
                .Where(c => !c.IsDeleted && c.IsArchived == isArchived);

            if (!string.IsNullOrWhiteSpace(reference))
            {
                query = query.Where(c => c.Reference == reference);
            }

            if (referenceType.HasValue)
            {
                query = query.Where(c => c.ReferenceType == referenceType.Value);
            }

            return await query
                .OrderBy(c => c.Reference)
                .ThenBy(c => c.SequenceNumber)
                .ThenBy(c => c.CreationTime)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 批量设置归档状态
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="isArchived">归档状态</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>更新的记录数</returns>
        public virtual async Task<int> SetArchivedStatusAsync(
            List<Guid> catalogueIds,
            bool isArchived,
            CancellationToken cancellationToken = default)
        {
            if (catalogueIds == null || catalogueIds.Count == 0)
            {
                return 0;
            }

            var dbSet = await GetDbSetAsync();
            var catalogues = await dbSet
                .Where(c => catalogueIds.Contains(c.Id) && !c.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var catalogue in catalogues)
            {
                catalogue.SetIsArchived(isArchived);
            }

            await SaveChangesAsync(cancellationToken);
            return catalogues.Count;
        }
    }
}
