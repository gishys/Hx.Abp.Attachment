using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using System.Linq.Dynamic.Core;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Guids;
using System.Dynamic;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachCatalogueTemplateRepository(
        IDbContextProvider<AttachmentDbContext> dbContextProvider,
        ISemanticMatcher semanticMatcher,
        IRulesEngine rulesEngine,
        IGuidGenerator guidGenerator) :
        EfCoreRepository<AttachmentDbContext, AttachCatalogueTemplate, Guid>(dbContextProvider),
        IAttachCatalogueTemplateRepository
    {
        private readonly ISemanticMatcher _semanticMatcher = semanticMatcher;
        private readonly IRulesEngine _rulesEngine = rulesEngine;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

        public async Task<List<AttachCatalogueTemplate>> FindBySemanticMatchAsync(string query, bool onlyLatest = true)
        {
            var dbSet = await GetDbSetAsync();
            var queryable = dbSet.AsQueryable();
            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            var templates = await queryable.ToListAsync();
            return await _semanticMatcher.MatchTemplatesAsync(query, templates);
        }
        
        public async Task<List<AttachCatalogueTemplate>> GetIntelligentRecommendationsAsync(
            string query, 
            double threshold = 0.3, 
            int topN = 10, 
            bool onlyLatest = true,
            bool includeHistory = false)
        {
            var dbContext = await GetDbContextAsync();
            var dbSet = await GetDbSetAsync();
            
            // 使用 PostgreSQL 的全文搜索和相似度函数进行智能推荐
            // 优化：分别处理 NamePattern 和 SemanticModel 的匹配
            var sql = @"
                SELECT t.*, 
                       COALESCE(
                           GREATEST(
                               -- SemanticModel 语义匹配（权重最高）
                               CASE WHEN t.""SemanticModel"" IS NOT NULL AND t.""SemanticModel"" != '' 
                                    THEN (
                                        similarity(t.""TemplateName"", @query) * 0.4 +
                                        similarity(t.""SemanticModel"", @query) * 0.6
                                    ) * 1.3
                                    ELSE 0 END,
                               -- NamePattern 模式匹配（权重中等）
                               CASE WHEN t.""NamePattern"" IS NOT NULL AND t.""NamePattern"" != '' 
                                    THEN (
                                        similarity(t.""TemplateName"", @query) * 0.5 +
                                        similarity(t.""NamePattern"", @query) * 0.5
                                    ) * 1.1
                                    ELSE 0 END,
                               -- RuleExpression 规则匹配（权重较低）
                               CASE WHEN t.""RuleExpression"" IS NOT NULL AND t.""RuleExpression"" != '' 
                                    THEN similarity(t.""TemplateName"", @query) * 1.0 
                                    ELSE 0 END,
                               -- 基础名称匹配（权重最低）
                               similarity(t.""TemplateName"", @query) * 0.8
                           ), 0
                       ) as match_score
                FROM ""AttachCatalogueTemplates"" t
                WHERE (@onlyLatest = false OR t.""IsLatest"" = true)
                  AND (
                      -- 基础文本匹配
                      t.""TemplateName"" ILIKE @queryPattern
                      OR t.""TemplateName"" % @query
                      OR @query % t.""TemplateName""
                      -- SemanticModel 关键字匹配
                      OR (t.""SemanticModel"" IS NOT NULL AND t.""SemanticModel"" ILIKE @queryPattern)
                      -- NamePattern 模式匹配
                      OR (t.""NamePattern"" IS NOT NULL AND t.""NamePattern"" ILIKE @queryPattern)
                      -- 相似度匹配
                      OR similarity(t.""TemplateName"", @query) > @threshold
                  )
                ORDER BY match_score DESC, t.""SequenceNumber"" ASC
                LIMIT @topN";
            
            var queryPattern = $"%{query}%";
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@query", query),
                new Npgsql.NpgsqlParameter("@queryPattern", queryPattern),
                new Npgsql.NpgsqlParameter("@threshold", threshold),
                new Npgsql.NpgsqlParameter("@topN", topN),
                new Npgsql.NpgsqlParameter("@onlyLatest", onlyLatest)
            };
            
            // 使用动态查询来获取包含分数的结果
            var rawResults = await dbContext.Database
                .SqlQueryRaw<dynamic>(sql, parameters)
                .ToListAsync();
                
            var results = new List<AttachCatalogueTemplate>();
            foreach (var rawResult in rawResults)
            {
                // 从原始结果中提取模板ID
                var templateId = Guid.Parse(rawResult.Id.ToString());
                
                // 获取完整的模板实体
                var template = await dbSet.FindAsync(templateId);
                if (template != null)
                {
                    // 将数据库计算的分数存储到扩展属性中
                    if (rawResult.match_score != null)
                    {
                        template.ExtraProperties["MatchScore"] = Convert.ToDouble(rawResult.match_score);
                    }
                    results.Add(template);
                }
            }
                
            Logger.LogInformation("智能推荐查询完成，查询：{query}，找到 {count} 个匹配模板", 
                query, results.Count);
                
            return results;
        }
        
        public async Task<List<AttachCatalogueTemplate>> GetRecommendationsByBusinessAsync(
            string businessDescription,
            List<string> fileTypes,
            int expectedLevels = 3,
            bool onlyLatest = true)
        {
            var dbSet = await GetDbSetAsync();
            
            // 构建业务相关的查询条件
            var businessKeywords = ExtractBusinessKeywords(businessDescription);
            var fileTypeKeywords = fileTypes.Select(ft => ft.ToUpper()).ToList();
            
            // 使用 PostgreSQL 的全文搜索进行业务匹配
            var sql = @"
                SELECT t.*, 
                       COALESCE(
                           GREATEST(
                               CASE WHEN t.""SemanticModel"" IS NOT NULL AND t.""SemanticModel"" != '' 
                                    THEN ts_rank(to_tsvector('chinese', t.""TemplateName""), plainto_tsquery('chinese', @businessQuery)) * 1.2 
                                    ELSE 0 END,
                               CASE WHEN t.""RuleExpression"" IS NOT NULL AND t.""RuleExpression"" != '' 
                                    THEN ts_rank(to_tsvector('chinese', t.""TemplateName""), plainto_tsquery('chinese', @businessQuery)) * 1.1 
                                    ELSE 0 END,
                               ts_rank(to_tsvector('chinese', t.""TemplateName""), plainto_tsquery('chinese', @businessQuery)) * 0.8
                           ), 0
                       ) as business_score
                FROM ""AttachCatalogueTemplates"" t
                WHERE (@onlyLatest = false OR t.""IsLatest"" = true)
                  AND (
                      ts_rank(to_tsvector('chinese', t.""TemplateName""), plainto_tsquery('chinese', @businessQuery)) > 0.1
                      OR t.""TemplateName"" ILIKE ANY(@fileTypePatterns)
                  )
                ORDER BY business_score DESC, t.""SequenceNumber"" ASC
                LIMIT @expectedLevels";
            
            var businessQuery = string.Join(" ", businessKeywords);
            var fileTypePatterns = fileTypeKeywords.Select(ft => $"%{ft}%").ToArray();
            
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@businessQuery", businessQuery),
                new Npgsql.NpgsqlParameter("@fileTypePatterns", fileTypePatterns),
                new Npgsql.NpgsqlParameter("@expectedLevels", expectedLevels),
                new Npgsql.NpgsqlParameter("@onlyLatest", onlyLatest)
            };
            
            var results = await dbSet
                .FromSqlRaw(sql, parameters)
                .ToListAsync();
                
            Logger.LogInformation("业务推荐查询完成，业务描述：{description}，文件类型：{fileTypes}，找到 {count} 个匹配模板", 
                businessDescription, string.Join(", ", fileTypes), results.Count);
                
            return results;
        }
        
        private static List<string> ExtractBusinessKeywords(string businessDescription)
        {
            // 简单的关键词提取逻辑
            var keywords = new List<string>();
            var commonBusinessTerms = new[] 
            { 
                "合同", "协议", "报告", "申请", "审批", "流程", "文档", "文件", 
                "项目", "业务", "管理", "系统", "数据", "信息", "记录", "档案"
            };
            
            foreach (var term in commonBusinessTerms)
            {
                if (businessDescription.Contains(term))
                {
                    keywords.Add(term);
                }
            }
            
            // 如果没有找到预定义关键词，返回原始描述的关键词
            if (keywords.Count == 0)
            {
                keywords.AddRange(businessDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 1)
                    .Take(5));
            }
            
            return keywords;
        }

        public async Task<List<AttachCatalogueTemplate>> FindByRuleMatchAsync(Dictionary<string, object> context, bool onlyLatest = true)
        {
            var dbSet = await GetDbSetAsync();
            var queryable = dbSet.AsQueryable();
            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            var templates = await queryable.ToListAsync();
            var matchedTemplates = new List<AttachCatalogueTemplate>();

            foreach (var template in templates.Where(t => !string.IsNullOrEmpty(t.RuleExpression)))
            {
                try
                {
                    if (string.IsNullOrEmpty(template.RuleExpression)) continue;
                    var workflow = JsonConvert.DeserializeObject<Workflow>(template.RuleExpression);
                    if (workflow == null) continue;
                    var resultList = await _rulesEngine.ExecuteAllRulesAsync(workflow.WorkflowName, context);

                    if (resultList.Any(r => r.IsSuccess))
                    {
                        matchedTemplates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error executing rule for template {TemplateId}", template.Id);
                }
            }
            return matchedTemplates;
        }

        public async Task<List<AttachCatalogueTemplate>> GetChildrenAsync(Guid parentId, bool onlyLatest = true)
        {
            var queryable = (await GetDbSetAsync())
                .Where(t => t.ParentId == parentId);

            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            return await queryable
                .OrderBy(t => t.SequenceNumber)
                .ToListAsync();
        }

        // 新增版本管理方法
        public async Task<AttachCatalogueTemplate?> GetLatestVersionAsync(string templateName)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName && t.IsLatest)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(string templateName)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId)
        {
            var template = await GetAsync(templateId);
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == template.TemplateName)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task SetAsLatestVersionAsync(Guid templateId)
        {
            var template = await GetAsync(templateId);
            await SetAllOtherVersionsAsNotLatestAsync(template.TemplateName, templateId);

            template.SetVersion(template.Version, true);
            await UpdateAsync(template, autoSave: true);
        }

        public async Task SetAllOtherVersionsAsNotLatestAsync(string templateName, Guid excludeId)
        {
            var templates = await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName && t.Id != excludeId)
                .ToListAsync();

            foreach (var template in templates)
            {
                template.SetVersion(template.Version, false);
                await UpdateAsync(template);
            }
        }

        #region 关键字维护方法

        /// <summary>
        /// 更新模板的 SemanticModel 关键字
        /// </summary>
        public async Task UpdateSemanticModelKeywordsAsync(Guid templateId, List<string> keywords)
        {
            var dbContext = await GetDbContextAsync();
            
            var sql = @"
                UPDATE ""AttachCatalogueTemplates"" 
                SET ""SemanticModel"" = @keywords
                WHERE ""Id"" = @templateId";
            
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@templateId", templateId),
                new Npgsql.NpgsqlParameter("@keywords", string.Join(",", keywords))
            };
            
            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
            
            Logger.LogInformation("更新模板语义模型关键字完成，模板ID：{templateId}，关键字：{keywords}", 
                templateId, string.Join(",", keywords));
        }

        /// <summary>
        /// 更新模板的 NamePattern 模式
        /// </summary>
        public async Task UpdateNamePatternAsync(Guid templateId, string namePattern)
        {
            var dbContext = await GetDbContextAsync();
            
            var sql = @"
                UPDATE ""AttachCatalogueTemplates"" 
                SET ""NamePattern"" = @namePattern
                WHERE ""Id"" = @templateId";
            
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@templateId", templateId),
                new Npgsql.NpgsqlParameter("@namePattern", namePattern)
            };
            
            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
            
            Logger.LogInformation("更新模板名称模式完成，模板ID：{templateId}，模式：{namePattern}", 
                templateId, namePattern);
        }

        /// <summary>
        /// 基于使用历史自动提取 SemanticModel 关键字
        /// </summary>
        public async Task<List<string>> ExtractSemanticKeywordsFromUsageAsync(Guid templateId)
        {
            var dbContext = await GetDbContextAsync();
            
            // 基于模板使用历史提取关键字
            var sql = @"
                SELECT DISTINCT 
                    unnest(string_to_array(t.""SemanticModel"", ',')) as keyword
                FROM ""AttachCatalogueTemplates"" t
                WHERE t.""Id"" = @templateId
                   OR t.""ParentId"" = @templateId
                UNION
                SELECT DISTINCT 
                    unnest(string_to_array(t.""TemplateName"", ' ')) as keyword
                FROM ""AttachCatalogueTemplates"" t
                WHERE t.""Id"" = @templateId
                   OR t.""ParentId"" = @templateId
                LIMIT 10";
            
            var parameters = new[] { new Npgsql.NpgsqlParameter("@templateId", templateId) };
            
            var keywords = await dbContext.Database
                .SqlQueryRaw<string>(sql, parameters)
                .ToListAsync();
            
            return [.. keywords.Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1)];
        }

        /// <summary>
        /// 基于文件命名模式自动提取 NamePattern
        /// </summary>
        public async Task<string> ExtractNamePatternFromFilesAsync(Guid templateId)
        {
            var dbContext = await GetDbContextAsync();
            
            // 基于关联文件的命名模式提取
            var sql = @"
                SELECT DISTINCT 
                    CASE 
                        WHEN af.""FileName"" LIKE '%{ProjectName}%' THEN '项目_{ProjectName}_{Date}_{Version}'
                        WHEN af.""FileName"" LIKE '%{Date}%' THEN '{Type}_{Date}_{Version}'
                        WHEN af.""FileName"" LIKE '%{Version}%' THEN '{Type}_{ProjectName}_{Version}'
                        ELSE '{Type}_{ProjectName}_{Date}'
                    END as name_pattern
                FROM ""AttachFiles"" af
                INNER JOIN ""AttachCatalogues"" ac ON af.""CatalogueId"" = ac.""Id""
                WHERE ac.""TemplateId"" = @templateId
                LIMIT 1";
            
            var parameters = new[] { new Npgsql.NpgsqlParameter("@templateId", templateId) };
            
            var namePattern = await dbContext.Database
                .SqlQueryRaw<string>(sql, parameters)
                .FirstOrDefaultAsync();
            
            return namePattern ?? "{Type}_{ProjectName}_{Date}";
        }

        /// <summary>
        /// 智能更新模板关键字（基于使用数据）
        /// </summary>
        public async Task UpdateTemplateKeywordsIntelligentlyAsync(Guid templateId)
        {
            try
            {
                // 1. 提取语义关键字
                var semanticKeywords = await ExtractSemanticKeywordsFromUsageAsync(templateId);
                if (semanticKeywords.Count > 0)
                {
                    await UpdateSemanticModelKeywordsAsync(templateId, semanticKeywords);
                }

                // 2. 提取名称模式
                var namePattern = await ExtractNamePatternFromFilesAsync(templateId);
                await UpdateNamePatternAsync(templateId, namePattern);

                Logger.LogInformation("智能更新模板关键字完成，模板ID：{templateId}", templateId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "智能更新模板关键字失败，模板ID：{templateId}", templateId);
                throw;
            }
        }

        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        public async Task<int> GetTemplateUsageCountAsync(Guid templateId)
        {
            var dbContext = await GetDbContextAsync();
            
            // 统计基于该模板创建的分类数量
            var sql = @"
                SELECT COUNT(*) as usage_count
                FROM ""AttachCatalogues"" ac
                WHERE ac.""TemplateId"" = @templateId
                  AND ac.""IsDeleted"" = false";
            
            var parameters = new[] { new Npgsql.NpgsqlParameter("@templateId", templateId) };
            
            var usageCount = await dbContext.Database
                .SqlQueryRaw<int>(sql, parameters)
                .FirstOrDefaultAsync();
            
            Logger.LogInformation("获取模板使用次数完成，模板ID：{templateId}，使用次数：{usageCount}", 
                templateId, usageCount);
            
            return usageCount;
        }

        #endregion
    }
}
