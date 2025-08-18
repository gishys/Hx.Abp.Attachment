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

            var sql = $@"
                SELECT * FROM ""{nameof(_context.AttachCatalogues)}"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""{nameof(AttachCatalogue.CatalogueName)}"", '') || ' ' || 
                    COALESCE(""{nameof(AttachCatalogue.FullTextContent)}"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                ORDER BY ts_rank(
                    to_tsvector('chinese_fts', 
                        COALESCE(""{nameof(AttachCatalogue.CatalogueName)}"", '') || ' ' || 
                        COALESCE(""{nameof(AttachCatalogue.FullTextContent)}"", '')
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

            var sql = $@"
                SELECT * FROM ""{nameof(_context.AttachFiles)}"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""{nameof(AttachFile.FileName)}"", '') || ' ' || 
                    COALESCE(""{nameof(AttachFile.OcrContent)}"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                ORDER BY ts_rank(
                    to_tsvector('chinese_fts', 
                        COALESCE(""{nameof(AttachFile.FileName)}"", '') || ' ' || 
                        COALESCE(""{nameof(AttachFile.OcrContent)}"", '')
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
        /// 模糊搜索目录
        /// </summary>
        public async Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = $@"
                SELECT * FROM ""{nameof(_context.AttachCatalogues)}"" 
                WHERE ""{nameof(AttachCatalogue.CatalogueName)}"" % @query
                ORDER BY similarity(""{nameof(AttachCatalogue.CatalogueName)}"", @query) DESC
            ";

            try
            {
                return await _context.AttachCatalogues
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模糊搜索目录失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        public async Task<List<AttachFile>> FuzzySearchFilesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var sql = $@"
                SELECT * FROM ""{nameof(_context.AttachFiles)}"" 
                WHERE ""{nameof(AttachFile.FileName)}"" % @query
                ORDER BY similarity(""{nameof(AttachFile.FileName)}"", @query) DESC
            ";

            try
            {
                return await _context.AttachFiles
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模糊搜索文件失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        public async Task<List<AttachCatalogue>> CombinedSearchCataloguesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var catalogueName = nameof(AttachCatalogue.CatalogueName);
            var fullTextContent = nameof(AttachCatalogue.FullTextContent);
            var sql = $@"
                SELECT DISTINCT * FROM ""{nameof(_context.AttachCatalogues)}"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""{catalogueName}"", '') || ' ' || 
                    COALESCE(""{fullTextContent}"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                   OR ""{catalogueName}"" % @query
                ORDER BY 
                    CASE 
                        WHEN to_tsvector('chinese_fts', 
                            COALESCE(""{catalogueName}"", '') || ' ' || 
                            COALESCE(""{fullTextContent}"", '')
                        ) @@ plainto_tsquery('chinese_fts', @query) 
                        THEN ts_rank(
                            to_tsvector('chinese_fts', 
                                COALESCE(""{catalogueName}"", '') || ' ' || 
                                COALESCE(""{fullTextContent}"", '')
                            ), 
                            plainto_tsquery('chinese_fts', @query)
                        )
                        ELSE 0 
                    END DESC,
                    similarity(""{catalogueName}"", @query) DESC
            ";

            try
            {
                return await _context.AttachCatalogues
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "组合搜索目录失败，查询语句：{sql}，参数：{query}", sql, query);
                return [];
            }
        }

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        public async Task<List<AttachFile>> CombinedSearchFilesAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var fileName = nameof(AttachFile.FileName);
            var ocrContent = nameof(AttachFile.OcrContent);
            var sql = $@"
                SELECT DISTINCT * FROM ""{nameof(_context.AttachFiles)}"" 
                WHERE to_tsvector('chinese_fts', 
                    COALESCE(""{fileName}"", '') || ' ' || 
                    COALESCE(""{ocrContent}"", '')
                ) @@ plainto_tsquery('chinese_fts', @query)
                   OR ""{fileName}"" % @query
                ORDER BY 
                    CASE 
                        WHEN to_tsvector('chinese_fts', 
                            COALESCE(""{fileName}"", '') || ' ' || 
                            COALESCE(""{ocrContent}"", '')
                        ) @@ plainto_tsquery('chinese_fts', @query) 
                        THEN ts_rank(
                            to_tsvector('chinese_fts', 
                                COALESCE(""{fileName}"", '') || ' ' || 
                                COALESCE(""{ocrContent}"", '')
                            ), 
                            plainto_tsquery('chinese_fts', @query)
                        )
                        ELSE 0 
                    END DESC,
                    similarity(""{fileName}"", @query) DESC
            ";

            try
            {
                return await _context.AttachFiles
                    .FromSqlRaw(sql, new NpgsqlParameter("@query", query))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "组合搜索文件失败，查询语句：{sql}，参数：{query}", sql, query);
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

                // 替换 Npgsql.Parameter 为 Npgsql.NpgsqlParameter
                var result = await _context.Database
                    .SqlQueryRaw<string>(sql, new Npgsql.NpgsqlParameter("@testText", testText))
                    .FirstOrDefaultAsync();

                return result ?? "测试失败";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "测试全文搜索功能失败，测试文本：{testText}", testText);
                return $"测试失败: {ex.Message}";
            }
        }
    }
}
