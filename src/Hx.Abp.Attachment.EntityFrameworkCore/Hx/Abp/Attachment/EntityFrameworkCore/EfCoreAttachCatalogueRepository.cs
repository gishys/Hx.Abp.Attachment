using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
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
                            COALESCE(ft.""Id"", fz.""Id"") as ""ID"",
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
                    .IncludeDetails()
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
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>匹配的分类列表</returns>
        public async Task<List<AttachCatalogue>> SearchByHybridAsync(
            string searchText,
            string? reference = null,
            int? referenceType = null,
            int limit = 10,
            string? queryTextVector = null,
            float similarityThreshold = 0.7f,
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
                        ""Id"", ""CATALOGUE_NAME"", ""REFERENCE"", ""REFERENCE_TYPE"", ""PARENT_ID"",
                        ""SEQUENCE_NUMBER"", ""IS_DELETED"", ""CREATION_TIME"", ""CREATOR_ID"",
                        ""LAST_MODIFICATION_TIME"", ""LAST_MODIFIER_ID"", ""TEXT_VECTOR"", ""VECTOR_DIMENSION"",
                        ""FULL_TEXT_CONTENT"", ""FULL_TEXT_CONTENT_UPDATED_TIME"", ""ATTACH_COUNT"",
                        ""ATTACH_RECEIVE_TYPE"", ""PAGE_COUNT"", ""IS_VERIFICATION"", ""VERIFICATION_PASSED"",
                        ""IS_REQUIRED"", ""IS_STATIC"", ""TEMPLATE_ID"", ""CATALOGUE_FACET_TYPE"", ""CATALOGUE_PURPOSE"",
                        ""TAGS"", ""PERMISSIONS"", ""META_FIELDS"", ""EXTRA_PROPERTIES"", ""CONCURRENCY_STAMP"", ""DELETER_ID"", ""DELETION_TIME"",
                        final_score, vector_score, fulltext_score, usage_score, time_score
                    FROM final_scoring
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
            string searchText,
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
                // 参数验证
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    throw new UserFriendlyException("搜索文本不能为空");
                }

                var dbContext = await GetDbContextAsync();

                // 计算向量召回数量
                var vectorTopN = Math.Max(limit * 2, 50);

                // 构建搜索查询
                var searchQuery = BuildFullTextQuery(searchText, enablePrefixMatch);
                var searchPattern = $"%{searchText}%";

                // 构建动态SQL查询
                var sqlBuilder = new StringBuilder();

                // 第一阶段：向量召回（如果启用）
                if (enableVectorSearch)
                {
                    sqlBuilder.AppendLine(@"
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
                    sqlBuilder.AppendLine(@"
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
                sqlBuilder.AppendLine(@"
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
                    sqlBuilder.AppendLine("                        WHERE (@hasTextVector::boolean = false OR vr.vector_score > @similarityThreshold)");
                }

                sqlBuilder.AppendLine(@"
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
                        ""Id"", ""CATALOGUE_NAME"", ""REFERENCE"", ""REFERENCE_TYPE"", ""PARENT_ID"",
                        ""SEQUENCE_NUMBER"", ""IS_DELETED"", ""CREATION_TIME"", ""CREATOR_ID"",
                        ""LAST_MODIFICATION_TIME"", ""LAST_MODIFIER_ID"", ""TEXT_VECTOR"", ""VECTOR_DIMENSION"",
                        ""FULL_TEXT_CONTENT"", ""FULL_TEXT_CONTENT_UPDATED_TIME"", ""ATTACH_COUNT"",
                        ""ATTACH_RECEIVE_TYPE"", ""PAGE_COUNT"", ""IS_VERIFICATION"", ""VERIFICATION_PASSED"",
                        ""IS_REQUIRED"", ""IS_STATIC"", ""TEMPLATE_ID"", ""CATALOGUE_FACET_TYPE"", ""CATALOGUE_PURPOSE"",
                        ""TAGS"", ""PERMISSIONS"", ""META_FIELDS"", ""EXTRA_PROPERTIES"", ""CONCURRENCY_STAMP"", ""DELETER_ID"", ""DELETION_TIME"",
                        final_score, vector_score, fulltext_score, usage_score, time_score
                    FROM final_scoring
                    ORDER BY final_score DESC
                    LIMIT @limit");

                var sql = sqlBuilder.ToString();
                var parameters = new List<object>
                {
                    new NpgsqlParameter("@searchText", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchText },
                    new NpgsqlParameter("@searchQuery", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchQuery },
                    new NpgsqlParameter("@searchPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchPattern },
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
                throw new UserFriendlyException(message: $"增强版混合检索执行失败: {ex.Message}");
            }
        }
    }
}
