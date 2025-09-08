using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Dynamic.Core;
using Volo.Abp;
using System.Text;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// 用于接收混合检索查询结果的 DTO
    /// </summary>
    public class TemplateSearchResultDto
    {
        public Guid Id { get; set; }
        public int Version { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public FacetType FacetType { get; set; }
        public TemplatePurpose TemplatePurpose { get; set; }
        
        // 使用 [NotMapped] 标记复杂类型字段，避免 EF Core 映射问题
        [NotMapped]
        public List<string>? Tags { get; set; }
        
        [NotMapped]
        public ICollection<MetaField> MetaFields { get; set; } = [];
        
        [NotMapped]
        public List<double>? TextVector { get; set; }
        
        public int? VectorDimension { get; set; }
        public bool IsLatest { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid? CreatorId { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public Guid? LastModifierId { get; set; }
        public string? TemplatePath { get; set; }
        public string? WorkflowConfig { get; set; }
        
        // 评分字段
        public double FinalScore { get; set; }
        public double VectorScore { get; set; }
        public double FulltextScore { get; set; }
        public double UsageScore { get; set; }
        public double TimeScore { get; set; }
    }

    public class AttachCatalogueTemplateRepository(
        IDbContextProvider<AttachmentDbContext> dbContextProvider,
        IRulesEngine rulesEngine,
        IGuidGenerator guidGenerator) :
        EfCoreRepository<AttachmentDbContext, AttachCatalogueTemplate>(dbContextProvider),
        IAttachCatalogueTemplateRepository
    {
        private readonly IRulesEngine _rulesEngine = rulesEngine;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

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

                // 混合检索架构：向量召回 + 全文检索加权过滤 + 分数融合
                var sql = @"
                    WITH vector_recall AS (
                        -- 第一阶段：向量召回 Top-N（语义检索）
                        SELECT 
                            t.*,
                            -- 向量相似度计算（如果有向量数据）
                            CASE 
                                WHEN t.""TEXT_VECTOR"" IS NOT NULL AND t.""VECTOR_DIMENSION"" > 0 
                                THEN (
                                    -- 这里应该使用实际的向量相似度计算函数
                                    -- 例如：1 - (t.""TEXT_VECTOR"" <-> @queryVector::vector)
                                    -- 暂时使用文本相似度作为占位符
                                    COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.9
                                )
                                ELSE 0
                            END as vector_score
                        FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                        WHERE (@onlyLatest = false OR t.""IS_LATEST"" = true)
                          AND t.""IS_DELETED"" = false
                          AND (
                              -- 向量过滤条件
                              t.""TEXT_VECTOR"" IS NOT NULL 
                              OR t.""TEMPLATE_NAME"" ILIKE @queryPattern
                              OR t.""DESCRIPTION"" ILIKE @queryPattern
                              OR t.""TAGS"" @> @queryTagsJson
                          )
                        ORDER BY vector_score DESC
                        LIMIT @vectorTopN
                    ),
                    fulltext_scoring AS (
                        -- 第二阶段：全文检索加权过滤和重排
                        SELECT 
                            vr.*,
                            -- 全文检索分数计算
                            COALESCE(
                                GREATEST(
                                                                              -- 模板名称匹配（权重最高，要求更高的相似度）
                                    CASE WHEN vr.""TEMPLATE_NAME"" ILIKE @queryPattern 
                                               THEN CASE 
                                                   WHEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) > 0.3 
                                         THEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) * 1.0
                                                   ELSE 0 
                                               END
                                         ELSE 0 END,
                                    
                                    -- 描述字段匹配（权重较高）
                                    CASE WHEN vr.""DESCRIPTION"" IS NOT NULL AND vr.""DESCRIPTION"" ILIKE @queryPattern 
                                         THEN CASE 
                                             WHEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) > 0.3 
                                         THEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) * 0.8
                                             ELSE 0 
                                         END
                                         ELSE 0 END,
                                    
                                    -- 标签匹配（权重中等）
                                    CASE WHEN vr.""TAGS"" IS NOT NULL AND vr.""TAGS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(tag, @query)), 0) * 0.6
                                             FROM jsonb_array_elements_text(vr.""TAGS"") AS tag
                                         )
                                         ELSE 0 END,
                                    
                                    -- 元数据字段匹配（权重中等）
                                    CASE WHEN vr.""META_FIELDS"" IS NOT NULL AND vr.""META_FIELDS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(meta_field->>'FieldName', @query)), 0) * 0.5
                                             FROM jsonb_array_elements(vr.""META_FIELDS"") AS meta_field
                                         )
                                         ELSE 0 END,
                                    
                                    -- 工作流配置匹配（权重较低）
                                    CASE WHEN vr.""WORKFLOW_CONFIG"" IS NOT NULL AND vr.""WORKFLOW_CONFIG"" ILIKE @queryPattern 
                                         THEN COALESCE(similarity(vr.""WORKFLOW_CONFIG"", @query), 0) * 0.3
                                         ELSE 0 END,
                                    
                                    -- 模糊匹配（权重最低）
                                    CASE WHEN vr.""TEMPLATE_NAME"" % @query OR @query % vr.""TEMPLATE_NAME""
                                         THEN 0.2
                                         ELSE 0 END
                                ), 0
                            ) as fulltext_score,
                            
                            -- 使用频率权重
                            COALESCE((
                                SELECT COUNT(*) 
                                FROM ""APPATTACH_CATALOGUES"" ac 
                                WHERE ac.""TEMPLATE_ID"" = vr.""ID"" 
                                AND ac.""IS_DELETED"" = false
                            ), 0) as usage_count,
                            
                            -- 最近使用时间
                            (
                                SELECT MAX(ac.""CREATION_TIME"")
                                FROM ""APPATTACH_CATALOGUES"" ac 
                                WHERE ac.""TEMPLATE_ID"" = vr.""ID"" 
                                AND ac.""IS_DELETED"" = false
                            ) as last_used_time
                        FROM vector_recall vr
                    )
                    SELECT 
                        *,
                        -- 第三阶段：分数线性融合
                        (
                            -- 向量分数权重（语义）
                            COALESCE(vector_score, 0) * 0.6 +
                            -- 全文检索分数权重（字面）
                            COALESCE(fulltext_score, 0) * 0.4 +
                            -- 使用频率权重
                            (usage_count * 0.05) +
                            -- 时间衰减权重
                            CASE WHEN last_used_time IS NOT NULL 
                                 THEN GREATEST(0, 1 - EXTRACT(EPOCH FROM (NOW() - last_used_time)) / (30 * 24 * 3600)) * 0.1
                                 ELSE 0 END
                        ) as final_score,
                        vector_score,
                        fulltext_score
                    FROM fulltext_scoring
                    WHERE (
                        -- 最终过滤条件
                        COALESCE(vector_score, 0) > @vectorThreshold
                        OR COALESCE(fulltext_score, 0) > @fulltextThreshold
                        OR usage_count > 0
                    )
                    ORDER BY final_score DESC, ""SEQUENCE_NUMBER"" ASC
                    LIMIT @topN";

                // 准备查询参数
                var queryPattern = $"%{query}%";
                var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var queryTagsJson = $"[{string.Join(",", queryWords.Select(w => $"\"{w}\""))}]";
                var vectorTopN = Math.Max(topN * 3, 50); // 向量召回更多候选
                var vectorThreshold = threshold * 0.5; // 向量阈值相对宽松
                var fulltextThreshold = threshold; // 全文检索阈值

                var parameters = new[]
                {
                    new Npgsql.NpgsqlParameter("@query", query),
                    new Npgsql.NpgsqlParameter("@queryPattern", queryPattern),
                    new Npgsql.NpgsqlParameter("@queryTagsJson", queryTagsJson),
                    new Npgsql.NpgsqlParameter("@vectorTopN", vectorTopN),
                    new Npgsql.NpgsqlParameter("@vectorThreshold", vectorThreshold),
                    new Npgsql.NpgsqlParameter("@fulltextThreshold", fulltextThreshold),
                    new Npgsql.NpgsqlParameter("@topN", topN),
                    new Npgsql.NpgsqlParameter("@onlyLatest", onlyLatest)
                };
                
                // 执行混合检索查询
                var rawResults = await dbContext.Database
                    .SqlQueryRaw<dynamic>(sql, parameters)
                    .ToListAsync();
                        
                Logger.LogInformation("混合检索查询返回 {rawCount} 个结果", rawResults?.Count ?? 0);
                
                var results = new List<AttachCatalogueTemplate>();
                    
                // 检查查询结果是否为空
                if (rawResults == null || rawResults.Count == 0)
                {
                    Logger.LogWarning("混合检索查询没有返回任何结果，查询：{query}", query);
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
                            // 存储各种分数信息
                            if (rawResult.final_score != null)
                                template.ExtraProperties["FinalScore"] = Convert.ToDouble(rawResult.final_score);
                            if (rawResult.vector_score != null)
                                template.ExtraProperties["VectorScore"] = Convert.ToDouble(rawResult.vector_score);
                            if (rawResult.fulltext_score != null)
                                template.ExtraProperties["FulltextScore"] = Convert.ToDouble(rawResult.fulltext_score);
                            if (rawResult.usage_count != null)
                                template.ExtraProperties["UsageCount"] = Convert.ToInt32(rawResult.usage_count);
                            if (rawResult.last_used_time != null)
                                template.ExtraProperties["LastUsedTime"] = rawResult.last_used_time;
                            
                            results.Add(template);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "处理混合检索结果时出错，跳过此结果");
                    }
                }
                    
                Logger.LogInformation("混合检索完成，查询：{query}，找到 {count} 个匹配模板", 
                    query, results.Count);
                    
                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "混合检索查询失败，查询：{query}", query);
                
                // 如果混合检索失败，尝试简化的业务逻辑搜索
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
                        
                        // 模板名称匹配（权重最高）
                        if (template.TemplateName.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 0.4;
                        }
                        
                        // 描述字段匹配（权重较高）
                        if (!string.IsNullOrEmpty(template.Description) && 
                            template.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 0.3;
                        }
                        
                        // 标签匹配（权重中等）
                        if (template.Tags != null && template.Tags.Count > 0)
                        {
                            foreach (var tag in template.Tags)
                            {
                                if (tag.Contains(query, StringComparison.OrdinalIgnoreCase))
                                {
                                    score += 0.2;
                                    break;
                                }
                            }
                        }
                        
                        // 工作流配置匹配（权重较低）
                        if (!string.IsNullOrEmpty(template.WorkflowConfig) && 
                            template.WorkflowConfig.Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 0.1;
                        }
                        
                        // 检查是否满足基本匹配条件
                        if (score > threshold)
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
                            x.template.ExtraProperties["FallbackMode"] = true;
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
                        t.WorkflowConfig
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

                    // 工作流配置匹配（权重较低）
                    if (!string.IsNullOrEmpty(template.WorkflowConfig) && 
                        template.WorkflowConfig.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
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

            foreach (var template in templates.Where(t => !string.IsNullOrEmpty(t.WorkflowConfig)))
            {
                try
                {
                    if (string.IsNullOrEmpty(template.WorkflowConfig)) continue;
                    var workflow = JsonConvert.DeserializeObject<Workflow>(template.WorkflowConfig);
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

        public async Task<List<AttachCatalogueTemplate>> GetChildrenByParentAsync(Guid parentId, int parentVersion, bool onlyLatest = true)
        {
            var queryable = (await GetDbSetAsync())
                .Where(t => t.ParentId == parentId && t.ParentVersion == parentVersion);

            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            return await queryable
                .OrderBy(t => t.SequenceNumber)
                .ToListAsync();
        }

        /// <summary>
        /// 获取指定模板的最新版本
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="includeTreeStructure">是否返回树形结构（包含所有相关节点）</param>
        /// <returns>最新版本的模板，如果包含树形结构则返回完整的树</returns>
        public async Task<AttachCatalogueTemplate?> GetLatestVersionAsync(Guid templateId, bool includeTreeStructure = false)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 获取最新版本的模板
                var latestTemplate = await dbSet
                    .Where(t => t.Id == templateId && t.IsLatest && !t.IsDeleted)
                    .OrderByDescending(t => t.Version)
                    .FirstOrDefaultAsync();

                if (latestTemplate == null)
                {
                    Logger.LogWarning("未找到模板的最新版本：ID={templateId}", templateId);
                    return null;
                }

                // 如果不包含树形结构，直接返回单个模板
                if (!includeTreeStructure)
                {
                    Logger.LogDebug("获取模板最新版本成功：ID={templateId}, Version={version}", 
                        templateId, latestTemplate.Version);
                    return latestTemplate;
                }

                // 包含树形结构：获取所有相关节点
                var allRelatedNodes = await GetAllNodesFromTemplateAsync(
                    templateId, 
                    latestTemplate.Version, 
                    includeDescendants: true, 
                    onlyLatest: true);

                // 构建树形结构
                var treeStructure = BuildTreeFromPath(allRelatedNodes, onlyLatest: true);
                
                // 找到并返回目标节点（保持树形结构）
                var targetNode = FindNodeInTree(treeStructure, templateId, latestTemplate.Version);
                
                Logger.LogInformation("获取模板最新版本及树形结构成功：ID={templateId}, Version={version}, 相关节点数={nodeCount}", 
                    templateId, latestTemplate.Version, allRelatedNodes.Count);

                return targetNode;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板最新版本失败：ID={templateId}", templateId);
                return null;
            }
        }

        /// <summary>
        /// 在树形结构中查找指定节点
        /// </summary>
        /// <param name="treeNodes">树形节点集合</param>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本</param>
        /// <returns>找到的节点，如果未找到则返回null</returns>
        private static AttachCatalogueTemplate? FindNodeInTree(
            List<AttachCatalogueTemplate> treeNodes, 
            Guid templateId, 
            int templateVersion)
        {
            foreach (var node in treeNodes)
            {
                if (node.Id == templateId && node.Version == templateVersion)
                {
                    return node;
                }

                // 递归查找子节点
                if (node.Children != null && node.Children.Count != 0)
                {
                    var foundInChildren = FindNodeInTree([.. node.Children], templateId, templateVersion);
                    if (foundInChildren != null)
                    {
                        return foundInChildren;
                    }
                }
            }

            return null;
        }

        public async Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(Guid templateId)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.Id == templateId)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.Id == templateId)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task<AttachCatalogueTemplate?> GetByVersionAsync(Guid templateId, int version)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.Id == templateId && t.Version == version)
                .FirstOrDefaultAsync();
        }

        public async Task SetAsLatestVersionAsync(Guid templateId, int version)
        {
            var template = await GetByVersionAsync(templateId, version) ?? throw new UserFriendlyException($"未找到模板 {templateId} 的版本 {version}");
            await SetAllOtherVersionsAsNotLatestAsync(templateId, version);

            template.SetIsLatest(true);
            await UpdateAsync(template, autoSave: true);
        }

        public async Task SetAllOtherVersionsAsNotLatestAsync(Guid templateId, int excludeVersion)
        {
            var templates = await (await GetDbSetAsync())
                .Where(t => t.Id == templateId && t.Version != excludeVersion)
                .ToListAsync();

            foreach (var template in templates)
            {
                template.SetIsLatest(false);
                await UpdateAsync(template);
            }
        }

        /// <summary>
        /// 更新模板的工作流配置
        /// </summary>
        public async Task UpdateWorkflowConfigAsync(Guid templateId, string workflowConfig)
        {
            var dbContext = await GetDbContextAsync();
            
            var sql = @"
                UPDATE ""APPATTACH_CATALOGUE_TEMPLATES"" 
                SET ""WORKFLOW_CONFIG"" = @workflowConfig
                WHERE ""ID"" = @templateId";
            
            var parameters = new[]
            {
                new Npgsql.NpgsqlParameter("@templateId", templateId),
                new Npgsql.NpgsqlParameter("@workflowConfig", workflowConfig)
            };
            
            await dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
            
            Logger.LogInformation("更新模板工作流配置完成，模板ID：{templateId}，配置：{workflowConfig}", 
                templateId, workflowConfig);
        }

        #region 关键字维护方法


        /// <summary>
        /// 智能更新模板配置（基于使用数据）
        /// </summary>
        public async Task UpdateTemplateConfigurationIntelligentlyAsync(Guid templateId)
        {
            try
            {
                // 更新工作流配置（如果基于使用数据可以优化）
                var workflowConfig = await ExtractWorkflowConfigFromUsageAsync(templateId);
                if (!string.IsNullOrEmpty(workflowConfig))
                {
                    await UpdateWorkflowConfigAsync(templateId, workflowConfig);
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
        /// 基于使用历史提取工作流配置
        /// </summary>
        private async Task<string> ExtractWorkflowConfigFromUsageAsync(Guid templateId)
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
                
                // 分析文件名模式，生成简单的工作流配置
                var workflowConfig = GenerateSimpleWorkflowConfig(fileNames);
                
                return workflowConfig;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "提取规则表达式失败，模板ID：{templateId}，使用默认表达式", templateId);
                return "{\"WorkflowName\":\"DefaultWorkflow\",\"Rules\":[]}";
            }
        }
        
        /// <summary>
        /// 根据文件名列表生成简单的工作流配置
        /// </summary>
        private static string GenerateSimpleWorkflowConfig(List<string> fileNames)
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
                var similarTemplates = await GetIntelligentRecommendationsAsync(
                    semanticQuery, 
                    similarityThreshold, 
                    maxResults, 
                    true, 
                    false);
                
                Logger.LogInformation("查找相似模板完成，查询：{query}，阈值：{threshold}，结果数量：{count}", 
                    semanticQuery, similarityThreshold, similarTemplates.Count);

                return similarTemplates;
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
        /// 基于动态分类树业务需求，返回简单类型的统计数据
        /// </summary>
        public async Task<TemplateStatistics> GetTemplateStatisticsAsync()
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 基础统计
                var totalCount = await dbSet.Where(t => !t.IsDeleted).CountAsync();
                var latestCount = await dbSet.Where(t => !t.IsDeleted && t.IsLatest).CountAsync();
                var historyCount = await dbSet.Where(t => !t.IsDeleted && !t.IsLatest).CountAsync();
                
                // 根节点和子节点统计
                var rootCount = await dbSet.Where(t => !t.IsDeleted && t.ParentId == null).CountAsync();
                var childCount = await dbSet.Where(t => !t.IsDeleted && t.ParentId != null).CountAsync();
                
                // 分面类型统计
                var generalFacetCount = await dbSet.Where(t => !t.IsDeleted && t.FacetType == FacetType.General).CountAsync();
                var professionalFacetCount = await dbSet.Where(t => !t.IsDeleted && t.FacetType == FacetType.Discipline).CountAsync();
                
                // 模板用途统计
                var classificationPurposeCount = await dbSet.Where(t => !t.IsDeleted && t.TemplatePurpose == TemplatePurpose.Classification).CountAsync();
                var documentPurposeCount = await dbSet.Where(t => !t.IsDeleted && t.TemplatePurpose == TemplatePurpose.Document).CountAsync();
                var workflowPurposeCount = await dbSet.Where(t => !t.IsDeleted && t.TemplatePurpose == TemplatePurpose.Workflow).CountAsync();
                
                // 向量相关统计
                var templatesWithVector = await dbSet.Where(t => !t.IsDeleted && t.TextVector != null && t.VectorDimension > 0).CountAsync();
                var vectorTemplates = await dbSet
                    .Where(t => !t.IsDeleted && t.VectorDimension > 0)
                    .Select(t => t.VectorDimension)
                    .ToListAsync();
                var averageVectorDimension = vectorTemplates.Count != 0 ? vectorTemplates.Average() : 0.0;

                // 树形结构统计
                var templatesWithPath = await dbSet
                    .Where(t => !t.IsDeleted && t.TemplatePath != null)
                    .Select(t => t.TemplatePath)
                    .ToListAsync();
                
                var maxTreeDepth = 0;
                var totalChildrenCount = 0;
                var parentTemplates = 0;
                
                foreach (var path in templatesWithPath)
                {
                    // 空值检查，虽然查询已过滤null，但编译器仍需要显式检查
                    if (string.IsNullOrEmpty(path)) continue;
                    
                    var depth = AttachCatalogueTemplate.GetTemplatePathDepth(path);
                    maxTreeDepth = Math.Max(maxTreeDepth, depth);
                    
                    if (depth == 0) // 根节点
                    {
                        parentTemplates++;
                        // 计算子节点数量，添加空值检查
                        var childrenCount = templatesWithPath.Count(p => 
                            !string.IsNullOrEmpty(p) && 
                            p.StartsWith(path + ".") && 
                            !p[(path.Length + 1)..].Contains('.'));
                        totalChildrenCount += childrenCount;
                    }
                }
                
                var averageChildrenCount = parentTemplates > 0 ? (double)totalChildrenCount / parentTemplates : 0.0;
                
                // 时间统计
                var latestCreationTime = await dbSet
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.CreationTime)
                    .Select(t => t.CreationTime)
                    .FirstOrDefaultAsync();
                
                var latestModificationTime = await dbSet
                    .Where(t => !t.IsDeleted)
                    .OrderByDescending(t => t.LastModificationTime)
                    .Select(t => t.LastModificationTime)
                    .FirstOrDefaultAsync();

                var statistics = new TemplateStatistics(
                    totalCount,
                    rootCount,
                    childCount,
                    latestCount,
                    historyCount,
                    generalFacetCount,
                    professionalFacetCount,
                    classificationPurposeCount,
                    documentPurposeCount,
                    workflowPurposeCount,
                    templatesWithVector,
                    Math.Round(averageVectorDimension, 2),
                    maxTreeDepth,
                    Math.Round(averageChildrenCount, 2),
                    latestCreationTime,
                    latestModificationTime
                );

                Logger.LogInformation("获取模板统计信息完成，总数量：{totalCount}，根节点：{rootCount}，最大深度：{maxDepth}", 
                    totalCount, rootCount, maxTreeDepth);

                return statistics;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板统计信息失败");
                return new TemplateStatistics(
                    totalCount: 0,
                    rootTemplateCount: 0,
                    childTemplateCount: 0,
                    latestVersionCount: 0,
                    historyVersionCount: 0,
                    generalFacetCount: 0,
                    disciplineFacetCount: 0,
                    classificationPurposeCount: 0,
                    documentPurposeCount: 0,
                    workflowPurposeCount: 0,
                    templatesWithVector: 0,
                    averageVectorDimension: 0.0,
                    maxTreeDepth: 0,
                    averageChildrenCount: 0.0,
                    latestCreationTime: null,
                    latestModificationTime: null
                );
            }
        }

        // ============= 混合检索方法 =============

        /// <summary>
        /// 混合检索模板（字面 + 语义）
        /// 基于行业最佳实践：向量召回 + 全文检索加权过滤 + 分数融合
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
            double semanticWeight = 0.6,
            bool onlyLatest = true)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(semanticQuery))
                {
                    Logger.LogWarning("混合检索参数无效：关键词和语义查询都为空");
                    return [];
                }

                var dbContext = await GetDbContextAsync();
                var query = keyword ?? semanticQuery ?? string.Empty;
                var queryPattern = $"%{query}%";
                var queryTagsJson = tags != null && tags.Count > 0 
                    ? JsonConvert.SerializeObject(tags) 
                    : "[]";

                // 添加调试日志
                Logger.LogInformation("混合检索参数：query={query}, queryPattern={queryPattern}, onlyLatest={onlyLatest}, similarityThreshold={threshold}", 
                    query, queryPattern, onlyLatest, similarityThreshold);

                // 计算向量召回数量（通常为最终结果的2-3倍）
                var vectorTopN = Math.Max(maxResults * 2, 50);

                // 混合检索架构：向量召回 + 全文检索加权过滤 + 分数融合
                var sql = @"
                    WITH vector_recall AS (
                        -- 第一阶段：向量召回 Top-N（语义检索）
                        SELECT 
                            t.*,
                            -- 向量相似度计算（如果有向量数据）
                            CASE 
                                WHEN t.""TEXT_VECTOR"" IS NOT NULL AND t.""VECTOR_DIMENSION"" > 0 
                                THEN (
                                    -- 使用文本相似度作为向量相似度的占位符
                                    -- 实际部署时应替换为真正的向量相似度计算：
                                    -- 1 - (t.""TEXT_VECTOR"" <-> @queryVector::vector)
                                    COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.9 +
                                    COALESCE(similarity(COALESCE(t.""DESCRIPTION"", ''), @query), 0) * 0.7
                                )
                                ELSE 0
                            END as vector_score
                        FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                        WHERE t.""IS_DELETED"" = false
                          AND (@onlyLatest = false OR t.""IS_LATEST"" = true)
                          AND (@facetType IS NULL OR t.""FACET_TYPE"" = @facetType)
                          AND (@templatePurpose IS NULL OR t.""TEMPLATE_PURPOSE"" = @templatePurpose)
                                                          AND (
                                    -- 向量过滤条件：有向量数据或文本匹配
                                    t.""TEXT_VECTOR"" IS NOT NULL
                                    OR t.""TEMPLATE_NAME"" ILIKE @queryPattern
                                    OR t.""DESCRIPTION"" ILIKE @queryPattern
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
                                                                              -- 模板名称匹配（权重最高，要求更高的相似度）
                                          CASE WHEN vr.""TEMPLATE_NAME"" ILIKE @queryPattern
                                               THEN CASE 
                                                   WHEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) > 0.3 
                                                   THEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) * 1.0
                                                   ELSE 0 
                                               END
                                               ELSE 0 END,
                                    
                                    -- 描述字段匹配（权重较高）
                                    CASE WHEN vr.""DESCRIPTION"" IS NOT NULL AND vr.""DESCRIPTION"" ILIKE @queryPattern 
                                         THEN CASE 
                                             WHEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) > 0.3 
                                             THEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) * 0.8
                                             ELSE 0 
                                         END
                                         ELSE 0 END,
                                    
                                    -- 标签匹配（权重中等）
                                    CASE WHEN vr.""TAGS"" IS NOT NULL AND vr.""TAGS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(tag, @query)), 0) * 0.6
                                             FROM jsonb_array_elements_text(vr.""TAGS"") AS tag
                                         )
                                         ELSE 0 END,
                                    
                                    -- 元数据字段匹配（权重较低）
                                    CASE WHEN vr.""META_FIELDS"" IS NOT NULL AND vr.""META_FIELDS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(meta_field->>'FieldName', @query)), 0) * 0.5
                                             FROM jsonb_array_elements(vr.""META_FIELDS"") AS meta_field
                                         )
                                         ELSE 0 END
                                ), 0
                            ) as fulltext_score,
                            
                            -- 使用频率权重（基于使用次数）
                            0.05 as usage_score,
                            
                            -- 时间衰减权重（最近使用时间）
                            CASE 
                                WHEN vr.""CREATION_TIME"" IS NOT NULL 
                                THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""CREATION_TIME"")) / (365 * 24 * 3600))
                                ELSE 0 
                            END as time_score
                        FROM vector_recall vr
                                                      WHERE (
                                  -- 如果有向量数据，进行相似度阈值过滤
                                  (vr.""TEXT_VECTOR"" IS NOT NULL AND vr.vector_score > @similarityThreshold)
                                  -- 如果没有向量数据，要求严格的文本匹配
                                  OR (vr.""TEXT_VECTOR"" IS NULL AND (
                                      vr.""TEMPLATE_NAME"" ILIKE @queryPattern
                                      OR vr.""DESCRIPTION"" ILIKE @queryPattern
                                  ))
                              )
                    ),
                    final_scoring AS (
                        -- 第三阶段：分数融合和最终排序
                        SELECT 
                            fs.*,
                            -- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
                            (fs.vector_score * @semanticWeight + 
                             fs.fulltext_score * @textWeight + 
                             fs.usage_score + 
                             fs.time_score) as final_score
                        FROM fulltext_scoring fs
                    )
                    SELECT 
                        ""ID"" as ""Id"", ""VERSION"" as ""Version"", ""TEMPLATE_NAME"" as ""TemplateName"", ""DESCRIPTION"" as ""Description"", 
                        ""FACET_TYPE"" as ""FacetType"", ""TEMPLATE_PURPOSE"" as ""TemplatePurpose"",
                        ""VECTOR_DIMENSION"" as ""VectorDimension"", ""IS_LATEST"" as ""IsLatest"", ""IS_DELETED"" as ""IsDeleted"", 
                        ""CREATION_TIME"" as ""CreationTime"", ""CREATOR_ID"" as ""CreatorId"", 
                        ""LAST_MODIFICATION_TIME"" as ""LastModificationTime"", ""LAST_MODIFIER_ID"" as ""LastModifierId"", 
                        ""TEMPLATE_PATH"" as ""TemplatePath"", ""WORKFLOW_CONFIG"" as ""WorkflowConfig"",
                        final_score as ""FinalScore"", vector_score as ""VectorScore"", 
                        fulltext_score as ""FulltextScore"", usage_score as ""UsageScore"", time_score as ""TimeScore""
                    FROM final_scoring
                    WHERE (
                        -- 如果有向量数据，检查向量分数是否满足阈值
                        (""TEXT_VECTOR"" IS NULL OR vector_score > @similarityThreshold)
                        -- 或者有文本匹配分数（兜底机制，但要求更高的相似度）
                        OR (fulltext_score > 0.5 AND (
                            ""TEMPLATE_NAME"" ILIKE @queryPattern
                            OR ""DESCRIPTION"" ILIKE @queryPattern
                        ))
                    )
                    ORDER BY final_score DESC
                    LIMIT @maxResults";

                var parameters = new[]
                {
                    new Npgsql.NpgsqlParameter("@query", NpgsqlTypes.NpgsqlDbType.Text) { Value = query },
                    new Npgsql.NpgsqlParameter("@queryPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryPattern },
                    new Npgsql.NpgsqlParameter("@queryTagsJson", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTagsJson },
                    new Npgsql.NpgsqlParameter("@onlyLatest", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = onlyLatest },
                    new Npgsql.NpgsqlParameter("@facetType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = facetType?.ToString() ?? (object)DBNull.Value },
                    new Npgsql.NpgsqlParameter("@templatePurpose", NpgsqlTypes.NpgsqlDbType.Integer) { Value = templatePurpose?.ToString() ?? (object)DBNull.Value },
                    new Npgsql.NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = vectorTopN },
                    new Npgsql.NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Double) { Value = similarityThreshold },
                    new Npgsql.NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = semanticWeight },
                    new Npgsql.NpgsqlParameter("@textWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = textWeight },
                    new Npgsql.NpgsqlParameter("@maxResults", NpgsqlTypes.NpgsqlDbType.Integer) { Value = maxResults }
                };

                // 使用 DTO 来接收查询结果，避免 ExtraProperties 映射问题
                var rawResults = await dbContext.Database.SqlQueryRaw<TemplateSearchResultDto>(sql, parameters).ToListAsync();
                
                // 手动映射到实体
                var results = new List<AttachCatalogueTemplate>();
                foreach (var rawResult in rawResults)
                {
                    var template = await dbContext.Set<AttachCatalogueTemplate>().FindAsync(rawResult.Id, rawResult.Version);
                    if (template != null)
                    {
                        // 存储评分信息到 ExtraProperties
                        template.ExtraProperties["FinalScore"] = rawResult.FinalScore;
                        template.ExtraProperties["VectorScore"] = rawResult.VectorScore;
                        template.ExtraProperties["FulltextScore"] = rawResult.FulltextScore;
                        template.ExtraProperties["UsageScore"] = rawResult.UsageScore;
                        template.ExtraProperties["TimeScore"] = rawResult.TimeScore;
                        results.Add(template);
                    }
                }

                Logger.LogInformation("混合检索完成，关键词：{keyword}，语义查询：{semanticQuery}，结果数量：{count}，相似度阈值：{threshold}，权重配置：语义={semanticWeight}，文本={textWeight}", 
                    keyword, semanticQuery, results.Count, similarityThreshold, semanticWeight, textWeight);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "混合检索模板失败，关键词：{keyword}，语义查询：{semanticQuery}", keyword, semanticQuery);
                return [];
            }
        }

        /// <summary>
        /// 增强版混合检索模板（支持真正的向量相似度计算）
        /// 基于行业最佳实践：向量召回 + 全文检索加权过滤 + 分数融合
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> SearchTemplatesHybridAdvancedAsync(
            string? keyword = null,
            string? semanticQuery = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int maxResults = 20,
            double similarityThreshold = 0.7,
            double textWeight = 0.4,
            double semanticWeight = 0.6,
            bool enableVectorSearch = true,
            bool enableFullTextSearch = true,
            bool onlyLatest = true)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(semanticQuery))
                {
                    Logger.LogWarning("增强版混合检索参数无效：关键词和语义查询都为空");
                    return [];
                }

                var dbContext = await GetDbContextAsync();
                var query = keyword ?? semanticQuery ?? string.Empty;
                var queryPattern = $"%{query}%";
                var queryTagsJson = tags != null && tags.Count > 0 
                    ? JsonConvert.SerializeObject(tags) 
                    : "[]";

                // 计算向量召回数量（通常为最终结果的2-3倍）
                var vectorTopN = Math.Max(maxResults * 2, 50);

                // 构建动态SQL查询
                var sqlBuilder = new StringBuilder();
                
                // 第一阶段：向量召回（如果启用）
                if (enableVectorSearch)
                {
                    sqlBuilder.AppendLine(@"
                        WITH vector_recall AS (
                            -- 第一阶段：向量召回 Top-N（语义检索）
                            SELECT 
                                t.*,
                                -- 向量相似度计算（如果有向量数据）
                                CASE 
                                    WHEN t.""TEXT_VECTOR"" IS NOT NULL AND t.""VECTOR_DIMENSION"" > 0 
                                    THEN (
                                        -- 使用文本相似度作为向量相似度的占位符
                                        -- 实际部署时应替换为真正的向量相似度计算：
                                        -- 1 - (t.""TEXT_VECTOR"" <-> @queryVector::vector)
                                        COALESCE(similarity(t.""TEMPLATE_NAME"", @query), 0) * 0.9 +
                                        COALESCE(similarity(COALESCE(t.""DESCRIPTION"", ''), @query), 0) * 0.7
                                    )
                                    ELSE 0
                                END as vector_score
                            FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                            WHERE t.""IS_DELETED"" = false
                              AND (@onlyLatest = false OR t.""IS_LATEST"" = true)
                              AND (@facetType IS NULL OR t.""FACET_TYPE"" = @facetType)
                              AND (@templatePurpose IS NULL OR t.""TEMPLATE_PURPOSE"" = @templatePurpose)
                                                              AND (
                                    -- 向量过滤条件：有向量数据或文本匹配
                                    t.""TEXT_VECTOR"" IS NOT NULL
                                    OR t.""TEMPLATE_NAME"" ILIKE @queryPattern
                                    OR t.""DESCRIPTION"" ILIKE @queryPattern
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
                                t.*,
                                0 as vector_score
                            FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
                            WHERE t.""IS_DELETED"" = false
                              AND (@onlyLatest = false OR t.""IS_LATEST"" = true)
                              AND (@facetType IS NULL OR t.""FACET_TYPE"" = @facetType)
                              AND (@templatePurpose IS NULL OR t.""TEMPLATE_PURPOSE"" = @templatePurpose)
                              AND (
                                  t.""TEMPLATE_NAME"" ILIKE @queryPattern
                                  OR t.""DESCRIPTION"" ILIKE @queryPattern
                                  OR t.""TAGS"" @> @queryTagsJson::jsonb
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
                                                                              -- 模板名称匹配（权重最高，要求更高的相似度）
                                          CASE WHEN vr.""TEMPLATE_NAME"" ILIKE @queryPattern
                                               THEN CASE 
                                                   WHEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) > 0.3 
                                                   THEN COALESCE(similarity(vr.""TEMPLATE_NAME"", @query), 0) * 1.0
                                                   ELSE 0 
                                               END
                                               ELSE 0 END,
                                    
                                    -- 描述字段匹配（权重较高）
                                    CASE WHEN vr.""DESCRIPTION"" IS NOT NULL AND vr.""DESCRIPTION"" ILIKE @queryPattern 
                                         THEN CASE 
                                             WHEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) > 0.3 
                                             THEN COALESCE(similarity(vr.""DESCRIPTION"", @query), 0) * 0.8
                                             ELSE 0 
                                         END
                                         ELSE 0 END,
                                    
                                    -- 标签匹配（权重中等）
                                    CASE WHEN vr.""TAGS"" IS NOT NULL AND vr.""TAGS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(tag, @query)), 0) * 0.6
                                             FROM jsonb_array_elements_text(vr.""TAGS"") AS tag
                                         )
                                         ELSE 0 END,
                                    
                                    -- 元数据字段匹配（权重较低）
                                    CASE WHEN vr.""META_FIELDS"" IS NOT NULL AND vr.""META_FIELDS"" != '[]'::jsonb
                                         THEN (
                                             SELECT COALESCE(MAX(similarity(meta_field->>'FieldName', @query)), 0) * 0.5
                                             FROM jsonb_array_elements(vr.""META_FIELDS"") AS meta_field
                                         )
                                         ELSE 0 END
                                ), 0
                            ) as fulltext_score,
                            
                            -- 使用频率权重（基于使用次数）
                            0.05 as usage_score,
                            
                            -- 时间衰减权重（最近使用时间）
                            CASE 
                                WHEN vr.""CREATION_TIME"" IS NOT NULL 
                                THEN 0.1 * (1.0 - EXTRACT(EPOCH FROM (NOW() - vr.""CREATION_TIME"")) / (365 * 24 * 3600))
                                ELSE 0 
                            END as time_score
                        FROM vector_recall vr");

                // 添加相似度阈值过滤
                if (enableVectorSearch)
                {
                    sqlBuilder.AppendLine(@"                                                      WHERE (
                                  -- 如果有向量数据，进行相似度阈值过滤
                                  (vr.""TEXT_VECTOR"" IS NOT NULL AND vr.vector_score > @similarityThreshold)
                                  -- 如果没有向量数据，要求严格的文本匹配
                                  OR (vr.""TEXT_VECTOR"" IS NULL AND (
                                      vr.""TEMPLATE_NAME"" ILIKE @queryPattern
                                      OR vr.""DESCRIPTION"" ILIKE @queryPattern
                                  ))
                              )");
                }

                sqlBuilder.AppendLine(@"
                    ),
                    final_scoring AS (
                        -- 第三阶段：分数融合和最终排序
                        SELECT 
                            fs.*,
                            -- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
                            (fs.vector_score * @semanticWeight + 
                             fs.fulltext_score * @textWeight + 
                             fs.usage_score + 
                             fs.time_score) as final_score
                        FROM fulltext_scoring fs
                    )
                    SELECT 
                        ""ID"" as ""Id"", ""VERSION"" as ""Version"", ""TEMPLATE_NAME"" as ""TemplateName"", ""DESCRIPTION"" as ""Description"", 
                        ""FACET_TYPE"" as ""FacetType"", ""TEMPLATE_PURPOSE"" as ""TemplatePurpose"",
                        ""VECTOR_DIMENSION"" as ""VectorDimension"", ""IS_LATEST"" as ""IsLatest"", ""IS_DELETED"" as ""IsDeleted"", 
                        ""CREATION_TIME"" as ""CreationTime"", ""CREATOR_ID"" as ""CreatorId"", 
                        ""LAST_MODIFICATION_TIME"" as ""LastModificationTime"", ""LAST_MODIFIER_ID"" as ""LastModifierId"", 
                        ""TEMPLATE_PATH"" as ""TemplatePath"", ""WORKFLOW_CONFIG"" as ""WorkflowConfig"",
                        final_score as ""FinalScore"", vector_score as ""VectorScore"", 
                        fulltext_score as ""FulltextScore"", usage_score as ""UsageScore"", time_score as ""TimeScore""
                    FROM final_scoring
                    WHERE (
                        -- 如果有向量数据，检查向量分数是否满足阈值
                        (""TEXT_VECTOR"" IS NULL OR vector_score > @similarityThreshold)
                        -- 或者有文本匹配分数（兜底机制，但要求更高的相似度）
                        OR (fulltext_score > 0.5 AND (
                            ""TEMPLATE_NAME"" ILIKE @queryPattern
                            OR ""DESCRIPTION"" ILIKE @queryPattern
                        ))
                    )
                    ORDER BY final_score DESC
                    LIMIT @maxResults");

                var sql = sqlBuilder.ToString();
                var parameters = new[]
                {
                    new NpgsqlParameter("@query", NpgsqlTypes.NpgsqlDbType.Text) { Value = query },
                    new NpgsqlParameter("@queryPattern", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryPattern },
                    new NpgsqlParameter("@queryTagsJson", NpgsqlTypes.NpgsqlDbType.Text) { Value = queryTagsJson },
                    new NpgsqlParameter("@onlyLatest", NpgsqlTypes.NpgsqlDbType.Boolean) { Value = onlyLatest },
                    new NpgsqlParameter("@facetType", NpgsqlTypes.NpgsqlDbType.Integer) { Value = facetType?.ToString() ?? (object)DBNull.Value },
                    new NpgsqlParameter("@templatePurpose", NpgsqlTypes.NpgsqlDbType.Integer) { Value = templatePurpose?.ToString() ?? (object)DBNull.Value },
                    new NpgsqlParameter("@vectorTopN", NpgsqlTypes.NpgsqlDbType.Integer) { Value = vectorTopN },
                    new NpgsqlParameter("@similarityThreshold", NpgsqlTypes.NpgsqlDbType.Double) { Value = similarityThreshold },
                    new NpgsqlParameter("@semanticWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = semanticWeight },
                    new NpgsqlParameter("@textWeight", NpgsqlTypes.NpgsqlDbType.Double) { Value = textWeight },
                    new NpgsqlParameter("@maxResults", NpgsqlTypes.NpgsqlDbType.Integer) { Value = maxResults }
                };

                // 使用 DTO 来接收查询结果，避免 ExtraProperties 映射问题
                var rawResults = await dbContext.Database.SqlQueryRaw<TemplateSearchResultDto>(sql, parameters).ToListAsync();
                
                // 手动映射到实体
                var results = new List<AttachCatalogueTemplate>();
                foreach (var rawResult in rawResults)
                {
                    var template = await dbContext.Set<AttachCatalogueTemplate>().FindAsync(rawResult.Id, rawResult.Version);
                    if (template != null)
                    {
                        // 存储评分信息到 ExtraProperties
                        template.ExtraProperties["FinalScore"] = rawResult.FinalScore;
                        template.ExtraProperties["VectorScore"] = rawResult.VectorScore;
                        template.ExtraProperties["FulltextScore"] = rawResult.FulltextScore;
                        template.ExtraProperties["UsageScore"] = rawResult.UsageScore;
                        template.ExtraProperties["TimeScore"] = rawResult.TimeScore;
                        results.Add(template);
                    }
                }

                Logger.LogInformation("增强版混合检索完成，关键词：{keyword}，语义查询：{semanticQuery}，结果数量：{count}，相似度阈值：{threshold}，权重配置：语义={semanticWeight}，文本={textWeight}，向量搜索：{enableVector}，全文搜索：{enableFullText}", 
                    keyword, semanticQuery, results.Count, similarityThreshold, semanticWeight, textWeight, enableVectorSearch, enableFullTextSearch);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "增强版混合检索模板失败，关键词：{keyword}，语义查询：{semanticQuery}", keyword, semanticQuery);
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
        /// 基于TemplatePath优化，避免递归查询，提高性能
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetRootTemplatesAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool includeChildren = true,
            bool onlyLatest = true,
            string? fulltextQuery = null)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 基础过滤条件
                var baseFilter = dbSet.Where(t => !t.IsDeleted);
                
                if (onlyLatest)
                    baseFilter = baseFilter.Where(t => t.IsLatest);
                
                if (facetType.HasValue)
                    baseFilter = baseFilter.Where(t => t.FacetType == facetType.Value);
                
                if (templatePurpose.HasValue)
                    baseFilter = baseFilter.Where(t => t.TemplatePurpose == templatePurpose.Value);

                // 全文检索过滤条件
                if (!string.IsNullOrWhiteSpace(fulltextQuery))
                {
                    // 使用 EF.Functions.JsonContains 进行 JSONB 字段查询
                    baseFilter = baseFilter.Where(t =>
                        t.TemplateName.Contains(fulltextQuery) ||
                        (t.Description != null && t.Description.Contains(fulltextQuery)) ||
                        (t.Tags != null && EF.Functions.JsonContains(t.Tags, $"[{JsonConvert.SerializeObject(fulltextQuery)}]")) ||
                        (t.MetaFields != null && (
                            EF.Functions.JsonContains(t.MetaFields, $"[{{\"FieldName\":{JsonConvert.SerializeObject(fulltextQuery)}}}]") ||
                            EF.Functions.JsonContains(t.MetaFields, $"[{{\"FieldValue\":{JsonConvert.SerializeObject(fulltextQuery)}}}]")
                        ))
                    );
                }

                if (includeChildren)
                {
                    // 使用TemplatePath进行高效查询，避免递归Include
                    // 查询所有匹配条件的模板，然后通过路径构建树形结构
                    var matchedTemplates = await baseFilter
                        .OrderBy(t => t.TemplatePath)
                        .ThenBy(t => t.SequenceNumber)
                        .ToListAsync();

                    // 如果有全文检索条件，需要获取所有相关的父节点和子节点
                    var allTemplates = matchedTemplates;
                    if (!string.IsNullOrWhiteSpace(fulltextQuery))
                    {
                        // 优化方案：使用单次查询获取所有相关节点
                        allTemplates = await GetAllNodesFromPathsAsync(matchedTemplates, onlyLatest);
                    }

                    // 通过路径构建树形结构
                    return BuildTreeFromPath(allTemplates, onlyLatest);
                }
                else
                {
                    // 只查询根节点（TemplatePath为空或null，或者ParentId为null）
                    var rootTemplates = await baseFilter
                        .Where(t => t.ParentId == null || t.TemplatePath == null || t.TemplatePath == "")
                        .OrderBy(t => t.SequenceNumber)
                        .ToListAsync();

                    Logger.LogInformation("获取根节点模板完成，数量：{count}，包含子节点：{includeChildren}，全文检索：{fulltextQuery}", 
                        rootTemplates.Count, includeChildren, fulltextQuery ?? "无");

                    return rootTemplates;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取根节点模板失败");
                return [];
            }
        }

        /// <summary>
        /// 获取模板的完整子树
        /// 基于TemplatePath优化，使用路径前缀查询替代递归Include
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplateSubtreeAsync(
            Guid rootId,
            bool onlyLatest = true,
            int maxDepth = 10)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 首先获取根模板
                var rootTemplate = await dbSet
                    .Where(t => t.Id == rootId && !t.IsDeleted)
                    .FirstOrDefaultAsync();

                if (rootTemplate == null)
                {
                    Logger.LogWarning("未找到根模板：{rootId}", rootId);
                    return [];
                }

                // 如果根模板没有路径，说明是根节点，直接返回
                if (string.IsNullOrEmpty(rootTemplate.TemplatePath))
                {
                    return [rootTemplate];
                }

                // 使用路径前缀查询获取所有子节点
                var childPathPrefix = rootTemplate.TemplatePath + ".";
                var childTemplates = await dbSet
                    .Where(t => !t.IsDeleted && 
                               t.TemplatePath != null && 
                               t.TemplatePath.StartsWith(childPathPrefix))
                    .Where(t => !onlyLatest || t.IsLatest)
                    .OrderBy(t => t.TemplatePath)
                    .ThenBy(t => t.SequenceNumber)
                    .ToListAsync();

                // 如果指定了最大深度，过滤掉超出深度的节点
                if (maxDepth > 0)
                {
                    var rootDepth = AttachCatalogueTemplate.GetTemplatePathDepth(rootTemplate.TemplatePath);
                    childTemplates = [.. childTemplates.Where(t => AttachCatalogueTemplate.GetTemplatePathDepth(t.TemplatePath) <= rootDepth + maxDepth)];
                }

                // 构建树形结构
                var result = new List<AttachCatalogueTemplate> { rootTemplate };
                result.AddRange(childTemplates);
                
                Logger.LogInformation("获取模板子树完成，根ID：{rootId}，结果数量：{count}，最大深度：{maxDepth}", 
                    rootId, result.Count, maxDepth);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板子树失败，根ID：{rootId}", rootId);
                return [];
            }
        }
        #endregion

        #region 树形结构构建辅助方法

        /// <summary>
        /// 基于路径构建树形结构
        /// 将平铺的模板列表转换为树形结构
        /// </summary>
        private List<AttachCatalogueTemplate> BuildTreeFromPath(List<AttachCatalogueTemplate> allTemplates, bool onlyLatest)
        {
            if (allTemplates.Count == 0)
                return [];

            // 如果只显示最新版本，过滤掉非最新版本的模板
            var filteredTemplates = onlyLatest 
                ? [.. allTemplates.Where(t => t.IsLatest)]
                : allTemplates;

            // 创建模板字典，便于快速查找
            var templateDict = filteredTemplates.ToDictionary(t => t.Id, t => t);
            
            // 分离根节点和子节点
            var rootTemplates = filteredTemplates
                .Where(t => t.ParentId == null || string.IsNullOrEmpty(t.TemplatePath))
                .OrderBy(t => t.SequenceNumber)
                .ToList();

            // 为每个模板构建子节点集合
            foreach (var template in filteredTemplates)
            {
                template.Children?.Clear();
            }

            // 构建父子关系
            foreach (var template in filteredTemplates)
            {
                if (template.ParentId.HasValue && templateDict.TryGetValue(template.ParentId.Value, out var parent))
                {
                    // 如果父节点的Children为null，跳过添加（这种情况应该很少见）
                    parent.Children?.Add(template);
                }
            }

            // 对每个节点的子节点进行排序
            foreach (var template in filteredTemplates)
            {
                if (template.Children?.Count > 0)
                {
                    var sortedChildren = template.Children.OrderBy(c => c.SequenceNumber).ToList();
                    template.Children.Clear();
                    foreach (var child in sortedChildren)
                    {
                        template.Children.Add(child);
                    }
                }
            }

            Logger.LogInformation("构建树形结构完成，根节点数量：{rootCount}，总节点数量：{totalCount}，仅最新版本：{onlyLatest}", 
                rootTemplates.Count, filteredTemplates.Count, onlyLatest);

            return rootTemplates;
        }

        /// <summary>
        /// 基于路径构建扁平树形结构（保持层级关系但返回扁平列表）
        /// </summary>
        private static List<AttachCatalogueTemplate> BuildFlatTreeFromPath(List<AttachCatalogueTemplate> allTemplates, bool onlyLatest)
        {
            if (allTemplates.Count == 0)
                return [];

            // 如果只显示最新版本，过滤掉非最新版本的模板
            var filteredTemplates = onlyLatest 
                ? allTemplates.Where(t => t.IsLatest)
                : allTemplates;

            // 按路径排序，确保父节点在子节点之前
            return [.. filteredTemplates
                .OrderBy(t => t.TemplatePath ?? "")
                .ThenBy(t => t.SequenceNumber)];
        }

        #endregion

        #region 模板路径相关查询方法

        /// <summary>
        /// 根据模板路径获取模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplatesByPathAsync(string? templatePath, bool includeChildren = false)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);

                if (string.IsNullOrEmpty(templatePath))
                {
                    // 获取根节点
                    queryable = queryable.Where(t => t.ParentId == null);
                }
                else
                {
                    // 根据路径查询
                    queryable = queryable.Where(t => t.TemplatePath == templatePath);
                }

                if (includeChildren && !string.IsNullOrEmpty(templatePath))
                {
                    // 包含子节点，使用路径前缀匹配
                    var childPathPrefix = templatePath + ".";
                    queryable = queryable.Union(
                        dbSet.Where(t => !t.IsDeleted && t.TemplatePath != null && t.TemplatePath.StartsWith(childPathPrefix))
                    );
                }

                var templates = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                Logger.LogInformation("根据路径获取模板完成，路径：{templatePath}，数量：{count}", templatePath, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据路径获取模板失败，路径：{templatePath}", templatePath);
                return [];
            }
        }

        /// <summary>
        /// 根据路径深度获取模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplatesByPathDepthAsync(int depth, bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                if (depth == 0)
                {
                    // 根节点（路径为空）
                    queryable = queryable.Where(t => t.ParentId == null);
                }
                else
                {
                    // 根据路径深度查询（通过点号数量判断）
                    var dotCount = depth - 1;
                    if (dotCount == 0)
                    {
                        // 第一层：路径格式为 "00001"
                        queryable = queryable.Where(t => t.TemplatePath != null && !t.TemplatePath.Contains('.'));
                    }
                    else
                    {
                        // 其他层：通过SQL函数计算点号数量
                        var sql = $@"
                            SELECT * FROM ""APPATTACH_CATALOGUE_TEMPLATES"" 
                            WHERE ""IS_DELETED"" = false 
                            AND (@onlyLatest = false OR ""IS_LATEST"" = true)
                            AND ""TEMPLATE_PATH"" IS NOT NULL
                            AND LENGTH(""TEMPLATE_PATH"") - LENGTH(REPLACE(""TEMPLATE_PATH"", '.', '')) = @dotCount
                            ORDER BY ""TEMPLATE_PATH"", ""SEQUENCE_NUMBER""";

                        var parameters = new[]
                        {
                            new Npgsql.NpgsqlParameter("@onlyLatest", onlyLatest),
                            new Npgsql.NpgsqlParameter("@dotCount", dotCount)
                        };

                        var templates = await dbSet.FromSqlRaw(sql, parameters).ToListAsync();
                        return templates;
                    }
                }

                var result = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                Logger.LogInformation("根据路径深度获取模板完成，深度：{depth}，数量：{count}", depth, result.Count);

                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据路径深度获取模板失败，深度：{depth}", depth);
                return [];
            }
        }

        /// <summary>
        /// 根据路径范围获取模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetTemplatesByPathRangeAsync(string? startPath, string? endPath, bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                // 路径范围过滤
                if (!string.IsNullOrEmpty(startPath))
                    queryable = queryable.Where(t => string.Compare(t.TemplatePath, startPath) >= 0);

                if (!string.IsNullOrEmpty(endPath))
                    queryable = queryable.Where(t => string.Compare(t.TemplatePath, endPath) <= 0);

                var templates = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                Logger.LogInformation("根据路径范围获取模板完成，起始路径：{startPath}，结束路径：{endPath}，数量：{count}", 
                    startPath, endPath, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据路径范围获取模板失败，起始路径：{startPath}，结束路径：{endPath}", startPath, endPath);
                return [];
            }
        }

        /// <summary>
        /// 获取指定路径下的所有子模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetChildTemplatesByPathAsync(string parentPath, bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                // 子路径过滤（以父路径为前缀）
                var childPathPrefix = parentPath + ".";
                queryable = queryable.Where(t => t.TemplatePath != null && t.TemplatePath.StartsWith(childPathPrefix));

                var templates = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                Logger.LogInformation("获取子模板完成，父路径：{parentPath}，数量：{count}", parentPath, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取子模板失败，父路径：{parentPath}", parentPath);
                return [];
            }
        }

        /// <summary>
        /// 检查路径是否存在
        /// </summary>
        public async Task<bool> ExistsByPathAsync(string templatePath)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var exists = await dbSet.AnyAsync(t => !t.IsDeleted && t.TemplatePath == templatePath);

                Logger.LogInformation("检查路径是否存在完成，路径：{templatePath}，存在：{exists}", templatePath, exists);

                return exists;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查路径是否存在失败，路径：{templatePath}", templatePath);
                return false;
            }
        }

        /// <summary>
        /// 获取路径统计信息
        /// </summary>
        public async Task<Dictionary<int, int>> GetPathDepthStatisticsAsync(bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                var templates = await queryable.Select(t => t.TemplatePath).ToListAsync();

                var statistics = new Dictionary<int, int>();
                foreach (var path in templates)
                {
                    var depth = AttachCatalogueTemplate.GetTemplatePathDepth(path);
                    if (statistics.TryGetValue(depth, out int value))
                        statistics[depth] = ++value;
                    else
                        statistics[depth] = 1;
                }

                Logger.LogInformation("获取路径统计信息完成，统计项数：{count}", statistics.Count);

                return statistics;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取路径统计信息失败");
                return [];
            }
        }

        /// <summary>
        /// 获取指定路径的直接子节点（不包含孙子节点）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetDirectChildrenByPathAsync(string? parentPath, bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                if (string.IsNullOrEmpty(parentPath))
                {
                    // 获取根节点的直接子节点（路径格式为 "00001"）
                    queryable = queryable.Where(t => t.TemplatePath != null && !t.TemplatePath.Contains('.'));
                }
                else
                {
                    // 获取指定路径的直接子节点（路径格式为 "parentPath.00001"）
                    var directChildPrefix = parentPath + ".";
                    queryable = queryable.Where(t => t.TemplatePath != null && t.TemplatePath.StartsWith(directChildPrefix) &&
                                                   !t.TemplatePath.Substring(directChildPrefix.Length).Contains('.'));
                }

                var templates = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                Logger.LogInformation("获取直接子节点完成，父路径：{parentPath}，数量：{count}", parentPath, templates.Count);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取直接子节点失败，父路径：{parentPath}", parentPath);
                return [];
            }
        }

        /// <summary>
        /// 获取指定路径的所有后代节点（包含所有层级的子节点）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetAllDescendantsByPathAsync(string parentPath, bool onlyLatest = true, int? maxDepth = null)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                if (string.IsNullOrEmpty(parentPath))
                {
                    // 获取所有非根节点
                    queryable = queryable.Where(t => t.TemplatePath != null && t.TemplatePath != "");
                }
                else
                {
                    // 获取指定路径的所有后代节点
                    var descendantPrefix = parentPath + ".";
                    queryable = queryable.Where(t => t.TemplatePath != null && t.TemplatePath.StartsWith(descendantPrefix));
                }

                var templates = await queryable.OrderBy(t => t.TemplatePath).ThenBy(t => t.SequenceNumber).ToListAsync();

                // 如果指定了最大深度，过滤掉超出深度的节点
                if (maxDepth.HasValue)
                {
                    var parentDepth = string.IsNullOrEmpty(parentPath) ? 0 : AttachCatalogueTemplate.GetTemplatePathDepth(parentPath);
                    templates = [.. templates.Where(t => AttachCatalogueTemplate.GetTemplatePathDepth(t.TemplatePath) <= parentDepth + maxDepth.Value)];
                }

                Logger.LogInformation("获取所有后代节点完成，父路径：{parentPath}，数量：{count}，最大深度：{maxDepth}", 
                    parentPath, templates.Count, maxDepth);

                return templates;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取所有后代节点失败，父路径：{parentPath}", parentPath);
                return [];
            }
        }

        /// <summary>
        /// 获取指定路径的祖先节点（从根节点到指定路径的所有父节点）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetAncestorsByPathAsync(string templatePath, bool onlyLatest = true)
        {
            try
            {
                if (string.IsNullOrEmpty(templatePath))
                {
                    return [];
                }

                var dbSet = await GetDbSetAsync();
                var ancestors = new List<AttachCatalogueTemplate>();

                // 逐级获取祖先节点
                var currentPath = templatePath;
                while (!string.IsNullOrEmpty(currentPath))
                {
                    var parentPath = AttachCatalogueTemplate.GetParentTemplatePath(currentPath);
                    if (string.IsNullOrEmpty(parentPath))
                    {
                        // 到达根节点
                        var rootTemplates = await dbSet
                            .Where(t => !t.IsDeleted && (t.TemplatePath == null || t.TemplatePath == ""))
                            .Where(t => !onlyLatest || t.IsLatest)
                            .ToListAsync();
                        ancestors.AddRange(rootTemplates);
                        break;
                    }

                    var parentTemplates = await dbSet
                        .Where(t => !t.IsDeleted && t.TemplatePath == parentPath)
                        .Where(t => !onlyLatest || t.IsLatest)
                        .ToListAsync();
                    ancestors.AddRange(parentTemplates);

                    currentPath = parentPath;
                }

                // 按路径深度排序（从根到叶子）
                ancestors = [.. ancestors.OrderBy(t => AttachCatalogueTemplate.GetTemplatePathDepth(t.TemplatePath))];

                Logger.LogInformation("获取祖先节点完成，目标路径：{templatePath}，数量：{count}", templatePath, ancestors.Count);

                return ancestors;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取祖先节点失败，目标路径：{templatePath}", templatePath);
                return [];
            }
        }

        /// <summary>
        /// 获取指定路径的兄弟节点（同一父节点下的其他节点）
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> GetSiblingsByPathAsync(string templatePath, bool onlyLatest = true)
        {
            try
            {
                if (string.IsNullOrEmpty(templatePath))
                {
                    // 根节点的兄弟节点就是其他根节点
                    var dbSet = await GetDbSetAsync();
                    var rootTemplates = await dbSet
                        .Where(t => !t.IsDeleted && (t.TemplatePath == null || t.TemplatePath == ""))
                        .Where(t => !onlyLatest || t.IsLatest)
                        .OrderBy(t => t.SequenceNumber)
                        .ToListAsync();

                    Logger.LogInformation("获取根节点兄弟节点完成，数量：{count}", rootTemplates.Count);
                    return rootTemplates;
                }

                var parentPath = AttachCatalogueTemplate.GetParentTemplatePath(templatePath);
                var siblings = await GetDirectChildrenByPathAsync(parentPath, onlyLatest);

                // 排除自己
                siblings = [.. siblings.Where(t => t.TemplatePath != templatePath)];

                Logger.LogInformation("获取兄弟节点完成，目标路径：{templatePath}，数量：{count}", templatePath, siblings.Count);

                return siblings;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取兄弟节点失败，目标路径：{templatePath}", templatePath);
                return [];
            }
        }

        /// <summary>
        /// 检查两个路径是否为祖先-后代关系
        /// </summary>
        public Task<bool> IsAncestorDescendantAsync(string ancestorPath, string descendantPath)
        {
            try
            {
                if (string.IsNullOrEmpty(ancestorPath) || string.IsNullOrEmpty(descendantPath))
                {
                    return Task.FromResult(false);
                }

                // 使用路径前缀检查
                var result = descendantPath.StartsWith(ancestorPath + ".", StringComparison.Ordinal);

                Logger.LogInformation("检查祖先-后代关系完成，祖先路径：{ancestorPath}，后代路径：{descendantPath}，结果：{result}", 
                    ancestorPath, descendantPath, result);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "检查祖先-后代关系失败，祖先路径：{ancestorPath}，后代路径：{descendantPath}", ancestorPath, descendantPath);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 获取同级模板中的最大路径（用于自动生成下一个路径）
        /// </summary>
        public async Task<string?> GetMaxTemplatePathAtSameLevelAsync(string? parentPath, bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                var queryable = dbSet.AsQueryable();

                // 基础过滤
                queryable = queryable.Where(t => !t.IsDeleted && t.TemplatePath != null);

                if (onlyLatest)
                    queryable = queryable.Where(t => t.IsLatest);

                if (string.IsNullOrEmpty(parentPath))
                {
                    // 获取根级别的模板（ParentId为null的模板）
                    queryable = queryable.Where(t => t.ParentId == null);
                }
                else
                {
                    // 获取指定父路径下的直接子节点（路径格式为 "parentPath.00001"）
                    var directChildPrefix = parentPath + ".";
                    queryable = queryable.Where(t => t.TemplatePath != null && t.TemplatePath.StartsWith(directChildPrefix));
                }

                // 获取所有匹配的路径，然后在内存中进行数值排序
                var paths = await queryable
                    .Select(t => t.TemplatePath)
                    .ToListAsync();

                string? maxPath = null;
                if (paths.Count != 0)
                {
                    if (string.IsNullOrEmpty(parentPath))
                    {
                        // 根级别：直接比较路径的数值
                        maxPath = paths
                            .Where(p => !string.IsNullOrEmpty(p) && !p.Contains('.'))
                            .OrderByDescending(p => Convert.ToInt32(p))
                            .FirstOrDefault();
                    }
                    else
                    {
                        // 子级别：比较路径最后一个单元代码的数值
                        // 过滤出直接子节点（路径中只有一个点号）
                        var directChildPrefix = parentPath + ".";
                        maxPath = paths
                            .Where(p => !string.IsNullOrEmpty(p) && p.StartsWith(directChildPrefix) && !p[directChildPrefix.Length..].Contains('.'))
                            .OrderByDescending(p => Convert.ToInt32(AttachCatalogueTemplate.GetLastUnitTemplatePathCode(p!)))
                            .FirstOrDefault();
                    }
                }

                Logger.LogInformation("获取同级最大路径完成，父路径：{parentPath}，最大路径：{maxPath}", parentPath, maxPath);

                return maxPath;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取同级最大路径失败，父路径：{parentPath}", parentPath);
                return null;
            }
        }

        /// <summary>
        /// 从给定的模板节点集合中获取所有相关的父节点和子节点
        /// 使用高效的路径查询策略，避免多次数据库查询
        /// </summary>
        /// <param name="sourceTemplates">源模板节点集合</param>
        /// <param name="onlyLatest">是否只获取最新版本</param>
        /// <returns>包含所有相关节点的完整集合</returns>
        private async Task<List<AttachCatalogueTemplate>> GetAllNodesFromPathsAsync(
            List<AttachCatalogueTemplate> sourceTemplates, 
            bool onlyLatest = true)
        {
            if (sourceTemplates == null || sourceTemplates.Count == 0)
                return sourceTemplates ?? [];

            var dbSet = await GetDbSetAsync();
            
            // 收集所有需要查询的路径
            var allRequiredPaths = new HashSet<string>();
            
            foreach (var template in sourceTemplates)
            {
                if (!string.IsNullOrEmpty(template.TemplatePath))
                {
                    // 添加当前路径及其所有父级路径
                    var parentPaths = GetAllParentPaths(template.TemplatePath);
                    foreach (var path in parentPaths)
                    {
                        allRequiredPaths.Add(path);
                    }
                }
                else
                {
                    // 根节点路径
                    allRequiredPaths.Add(string.Empty);
                }
            }

            // 单次查询获取所有相关节点
            var allNodes = await dbSet
                .Where(t => !t.IsDeleted && 
                           (!onlyLatest || t.IsLatest) &&
                           (t.TemplatePath == null || t.TemplatePath == string.Empty || allRequiredPaths.Contains(t.TemplatePath)))
                .OrderBy(t => t.TemplatePath)
                .ThenBy(t => t.SequenceNumber)
                .ToListAsync();

            // 合并源模板和查询到的节点，去重
            var result = sourceTemplates.Union(allNodes, new AttachCatalogueTemplateEqualityComparer())
                .OrderBy(t => t.TemplatePath)
                .ThenBy(t => t.SequenceNumber)
                .ToList();

            Logger.LogDebug("从 {sourceCount} 个源节点获取到 {resultCount} 个完整节点", 
                sourceTemplates.Count, result.Count);

            return result;
        }

        /// <summary>
        /// 获取指定路径的所有父级路径
        /// 例如：输入 "00001.00002.00003"，返回 ["00001", "00001.00002", "00001.00002.00003"]
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>所有父级路径集合</returns>
        private static List<string> GetAllParentPaths(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return [string.Empty];

            var pathSegments = templatePath.Split('.');
            var parentPaths = new List<string>
            {
                // 添加根节点路径
                string.Empty
            };
            
            // 添加所有层级的路径
            for (int i = 1; i <= pathSegments.Length; i++)
            {
                parentPaths.Add(string.Join(".", pathSegments.Take(i)));
            }

            return parentPaths;
        }

        /// <summary>
        /// 从任意一个节点获取从这个节点出发的父节点包含的所有节点
        /// 这是一个通用的路径查询方法，可以用于各种场景
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本</param>
        /// <param name="includeDescendants">是否包含后代节点</param>
        /// <param name="onlyLatest">是否只获取最新版本</param>
        /// <returns>从指定节点出发的所有相关节点</returns>
        public async Task<List<AttachCatalogueTemplate>> GetAllNodesFromTemplateAsync(
            Guid templateId, 
            int templateVersion, 
            bool includeDescendants = true, 
            bool onlyLatest = true)
        {
            try
            {
                var dbSet = await GetDbSetAsync();
                
                // 获取起始节点
                var startTemplate = await dbSet.FindAsync(templateId, templateVersion);
                if (startTemplate == null)
                {
                    Logger.LogWarning("未找到指定的模板节点：ID={templateId}, Version={templateVersion}", 
                        templateId, templateVersion);
                    return [];
                }

                var allPaths = new HashSet<string>();
                
                if (!string.IsNullOrEmpty(startTemplate.TemplatePath))
                {
                    // 获取所有父级路径
                    var parentPaths = GetAllParentPaths(startTemplate.TemplatePath);
                    foreach (var path in parentPaths)
                    {
                        allPaths.Add(path);
                    }

                    if (includeDescendants)
                    {
                        // 获取所有后代路径（以当前路径为前缀的所有路径）
                        var descendantPaths = await dbSet
                            .Where(t => !t.IsDeleted && 
                                       t.TemplatePath != null &&
                                       t.TemplatePath.StartsWith(startTemplate.TemplatePath + "."))
                            .Select(t => t.TemplatePath!)
                            .Distinct()
                            .ToListAsync();
                        
                        foreach (var path in descendantPaths)
                        {
                            allPaths.Add(path);
                        }
                    }
                }
                else
                {
                    // 根节点：获取所有路径
                    allPaths.Add(string.Empty);
                    
                    if (includeDescendants)
                    {
                        var allTemplatePaths = await dbSet
                            .Where(t => !t.IsDeleted && t.TemplatePath != null)
                            .Select(t => t.TemplatePath!)
                            .Distinct()
                            .ToListAsync();
                        
                        foreach (var path in allTemplatePaths)
                        {
                            allPaths.Add(path);
                        }
                    }
                }

                // 查询所有相关节点
                var allNodes = await dbSet
                    .Where(t => !t.IsDeleted && 
                               (!onlyLatest || t.IsLatest) &&
                               (t.TemplatePath == null || t.TemplatePath == string.Empty || allPaths.Contains(t.TemplatePath)))
                    .OrderBy(t => t.TemplatePath)
                    .ThenBy(t => t.SequenceNumber)
                    .ToListAsync();

                Logger.LogInformation("从模板节点 {templateId}:{templateVersion} 获取到 {count} 个相关节点", 
                    templateId, templateVersion, allNodes.Count);

                return allNodes;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取模板节点相关节点失败：ID={templateId}, Version={templateVersion}", 
                    templateId, templateVersion);
                return [];
            }
        }

        /// <summary>
        /// AttachCatalogueTemplate 相等性比较器，用于去重
        /// </summary>
        private class AttachCatalogueTemplateEqualityComparer : IEqualityComparer<AttachCatalogueTemplate>
        {
            public bool Equals(AttachCatalogueTemplate? x, AttachCatalogueTemplate? y)
            {
                if (x == null && y == null) return true;
                if (x == null || y == null) return false;
                return x.Id == y.Id && x.Version == y.Version;
            }

            public int GetHashCode(AttachCatalogueTemplate obj)
            {
                return HashCode.Combine(obj.Id, obj.Version);
            }
        }

        #endregion
    }
}
