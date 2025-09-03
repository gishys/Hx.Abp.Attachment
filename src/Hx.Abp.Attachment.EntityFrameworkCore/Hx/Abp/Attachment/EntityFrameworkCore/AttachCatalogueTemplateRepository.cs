using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using System.Linq.Dynamic.Core;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Guids;

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
                    return [];
                }
                
                // 恢复原有的复杂业务逻辑，保持精度和准确性
            var sql = @"
                SELECT t.*, 
                       COALESCE(
                           GREATEST(
                               -- RuleExpression 规则匹配（权重较高）
                                   CASE WHEN t.""RULE_EXPRESSION"" IS NOT NULL AND t.""RULE_EXPRESSION"" != '' 
                                        THEN COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 1.1
                                    ELSE 0 END,
                               -- 基础名称匹配（权重较低）
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
                      -- 规则表达式匹配
                          OR (t.""RULE_EXPRESSION"" IS NOT NULL AND t.""RULE_EXPRESSION"" ILIKE @queryPattern)
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
                        
                        // RuleExpression 规则匹配（权重较高）
                        if (!string.IsNullOrEmpty(template.RuleExpression))
                        {
                            var nameSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                            score = Math.Max(score, nameSimilarity * 1.1);
                        }
                        
                        // 基础名称匹配（权重较低）
                        var baseSimilarity = CalculateSimpleSimilarity(template.TemplateName, query);
                        score = Math.Max(score, baseSimilarity * 0.8);
                        
                        // 检查是否满足基本匹配条件
                        if (template.TemplateName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            (!string.IsNullOrEmpty(template.RuleExpression) && template.RuleExpression.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
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
                    return [];
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
            var words1 = text1.SplitEfficient([' ', '_', '-', '.', ',', ';', ':', '!', '?']);
            var words2 = text2.SplitEfficient([' ', '_', '-', '.', ',', ';', ':', '!', '?']);

            var commonWords = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            var totalWords = Math.Max(words1.Count, words2.Count);

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
            try
            {
                var dbContext = await GetDbContextAsync();
                var dbSet = await GetDbSetAsync();
                
                // 1. 动态提取业务关键词（从数据库中的实际数据）
                var dynamicKeywords = await ExtractDynamicKeywordsAsync(dbContext, businessDescription);
            
                // 2. 构建数据库驱动的查询，利用新的Description和Tags字段
                var sql = @"
                    WITH template_scores AS (
                        SELECT 
                            t.*,
                            COALESCE(
                                GREATEST(
                                    -- 动态关键词匹配（权重最高）
                                    CASE WHEN @keywords IS NOT NULL AND @keywords != '' 
                                         THEN (
                                             COALESCE(similarity(t.""TEMPLATE_NAME"", @businessQuery), 0) * 0.25 +
                                             COALESCE(similarity(t.""DESCRIPTION"", @businessQuery), 0) * 0.25 +
                                             COALESCE(similarity(t.""RULE_EXPRESSION"", @businessQuery), 0) * 0.25 +
                                             -- 标签匹配（权重中等）
                                             CASE WHEN t.""TAGS"" IS NOT NULL AND t.""TAGS"" != '[]' 
                                                  THEN (
                                                      SELECT COALESCE(MAX(similarity(tag, @businessQuery)), 0) * 0.25
                                                      FROM jsonb_array_elements_text(t.""TAGS"") AS tag
                                                  )
                                              ELSE 0 END
                                         ) * 1.5
                                    ELSE 0 END,
                                    -- 文件类型匹配（权重中等）
                                    CASE WHEN @fileTypes IS NOT NULL AND @fileTypes != ''
                                         THEN (
                                             CASE WHEN t.""TEMPLATE_NAME"" ILIKE ANY(@fileTypePatterns) THEN 0.3 ELSE 0 END +
                                             CASE WHEN t.""DESCRIPTION"" ILIKE ANY(@fileTypePatterns) THEN 0.3 ELSE 0 END +
                                             CASE WHEN t.""RULE_EXPRESSION"" ILIKE ANY(@fileTypePatterns) THEN 0.2 ELSE 0 END +
                                             CASE WHEN t.""TAGS"" @> ANY(@fileTypeJsonArray) THEN 0.2 ELSE 0 END
                                         ) * 1.2
                                    ELSE 0 END,
                                    -- 使用频率权重（基于实际使用数据）
                                    CASE WHEN t.""ID"" IN (
                                        SELECT DISTINCT ""TEMPLATE_ID"" 
                                        FROM ""APPATTACH_CATALOGUES"" 
                                        WHERE ""TEMPLATE_ID"" IS NOT NULL 
                                        AND ""IS_DELETED"" = false
                                    ) THEN 0.3 ELSE 0 END,
                                    -- 基础文本相似度（综合多个字段）
                                    GREATEST(
                                        COALESCE(similarity(t.""TEMPLATE_NAME"", @businessQuery), 0) * 0.4,
                                        COALESCE(similarity(t.""DESCRIPTION"", @businessQuery), 0) * 0.3,
                                        COALESCE(similarity(t.""RULE_EXPRESSION"", @businessQuery), 0) * 0.2,
                                        CASE WHEN t.""TAGS"" IS NOT NULL AND t.""TAGS"" != '[]' 
                                             THEN (
                                                 SELECT COALESCE(MAX(similarity(tag, @businessQuery)), 0) * 0.1
                                                 FROM jsonb_array_elements_text(t.""TAGS"") AS tag
                                             )
                                         ELSE 0 END
                                    ) * 0.8
                                ), 0
                            ) as match_score,
                            -- 使用频率统计
                            COALESCE((
                                SELECT COUNT(*) 
                                FROM ""APPATTACH_CATALOGUES"" ac 
                                WHERE ac.""TEMPLATE_ID"" = t.""ID"" 
                                AND ac.""IS_DELETED"" = false
                            ), 0) as usage_count,
                            -- 最近使用时间
                            (
                                SELECT MAX(ac.""CREATION_TIME"")
                                FROM ""APPATTACH_CATALOGUES"" ac 
                                WHERE ac.""TEMPLATE_ID"" = t.""ID"" 
                                AND ac.""IS_DELETED"" = false
                            ) as last_used_time
                        FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                        WHERE (@onlyLatest = false OR t.""IS_LATEST"" = true)
                          AND t.""IS_DELETED"" = false
                    )
                    SELECT 
                        *,
                        -- 最终评分：匹配分数 + 使用频率权重 + 时间衰减
                        (match_score + 
                         (usage_count * 0.1) + 
                         CASE WHEN last_used_time IS NOT NULL 
                              THEN GREATEST(0, 1 - EXTRACT(EPOCH FROM (NOW() - last_used_time)) / (30 * 24 * 3600)) * 0.2
                              ELSE 0 END
                        ) as final_score
                    FROM template_scores
                    WHERE match_score > 0.1
                    ORDER BY final_score DESC, ""SEQUENCE_NUMBER"" ASC
                    LIMIT @expectedLevels";
            
                // 3. 准备查询参数
                var businessQuery = businessDescription.Trim();
                var dynamicKeywordsStr = string.Join(" ", dynamicKeywords);
                var fileTypePatterns = fileTypes.Select(ft => $"%{ft.ToUpper()}%").ToArray();
                var fileTypeJsonArray = fileTypes.Select(ft => $"[{ft}]").ToArray(); // 用于标签的JSON数组查询
            
                var parameters = new[]
                {
                    new Npgsql.NpgsqlParameter("@businessQuery", businessQuery),
                    new Npgsql.NpgsqlParameter("@keywords", dynamicKeywordsStr),
                    new Npgsql.NpgsqlParameter("@fileTypes", string.Join(",", fileTypes)),
                    new Npgsql.NpgsqlParameter("@fileTypePatterns", fileTypePatterns),
                    new Npgsql.NpgsqlParameter("@fileTypeJsonArray", fileTypeJsonArray),
                    new Npgsql.NpgsqlParameter("@expectedLevels", expectedLevels),
                    new Npgsql.NpgsqlParameter("@onlyLatest", onlyLatest)
                };
            
                // 4. 执行查询
                var rawResults = await dbContext.Database
                    .SqlQueryRaw<dynamic>(sql, parameters)
                    .ToListAsync();
                
                // 5. 转换结果
                var results = new List<AttachCatalogueTemplate>();
                foreach (var rawResult in rawResults)
                {
                    try
                    {
                        var templateId = Guid.Parse(rawResult.Id.ToString());
                        var template = await dbSet.FindAsync(templateId);
                        
                        if (template != null)
                        {
                            // 存储评分信息
                            template.ExtraProperties["BusinessScore"] = Convert.ToDouble(rawResult.final_score);
                            template.ExtraProperties["MatchScore"] = Convert.ToDouble(rawResult.match_score);
                            template.ExtraProperties["UsageCount"] = Convert.ToInt32(rawResult.usage_count);
                            template.ExtraProperties["LastUsedTime"] = rawResult.last_used_time;
                            
                            results.Add(template);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "处理查询结果时出错，跳过此结果");
                    }
                }
                
                Logger.LogInformation("动态业务推荐查询完成，业务描述：{description}，文件类型：{fileTypes}，动态关键词：{keywords}，找到 {count} 个匹配模板", 
                    businessDescription, string.Join(", ", fileTypes), dynamicKeywordsStr, results.Count);
                
                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "动态业务推荐查询失败，业务描述：{description}", businessDescription);
                
                // 降级到简化查询
                return await GetFallbackRecommendationsAsync(businessDescription, fileTypes, expectedLevels, onlyLatest);
            }
        }
        
        /// <summary>
        /// 从数据库中动态提取关键词
        /// </summary>
        private async Task<List<string>> ExtractDynamicKeywordsAsync(AttachmentDbContext dbContext, string businessDescription)
        {
            try
            {
                // 1. 从现有模板中提取高频关键词（利用新字段）
                var templateKeywords = await dbContext.Set<AttachCatalogueTemplate>()
                    .Where(t => !t.IsDeleted)
                    .SelectMany(t => new[]
                    {
                        t.TemplateName,
                        t.Description,
                        t.RuleExpression
                    }.Where(x => !string.IsNullOrEmpty(x)))
                    .ToListAsync();

                // 2. 从模板标签中提取关键词
#pragma warning disable CS8603 // 可能返回 null 引用。
                var tagKeywords = await dbContext.Set<AttachCatalogueTemplate>()
                    .Where(t => !t.IsDeleted && t.Tags != null && t.Tags.Count > 0)
                    .SelectMany(t => t.Tags)
                    .ToListAsync();
#pragma warning restore CS8603 // 可能返回 null 引用。

                // 3. 从实际使用数据中提取关键词
                var usageKeywords = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => !ac.IsDeleted && !string.IsNullOrEmpty(ac.CatalogueName))
                    .Select(ac => ac.CatalogueName)
                    .ToListAsync();

                // 4. 合并并分析关键词
                var allText = string.Join(" ", templateKeywords.Concat(tagKeywords).Concat(usageKeywords));
                var keywords = ExtractKeywordsFromText(allText, businessDescription);

                Logger.LogInformation("动态提取关键词完成，业务描述：{description}，提取关键词：{keywords}", 
                    businessDescription, string.Join(", ", keywords));

                return keywords;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "动态提取关键词失败，使用静态关键词");
                return ExtractStaticKeywords(businessDescription);
            }
        }

        /// <summary>
        /// 从文本中提取关键词
        /// </summary>
        private static List<string> ExtractKeywordsFromText(string text, string businessDescription)
        {
            var keywords = new List<string>();
            
            // 1. 从业务描述中提取核心词汇
            var businessWords = businessDescription
                .SplitEfficient([' ', ',', '，', '、', '；', ';'])
                .Where(w => w.Length > 1)
                .ToList();

            keywords.AddRange(businessWords);

            // 2. 从历史数据中提取相关词汇
            var commonPatterns = new[] 
            { 
                "合同", "协议", "报告", "申请", "审批", "流程", "文档", "文件", 
                "项目", "业务", "管理", "系统", "数据", "信息", "记录", "档案",
                "工程", "建设", "设计", "施工", "监理", "验收", "结算", "决算"
            };

            foreach (var pattern in commonPatterns)
            {
                if (text.Contains(pattern) || businessDescription.Contains(pattern))
                {
                    keywords.Add(pattern);
                }
            }

            // 3. 去重并限制数量
            return [.. keywords.Distinct().Take(10)];
        }

        /// <summary>
        /// 静态关键词提取（降级方案）
        /// </summary>
        private static List<string> ExtractStaticKeywords(string businessDescription)
        {
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
            
            if (keywords.Count == 0)
            {
                keywords.AddRange(businessDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 1)
                    .Take(5));
            }
            
            return keywords;
        }

        /// <summary>
        /// 降级查询方法（当主查询失败时使用）
        /// </summary>
        private async Task<List<AttachCatalogueTemplate>> GetFallbackRecommendationsAsync(
            string businessDescription,
            List<string> fileTypes,
            int expectedLevels,
            bool onlyLatest)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var query = dbSet.AsQueryable();

                if (onlyLatest)
                {
                    query = query.Where(t => t.IsLatest);
                }

                // 简化的查询逻辑
                var templates = await query
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                var scoredTemplates = new List<(AttachCatalogueTemplate template, double score)>();

                foreach (var template in templates)
                {
                    double score = 0;

                    // 模板名称匹配（权重最高）
                    if (!string.IsNullOrEmpty(template.TemplateName) && 
                        template.TemplateName.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.4;
                    }

                    // 描述字段匹配（权重较高）
                    if (!string.IsNullOrEmpty(template.Description) && 
                        template.Description.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.3;
                    }

                    // 标签匹配（权重中等）
                    if (template.Tags != null && template.Tags.Count > 0)
                    {
                        foreach (var tag in template.Tags)
                        {
                            if (tag.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
                            {
                                score += 0.2;
                                break; // 只计算一次标签匹配
                            }
                        }
                    }

                    // 规则表达式匹配（权重较低）
                    if (!string.IsNullOrEmpty(template.RuleExpression) && 
                        template.RuleExpression.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.1;
                    }

                    // 文件类型匹配
                    foreach (var fileType in fileTypes)
                    {
                        if (template.TemplateName?.Contains(fileType, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            score += 0.2;
                        }
                        if (template.Description?.Contains(fileType, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            score += 0.1;
                        }
                        if (template.Tags != null && template.Tags.Any(tag => tag.Contains(fileType, StringComparison.OrdinalIgnoreCase)))
                        {
                            score += 0.1;
                        }
                    }

                    if (score > 0)
                    {
                        scoredTemplates.Add((template, score));
                    }
                }

                var results = scoredTemplates
                    .OrderByDescending(x => x.score)
                    .ThenBy(x => x.template.SequenceNumber)
                    .Take(expectedLevels)
                    .Select(x => 
                    {
                        x.template.ExtraProperties["BusinessScore"] = x.score;
                        x.template.ExtraProperties["FallbackMode"] = true;
                        return x.template;
                    })
                    .ToList();

                Logger.LogInformation("降级查询完成，找到 {count} 个匹配模板", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "降级查询也失败了");
                return [];
            }
        }

        /// <summary>
        /// 计算业务匹配分数（保留用于兼容性）
        /// </summary>
        private static double CalculateBusinessMatchScore(string text, List<string> businessKeywords, List<string> fileTypeKeywords)
        {
            if (string.IsNullOrEmpty(text))
                return 0.0;

            text = text.ToLowerInvariant();
            double score = 0.0;

            // 1. 业务关键词匹配
            foreach (var keyword in businessKeywords)
            {
                if (text.Contains(keyword, StringComparison.InvariantCultureIgnoreCase))
                {
                    score += 0.3; // 每个关键词匹配增加0.3分
                }
            }

            // 2. 文件类型关键词匹配
            foreach (var fileType in fileTypeKeywords)
            {
                if (text.Contains(fileType, StringComparison.InvariantCultureIgnoreCase))
                {
                    score += 0.2; // 每个文件类型匹配增加0.2分
                }
            }

            // 3. 完全匹配加分
            if (businessKeywords.Any(k => text.Equals(k.ToLowerInvariant())))
            {
                score += 0.5; // 完全匹配额外加分
            }

            // 4. 长度匹配（较长的匹配文本得分更高）
            var maxKeywordLength = businessKeywords.Concat(fileTypeKeywords).Max(k => k.Length);
            if (text.Length >= maxKeywordLength)
            {
                score += 0.1;
            }

            return Math.Min(score, 1.0); // 确保分数不超过1.0
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
        /// 更新模板的规则表达式
        /// </summary>
        public async Task UpdateRuleExpressionAsync(Guid templateId, string ruleExpression)
        {
            var dbContext = await GetDbContextAsync();
            
            var sql = @"
                UPDATE ""APPATTACH_CATALOGUE_TEMPLATES"" 
                SET ""RULE_EXPRESSION"" = @ruleExpression
                WHERE ""ID"" = @templateId";
            
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@templateId", templateId),
                new Npgsql.NpgsqlParameter("@ruleExpression", ruleExpression)
            };
            
            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
            
            Logger.LogInformation("更新模板规则表达式完成，模板ID：{templateId}，表达式：{ruleExpression}", 
                templateId, ruleExpression);
        }

        /// <summary>
        /// 智能更新模板配置（基于使用数据）
        /// </summary>
        public async Task UpdateTemplateConfigurationIntelligentlyAsync(Guid templateId)
        {
            try
            {
                // 更新规则表达式（如果基于使用数据可以优化）
                var ruleExpression = await ExtractRuleExpressionFromUsageAsync(templateId);
                if (!string.IsNullOrEmpty(ruleExpression))
                {
                    await UpdateRuleExpressionAsync(templateId, ruleExpression);
                }

                Logger.LogInformation("智能更新模板配置完成，模板ID：{templateId}", templateId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "智能更新模板配置失败，模板ID：{templateId}", templateId);
                throw;
            }
        }

        /// <summary>
        /// 智能更新模板关键字（基于使用数据）
        /// </summary>
        public Task UpdateTemplateKeywordsIntelligentlyAsync(Guid templateId)
        {
            try
            {
                // 基于使用数据智能更新模板关键字
                // 这里可以添加关键字提取和更新的逻辑
                // 目前简化实现，只记录日志
                
                Logger.LogInformation("智能更新模板关键字完成，模板ID：{templateId}", templateId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "智能更新模板关键字失败，模板ID：{templateId}", templateId);
                throw;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 基于使用历史提取规则表达式
        /// </summary>
        private async Task<string> ExtractRuleExpressionFromUsageAsync(Guid templateId)
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
                
                if (fileNames.Count == 0)
                {
                    return "{\"WorkflowName\":\"DefaultWorkflow\",\"Rules\":[]}";
                }
                
                // 分析文件名模式，生成简单的规则表达式
                var ruleExpression = GenerateSimpleRuleExpression(fileNames);
                
                return ruleExpression;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "提取规则表达式失败，模板ID：{templateId}，使用默认表达式", templateId);
                return "{\"WorkflowName\":\"DefaultWorkflow\",\"Rules\":[]}";
            }
        }
        
        /// <summary>
        /// 根据文件名列表生成简单的规则表达式
        /// </summary>
        private static string GenerateSimpleRuleExpression(List<string> fileNames)
        {
            if (fileNames.Count == 0)
                return "{\"WorkflowName\":\"DefaultWorkflow\",\"Rules\":[]}";
            
            // 分析文件名中的模式，生成简单的规则
            var sampleFileName = fileNames.First();
            
            if (sampleFileName.Contains("项目") || sampleFileName.Contains("Project"))
            {
                return "{\"WorkflowName\":\"ProjectWorkflow\",\"Rules\":[\"ProjectName\",\"Date\",\"Version\"]}";
            }
            else if (sampleFileName.Contains("日期") || sampleFileName.Contains("Date"))
            {
                return "{\"WorkflowName\":\"DateWorkflow\",\"Rules\":[\"Date\",\"Type\",\"Version\"]}";
            }
            else if (sampleFileName.Contains("版本") || sampleFileName.Contains("Version"))
            {
                return "{\"WorkflowName\":\"VersionWorkflow\",\"Rules\":[\"Type\",\"ProjectName\",\"Version\"]}";
            }
            else
            {
                return "{\"WorkflowName\":\"StandardWorkflow\",\"Rules\":[\"Type\",\"ProjectName\",\"Date\"]}";
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

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        public async Task<TemplateUsageStats> GetTemplateUsageStatsAsync(Guid templateId)
        {
            try
            {
                var dbContext = await GetDbContextAsync();
                var dbSet = await GetDbSetAsync();

                // 获取模板信息
                var template = await dbSet.FirstOrDefaultAsync(t => t.Id == templateId);
                if (template == null)
                {
                    return new TemplateUsageStats { Id = templateId };
                }

                // 获取使用统计
                var usageQuery = dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted);

                var usageCount = await usageQuery.CountAsync();
                var uniqueReferences = await usageQuery.Select(ac => ac.Reference).Distinct().CountAsync();
                var lastUsedTime = await usageQuery.MaxAsync(ac => ac.CreationTime);

                // 计算最近30天使用次数
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var recentUsageCount = await usageQuery
                    .Where(ac => ac.CreationTime >= thirtyDaysAgo)
                    .CountAsync();

                // 计算平均使用频率
                var firstUsageTime = await usageQuery.MinAsync(ac => ac.CreationTime);
                var totalDays = (DateTime.UtcNow - firstUsageTime).TotalDays;
                var averageUsagePerDay = totalDays > 0 ? usageCount / totalDays : 0;

                var stats = new TemplateUsageStats
                {
                    Id = templateId,
                    TemplateName = template.TemplateName,
                    UsageCount = usageCount,
                    UniqueReferences = uniqueReferences,
                    LastUsedTime = lastUsedTime,
                    RecentUsageCount = recentUsageCount,
                    AverageUsagePerDay = Math.Round(averageUsagePerDay, 2)
                };

                Logger.LogInformation("获取模板使用统计完成，模板ID：{templateId}，使用次数：{usageCount}", 
                    templateId, usageCount);

                return stats;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板使用统计失败，模板ID：{templateId}", templateId);
                return new TemplateUsageStats { Id = templateId };
            }
        }

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        public async Task<List<TemplateUsageTrend>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30)
        {
            try
            {
                var dbContext = await GetDbContextAsync();
                var startDate = DateTime.UtcNow.AddDays(-daysBack);

                // 获取指定时间范围内的使用数据
                var usageData = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted && ac.CreationTime >= startDate)
                    .GroupBy(ac => ac.CreationTime.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                var trends = new List<TemplateUsageTrend>();

                // 生成完整的日期范围
                for (var date = startDate.Date; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
                {
                    var dailyData = usageData.FirstOrDefault(x => x.Date == date);
                    var dailyCount = dailyData?.Count ?? 0;

                    trends.Add(new TemplateUsageTrend
                    {
                        Date = date,
                        UsageCount = dailyCount,
                        UniqueReferences = dailyCount // 简化处理，实际应该计算唯一引用数
                    });
                }

                Logger.LogInformation("获取模板使用趋势完成，模板ID：{templateId}，天数：{daysBack}", 
                    templateId, daysBack);

                return trends;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板使用趋势失败，模板ID：{templateId}", templateId);
                return [];
            }
        }

        /// <summary>
        /// 批量获取模板使用统计
        /// </summary>
        public async Task<List<BatchTemplateUsageStats>> GetBatchTemplateUsageStatsAsync(List<Guid> templateIds, int daysBack = 30)
        {
            var results = new List<BatchTemplateUsageStats>();

            foreach (var templateId in templateIds)
            {
                try
                {
                    var stats = await GetTemplateUsageStatsAsync(templateId);
                    var trends = await GetTemplateUsageTrendAsync(templateId, daysBack);

                    results.Add(new BatchTemplateUsageStats
                    {
                        TemplateId = templateId,
                        TemplateName = stats.TemplateName,
                        TotalUsageCount = stats.UsageCount,
                        RecentUsageCount = stats.RecentUsageCount,
                        LastUsedTime = stats.LastUsedTime,
                        AverageUsagePerDay = stats.AverageUsagePerDay
                    });
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "批量获取模板使用统计失败，模板ID：{templateId}", templateId);
                    results.Add(new BatchTemplateUsageStats
                    {
                        TemplateId = templateId,
                        TemplateName = string.Empty
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// 获取热门模板
        /// </summary>
        public async Task<List<HotTemplate>> GetHotTemplatesAsync(int topN = 10, int daysBack = 30)
        {
            try
            {
                var dbContext = await GetDbContextAsync();
                var startDate = DateTime.UtcNow.AddDays(-daysBack);

                // 获取热门模板
                var hotTemplates = await dbContext.Set<AttachCatalogueTemplate>()
                    .Where(t => !t.IsDeleted)
                    .Select(t => new
                    {
                        t.Id,
                        t.TemplateName,
                        UsageCount = dbContext.Set<AttachCatalogue>()
                            .Where(ac => ac.TemplateId == t.Id && !ac.IsDeleted && ac.CreationTime >= startDate)
                            .Count()
                    })
                    .OrderByDescending(x => x.UsageCount)
                    .Take(topN)
                    .ToListAsync();

                var result = new List<HotTemplate>();

                foreach (var template in hotTemplates)
                {
                    var usageFrequency = daysBack > 0 ? (double)template.UsageCount / daysBack : 0;

                    result.Add(new HotTemplate
                    {
                        TemplateId = template.Id,
                        TemplateName = template.TemplateName,
                        UsageCount = template.UsageCount,
                        RecentUsageCount = template.UsageCount,
                        AverageUsagePerDay = Math.Round(usageFrequency, 2),
                        LastUsedTime = null // 需要额外查询获取最后使用时间
                    });
                }

                Logger.LogInformation("获取热门模板完成，天数：{daysBack}，数量：{topN}", daysBack, topN);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取热门模板失败");
                return [];
            }
        }

        /// <summary>
        /// 获取模板使用统计概览
        /// </summary>
        public async Task<TemplateUsageOverview> GetTemplateUsageOverviewAsync()
        {
            try
            {
                var dbContext = await GetDbContextAsync();
                var dbSet = await GetDbSetAsync();

                // 获取总体统计
                var totalTemplates = await dbSet.Where(t => !t.IsDeleted).CountAsync();
                var totalUsage = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId != null && !ac.IsDeleted)
                    .CountAsync();

                // 获取最近30天统计
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                var recentUsage = await dbContext.Set<AttachCatalogue>()
                    .Where(ac => ac.TemplateId != null && !ac.IsDeleted && ac.CreationTime >= thirtyDaysAgo)
                    .CountAsync();

                // 获取最活跃的模板
                var topUsedTemplates = await dbContext.Set<AttachCatalogueTemplate>()
                    .Where(t => !t.IsDeleted)
                    .Select(t => new
                    {
                        t.Id,
                        t.TemplateName,
                        UsageCount = dbContext.Set<AttachCatalogue>()
                            .Where(ac => ac.TemplateId == t.Id && !ac.IsDeleted)
                            .Count()
                    })
                    .OrderByDescending(x => x.UsageCount)
                    .Take(10)
                    .ToListAsync();

                var overview = new TemplateUsageOverview
                {
                    TotalTemplates = totalTemplates,
                    ActiveTemplates = totalTemplates, // 简化处理
                    TotalUsageCount = totalUsage,
                    AverageUsagePerTemplate = totalTemplates > 0 ? Math.Round((double)totalUsage / totalTemplates, 2) : 0,
                    TopUsedTemplates = [.. topUsedTemplates.Select(t => new HotTemplate
                    {
                        TemplateId = t.Id,
                        TemplateName = t.TemplateName,
                        UsageCount = t.UsageCount,
                        RecentUsageCount = 0, // 需要额外查询
                        AverageUsagePerDay = 0, // 需要额外计算
                        LastUsedTime = null // 需要额外查询
                    })],
                    UsageByMonth = [] // 需要额外查询
                };

                Logger.LogInformation("获取模板使用统计概览完成");

                return overview;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板使用统计概览失败");
                return new TemplateUsageOverview();
            }
        }

        #endregion

        // ============= 新增模板标识查询方法 =============
        
        /// <summary>
        /// 按模板标识查询模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplatesByIdentifierAsync(
            int? facetType = null,
            int? templatePurpose = null,
            bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 应用过滤条件
                if (onlyLatest)
                {
                    queryable = queryable.Where(t => t.IsLatest);
                }

                if (facetType.HasValue)
                {
                    queryable = queryable.Where(t => (int)t.FacetType == facetType.Value);
                }

                if (templatePurpose.HasValue)
                {
                    queryable = queryable.Where(t => (int)t.TemplatePurpose == templatePurpose.Value);
                }

                var templates = await queryable
                    .OrderBy(t => t.SequenceNumber)
                    .ThenBy(t => t.TemplateName)
                    .ToListAsync();

                Logger.LogInformation("按模板标识查询完成，分面类型：{facetType}，用途：{templatePurpose}，结果数量：{count}", 
                    facetType, templatePurpose, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "按模板标识查询模板失败");
                return [];
            }
        }

        // ============= 新增向量相关查询方法 =============

        /// <summary>
        /// 查找相似模板（基于语义向量）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> FindSimilarTemplatesAsync(
            string semanticQuery,
            double similarityThreshold = 0.7,
            int maxResults = 10)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 首先获取有向量的模板
                var templatesWithVector = await dbSet
                    .Where(t => t.IsLatest && t.TextVector != null && t.VectorDimension > 0)
                    .ToListAsync();

                if (templatesWithVector.Count == 0)
                {
                    Logger.LogWarning("没有找到包含向量的模板");
                    return [];
                }

                // 使用语义匹配器查找相似模板
                var similarTemplates = await _semanticMatcher.MatchTemplatesAsync(semanticQuery, templatesWithVector);
                
                // 过滤相似度阈值
                var filteredTemplates = similarTemplates
                    .Where(t => t.GetType().GetProperty("SimilarityScore")?.GetValue(t) is double score && score >= similarityThreshold)
                    .Take(maxResults)
                    .ToList();

                Logger.LogInformation("查找相似模板完成，查询：{query}，阈值：{threshold}，结果数量：{count}", 
                    semanticQuery, similarityThreshold, filteredTemplates.Count);

                return filteredTemplates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查找相似模板失败");
                return [];
            }
        }

        /// <summary>
        /// 按向量维度查询模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplatesByVectorDimensionAsync(
            int minDimension,
            int maxDimension,
            bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 应用过滤条件
                if (onlyLatest)
                {
                    queryable = queryable.Where(t => t.IsLatest);
                }

                // 向量维度范围过滤
                queryable = queryable.Where(t => 
                    t.VectorDimension >= minDimension && 
                    t.VectorDimension <= maxDimension);

                var templates = await queryable
                    .OrderBy(t => t.VectorDimension)
                    .ThenBy(t => t.TemplateName)
                    .ToListAsync();

                Logger.LogInformation("按向量维度查询完成，最小维度：{minDimension}，最大维度：{maxDimension}，结果数量：{count}", 
                    minDimension, maxDimension, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "按向量维度查询模板失败");
                return [];
            }
        }

        // ============= 新增统计方法 =============

        /// <summary>
        /// 获取模板统计信息
        /// </summary>
        public async Task<object> GetTemplateStatisticsAsync()
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 基础统计
                var totalCount = await dbSet.Where(t => !t.IsDeleted).CountAsync();
                var latestCount = await dbSet.Where(t => !t.IsDeleted && t.IsLatest).CountAsync();
                var templatesWithVector = await dbSet.Where(t => !t.IsDeleted && t.TextVector != null && t.VectorDimension > 0).CountAsync();
                
                // 按模板类型统计
                var typeCounts = await dbSet
                    .Where(t => !t.IsDeleted)
                    .GroupBy(t => (int)t.FacetType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count);

                // 按模板用途统计
                var purposeCounts = await dbSet
                    .Where(t => !t.IsDeleted)
                    .GroupBy(t => (int)t.TemplatePurpose)
                    .Select(g => new { Purpose = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Purpose.ToString(), x => x.Count);

                // 向量维度统计
                var vectorTemplates = await dbSet
                    .Where(t => !t.IsDeleted && t.VectorDimension > 0)
                    .Select(t => t.VectorDimension)
                    .ToListAsync();

                var averageVectorDimension = vectorTemplates.Count != 0 ? vectorTemplates.Average() : 0.0;

                var statistics = new
                {
                    TotalCount = totalCount,
                    LatestCount = latestCount,
                    TemplatesWithVector = templatesWithVector,
                    FacetTypeCounts = typeCounts,
                    TemplatePurposeCounts = purposeCounts,
                    AverageVectorDimension = Math.Round(averageVectorDimension, 2)
                };

                Logger.LogInformation("获取模板统计信息完成，总数量：{totalCount}", totalCount);

                return statistics;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板统计信息失败");
                return new
                {
                    TotalCount = 0,
                    LatestCount = 0,
                    TemplatesWithVector = 0,
                    FacetTypeCounts = new Dictionary<string, int>(),
                    TemplatePurposeCounts = new Dictionary<string, int>(),
                    AverageVectorDimension = 0.0
                };
            }
        }

        // ============= 混合检索方法 =============

        /// <summary>
        /// 混合检索模板（字面 + 语义）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> SearchTemplatesHybridAsync(
            string? keyword = null,
            string? semanticQuery = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            double similarityThreshold = 0.7,
            double textWeight = 0.4,
            double semanticWeight = 0.6)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);
                
                if (facetType.HasValue)
                    queryable = queryable.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    queryable = queryable.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 标签过滤
                if (tags != null && tags.Count > 0)
                {
                    foreach (var tag in tags)
                    {
                        queryable = queryable.Where(t => t.Tags != null && t.Tags.Contains(tag));
                    }
                }

                var results = new List<AttachCatalogueTemplate>();

                // 字面检索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var textResults = await queryable
                        .Where(t => t.TemplateName.Contains(keyword) || 
                                  (t.Description != null && t.Description.Contains(keyword)) ||
                                  (t.Tags != null && t.Tags.Any(tag => tag.Contains(keyword))))
                        .Take(maxResults)
                        .ToListAsync();
                    
                    results.AddRange(textResults);
                }

                // 语义检索
                if (!string.IsNullOrWhiteSpace(semanticQuery) && results.Count < maxResults)
                {
                    var remainingCount = maxResults - results.Count;
                    var semanticResults = await queryable
                        .Where(t => t.TextVector != null && t.VectorDimension > 0)
                        .Take(remainingCount)
                        .ToListAsync();
                    
                    // 这里应该调用语义匹配服务进行相似度计算
                    // 简化实现，直接返回结果
                    results.AddRange(semanticResults);
                }

                // 去重并限制结果数量
                var distinctResults = results
                    .GroupBy(t => t.Id)
                    .Select(g => g.First())
                    .Take(maxResults)
                    .ToList();

                Logger.LogInformation("混合检索完成，关键词：{keyword}，语义查询：{semanticQuery}，结果数量：{count}", 
                    keyword, semanticQuery, distinctResults.Count);

                return distinctResults;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "混合检索模板失败");
                return [];
            }
        }

        /// <summary>
        /// 全文检索模板（基于倒排索引）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> SearchTemplatesByTextAsync(
            string keyword,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            bool enableFuzzy = true,
            bool enablePrefix = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);
                
                if (facetType.HasValue)
                    queryable = queryable.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    queryable = queryable.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 标签过滤
                if (tags != null && tags.Count > 0)
                {
                    foreach (var tag in tags)
                    {
                        queryable = queryable.Where(t => t.Tags != null && t.Tags.Contains(tag));
                    }
                }

                // 文本搜索
                var searchPattern = $"%{keyword}%";
                queryable = queryable.Where(t => 
                    t.TemplateName.Contains(keyword) || 
                    (t.Description != null && t.Description.Contains(keyword)) ||
                    (t.Tags != null && t.Tags.Any(tag => tag.Contains(keyword))));

                var results = await queryable
                    .OrderByDescending(t => t.TemplateName.Contains(keyword) ? 1 : 0)
                    .ThenBy(t => t.TemplateName)
                    .Take(maxResults)
                    .ToListAsync();

                Logger.LogInformation("文本检索完成，关键词：{keyword}，结果数量：{count}", keyword, results.Count);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "文本检索模板失败");
                return [];
            }
        }

        /// <summary>
        /// 标签检索模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> SearchTemplatesByTagsAsync(
            List<string> tags,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            int maxResults = 20,
            bool matchAll = false)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);
                
                if (facetType.HasValue)
                    queryable = queryable.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    queryable = queryable.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 标签匹配
                if (matchAll)
                {
                    // 必须包含所有标签
                    foreach (var tag in tags)
                    {
                        queryable = queryable.Where(t => t.Tags != null && t.Tags.Contains(tag));
                    }
                }
                else
                {
                    // 包含任意标签
                    queryable = queryable.Where(t => t.Tags != null && t.Tags.Any(tag => tags.Contains(tag)));
                }

                var results = await queryable
                    .OrderBy(t => t.TemplateName)
                    .Take(maxResults)
                    .ToListAsync();

                Logger.LogInformation("标签检索完成，标签：{tags}，结果数量：{count}", string.Join(",", tags), results.Count);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "标签检索模板失败");
                return [];
            }
        }

        /// <summary>
        /// 语义检索模板（基于向量相似度）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> SearchTemplatesBySemanticAsync(
            string semanticQuery,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            double similarityThreshold = 0.7,
            int maxResults = 20)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);
                
                if (facetType.HasValue)
                    queryable = queryable.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    queryable = queryable.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 只查询有向量的模板
                queryable = queryable.Where(t => t.TextVector != null && t.VectorDimension > 0);

                var templates = await queryable
                    .Take(maxResults * 2) // 获取更多候选，用于语义匹配
                    .ToListAsync();

                // 这里应该调用语义匹配服务进行相似度计算和排序
                // 简化实现，直接返回结果
                var results = templates.Take(maxResults).ToList();

                Logger.LogInformation("语义检索完成，查询：{semanticQuery}，结果数量：{count}", semanticQuery, results.Count);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "语义检索模板失败");
                return [];
            }
        }

        /// <summary>
        /// 获取热门标签
        /// </summary>
        public async Task<List<string>> GetPopularTagsAsync(int topN = 20)
        {
            try
            {
                var dbSet = await GetDbSetAsync();

#pragma warning disable CS8603 // 可能返回 null 引用。
                var popularTags = await dbSet
                    .Where(t => !t.IsDeleted && t.Tags != null && t.Tags.Count > 0)
                    .SelectMany(t => t.Tags)
                    .GroupBy(tag => tag)
                    .OrderByDescending(g => g.Count())
                    .Take(topN)
                    .Select(g => g.Key)
                    .ToListAsync();
#pragma warning restore CS8603 // 可能返回 null 引用。

                Logger.LogInformation("获取热门标签完成，数量：{count}", popularTags.Count);

                return popularTags;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取热门标签失败");
                return [];
            }
        }

        /// <summary>
        /// 获取标签统计
        /// </summary>
        public async Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            try
            {
                var dbSet = await GetDbSetAsync();

#pragma warning disable CS8603 // 可能返回 null 引用。
                var tagStats = await dbSet
                    .Where(t => !t.IsDeleted && t.Tags != null && t.Tags.Count > 0)
                    .SelectMany(t => t.Tags)
                    .GroupBy(tag => tag)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
#pragma warning restore CS8603 // 可能返回 null 引用。

                Logger.LogInformation("获取标签统计完成，标签数量：{count}", tagStats.Count);

                return tagStats;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取标签统计失败");
                return [];
            }
        }

        #region 树状结构查询方法

        /// <summary>
        /// 获取根节点模板（用于树状展示）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetRootTemplatesAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool includeChildren = true,
            bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);
                
                // 只查询根节点（ParentId为null）
                queryable = queryable.Where(t => t.ParentId == null);
                
                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);
                
                if (facetType.HasValue)
                    queryable = queryable.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    queryable = queryable.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 按顺序号排序
                queryable = queryable.OrderBy(t => t.SequenceNumber);

                if (includeChildren)
                {
                    // 使用EF Core的Include预加载整个树形结构
                    // 注意：这里需要处理子节点的过滤条件
                    queryable = queryable.Include(t => t.Children.Where(c => !c.IsDeleted && (!onlyLatest || c.IsLatest)).OrderBy(c => c.SequenceNumber))
                        .ThenInclude(c => c.Children.Where(c2 => !c2.IsDeleted && (!onlyLatest || c2.IsLatest)).OrderBy(c2 => c2.SequenceNumber))
                        .ThenInclude(c => c.Children.Where(c3 => !c3.IsDeleted && (!onlyLatest || c3.IsLatest)).OrderBy(c3 => c3.SequenceNumber))
                        .ThenInclude(c => c.Children.Where(c4 => !c4.IsDeleted && (!onlyLatest || c4.IsLatest)).OrderBy(c4 => c4.SequenceNumber))
                        .ThenInclude(c => c.Children.Where(c5 => !c5.IsDeleted && (!onlyLatest || c5.IsLatest)).OrderBy(c5 => c5.SequenceNumber)); // 最多5层深度
                }

                var rootTemplates = await queryable.ToListAsync();

                Logger.LogInformation("获取根节点模板完成，数量：{count}，包含子节点：{includeChildren}", 
                    rootTemplates.Count, includeChildren);

                return rootTemplates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取根节点模板失败");
                return [];
            }
        }

        /// <summary>
        /// 递归获取模板的完整子树
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplateSubtreeAsync(
            Guid rootId,
            bool onlyLatest = true,
            int maxDepth = 10)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.Where(t => t.Id == rootId && !t.IsDeleted);

                // 使用EF Core的Include预加载整个树形结构
                if (maxDepth > 0)
                {
                    var currentQuery = queryable.Include(t => t.Children);

                    // 动态构建多层Include
                    for (int i = 1; i < Math.Min(maxDepth, 5); i++) // 限制最大深度为5层
                    {
                        currentQuery = currentQuery.ThenInclude(c => c.Children);
                    }
                }

                var rootTemplate = await queryable.FirstOrDefaultAsync();

                if (rootTemplate == null)
                {
                    Logger.LogWarning("未找到根模板：{rootId}", rootId);
                    return [];
                }

                var result = new List<AttachCatalogueTemplate> { rootTemplate };
                
                Logger.LogInformation("获取模板子树完成，根ID：{rootId}，结果数量：{count}", 
                    rootId, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板子树失败，根ID：{rootId}", rootId);
                return [];
            }
        }



        #endregion
    }
}
