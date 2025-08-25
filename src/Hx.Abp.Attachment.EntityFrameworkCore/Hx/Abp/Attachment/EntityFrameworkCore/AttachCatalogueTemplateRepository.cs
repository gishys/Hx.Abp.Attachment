using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using System.Linq;
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
            try
            {
                var dbContext = await GetDbContextAsync();
                var dbSet = await GetDbSetAsync();
                
                // 首先检查数据库中是否有数据
                var totalCount = await dbSet.CountAsync();
                Logger.LogInformation("数据库中总共有 {totalCount} 个模板", totalCount);
                
                if (totalCount == 0)
                {
                    Logger.LogWarning("数据库中没有模板数据，返回空列表");
                    return new List<AttachCatalogueTemplate>();
                }
                
                // 恢复原有的复杂业务逻辑，保持精度和准确性
                var sql = @"
                    SELECT t.*, 
                           COALESCE(
                               GREATEST(
                                   -- SemanticModel 语义匹配（权重最高）
                                   CASE WHEN t.""SEMANTIC_MODEL"" IS NOT NULL AND t.""SEMANTIC_MODEL"" != '' 
                                        THEN (
                                            COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.4 +
                                            COALESCE(similarity(t.""SEMANTIC_MODEL"", @query), 0) * 0.6
                                        ) * 1.3
                                        ELSE 0 END,
                                   -- NamePattern 模式匹配（权重中等）
                                   CASE WHEN t.""NAME_PATTERN"" IS NOT NULL AND t.""NAME_PATTERN"" != '' 
                                        THEN (
                                            COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.5 +
                                            COALESCE(similarity(t.""NAME_PATTERN"", @query), 0) * 0.5
                                        ) * 1.1
                                        ELSE 0 END,
                                   -- RuleExpression 规则匹配（权重较低）
                                   CASE WHEN t.""RULE_EXPRESSION"" IS NOT NULL AND t.""RULE_EXPRESSION"" != '' 
                                        THEN COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 1.0 
                                        ELSE 0 END,
                                   -- 基础名称匹配（权重最低）
                                   COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.8
                               ), 0
                           ) as match_score
                    FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                    WHERE (@onlyLatest = false OR t.""IS_LATEST"" = true)
                      AND (
                          -- 基础文本匹配
                          t.""TEMPLATE_NAME"" ILIKE @queryPattern
                          OR t.""TEMPLATE_NAME"" % @query
                          OR @query % t.""TEMPLATE_NAME""
                          -- SemanticModel 关键字匹配
                          OR (t.""SEMANTIC_MODEL"" IS NOT NULL AND t.""SEMANTIC_MODEL"" ILIKE @queryPattern)
                          -- NamePattern 模式匹配
                          OR (t.""NAME_PATTERN"" IS NOT NULL AND t.""NAME_PATTERN"" ILIKE @queryPattern)
                          -- 相似度匹配
                          OR COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) > @threshold
                      )
                    ORDER BY match_score DESC, t.""SEQUENCE_NUMBER"" ASC
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
                    
                Logger.LogInformation("原始查询返回 {rawCount} 个结果", rawResults?.Count ?? 0);
                
                var results = new List<AttachCatalogueTemplate>();
                
                // 检查查询结果是否为空
                if (rawResults == null || rawResults.Count == 0)
                {
                    Logger.LogWarning("智能推荐查询没有返回任何结果，查询：{query}", query);
                    return results;
                }
                
                foreach (var rawResult in rawResults)
                {
                    try
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
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "处理查询结果时出错，跳过此结果");
                    }
                }
                    
                Logger.LogInformation("智能推荐查询完成，查询：{query}，找到 {count} 个匹配模板", 
                    query, results.Count);
                    
                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "智能推荐查询失败，查询：{query}", query);
                
                // 如果复杂查询失败，尝试简化的业务逻辑搜索
                try
                {
                    Logger.LogInformation("尝试使用简化业务逻辑搜索作为备选方案");
                    var dbSet = await GetDbSetAsync();
                    
                    // 保持业务逻辑的简化版本
                    var simpleResults = await dbSet
                        .Where(t => (!onlyLatest || t.IsLatest))
                        .ToListAsync();
                    
                    // 在内存中进行业务逻辑计算
                    var scoredResults = new List<(AttachCatalogueTemplate template, double score)>();
                    foreach (var template in simpleResults)
                    {
                        double score = 0;
                        
                        // SemanticModel 语义匹配（权重最高）
                        if (!string.IsNullOrEmpty(template.SemanticModel))
                        {
                            var nameSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                            var semanticSimilarity = CalculateSimpleSimilarity(template.SemanticModel, query);
                            score = Math.Max(score, (nameSimilarity * 0.4 + semanticSimilarity * 0.6) * 1.3);
                        }
                        
                        // NamePattern 模式匹配（权重中等）
                        if (!string.IsNullOrEmpty(template.NamePattern))
                        {
                            var nameSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                            var patternSimilarity = CalculateSimpleSimilarity(template.NamePattern, query);
                            score = Math.Max(score, (nameSimilarity * 0.5 + patternSimilarity * 0.5) * 1.1);
                        }
                        
                        // RuleExpression 规则匹配（权重较低）
                        if (!string.IsNullOrEmpty(template.RuleExpression))
                        {
                            var nameSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                            score = Math.Max(score, nameSimilarity * 1.0);
                        }
                        
                        // 基础名称匹配（权重最低）
                        var baseSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                        score = Math.Max(score, baseSimilarity * 0.8);
                        
                        // 检查是否满足基本匹配条件
                        if (template.TemplateName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(template.SemanticModel) && template.SemanticModel.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(template.NamePattern) && template.NamePattern.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                            score > threshold)
                        {
                            scoredResults.Add((template, score));
                        }
                    }
                    
                    // 按分数排序并返回前N个
                    var finalResults = scoredResults
                        .OrderByDescending(x => x.score)
                        .ThenBy(x => x.template.SequenceNumber)
                        .Take(topN)
                        .Select(x => 
                        {
                            x.template.ExtraProperties["MatchScore"] = x.score;
                            return x.template;
                        })
                        .ToList();
                    
                    Logger.LogInformation("简化业务逻辑搜索找到 {count} 个结果", finalResults.Count);
                    return finalResults;
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogError(fallbackEx, "备选搜索也失败了");
                    return new List<AttachCatalogueTemplate>();
                }
            }
        }
        
        /// <summary>
        /// 计算简单文本相似度（用于备选方案）
        /// </summary>
        private static double CalculateSimpleSimilarity(string text1, string text2)
        {
            if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
                return 0.0;

            text1 = text1.ToLowerInvariant();
            text2 = text2.ToLowerInvariant();

            // 完全匹配
            if (text1 == text2)
                return 1.0;

            // 包含关系
            if (text1.Contains(text2) || text2.Contains(text1))
                return 0.8;

            // 单词匹配
            var words1 = text1.Split(new[] { ' ', '_', '-', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.Split(new[] { ' ', '_', '-', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

            var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            var totalWords = Math.Max(words1.Length, words2.Length);

            if (totalWords == 0)
                return 0.0;

            return (double)commonWords / totalWords;
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
                               CASE WHEN t.""SEMANTIC_MODEL"" IS NOT NULL AND t.""SEMANTIC_MODEL"" != '' 
                                    THEN ts_rank(to_tsvector('chinese', t.""TEMPLATE_NAME""), plainto_tsquery('chinese', @businessQuery)) * 1.2 
                                    ELSE 0 END,
                               CASE WHEN t.""RULE_EXPRESSION"" IS NOT NULL AND t.""RULE_EXPRESSION"" != '' 
                                    THEN ts_rank(to_tsvector('chinese', t.""TEMPLATE_NAME""), plainto_tsquery('chinese', @businessQuery)) * 1.1 
                                    ELSE 0 END,
                               ts_rank(to_tsvector('chinese', t.""TEMPLATE_NAME""), plainto_tsquery('chinese', @businessQuery)) * 0.8
                           ), 0
                       ) as business_score
                FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                WHERE (@onlyLatest = false OR t.""IS_LATEST"" = true)
                  AND (
                      ts_rank(to_tsvector('chinese', t.""TEMPLATE_NAME""), plainto_tsquery('chinese', @businessQuery)) > 0.1
                      OR t.""TEMPLATE_NAME"" ILIKE ANY(@fileTypePatterns)
                  )
                ORDER BY business_score DESC, t.""SEQUENCE_NUMBER"" ASC
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
                UPDATE ""APPATTACH_CATALOGUE_TEMPLATES"" 
                SET ""SEMANTIC_MODEL"" = @keywords
                WHERE ""ID"" = @templateId";
            
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
                UPDATE ""APPATTACH_CATALOGUE_TEMPLATES"" 
                SET ""NAME_PATTERN"" = @namePattern
                WHERE ""ID"" = @templateId";
            
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
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 使用 EF Core 查询替代 SQL
                var templates = await dbSet
                    .Where(t => t.Id == templateId || t.ParentId == templateId)
                    .Select(t => new { t.SemanticModel, t.TemplateName })
                    .ToListAsync();
                
                var keywords = new List<string>();
                
                foreach (var template in templates)
                {
                    // 从 SemanticModel 提取关键字
                    if (!string.IsNullOrEmpty(template.SemanticModel))
                    {
                        var semanticKeywords = template.SemanticModel
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1);
                        
                        keywords.AddRange(semanticKeywords);
                    }
                    
                    // 从 TemplateName 提取关键字
                    if (!string.IsNullOrEmpty(template.TemplateName))
                    {
                        var nameKeywords = template.TemplateName
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1);
                        
                        keywords.AddRange(nameKeywords);
                    }
                }
                
                // 去重并限制数量
                return keywords.Distinct().Take(10).ToList();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "提取语义关键字失败，模板ID：{templateId}", templateId);
                return new List<string>();
            }
        }

        /// <summary>
        /// 基于文件命名模式自动提取 NamePattern
        /// </summary>
        public async Task<string> ExtractNamePatternFromFilesAsync(Guid templateId)
        {
            try
            {
                var dbContext = await GetDbContextAsync();
                
                // 使用 EF Core 查询替代 SQL
                var catalogues = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted)
                    .Include(ac => ac.AttachFiles)
                    .Select(ac => new { ac.AttachFiles })
                    .ToListAsync();
                
                var fileNames = catalogues
                    .SelectMany(c => c.AttachFiles)
                    .Select(af => af.FileName)
                    .Where(fn => !string.IsNullOrEmpty(fn))
                    .ToList();
                
                if (!fileNames.Any())
                {
                    return "{Type}_{ProjectName}_{Date}";
                }
                
                // 分析文件名模式
                var namePattern = DetermineNamePattern(fileNames);
                
                return namePattern;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "提取名称模式失败，模板ID：{templateId}，使用默认模式", templateId);
                return "{Type}_{ProjectName}_{Date}";
            }
        }
        
        /// <summary>
        /// 根据文件名列表确定命名模式
        /// </summary>
        private static string DetermineNamePattern(List<string> fileNames)
        {
            if (!fileNames.Any())
                return "{Type}_{ProjectName}_{Date}";
            
            // 分析文件名中的模式
            var sampleFileName = fileNames.First();
            
            if (sampleFileName.Contains("{ProjectName}") || sampleFileName.Contains("项目"))
            {
                return "{Type}_{ProjectName}_{Date}_{Version}";
            }
            else if (sampleFileName.Contains("{Date}") || sampleFileName.Contains("日期"))
            {
                return "{Type}_{Date}_{Version}";
            }
            else if (sampleFileName.Contains("{Version}") || sampleFileName.Contains("版本"))
            {
                return "{Type}_{ProjectName}_{Version}";
            }
            else
            {
                return "{Type}_{ProjectName}_{Date}";
            }
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
            try
            {
                var dbContext = await GetDbContextAsync();
                
                // 使用 EF Core 查询替代 SQL
                var usageCount = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted)
                    .CountAsync();
                
                Logger.LogInformation("获取模板使用次数完成，模板ID：{templateId}，使用次数：{usageCount}", 
                    templateId, usageCount);
                
                return usageCount;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板使用次数失败，模板ID：{templateId}", templateId);
                return 0; // 返回0而不是抛出异常，避免影响主流程
            }
        }

        #endregion
    }
}
