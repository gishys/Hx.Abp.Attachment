using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// 全文搜索仓储实现
    /// </summary>
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IFullTextSearchRepository))]
    public class FullTextSearchRepository(AttachmentDbContext context, ILogger<FullTextSearchRepository> logger) : IFullTextSearchRepository, ITransientDependency
    {
        private readonly AttachmentDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<FullTextSearchRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// 是否启用更改跟踪（实现 IRepository 接口要求）
        /// </summary>
        public bool? IsChangeTrackingEnabled => null;

        /// <summary>
        /// 全文搜索目录
        /// </summary>
        public async Task<List<AttachCatalogue>> SearchCataloguesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = @"
                SELECT * FROM ""APPATTACH_CATALOGUES"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""CATALOGUE_NAME"", '') || ' ' || 
                    COALESCE(""FULL_TEXT_CONTENT"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                ORDER BY ts_rank(
                    to_tsvector('chinese_fts', 
                        COALESCE(""CATALOGUE_NAME"", '') || ' ' || 
                        COALESCE(""FULL_TEXT_CONTENT"", '')
                    ), 
                    plainto_tsquery('chinese_fts', @query)
                ) DESC
            ";

            try
            {
                return await _context.AttachCatalogues
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全文搜索目录失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        public async Task<List<AttachFile>> SearchFilesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = @"
                SELECT * FROM ""APPATTACHFILE"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""FILEALIAS"", '') || ' ' || 
                    COALESCE(""OCR_CONTENT"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                ORDER BY ts_rank(
                    to_tsvector('chinese_fts', 
                        COALESCE(""FILEALIAS"", '') || ' ' || 
                        COALESCE(""OCR_CONTENT"", '')
                    ), 
                    plainto_tsquery('chinese_fts', @query)
                ) DESC
            ";

            try
            {
                return await _context.AttachFiles
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全文搜索文件失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 模糊搜索目录（优化版 - 支持长文本内容搜索）
        /// </summary>
        public async Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            // 多层次搜索策略：子字符串匹配 > 分词匹配 > 相似度匹配
            var sql = @"
                WITH search_results AS (
                    SELECT *,
                           CASE 
                               -- 1. 子字符串匹配（最高优先级）
                               WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                                    OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 3
                               -- 2. 分词匹配（中等优先级）
                               WHEN EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               ) OR EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               )
                               THEN 2
                               -- 3. 相似度匹配（最低优先级）
                               WHEN COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                                    OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05
                               THEN 1
                               ELSE 0
                           END as match_type,
                           GREATEST(
                               COALESCE(similarity(""CATALOGUE_NAME"", @query), 0),
                               COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0)
                           ) as similarity_score,
                           -- 计算匹配位置（越靠前分数越高）
                           CASE 
                               WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""CATALOGUE_NAME"", ''))) + 1)
                               WHEN LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""FULL_TEXT_CONTENT"", ''))) + 1)
                               ELSE 0
                           END as position_score
                    FROM ""APPATTACH_CATALOGUES""
                    WHERE 
                        -- 子字符串匹配
                        (LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                         OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern))
                        OR
                        -- 分词匹配
                        (EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ) OR EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ))
                        OR
                        -- 相似度匹配（降低阈值）
                        (COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                         OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05)
                )
                SELECT *,
                       (match_type * 10 + similarity_score + position_score) as final_score
                FROM search_results
                ORDER BY final_score DESC, similarity_score DESC
                LIMIT 50
            ";

            try
            {
                _logger.LogInformation("执行优化模糊搜索目录，查询：{query}", query);
                
                var results = await _context.AttachCatalogues
                    .FromSqlRaw(sql, 
                        new NpgsqlParameter("@query", query),
                        new NpgsqlParameter("@query_pattern", $"%{query}%"))
                    .ToListAsync();
                
                _logger.LogInformation("优化模糊搜索目录完成，找到 {count} 条结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化模糊搜索目录失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 模糊搜索文件（优化版 - 支持长文本内容搜索）
        /// </summary>
        public async Task<List<AttachFile>> FuzzySearchFilesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            // 多层次搜索策略：子字符串匹配 > 分词匹配 > 相似度匹配
            var sql = @"
                WITH search_results AS (
                    SELECT *,
                           CASE 
                               -- 1. 子字符串匹配（最高优先级）
                               WHEN LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                                    OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 3
                               -- 2. 分词匹配（中等优先级）
                               WHEN EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               ) OR EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               )
                               THEN 2
                               -- 3. 相似度匹配（最低优先级）
                               WHEN COALESCE(similarity(""FILEALIAS"", @query), 0) > 0.05
                                    OR COALESCE(similarity(""OCR_CONTENT"", @query), 0) > 0.05
                               THEN 1
                               ELSE 0
                           END as match_type,
                           GREATEST(
                               COALESCE(similarity(""FILEALIAS"", @query), 0),
                               COALESCE(similarity(""OCR_CONTENT"", @query), 0)
                           ) as similarity_score,
                           -- 计算匹配位置（越靠前分数越高）
                           CASE 
                               WHEN LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""FILEALIAS"", ''))) + 1)
                               WHEN LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""OCR_CONTENT"", ''))) + 1)
                               ELSE 0
                           END as position_score
                    FROM ""APPATTACHFILE""
                    WHERE 
                        -- 子字符串匹配
                        (LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                         OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern))
                        OR
                        -- 分词匹配
                        (EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ) OR EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ))
                        OR
                        -- 相似度匹配（降低阈值）
                        (COALESCE(similarity(""FILEALIAS"", @query), 0) > 0.05
                         OR COALESCE(similarity(""OCR_CONTENT"", @query), 0) > 0.05)
                )
                SELECT *,
                       (match_type * 10 + similarity_score + position_score) as final_score
                FROM search_results
                ORDER BY final_score DESC, similarity_score DESC
                LIMIT 50
            ";

            try
            {
                _logger.LogInformation("执行优化模糊搜索文件，查询：{query}", query);
                
                var results = await _context.AttachFiles
                    .FromSqlRaw(sql, 
                        new NpgsqlParameter("@query", query),
                        new NpgsqlParameter("@query_pattern", $"%{query}%"))
                    .ToListAsync();
                
                _logger.LogInformation("优化模糊搜索文件完成，找到 {count} 条结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化模糊搜索文件失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 组合搜索目录（全文 + 优化模糊搜索）
        /// </summary>
        public async Task<List<AttachCatalogue>> CombinedSearchCataloguesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = @"
                WITH search_results AS (
                    SELECT *,
                           -- 全文搜索排名
                           CASE 
                               WHEN to_tsvector('chinese_fts', 
                                   COALESCE(""CATALOGUE_NAME"", '') || ' ' || 
                                   COALESCE(""FULL_TEXT_CONTENT"", '')
                               ) @@ plainto_tsquery('chinese_fts', @query) 
                               THEN ts_rank(
                                   to_tsvector('chinese_fts', 
                                       COALESCE(""CATALOGUE_NAME"", '') || ' ' || 
                                       COALESCE(""FULL_TEXT_CONTENT"", '')
                                   ), 
                                   plainto_tsquery('chinese_fts', @query)
                               )
                               ELSE 0 
                           END as fulltext_rank,
                           -- 模糊搜索匹配类型
                           CASE 
                               -- 1. 子字符串匹配（最高优先级）
                               WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                                    OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 3
                               -- 2. 分词匹配（中等优先级）
                               WHEN EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               ) OR EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               )
                               THEN 2
                               -- 3. 相似度匹配（最低优先级）
                               WHEN COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                                    OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05
                               THEN 1
                               ELSE 0
                           END as fuzzy_match_type,
                           GREATEST(
                               COALESCE(similarity(""CATALOGUE_NAME"", @query), 0),
                               COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0)
                           ) as similarity_score,
                           -- 计算匹配位置（越靠前分数越高）
                           CASE 
                               WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""CATALOGUE_NAME"", ''))) + 1)
                               WHEN LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""FULL_TEXT_CONTENT"", ''))) + 1)
                               ELSE 0
                           END as position_score
                    FROM ""APPATTACH_CATALOGUES"" 
                    WHERE 
                        -- 全文搜索匹配
                        to_tsvector('chinese_fts', 
                            COALESCE(""CATALOGUE_NAME"", '') || ' ' || 
                            COALESCE(""FULL_TEXT_CONTENT"", '')
                        ) @@ plainto_tsquery('chinese_fts', @query)
                        OR
                        -- 子字符串匹配
                        (LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                         OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern))
                        OR
                        -- 分词匹配
                        (EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ) OR EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ))
                        OR
                        -- 相似度匹配（降低阈值）
                        (COALESCE(similarity(""CATALOGUE_NAME"", @query), 0) > 0.05
                         OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @query), 0) > 0.05)
                )
                SELECT *,
                       -- 综合评分：全文搜索权重更高，模糊搜索作为补充
                       (fulltext_rank * 100 + fuzzy_match_type * 10 + similarity_score + position_score) as final_score
                FROM search_results
                ORDER BY final_score DESC, fulltext_rank DESC, similarity_score DESC
                LIMIT 50
            ";

            try
            {
                _logger.LogInformation("执行优化组合搜索目录，查询：{query}", query);
                
                var results = await _context.AttachCatalogues
                    .FromSqlRaw(sql, 
                        new NpgsqlParameter("@query", query),
                        new NpgsqlParameter("@query_pattern", $"%{query}%"))
                    .ToListAsync();
                
                _logger.LogInformation("优化组合搜索目录完成，找到 {count} 条结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化组合搜索目录失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 组合搜索文件（全文 + 优化模糊搜索）
        /// </summary>
        public async Task<List<AttachFile>> CombinedSearchFilesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = @"
                WITH search_results AS (
                    SELECT *,
                           -- 全文搜索排名
                           CASE 
                               WHEN to_tsvector('chinese_fts', 
                                   COALESCE(""FILEALIAS"", '') || ' ' || 
                                   COALESCE(""OCR_CONTENT"", '')
                               ) @@ plainto_tsquery('chinese_fts', @query) 
                               THEN ts_rank(
                                   to_tsvector('chinese_fts', 
                                       COALESCE(""FILEALIAS"", '') || ' ' || 
                                       COALESCE(""OCR_CONTENT"", '')
                                   ), 
                                   plainto_tsquery('chinese_fts', @query)
                               )
                               ELSE 0 
                           END as fulltext_rank,
                           -- 模糊搜索匹配类型
                           CASE 
                               -- 1. 子字符串匹配（最高优先级）
                               WHEN LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                                    OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 3
                               -- 2. 分词匹配（中等优先级）
                               WHEN EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               ) OR EXISTS (
                                   SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                                   WHERE word LIKE LOWER(@query_pattern)
                               )
                               THEN 2
                               -- 3. 相似度匹配（最低优先级）
                               WHEN COALESCE(similarity(""FILEALIAS"", @query), 0) > 0.05
                                    OR COALESCE(similarity(""OCR_CONTENT"", @query), 0) > 0.05
                               THEN 1
                               ELSE 0
                           END as fuzzy_match_type,
                           GREATEST(
                               COALESCE(similarity(""FILEALIAS"", @query), 0),
                               COALESCE(similarity(""OCR_CONTENT"", @query), 0)
                           ) as similarity_score,
                           -- 计算匹配位置（越靠前分数越高）
                           CASE 
                               WHEN LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""FILEALIAS"", ''))) + 1)
                               WHEN LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern)
                               THEN 1.0 / (position(LOWER(@query) in LOWER(COALESCE(""OCR_CONTENT"", ''))) + 1)
                               ELSE 0
                           END as position_score
                    FROM ""APPATTACHFILE"" 
                    WHERE 
                        -- 全文搜索匹配
                        to_tsvector('chinese_fts', 
                            COALESCE(""FILEALIAS"", '') || ' ' || 
                            COALESCE(""OCR_CONTENT"", '')
                        ) @@ plainto_tsquery('chinese_fts', @query)
                        OR
                        -- 子字符串匹配
                        (LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                         OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern))
                        OR
                        -- 分词匹配
                        (EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ) OR EXISTS (
                            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                            WHERE word LIKE LOWER(@query_pattern)
                        ))
                        OR
                        -- 相似度匹配（降低阈值）
                        (COALESCE(similarity(""FILEALIAS"", @query), 0) > 0.05
                         OR COALESCE(similarity(""OCR_CONTENT"", @query), 0) > 0.05)
                )
                SELECT *,
                       -- 综合评分：全文搜索权重更高，模糊搜索作为补充
                       (fulltext_rank * 100 + fuzzy_match_type * 10 + similarity_score + position_score) as final_score
                FROM search_results
                ORDER BY final_score DESC, fulltext_rank DESC, similarity_score DESC
                LIMIT 50
            ";

            try
            {
                _logger.LogInformation("执行优化组合搜索文件，查询：{query}", query);
                
                var results = await _context.AttachFiles
                    .FromSqlRaw(sql, 
                        new NpgsqlParameter("@query", query),
                        new NpgsqlParameter("@query_pattern", $"%{query}%"))
                    .ToListAsync();
                
                _logger.LogInformation("优化组合搜索文件完成，找到 {count} 条结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "优化组合搜索文件失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        public async Task<string> TestFullTextSearchAsync(string testText)
        {
            try
            {
                var sql = @"
                    SELECT to_tsvector('chinese_fts', @testText) as result
                ";

                var result = await _context.Database
                    .SqlQueryRaw<string>(sql, new NpgsqlParameter("@testText", testText))
                    .FirstOrDefaultAsync();

                return result ?? "测试失败";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试全文搜索功能失败，测试文本：{testText}", testText);
                return $"测试失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 测试模糊搜索功能（优化版）
        /// </summary>
        public async Task<string> TestFuzzySearchAsync(string testText)
        {
            try
            {
                // 测试相似度函数
                var similaritySql = @"
                    SELECT similarity('测试中文', @testText) as similarity_result
                ";

                var similarityResult = await _context.Database
                    .SqlQueryRaw<decimal>(similaritySql, new NpgsqlParameter("@testText", testText))
                    .FirstOrDefaultAsync();

                // 测试目录表中的优化模糊搜索
                var catalogueSql = @"
                    WITH search_results AS (
                        SELECT *,
                               CASE 
                                   WHEN LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                                        OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern)
                                   THEN 3
                                   WHEN EXISTS (
                                       SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                                       WHERE word LIKE LOWER(@query_pattern)
                                   ) OR EXISTS (
                                       SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                                       WHERE word LIKE LOWER(@query_pattern)
                                   )
                                   THEN 2
                                   WHEN COALESCE(similarity(""CATALOGUE_NAME"", @testText), 0) > 0.05
                                        OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @testText), 0) > 0.05
                                   THEN 1
                                   ELSE 0
                               END as match_type,
                               GREATEST(
                                   COALESCE(similarity(""CATALOGUE_NAME"", @testText), 0),
                                   COALESCE(similarity(""FULL_TEXT_CONTENT"", @testText), 0)
                               ) as similarity_score
                        FROM ""APPATTACH_CATALOGUES""
                        WHERE 
                            (LOWER(COALESCE(""CATALOGUE_NAME"", '')) LIKE LOWER(@query_pattern) 
                             OR LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')) LIKE LOWER(@query_pattern))
                            OR
                            (EXISTS (
                                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""CATALOGUE_NAME"", '')), ' ')) word
                                WHERE word LIKE LOWER(@query_pattern)
                            ) OR EXISTS (
                                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FULL_TEXT_CONTENT"", '')), ' ')) word
                                WHERE word LIKE LOWER(@query_pattern)
                            ))
                            OR
                            (COALESCE(similarity(""CATALOGUE_NAME"", @testText), 0) > 0.05
                             OR COALESCE(similarity(""FULL_TEXT_CONTENT"", @testText), 0) > 0.05)
                    )
                    SELECT COUNT(*) as count, 
                           MAX(match_type) as max_match_type,
                           MAX(similarity_score) as max_similarity,
                           COUNT(CASE WHEN match_type = 3 THEN 1 END) as substring_matches,
                           COUNT(CASE WHEN match_type = 2 THEN 1 END) as word_matches,
                           COUNT(CASE WHEN match_type = 1 THEN 1 END) as similarity_matches
                    FROM search_results
                ";

                var catalogueResult = await _context.Database
                    .SqlQueryRaw<dynamic>(catalogueSql, 
                        new NpgsqlParameter("@testText", testText),
                        new NpgsqlParameter("@query_pattern", $"%{testText}%"))
                    .FirstOrDefaultAsync();

                // 测试文件表中的优化模糊搜索
                var fileSql = @"
                    WITH search_results AS (
                        SELECT *,
                               CASE 
                                   WHEN LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                                        OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern)
                                   THEN 3
                                   WHEN EXISTS (
                                       SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                                       WHERE word LIKE LOWER(@query_pattern)
                                   ) OR EXISTS (
                                       SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                                       WHERE word LIKE LOWER(@query_pattern)
                                   )
                                   THEN 2
                                   WHEN COALESCE(similarity(""FILEALIAS"", @testText), 0) > 0.05
                                        OR COALESCE(similarity(""OCR_CONTENT"", @testText), 0) > 0.05
                                   THEN 1
                                   ELSE 0
                               END as match_type,
                               GREATEST(
                                   COALESCE(similarity(""FILEALIAS"", @testText), 0),
                                   COALESCE(similarity(""OCR_CONTENT"", @testText), 0)
                               ) as similarity_score
                        FROM ""APPATTACHFILE""
                        WHERE 
                            (LOWER(COALESCE(""FILEALIAS"", '')) LIKE LOWER(@query_pattern) 
                             OR LOWER(COALESCE(""OCR_CONTENT"", '')) LIKE LOWER(@query_pattern))
                            OR
                            (EXISTS (
                                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""FILEALIAS"", '')), ' ')) word
                                WHERE word LIKE LOWER(@query_pattern)
                            ) OR EXISTS (
                                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(""OCR_CONTENT"", '')), ' ')) word
                                WHERE word LIKE LOWER(@query_pattern)
                            ))
                            OR
                            (COALESCE(similarity(""FILEALIAS"", @testText), 0) > 0.05
                             OR COALESCE(similarity(""OCR_CONTENT"", @testText), 0) > 0.05)
                    )
                    SELECT COUNT(*) as count, 
                           MAX(match_type) as max_match_type,
                           MAX(similarity_score) as max_similarity,
                           COUNT(CASE WHEN match_type = 3 THEN 1 END) as substring_matches,
                           COUNT(CASE WHEN match_type = 2 THEN 1 END) as word_matches,
                           COUNT(CASE WHEN match_type = 1 THEN 1 END) as similarity_matches
                    FROM search_results
                ";

                var fileResult = await _context.Database
                    .SqlQueryRaw<dynamic>(fileSql, 
                        new NpgsqlParameter("@testText", testText),
                        new NpgsqlParameter("@query_pattern", $"%{testText}%"))
                    .FirstOrDefaultAsync();

                return $"优化模糊搜索测试结果:\n" +
                       $"相似度函数测试: {similarityResult}\n" +
                       $"目录表总匹配数量: {catalogueResult?.count}\n" +
                       $"  子字符串匹配: {catalogueResult?.substring_matches}\n" +
                       $"  分词匹配: {catalogueResult?.word_matches}\n" +
                       $"  相似度匹配: {catalogueResult?.similarity_matches}\n" +
                       $"  最大相似度: {catalogueResult?.max_similarity}\n" +
                       $"文件表总匹配数量: {fileResult?.count}\n" +
                       $"  子字符串匹配: {fileResult?.substring_matches}\n" +
                       $"  分词匹配: {fileResult?.word_matches}\n" +
                       $"  相似度匹配: {fileResult?.similarity_matches}\n" +
                       $"  最大相似度: {fileResult?.max_similarity}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试优化模糊搜索功能失败，测试文本：{testText}", testText);
                return $"优化模糊搜索测试失败: {ex.Message}";
            }
        }
    }
}
