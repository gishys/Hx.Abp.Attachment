using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 智能推荐应用服务实现
    /// 基于增强的语义匹配服务提供智能推荐功能
    /// </summary>
    public class IntelligentRecommendationAppService(
        IAttachCatalogueTemplateRepository templateRepository,
        IGuidGenerator guidGenerator,
        ILogger<IntelligentRecommendationAppService> logger) : ApplicationService, IIntelligentRecommendationAppService
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly ILogger<IntelligentRecommendationAppService> _logger = logger;

        /// <summary>
        /// 智能推荐模板
        /// </summary>
        public async Task<IntelligentRecommendationResultDto> RecommendTemplatesAsync(IntelligentRecommendationInputDto input)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("开始智能推荐模板，查询：{query}，用户：{userId}", 
                    input.Query, input.UserId);

                // 1. 直接在数据库层面进行智能推荐（包含相似度分数）
                var matchedTemplates = await _templateRepository.GetIntelligentRecommendationsAsync(
                    input.Query, 
                    input.Threshold, 
                    input.TopN, 
                    !input.IncludeHistory,
                    input.IncludeHistory);

                // 2. 构建推荐结果（使用数据库计算的分数）
                var recommendedTemplates = await BuildRecommendedTemplatesFromDatabaseAsync(matchedTemplates, input.Query);

                // 3. 计算推荐置信度
                var confidence = CalculateRecommendationConfidence(recommendedTemplates);

                stopwatch.Stop();

                var result = new IntelligentRecommendationResultDto
                {
                    Query = input.Query,
                    RecommendationType = "Database-Driven",
                    RecommendedTemplates = recommendedTemplates,
                    RecommendationReasons = GenerateRecommendationReasons(recommendedTemplates),
                    Confidence = confidence,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };

                _logger.LogInformation("智能推荐完成，找到 {count} 个推荐模板，置信度：{confidence}，耗时：{time}ms", 
                    recommendedTemplates.Count, confidence, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能推荐模板失败，查询：{query}", input.Query);
                throw new UserFriendlyException("智能推荐失败，请稍后重试");
            }
        }

        /// <summary>
        /// 基于现有模板生成新模板
        /// </summary>
        public async Task<AttachCatalogueTemplateDto> GenerateTemplateFromExistingAsync(GenerateTemplateFromExistingInputDto input)
        {
            try
            {
                _logger.LogInformation("开始基于现有模板生成新模板，基础模板：{baseTemplateId}", input.BaseTemplateId);

                // 1. 获取基础模板
                var baseTemplate = await _templateRepository.GetAsync(input.BaseTemplateId) ?? throw new UserFriendlyException("基础模板不存在");

                // 2. 生成新模板名称
                var newTemplateName = string.IsNullOrWhiteSpace(input.NewTemplateName) 
                    ? $"{baseTemplate.TemplateName}_Modified_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : input.NewTemplateName;

                // 3. 创建新模板
                var newTemplate = new AttachCatalogueTemplate(
                    _guidGenerator.Create(),
                    newTemplateName,
                    baseTemplate.AttachReceiveType,
                    baseTemplate.SequenceNumber,
                    baseTemplate.IsRequired,
                    baseTemplate.IsStatic,
                    input.InheritFromParent ? baseTemplate.Id : null,
                    baseTemplate.RuleExpression,
                    baseTemplate.Version + 1,
                    true,
                    baseTemplate.FacetType,
                    baseTemplate.TemplatePurpose,
                    baseTemplate.TextVector);

                // 4. 保存新模板
                await _templateRepository.InsertAsync(newTemplate);

                var result = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(newTemplate);

                _logger.LogInformation("新模板生成成功：{templateName}，ID：{templateId}", 
                    newTemplateName, newTemplate.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于现有模板生成新模板失败，基础模板：{baseTemplateId}", input.BaseTemplateId);
                throw;
            }
        }

        /// <summary>
        /// 智能分类推荐
        /// </summary>
        public async Task<IntelligentCatalogueRecommendationDto> RecommendCatalogueStructureAsync(IntelligentCatalogueRecommendationInputDto input)
        {
            try
            {
                _logger.LogInformation("开始智能分类推荐，业务描述：{description}，文件类型：{fileTypes}", 
                    input.BusinessDescription, string.Join(", ", input.FileTypes));

                // 1. 基于业务描述在数据库层面推荐模板
                var matchedTemplates = await _templateRepository.GetRecommendationsByBusinessAsync(
                    input.BusinessDescription,
                    input.FileTypes,
                    input.ExpectedLevels,
                    true);

                var recommendedTemplate = matchedTemplates.Count != 0
                    ? ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(matchedTemplates.First())
                    : null;

                // 2. 生成分类结构（基于数据库推荐结果优化）
                var recommendedCatalogues = GenerateCatalogueStructureFromDatabase(input, matchedTemplates);

                // 3. 计算推荐置信度
                var confidence = CalculateCatalogueRecommendationConfidence(recommendedCatalogues, recommendedTemplate);

                var result = new IntelligentCatalogueRecommendationDto
                {
                    RecommendedCatalogues = recommendedCatalogues,
                    RecommendedTemplate = recommendedTemplate,
                    Confidence = confidence,
                    RecommendationExplanation = GenerateCatalogueRecommendationExplanation(input, recommendedCatalogues)
                };

                _logger.LogInformation("智能分类推荐完成，推荐 {count} 个分类，置信度：{confidence}", 
                    recommendedCatalogues.Count, confidence);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能分类推荐失败，业务描述：{description}", input.BusinessDescription);
                throw new UserFriendlyException("智能分类推荐失败，请稍后重试");
            }
        }

        /// <summary>
        /// 批量智能推荐
        /// </summary>
        public async Task<BatchIntelligentRecommendationResultDto> BatchRecommendAsync(BatchIntelligentRecommendationInputDto input)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = new List<IntelligentRecommendationResultDto>();
            var errorMessages = new List<string>();

            try
            {
                _logger.LogInformation("开始批量智能推荐，查询数量：{count}", input.Queries.Count);

                foreach (var query in input.Queries)
                {
                    try
                    {
                        var result = await RecommendTemplatesAsync(query);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "批量推荐单个查询失败：{query}", query.Query);
                        errorMessages.Add($"查询 '{query.Query}' 失败: {ex.Message}");
                    }
                }

                stopwatch.Stop();

                var batchResult = new BatchIntelligentRecommendationResultDto
                {
                    Results = results,
                    SuccessCount = results.Count,
                    FailureCount = errorMessages.Count,
                    TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessages = errorMessages
                };

                _logger.LogInformation("批量智能推荐完成，成功：{success}，失败：{failure}，耗时：{time}ms", 
                    batchResult.SuccessCount, batchResult.FailureCount, stopwatch.ElapsedMilliseconds);

                return batchResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量智能推荐失败");
                throw new UserFriendlyException("批量智能推荐失败，请稍后重试");
            }
        }

        /// <summary>
        /// 学习用户偏好
        /// </summary>
        public Task<UserPreferenceLearningResultDto> LearnUserPreferenceAsync(UserPreferenceLearningInputDto input)
        {
            try
            {
                _logger.LogInformation("开始学习用户偏好，用户：{userId}，行为：{behavior}，模板：{templateId}", 
                    input.UserId, input.BehaviorType, input.TemplateId);

                // 简化实现：记录用户行为日志，实际学习逻辑由数据库驱动
                var result = new UserPreferenceLearningResultDto
                {
                    Success = true,
                    LearnedFeatures = ["用户行为记录", "数据库驱动学习"],
                    UpdatedWeights = [],
                    Message = $"成功记录用户偏好，用户：{input.UserId}，行为：{input.BehaviorType}"
                };

                _logger.LogInformation("用户偏好学习完成，用户：{userId}，行为：{behavior}", 
                    input.UserId, input.BehaviorType);

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "学习用户偏好失败，用户：{userId}", input.UserId);
                return Task.FromResult(new UserPreferenceLearningResultDto
                {
                    Success = false,
                    Message = $"学习失败: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// 获取推荐统计信息
        /// </summary>
        public Task<RecommendationStatisticsDto> GetRecommendationStatisticsAsync()
        {
            try
            {
                // 简化实现：返回基础统计信息，实际统计由数据库驱动
                var result = new RecommendationStatisticsDto
                {
                    TotalRecommendations = 0,
                    SuccessfulRecommendations = 0,
                    AverageScore = 0.0,
                    TopTemplates = [],
                    RecommendationTypeDistribution = new Dictionary<string, int>
                    {
                        ["Database-Driven"] = 0
                    },
                    UserPreferences = []
                };

                _logger.LogInformation("获取推荐统计信息完成");

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐统计信息失败");
                throw new UserFriendlyException("获取推荐统计信息失败");
            }
        }

        /// <summary>
        /// 智能更新模板关键字
        /// </summary>
        public async Task<bool> UpdateTemplateKeywordsIntelligentlyAsync(Guid templateId)
        {
            try
            {
                _logger.LogInformation("开始智能更新模板关键字，模板ID：{templateId}", templateId);

                await _templateRepository.UpdateTemplateKeywordsIntelligentlyAsync(templateId);

                _logger.LogInformation("智能更新模板关键字完成，模板ID：{templateId}", templateId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能更新模板关键字失败，模板ID：{templateId}", templateId);
                return false;
            }
        }

        /// <summary>
        /// 批量智能更新模板关键字
        /// </summary>
        public async Task<BatchKeywordUpdateResultDto> BatchUpdateTemplateKeywordsAsync(List<Guid> templateIds)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var successCount = 0;
            var failureCount = 0;
            var errorMessages = new List<string>();
            var updateDetails = new List<KeywordUpdateDetailDto>();

            try
            {
                _logger.LogInformation("开始批量智能更新模板关键字，模板数量：{count}", templateIds.Count);

                foreach (var templateId in templateIds)
                {
                    var detailStopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var updateDetail = new KeywordUpdateDetailDto
                    {
                        TemplateId = templateId,
                        TemplateName = string.Empty,
                        IsSuccess = false
                    };

                    try
                    {
                        // 获取模板信息
                        var template = await _templateRepository.GetAsync(templateId);
                        if (template != null)
                        {
                            updateDetail.TemplateName = template.TemplateName;
                            updateDetail.OldRuleExpression = template.RuleExpression;
                        }

                        var success = await UpdateTemplateKeywordsIntelligentlyAsync(templateId);
                        if (success)
                        {
                            successCount++;
                            updateDetail.IsSuccess = true;

                            // 获取更新后的信息
                            var updatedTemplate = await _templateRepository.GetAsync(templateId);
                            if (updatedTemplate != null)
                            {
                                updateDetail.NewRuleExpression = updatedTemplate.RuleExpression;
                            }
                        }
                        else
                        {
                            failureCount++;
                            updateDetail.ErrorMessage = "更新失败";
                            errorMessages.Add($"模板 {templateId} 更新失败");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        updateDetail.ErrorMessage = ex.Message;
                        errorMessages.Add($"模板 {templateId} 更新异常: {ex.Message}");
                        _logger.LogError(ex, "批量更新单个模板关键字失败：{templateId}", templateId);
                    }
                    finally
                    {
                        detailStopwatch.Stop();
                        updateDetail.ProcessingTimeMs = detailStopwatch.ElapsedMilliseconds;
                        updateDetails.Add(updateDetail);
                    }
                }

                stopwatch.Stop();

                var result = new BatchKeywordUpdateResultDto
                {
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessages = errorMessages,
                    UpdateDetails = updateDetails
                };

                _logger.LogInformation("批量智能更新模板关键字完成，成功：{success}，失败：{failure}，耗时：{time}ms", 
                    successCount, failureCount, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量智能更新模板关键字失败");
                throw new UserFriendlyException("批量智能更新模板关键字失败");
            }
        }







        #region 私有方法

        /// <summary>
        /// 基于数据库结果构建推荐模板
        /// </summary>
        private async Task<List<RecommendedTemplateDto>> BuildRecommendedTemplatesFromDatabaseAsync(
            List<AttachCatalogueTemplate> templates, 
            string query)
        {
            var recommendedTemplates = new List<RecommendedTemplateDto>();

            foreach (var template in templates)
            {
                // 使用数据库计算的相似度分数（通过反射获取，如果数据库返回了分数）
                var score = GetDatabaseCalculatedScore(template, templates.IndexOf(template));
                var matchType = DetermineMatchType(template, query);
                var reason = GenerateRecommendationReason(template, query, score, matchType);

                var recommendedTemplate = new RecommendedTemplateDto
                {
                    Template = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template),
                    Score = score,
                    MatchType = matchType,
                    Reason = reason,
                    IsNewTemplate = template.Version == 1,
                    UsageCount = await GetTemplateUsageCountAsync(template.Id)
                };

                recommendedTemplates.Add(recommendedTemplate);
            }

            return recommendedTemplates;
        }
        
        /// <summary>
        /// 获取数据库计算的相似度分数
        /// </summary>
        private static double GetDatabaseCalculatedScore(AttachCatalogueTemplate template, int rank)
        {
            // 优先使用数据库计算的分数（如果模板有扩展属性存储分数）
            // 否则基于排名位置估算分数
            if (template.ExtraProperties?.ContainsKey("MatchScore") == true)
            {
                var score = template.ExtraProperties["MatchScore"];
                if (score is double dbScore)
                    return Math.Max(0.1, Math.Min(1.0, dbScore));
            }
            
            // 基于排名位置估算分数（作为后备方案）
            return EstimateScoreFromDatabaseRank(rank, 100); // 假设总数为100
        }
        
        /// <summary>
        /// 基于数据库排序位置估算分数（后备方案）
        /// </summary>
        private static double EstimateScoreFromDatabaseRank(int rank, int totalCount)
        {
            if (totalCount == 0) return 0.0;
            
            // 基于排名位置计算分数，排名越靠前分数越高
            var baseScore = 1.0 - (rank * 0.1); // 每个位置递减0.1
            
            // 确保分数在合理范围内
            return Math.Max(0.3, Math.Min(1.0, baseScore));
        }

        /// <summary>
        /// 确定匹配类型
        /// </summary>
        private static string DetermineMatchType(AttachCatalogueTemplate template, string query)
        {
            var queryLower = query.ToLowerInvariant();
            
            // 1. 检查 RuleExpression 规则匹配（优先级较高）
            if (!string.IsNullOrEmpty(template.RuleExpression) && 
                HasRuleMatch(template.RuleExpression, queryLower))
            {
                return "Rule";
            }
            
            // 2. 默认基于模板名称匹配
            return "Name";
        }

        /// <summary>
        /// 检查规则匹配
        /// </summary>
        private static bool HasRuleMatch(string ruleExpression, string queryLower)
        {
            if (string.IsNullOrEmpty(ruleExpression)) return false;
            
            var ruleKeywords = new[] { "规则", "条件", "表达式", "workflow" };
            return ruleKeywords.Any(k => queryLower.Contains(k));
        }

        /// <summary>
        /// 生成推荐原因
        /// </summary>
        private static string GenerateRecommendationReason(AttachCatalogueTemplate template, string query, double score, string matchType)
        {
            return matchType switch
            {
                "Rule" => GenerateRuleReason(template, query, score),
                "Name" => GenerateNameReason(template, query, score),
                _ => $"基于 {matchType} 匹配，相似度 {score:F2}"
            };
        }
        
        /// <summary>
        /// 生成规则匹配原因
        /// </summary>
        private static string GenerateRuleReason(AttachCatalogueTemplate template, string query, double score)
        {
            var reason = $"规则匹配度高 ({score:F2})";
            
            if (!string.IsNullOrEmpty(template.RuleExpression))
            {
                reason += $"，使用规则引擎：{template.RuleExpression}";
            }
            
            if (query.Length > 10)
            {
                reason += "，查询内容规则丰富";
            }
            
            return reason;
        }
        
        /// <summary>
        /// 生成名称匹配原因
        /// </summary>
        private static string GenerateNameReason(AttachCatalogueTemplate template, string query, double score)
        {
            var reason = $"名称匹配 ({score:F2})";
            
            // 检查模板名称和查询的相似性
            var templateNameLower = template.TemplateName.ToLowerInvariant();
            var queryLower = query.ToLowerInvariant();
            
            if (templateNameLower.Contains(queryLower) || queryLower.Contains(templateNameLower))
            {
                reason += "，模板名称与查询高度相关";
            }
            else if (score > 0.7)
            {
                reason += "，模板名称与查询文本相似";
            }
            else
            {
                reason += "，基于名称相似度推荐";
            }
            
            return reason;
        }
        
        /// <summary>
        /// 从规则表达式中提取关键词
        /// </summary>
        private static List<string> ExtractKeywordsFromRuleExpression(string ruleExpression)
        {
            var keywords = new List<string>();
            
            if (string.IsNullOrEmpty(ruleExpression))
                return keywords;
                
            // 提取常见的规则关键词
            var commonKeywords = new[] { "WorkflowName", "DocumentType", "FileType", "Category", "Status" };
            
            foreach (var keyword in commonKeywords)
            {
                if (ruleExpression.Contains(keyword))
                {
                    keywords.Add(keyword);
                }
            }
            
            return keywords;
        }

        /// <summary>
        /// 生成推荐原因列表
        /// </summary>
        private static List<string> GenerateRecommendationReasons(List<RecommendedTemplateDto> templates)
        {
            var reasons = new List<string>();

            // 基于分数质量
            var highScoreCount = templates.Count(t => t.Score > 0.8);
            if (highScoreCount > 0)
                reasons.Add($"找到 {highScoreCount} 个高相似度匹配的模板");

            // 基于匹配类型
            var semanticCount = templates.Count(t => t.MatchType == "Semantic");
            if (semanticCount > 0)
                reasons.Add($"基于语义分析推荐了 {semanticCount} 个模板");

            var patternCount = templates.Count(t => t.MatchType == "Pattern");
            if (patternCount > 0)
                reasons.Add($"基于模式匹配推荐了 {patternCount} 个模板");

            // 基于模板特征
            var newTemplateCount = templates.Count(t => t.IsNewTemplate);
            if (newTemplateCount > 0)
                reasons.Add($"包含 {newTemplateCount} 个最新创建的模板");

            // 如果没有特定原因，添加通用说明
            if (reasons.Count == 0)
                reasons.Add("基于综合匹配算法进行智能推荐");

            return reasons;
        }

        /// <summary>
        /// 计算推荐置信度
        /// </summary>
        private static double CalculateRecommendationConfidence(List<RecommendedTemplateDto> templates)
        {
            if (templates.Count == 0)
                return 0.0;

            // 基于最高分数和平均分数计算置信度
            var maxScore = templates.Max(t => t.Score);
            var avgScore = templates.Average(t => t.Score);
            
            // 考虑匹配类型的影响
            var semanticCount = templates.Count(t => t.MatchType == "Semantic");
            var semanticFactor = semanticCount > 0 ? 0.1 : 0.0;
            
            // 考虑结果数量的影响（适度数量更可信）
            var countFactor = templates.Count switch
            {
                0 => 0.0,
                1 => 0.05,
                2 => 0.08,
                3 => 0.1,
                _ => 0.1 // 超过3个结果，置信度不再增加
            };

            return Math.Min(1.0, maxScore * 0.6 + avgScore * 0.25 + semanticFactor + countFactor);
        }

        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        private async Task<int> GetTemplateUsageCountAsync(Guid templateId)
        {
            try
            {
                return await _templateRepository.GetTemplateUsageCountAsync(templateId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板使用次数失败，模板ID：{templateId}", templateId);
                return 0; // 出错时返回0
            }
        }

        /// <summary>
        /// 基于数据库推荐结果生成分类结构
        /// </summary>
        private static List<RecommendedCatalogueDto> GenerateCatalogueStructureFromDatabase(
            IntelligentCatalogueRecommendationInputDto input,
            List<AttachCatalogueTemplate> matchedTemplates)
        {
            var catalogues = new List<RecommendedCatalogueDto>();

            // 基于数据库推荐的模板生成分类
            for (int i = 0; i < Math.Min(matchedTemplates.Count, input.ExpectedLevels); i++)
            {
                var template = matchedTemplates[i];
                var catalogue = new RecommendedCatalogueDto
                {
                    Id = template.Id,
                    CatalogueName = template.TemplateName,
                    Description = $"基于模板 '{template.TemplateName}' 的分类",
                    IsRequired = input.IncludeRequired && i == 0,
                    SequenceNumber = i + 1,
                    Score = 1.0 - (i * 0.1),
                    Children = []
                };

                catalogues.Add(catalogue);
            }

            // 如果数据库推荐不足，基于文件类型补充
            if (catalogues.Count < input.ExpectedLevels)
            {
                var remainingSlots = input.ExpectedLevels - catalogues.Count;
                for (int i = 0; i < Math.Min(input.FileTypes.Count, remainingSlots); i++)
                {
                    var fileType = input.FileTypes[i];
                    var catalogue = new RecommendedCatalogueDto
                    {
                        CatalogueName = $"{fileType.ToUpper()}文件",
                        Description = $"包含{fileType}格式的文件",
                        IsRequired = false,
                        SequenceNumber = catalogues.Count + 1,
                        Score = 0.8 - (i * 0.1),
                        Children = []
                    };

                    catalogues.Add(catalogue);
                }
            }

            return catalogues;
        }

        /// <summary>
        /// 计算分类推荐置信度
        /// </summary>
        private static double CalculateCatalogueRecommendationConfidence(List<RecommendedCatalogueDto> catalogues, AttachCatalogueTemplateDto? template)
        {
            var catalogueScore = catalogues.Count != 0 ? catalogues.Average(c => c.Score) : 0.0;
            var templateScore = template != null ? 1.0 : 0.0;

            return (catalogueScore * 0.7 + templateScore * 0.3);
        }

        /// <summary>
        /// 生成分类推荐说明
        /// </summary>
        private static string GenerateCatalogueRecommendationExplanation(
            IntelligentCatalogueRecommendationInputDto input, 
            List<RecommendedCatalogueDto> catalogues)
        {
            var explanation = $"基于业务描述 '{input.BusinessDescription}' 和文件类型 {string.Join(", ", input.FileTypes)}，";

            if (catalogues.Count != 0)
            {
                explanation += $"推荐了 {catalogues.Count} 个分类结构，";
                explanation += $"包含必收项：{catalogues.Count(c => c.IsRequired)} 个，";
                explanation += $"平均推荐分数：{catalogues.Average(c => c.Score):F2}";
            }
            else
            {
                explanation += "未能生成合适的分类结构";
            }

            return explanation;
        }

        #endregion
    }
}
