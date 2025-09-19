using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 阿里云AI服务 - 使用OpenNLU进行智能文本分析
    /// </summary>
    public partial class AliyunAIService(
        ILogger<AliyunAIService> logger, 
        HttpClient httpClient,
        SemanticVectorService semanticVectorService) : IScopedDependency
    {
        private readonly ILogger<AliyunAIService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly SemanticVectorService _semanticVectorService = semanticVectorService;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DASHSCOPE_API_KEY");
        private readonly string _workspaceId = Environment.GetEnvironmentVariable("ALIYUN_WORKSPACE_ID")
                ?? throw new UserFriendlyException("缺少环境变量 ALIYUN_WORKSPACE_ID");
        private readonly string _baseUrl = "https://dashscope.aliyuncs.com/api/v1/services/nlp/nlu/understanding";
        private readonly string _dashScopeUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// 生成文本摘要 - 使用DashScope API进行智能文本摘要提取
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>摘要内容</returns>
        public async Task<string> GenerateSummaryAsync(string content, int maxLength = 500)
        {
            try
            {
                _logger.LogInformation("开始使用DashScope API生成摘要，文本长度: {TextLength}, 最大长度: {MaxLength}", 
                    content.Length, maxLength);

                // 限制输入长度，避免超过token限制
                var processedContent = content.Length > 3000 ? content[..3000] + "..." : content;

                // 生成通用的摘要提取提示词
                var systemPrompt = GenerateSummaryExtractionSystemPrompt();
                var userPrompt = GenerateSummaryExtractionUserPrompt(processedContent, maxLength);

                var response = await CallDashScopeApiAsync(systemPrompt, userPrompt);
                
                if (!string.IsNullOrEmpty(response))
                {
                    var summary = ExtractSummaryFromResponse(response);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        _logger.LogInformation("DashScope API摘要生成成功，长度: {SummaryLength}", summary.Length);
                        return summary.Length > maxLength ? summary[..maxLength] : summary;
                    }
                }

                // 降级方案：使用简单文本摘要
                _logger.LogWarning("DashScope API摘要生成失败，使用简单文本摘要作为降级方案");
                return ExtractSummaryWithSimpleAnalysis(processedContent, maxLength);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashScope API摘要生成失败");
                // 降级到简单文本摘要
                return ExtractSummaryWithSimpleAnalysis(content, maxLength);
            }
        }

        /// <summary>
        /// 提取关键词 - 使用DashScope API进行智能关键词提取
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>关键词列表</returns>
        public async Task<List<string>> ExtractKeywordsAsync(string content, int keywordCount = 5)
        {
            try
            {
                _logger.LogInformation("开始使用DashScope API提取关键词，文本长度: {TextLength}, 关键词数量: {KeywordCount}",
                    content.Length, keywordCount);

                // 限制输入长度，避免超过token限制
                var processedContent = content.Length > 3000 ? content[..3000] + "..." : content;

                // 生成通用的关键词提取提示词
                var systemPrompt = GenerateKeywordExtractionSystemPrompt();
                var userPrompt = GenerateKeywordExtractionUserPrompt(processedContent, keywordCount);

                var response = await CallDashScopeApiAsync(systemPrompt, userPrompt);
                
                if (!string.IsNullOrEmpty(response))
                {
                    var keywords = ParseKeywordsFromJson(response);
                    if (keywords.Count > 0)
                    {
                        _logger.LogInformation("DashScope API关键词提取成功，提取数量: {KeywordCount}", keywords.Count);
                        return keywords;
                    }
                }

                // 降级方案：使用简单文本分析
                _logger.LogWarning("DashScope API提取失败，使用简单文本分析作为降级方案");
                return ExtractKeywordsWithSimpleAnalysis(processedContent, keywordCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DashScope API关键词提取失败");
                // 降级到简单文本分析
                return ExtractKeywordsWithSimpleAnalysis(content, keywordCount);
            }
        }

        /// <summary>
        /// 智能分类推荐 - 适用于AttachCatalogueTemplate智能推荐
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>推荐分类结果</returns>
        public async Task<ClassificationResult> ClassifyTextAsync(string content, List<string> categoryOptions)
        {
            try
            {
                _logger.LogInformation("开始调用阿里云AI进行智能分类，文本长度: {TextLength}, 分类选项数量: {CategoryCount}",
                    content.Length, categoryOptions.Count);

                if (categoryOptions.Count == 0)
                {
                    throw new UserFriendlyException("分类选项不能为空");
                }

                var request = new AliyunNLURequest
                {
                    Model = "opennlu-v1",
                    Input = new AliyunNLUInput
                    {
                        Sentence = content,
                        Task = "classification",
                        Labels = string.Join(",", categoryOptions)
                    }
                };

                var response = await CallAliyunNLUApiAsync(request);
                
                if (!string.IsNullOrEmpty(response.Output?.Text))
                {
                    var result = ParseClassificationResponse(response.Output.Text, categoryOptions);
                    _logger.LogInformation("阿里云AI智能分类成功，推荐分类: {RecommendedCategory}", result.RecommendedCategory);
                    return result;
                }

                _logger.LogWarning("阿里云AI智能分类响应为空或解析失败");
                throw new UserFriendlyException("阿里云AI返回的分类结果为空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云AI智能分类失败");
                throw new UserFriendlyException("阿里云AI智能分类服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 综合分析 - 同时进行摘要、关键词提取和分类推荐
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <param name="maxSummaryLength">摘要最大长度</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <param name="generateSemanticVector">是否生成语义向量</param>
        /// <returns>综合分析结果</returns>
        public async Task<ComprehensiveAnalysisResult> AnalyzeComprehensivelyAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5,
            bool generateSemanticVector = false)
        {
            try
            {
                _logger.LogInformation("开始进行综合分析，文本长度: {TextLength}", content.Length);

                // 并行执行所有分析任务
                var summaryTask = GenerateSummaryAsync(content, maxSummaryLength);
                var keywordsTask = ExtractKeywordsAsync(content, keywordCount);
                var classificationTask = ClassifyTextAsync(content, categoryOptions);

                await Task.WhenAll(summaryTask, keywordsTask, classificationTask);

                var result = new ComprehensiveAnalysisResult
                {
                    Summary = summaryTask.Result,
                    Keywords = keywordsTask.Result,
                    Classification = classificationTask.Result,
                    AnalysisTime = DateTime.Now,
                    Confidence = 0.9
                };

                // 生成语义向量
                if (generateSemanticVector)
                {
                    result.SemanticVector = await GenerateSemanticVectorAsync(result.Summary, result.Keywords);
                }

                _logger.LogInformation("综合分析完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "综合分析失败");
                throw new UserFriendlyException("综合分析服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 实体识别 - 从文本中识别指定类型的实体
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="entityTypes">要识别的实体类型列表</param>
        /// <param name="includePosition">是否包含实体位置信息</param>
        /// <returns>实体识别结果</returns>
        public async Task<EntityRecognitionResultDto> RecognizeEntitiesAsync(
            string content, 
            List<string> entityTypes, 
            bool includePosition = false)
        {
            try
            {
                _logger.LogInformation("开始调用阿里云AI进行实体识别，文本长度: {TextLength}, 实体类型数量: {EntityTypeCount}",
                    content.Length, entityTypes.Count);

                if (entityTypes.Count == 0)
                {
                    throw new UserFriendlyException("实体类型不能为空");
                }

                var request = new AliyunNLURequest
                {
                    Model = "opennlu-v1",
                    Input = new AliyunNLUInput
                    {
                        Sentence = content,
                        Task = "extraction",
                        Labels = string.Join(",", entityTypes)
                    }
                };

                var response = await CallAliyunNLUApiAsync(request);
                
                if (!string.IsNullOrEmpty(response.Output?.Text))
                {
                    var result = ParseEntityRecognitionResponse(response.Output.Text, entityTypes, includePosition);
                    _logger.LogInformation("阿里云AI实体识别成功，识别实体数量: {EntityCount}", result.Entities.Count);
                    return result;
                }

                _logger.LogWarning("阿里云AI实体识别响应为空或解析失败");
                throw new UserFriendlyException("阿里云AI返回的实体识别结果为空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云AI实体识别失败");
                throw new UserFriendlyException("阿里云AI实体识别服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 分类名称推荐 - 基于文档内容推荐合适的分类名称
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="businessDomain">业务领域</param>
        /// <param name="documentType">文档类型</param>
        /// <param name="recommendationCount">推荐数量</param>
        /// <returns>分类名称推荐结果</returns>
        public async Task<CategoryNameRecommendationResultDto> RecommendCategoryNamesAsync(
            string content, 
            string? businessDomain = null, 
            string? documentType = null, 
            int recommendationCount = 5)
        {
            try
            {
                _logger.LogInformation("开始调用阿里云AI进行分类名称推荐，文本长度: {TextLength}, 推荐数量: {RecommendationCount}",
                    content.Length, recommendationCount);

                // 1) 构建候选分类标签（常见证照/文档类型 + 业务/文档类型的补充）
                var candidateLabels = BuildDefaultCategoryLabels(businessDomain, documentType);

                // 2) 首选使用 分类(classification) 任务让模型在候选分类中打分
                var classifyRequest = new AliyunNLURequest
                {
                    Model = "opennlu-v1",
                    Input = new AliyunNLUInput
                    {
                        Sentence = content,
                        Task = "classification",
                        Labels = string.Join(",", candidateLabels)
                    }
                };

                var response = await CallAliyunNLUApiAsync(classifyRequest);

                if (!string.IsNullOrEmpty(response.Output?.Text))
                {
                    // 0) 快速路径：如果模型直接返回“结清证明;不动产登记证明;”这类列表，直接解析
                    var direct = ExtractDirectCategoryList(
                        response.Output.Text, recommendationCount, candidateLabels);
                    if (direct.Count > 0)
                    {
                        return new CategoryNameRecommendationResultDto
                        {
                            RecommendedCategories = direct,
                            Confidence = Math.Min(0.92, 0.75 + direct.Count * 0.03),
                            RecommendationTime = DateTime.Now,
                            Metadata = new CategoryRecommendationMetadata
                            {
                                TextLength = content.Length,
                                ProcessingTimeMs = 0,
                                Model = "opennlu-v1",
                                RecommendedCategoryCount = direct.Count,
                                IdentifiedBusinessDomain = businessDomain,
                                IdentifiedDocumentType = documentType
                            }
                        };
                    }

                    // 从分类响应中解析 Top-N 分类
                    var classified = ExtractCategoriesFromClassificationText(
                        response.Output.Text, recommendationCount, candidateLabels);

                    // 如果分类有结果，直接返回
                    if (classified.Count > 0)
                    {
                        return new CategoryNameRecommendationResultDto
                        {
                            RecommendedCategories = classified,
                            Confidence = Math.Min(0.95, 0.6 + classified.Count * 0.05),
                            RecommendationTime = DateTime.Now,
                            Metadata = new CategoryRecommendationMetadata
                            {
                                TextLength = content.Length,
                                ProcessingTimeMs = 0,
                                Model = "opennlu-v1",
                                RecommendedCategoryCount = classified.Count,
                                IdentifiedBusinessDomain = businessDomain,
                                IdentifiedDocumentType = documentType
                            }
                        };
                    }

                    // 3) 若分类无效，尝试从文本中抽取分类片段作为备选
                    var extractedByText = ExtractCategoriesFromResponse(response.Output.Text, recommendationCount);
                    if (extractedByText.Count > 0)
                    {
                        return new CategoryNameRecommendationResultDto
                        {
                            RecommendedCategories = extractedByText,
                            Confidence = 0.7,
                            RecommendationTime = DateTime.Now,
                            Metadata = new CategoryRecommendationMetadata
                            {
                                TextLength = content.Length,
                                ProcessingTimeMs = 0,
                                Model = "opennlu-v1",
                                RecommendedCategoryCount = extractedByText.Count,
                                IdentifiedBusinessDomain = businessDomain,
                                IdentifiedDocumentType = documentType
                            }
                        };
                    }
                }

                _logger.LogWarning("阿里云AI分类名称推荐响应为空或解析失败");
                // 4) 规则启发式：基于关键字从内容中提议分类，作为降级
                var heuristic = HeuristicCategoriesFromContent(content, recommendationCount);
                if (heuristic.Count > 0)
                {
                    return new CategoryNameRecommendationResultDto
                    {
                        RecommendedCategories = heuristic,
                        Confidence = 0.6,
                        RecommendationTime = DateTime.Now,
                        Metadata = new CategoryRecommendationMetadata
                        {
                            TextLength = content.Length,
                            ProcessingTimeMs = 0,
                            Model = "opennlu-v1",
                            RecommendedCategoryCount = heuristic.Count,
                            IdentifiedBusinessDomain = businessDomain,
                            IdentifiedDocumentType = documentType
                        }
                    };
                }

                // 5) 最终兜底：领域/类型/通用默认建议
                var fallback = GenerateDefaultCategorySuggestions(businessDomain, documentType, recommendationCount);
                if (fallback.Count > 0)
                {
                    return new CategoryNameRecommendationResultDto
                    {
                        RecommendedCategories = fallback,
                        Confidence = 0.5,
                        RecommendationTime = DateTime.Now,
                        Metadata = new CategoryRecommendationMetadata
                        {
                            TextLength = content.Length,
                            ProcessingTimeMs = 0,
                            Model = "opennlu-v1",
                            RecommendedCategoryCount = fallback.Count,
                            IdentifiedBusinessDomain = businessDomain,
                            IdentifiedDocumentType = documentType
                        }
                    };
                }

                throw new UserFriendlyException("未能生成有效的分类名称建议");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云AI分类名称推荐失败");
                throw new UserFriendlyException("阿里云AI分类名称推荐服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 调用阿里云NLU API
        /// </summary>
        /// <param name="request">请求参数</param>
        /// <returns>API响应</returns>
        private async Task<AliyunNLUResponse> CallAliyunNLUApiAsync(AliyunNLURequest request)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
                {
                    Content = httpContent
                };

                // 添加必要的请求头
                httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
                httpRequest.Headers.Add("X-DashScope-WorkSpace", _workspaceId);

                _logger.LogDebug("发送阿里云NLU API请求: {Request}", jsonContent);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("阿里云NLU API响应: {Response}", responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonSerializer.Deserialize<AliyunErrorResponse>(responseContent, _jsonOptions);
                    _logger.LogError("阿里云NLU API调用失败: {ErrorCode} - {ErrorMessage}", 
                        errorResponse?.Code, errorResponse?.Message);
                    throw new UserFriendlyException($"阿里云AI服务调用失败: {errorResponse?.Message ?? response.StatusCode.ToString()}");
                }

                var result = JsonSerializer.Deserialize<AliyunNLUResponse>(responseContent, _jsonOptions) ?? throw new UserFriendlyException("阿里云AI服务返回的响应格式无效");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "解析阿里云NLU API响应失败");
                throw new UserFriendlyException("阿里云AI服务响应格式错误");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "阿里云NLU API网络请求失败");
                throw new UserFriendlyException("阿里云AI服务网络连接失败");
            }
        }

        /// <summary>
        /// 生成语义向量
        /// </summary>
        /// <param name="summary">摘要内容</param>
        /// <param name="keywords">关键词列表</param>
        /// <returns>语义向量</returns>
        public async Task<List<double>?> GenerateSemanticVectorAsync(string summary, List<string> keywords)
        {
            try
            {
                var vectorText = $"{summary} {string.Join(" ", keywords)}";
                return await _semanticVectorService.GenerateVectorAsync(vectorText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成语义向量失败，继续处理其他功能");
                return null;
            }
        }

        /// <summary>
        /// 从响应中提取关键词
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>提取的关键词列表</returns>
        private static List<string> ExtractKeywordsFromResponse(string responseText, int keywordCount)
        {
            var keywords = new List<string>();
            
            if (string.IsNullOrWhiteSpace(responseText))
                return keywords;

            // 尝试提取关键词字段
            var keywordMatch = AliyunAIService.KeywordsFieldRegex().Match(responseText);
            if (keywordMatch.Success)
            {
                var keywordText = keywordMatch.Groups[1].Value.Trim();
                // 按制表符、逗号、分号等分隔符分割
                var extractedKeywords = keywordText
                    .SplitEfficient(['\t', ',', '，', ';', '；', ' '])
                    .Select(k => k.Trim())
                    .Where(k => !string.IsNullOrEmpty(k) && k != "None")
                    .Take(keywordCount)
                    .ToList();

                keywords.AddRange(extractedKeywords);
            }

            // 如果关键词不够，尝试从其他字段提取
            if (keywords.Count < keywordCount)
            {
                var remainingCount = keywordCount - keywords.Count;
                
                // 尝试从重要词汇字段提取
                var importantMatch = AliyunAIService.ImportantWordsFieldRegex().Match(responseText);
                if (importantMatch.Success)
                {
                    var importantText = importantMatch.Groups[1].Value.Trim();
                    var additionalKeywords = importantText
                        .SplitEfficient(['\t', ',', '，', ';', '；', ' '])
                        .Select(k => k.Trim())
                        .Where(k => !string.IsNullOrEmpty(k) && k != "None" && !keywords.Contains(k))
                        .Take(remainingCount)
                        .ToList();

                    keywords.AddRange(additionalKeywords);
                    remainingCount = keywordCount - keywords.Count;
                }

                // 尝试从核心概念字段提取
                if (remainingCount > 0)
                {
                    var conceptMatch = AliyunAIService.CoreConceptFieldRegex().Match(responseText);
                    if (conceptMatch.Success)
                    {
                        var conceptText = conceptMatch.Groups[1].Value.Trim();
                        var conceptKeywords = conceptText
                            .SplitEfficient(['\t', ',', '，', ';', '；', ' '])
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrEmpty(k) && k != "None" && !keywords.Contains(k))
                            .Take(remainingCount)
                            .ToList();

                        keywords.AddRange(conceptKeywords);
                    }
                }
            }

            return keywords;
        }

        /// <summary>
        /// 解析智能分类响应
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>分类结果</returns>
        private static ClassificationResult ParseClassificationResponse(string responseText, List<string> categoryOptions)
        {
            var result = new ClassificationResult();

            if (string.IsNullOrWhiteSpace(responseText) || categoryOptions.Count == 0)
            {
                result.RecommendedCategory = categoryOptions.FirstOrDefault();
                result.Confidence = 0.0;
                return result;
            }

            try
            {
                // 尝试提取分类字段
                var classificationMatch = ClassificationFieldRegex().Match(responseText);
                if (classificationMatch.Success)
                {
                    var categoryText = classificationMatch.Groups[1].Value.Trim();
                    var categoryPairs = categoryText
                        .SplitEfficient(['\t', ',', '，', ';', '；', ' '])
                        .Select(pair => pair.Trim().Split(':', 2))
                        .Where(pair => pair.Length == 2)
                        .ToDictionary(pair => pair[0].Trim(), pair => double.Parse(pair[1].Trim()));

                    // 找到推荐分类 - 优先从提供的分类选项中选择
                    var validCategoryPairs = categoryPairs
                        .Where(pair => categoryOptions.Contains(pair.Key))
                        .OrderByDescending(c => c.Value)
                        .ToList();

                    if (validCategoryPairs.Count > 0)
                    {
                        var recommendedCategory = validCategoryPairs.First();
                        result.RecommendedCategory = recommendedCategory.Key;
                        result.Confidence = recommendedCategory.Value;
                    }
                    else
                    {
                        // 如果没有匹配的分类选项，选择置信度最高的
                        var recommendedCategory = categoryPairs.OrderByDescending(c => c.Value).FirstOrDefault();
                        if (recommendedCategory.Key != null)
                        {
                            result.RecommendedCategory = recommendedCategory.Key;
                            result.Confidence = recommendedCategory.Value;
                        }
                        else
                        {
                            result.RecommendedCategory = categoryOptions.FirstOrDefault();
                            result.Confidence = 0.0;
                        }
                    }
                }
                else
                {
                    // 如果没有找到分类字段，尝试从响应文本中直接匹配分类选项
                    var matchedCategory = categoryOptions.FirstOrDefault(category => 
                        responseText.Contains(category, StringComparison.OrdinalIgnoreCase));
                    
                    result.RecommendedCategory = matchedCategory ?? categoryOptions.FirstOrDefault();
                    result.Confidence = matchedCategory != null ? 0.7 : 0.0;
                }
            }
            catch (Exception)
            {
                // 解析失败时返回默认结果
                result.RecommendedCategory = categoryOptions.FirstOrDefault();
                result.Confidence = 0.0;
            }

            return result;
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"摘要:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex SummaryRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"主要信息[:：]\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex MainInfoFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"核心内容[:：]\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex CoreContentFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"\s+")]
        private static partial System.Text.RegularExpressions.Regex WhitespaceRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"text""\s*:\s*""([^""]+)""")]
        private static partial System.Text.RegularExpressions.Regex TextFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"关键词:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex KeywordsFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"重要词汇:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex ImportantWordsFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"核心概念:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex CoreConceptFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"分类:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex ClassificationFieldRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"\{.*\}")]
        private static partial System.Text.RegularExpressions.Regex JsonObjectRegex();

        /// <summary>
        /// 解析实体识别响应
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="entityTypes">实体类型列表</param>
        /// <param name="includePosition">是否包含位置信息</param>
        /// <returns>实体识别结果</returns>
        private static EntityRecognitionResultDto ParseEntityRecognitionResponse(
            string responseText, 
            List<string> entityTypes, 
            bool includePosition)
        {
            var result = new EntityRecognitionResultDto
            {
                Entities = [],
                Confidence = 0.8,
                RecognitionTime = DateTime.Now,
                EntityTypeCounts = entityTypes.ToDictionary(et => et, _ => 0),
                Metadata = new EntityRecognitionMetadata
                {
                    TextLength = 0,
                    ProcessingTimeMs = 0,
                    Model = "opennlu-v1",
                    RecognizedEntityTypeCount = 0,
                    TotalEntityCount = 0
                }
            };

            if (string.IsNullOrWhiteSpace(responseText))
                return result;

            try
            {
                // 尝试从响应中提取实体信息
                var entities = ExtractEntitiesFromResponse(responseText, entityTypes, includePosition);
                result.Entities = entities;
                result.Metadata!.TotalEntityCount = entities.Count;

                // 统计各类型实体数量
                foreach (var entity in entities)
                {
                    if (result.EntityTypeCounts.TryGetValue(entity.Type, out int value))
                    {
                        result.EntityTypeCounts[entity.Type] = ++value;
                    }
                }

                result.Metadata!.RecognizedEntityTypeCount = result.EntityTypeCounts.Count(kvp => kvp.Value > 0);
                result.Confidence = entities.Count > 0 ? 0.9 : 0.5;
            }
            catch (Exception)
            {
                // 解析失败时返回默认结果
                result.Confidence = 0.3;
            }

            return result;
        }

        /// <summary>
        /// 从响应中提取实体信息
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="entityTypes">实体类型列表</param>
        /// <param name="includePosition">是否包含位置信息</param>
        /// <returns>实体列表</returns>
        private static List<RecognizedEntity> ExtractEntitiesFromResponse(
            string responseText, 
            List<string> entityTypes, 
            bool includePosition)
        {
            var entities = new List<RecognizedEntity>();

            try
            {
                // 尝试解析JSON格式的响应
                var jsonMatch = AliyunAIService.JsonObjectRegex().Match(responseText);
                if (jsonMatch.Success)
                {
                    var jsonText = jsonMatch.Value;
                    var response = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
                    
                    if (response != null && response.TryGetValue("entities", out object? value))
                    {
                        // 处理结构化的实体数据
                        var entitiesData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(value.ToString() ?? "[]");
                        foreach (var entityData in entitiesData ?? [])
                        {
                            var entity = new RecognizedEntity
                            {
                                Name = entityData.GetValueOrDefault("name", "").ToString() ?? "",
                                Type = entityData.GetValueOrDefault("type", "").ToString() ?? "",
                                Value = entityData.GetValueOrDefault("value", "").ToString() ?? "",
                                Confidence = Convert.ToDouble(entityData.GetValueOrDefault("confidence", 0.8))
                            };

                            if (includePosition)
                            {
                                entity.StartPosition = Convert.ToInt32(entityData.GetValueOrDefault("start", 0));
                                entity.EndPosition = Convert.ToInt32(entityData.GetValueOrDefault("end", 0));
                            }

                            entities.Add(entity);
                        }
                    }
                }

                // 如果没有找到结构化数据，尝试从文本中提取
                if (entities.Count == 0)
                {
                    entities = ExtractEntitiesFromText(responseText, entityTypes);
                }
            }
            catch (Exception)
            {
                // 解析失败时使用文本提取作为备选方案
                entities = ExtractEntitiesFromText(responseText, entityTypes);
            }

            return entities;
        }

        /// <summary>
        /// 从文本中提取实体信息（备选方案）
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="entityTypes">实体类型列表</param>
        /// <returns>实体列表</returns>
        private static List<RecognizedEntity> ExtractEntitiesFromText(string text, List<string> entityTypes)
        {
            var entities = new List<RecognizedEntity>();

            // 简单的文本模式匹配提取
            foreach (var entityType in entityTypes)
            {
                var pattern = $@"{entityType}[:：]\s*([^，,;；\n\r]+)";
                var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var entityValue = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(entityValue))
                        {
                            entities.Add(new RecognizedEntity
                            {
                                Name = entityValue,
                                Type = entityType,
                                Value = entityValue,
                                Confidence = 0.7
                            });
                        }
                    }
                }
            }

            return entities;
        }

        /// <summary>
        /// 解析分类名称推荐响应
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="businessDomain">业务领域</param>
        /// <param name="documentType">文档类型</param>
        /// <param name="recommendationCount">推荐数量</param>
        /// <returns>分类名称推荐结果</returns>
        private static CategoryNameRecommendationResultDto ParseCategoryNameRecommendationResponse(
            string responseText, 
            string? businessDomain, 
            string? documentType, 
            int recommendationCount)
        {
            var result = new CategoryNameRecommendationResultDto
            {
                RecommendedCategories = [],
                Confidence = 0.8,
                RecommendationTime = DateTime.Now,
                Metadata = new CategoryRecommendationMetadata
                {
                    TextLength = 0,
                    ProcessingTimeMs = 0,
                    Model = "opennlu-v1",
                    RecommendedCategoryCount = 0,
                    IdentifiedBusinessDomain = businessDomain,
                    IdentifiedDocumentType = documentType
                }
            };

            if (string.IsNullOrWhiteSpace(responseText))
                return result;

            try
            {
                // 尝试从响应中提取分类信息
                var categories = ExtractCategoriesFromResponse(responseText, recommendationCount);
                
                // 验证提取的分类是否有效
                var validCategories = categories.Where(c => 
                    !string.IsNullOrEmpty(c.Name) && 
                    !c.Name.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                    !c.Name.Equals("无", StringComparison.OrdinalIgnoreCase) &&
                    !c.Name.Equals("空", StringComparison.OrdinalIgnoreCase) &&
                    c.Name.Length > 1).ToList();
                
                result.RecommendedCategories = validCategories;
                result.Metadata.RecommendedCategoryCount = validCategories.Count;
                
                // 根据有效分类数量调整置信度
                if (validCategories.Count > 0)
                {
                    result.Confidence = Math.Min(0.9, 0.5 + (validCategories.Count * 0.1));
                }
                else
                {
                    result.Confidence = 0.3;
                    // 如果没有有效分类，尝试生成一些默认分类建议
                    result.RecommendedCategories = GenerateDefaultCategorySuggestions(businessDomain, documentType, recommendationCount);
                }
            }
            catch (Exception)
            {
                // 解析失败时返回默认结果
                result.Confidence = 0.3;
                result.RecommendedCategories = GenerateDefaultCategorySuggestions(businessDomain, documentType, recommendationCount);
            }

            return result;
    }

    /// <summary>
        /// 从响应中提取分类信息
    /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <param name="recommendationCount">推荐数量</param>
        /// <returns>推荐分类列表</returns>
        private static List<RecommendedCategory> ExtractCategoriesFromResponse(string responseText, int recommendationCount)
        {
            var categories = new List<RecommendedCategory>();

            try
            {
                // 尝试解析JSON格式的响应
                var jsonMatch = AliyunAIService.JsonObjectRegex().Match(responseText);
                if (jsonMatch.Success)
                {
                    var jsonText = jsonMatch.Value;
                    var response = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
                    
                    if (response != null && response.TryGetValue("categories", out object? value))
                    {
                        // 处理结构化的分类数据
                        var categoriesData = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(value.ToString() ?? "[]");
                        foreach (var categoryData in categoriesData ?? [])
                        {
                            if (categories.Count >= recommendationCount) break;

                            var category = new RecommendedCategory
                            {
                                Name = categoryData.GetValueOrDefault("name", "").ToString() ?? "",
                                Description = categoryData.GetValueOrDefault("description", "").ToString(),
                                Confidence = Convert.ToDouble(categoryData.GetValueOrDefault("confidence", 0.8)),
                                Level = Convert.ToInt32(categoryData.GetValueOrDefault("level", 1))
                            };

                            if (categoryData.TryGetValue("keywords", out object? keywordsValue))
                            {
                                var keywords = JsonSerializer.Deserialize<List<string>>(keywordsValue.ToString() ?? "[]");
                                category.Keywords = keywords ?? [];
                            }

                            categories.Add(category);
                        }
                    }
                }

                // 如果没有找到结构化数据，尝试从文本中提取
                if (categories.Count == 0)
                {
                    categories = ExtractCategoriesFromText(responseText, recommendationCount);
                }
            }
            catch (Exception)
            {
                // 解析失败时使用文本提取作为备选方案
                categories = ExtractCategoriesFromText(responseText, recommendationCount);
            }

            return categories;
    }

    /// <summary>
        /// 从分类(classification)任务的文本结果中提取Top-N分类（形如：类别A:0.92, 类别B:0.76,...）
        /// 仅接受在候选集合中的类别。
    /// </summary>
        private static List<RecommendedCategory> ExtractCategoriesFromClassificationText(
            string responseText, int recommendationCount, List<string> candidateLabels)
        {
            var result = new List<RecommendedCategory>();
            if (string.IsNullOrWhiteSpace(responseText)) return result;

            var match = ClassificationFieldRegex().Match(responseText);
            if (!match.Success) return result;

            var pairs = match.Groups[1].Value
                .SplitEfficient(['\t', ',', '，', ';', '；', ' '])
                .Select(s => s.Trim())
                .Select(s => s.Split(':', 2))
                .Where(a => a.Length == 2)
                .Select(a => new { Name = a[0].Trim(), ScoreText = a[1].Trim() })
                .Where(a => !string.IsNullOrEmpty(a.Name) && candidateLabels.Contains(a.Name))
                .Select(a => new { a.Name, Score = double.TryParse(a.ScoreText, out var v) ? v : 0.0 })
                .OrderByDescending(a => a.Score)
                .Take(recommendationCount)
                .ToList();

            foreach (var p in pairs)
            {
                if (string.Equals(p.Name, "None", StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(new RecommendedCategory
                {
                    Name = p.Name,
                    Confidence = Math.Clamp(p.Score, 0.0, 1.0),
                    Level = 1
                });
            }
            return result;
    }

    /// <summary>
        /// 快速解析直接列表样式的分类结果，如："结清证明;不动产登记证明;" 或换行/逗号分隔。
        /// 仅保留在候选集合中或通过基础后缀白名单校验的条目。
    /// </summary>
        private static List<RecommendedCategory> ExtractDirectCategoryList(
            string responseText, int recommendationCount, List<string> candidateLabels)
        {
            var list = new List<RecommendedCategory>();
            if (string.IsNullOrWhiteSpace(responseText)) return list;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var tokens = responseText
                .SplitEfficient(['\n', '\r', ';', '；', ',', '，'])
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            foreach (var token in tokens)
            {
                if (list.Count >= recommendationCount) break;
                if (string.Equals(token, "None", StringComparison.OrdinalIgnoreCase)) continue;
                if (token.Length < 2) continue;
                if (!seen.Add(token)) continue;

                // 候选集合优先；否则应用简易白名单（以“证”“证明”“书”结尾）
                var pass = candidateLabels.Contains(token)
                           || token.EndsWith("证", StringComparison.OrdinalIgnoreCase)
                           || token.EndsWith("证明", StringComparison.OrdinalIgnoreCase)
                           || token.EndsWith("书", StringComparison.OrdinalIgnoreCase);
                if (!pass) continue;

                list.Add(new RecommendedCategory
                {
                    Name = token,
                    Confidence = 0.8,
                    Level = 1
                });
            }

            return list;
        }

    /// <summary>
        /// 规则启发式：根据内容中的关键词推断可能的分类（作为降级方案）。
    /// </summary>
        private static List<RecommendedCategory> HeuristicCategoriesFromContent(string content, int recommendationCount)
        {
            var suggestions = new List<RecommendedCategory>();
            if (string.IsNullOrWhiteSpace(content)) return suggestions;

            var map = new (string Keyword, string Category)[]
            {
                ("身份证", "身份证"),
                ("居民身份证", "身份证"),
                ("护照", "护照"),
                ("驾驶证", "驾驶证"),
                ("合同", "合同"),
                ("协议", "合同"),
                ("发票", "发票"),
                ("收据", "发票"),
                ("证明", "证明"),
                ("结清证明", "证明"),
                ("不动产登记", "不动产登记证明"),
                ("房产证", "不动产登记证明"),
                ("营业执照", "营业执照"),
                ("报告", "报告"),
                ("申请", "申请书"),
                ("批复", "批复"),
                ("通知", "通知"),
            };

            var lowered = content;
            foreach (var (keyword, category) in map)
            {
                if (lowered.Contains(keyword, StringComparison.OrdinalIgnoreCase) &&
                    !suggestions.Any(s => s.Name.Equals(category, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestions.Add(new RecommendedCategory
                    {
                        Name = category,
                        Confidence = 0.65,
                        Level = 1,
                        Keywords = [keyword]
                    });
                    if (suggestions.Count >= recommendationCount) break;
                }
            }
            return suggestions;
    }

    /// <summary>
        /// 构建默认候选分类集合（常见证照/文档类型 + 领域/类型补充）
    /// </summary>
        private static List<string> BuildDefaultCategoryLabels(string? businessDomain, string? documentType)
        {
            var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                // 常见证照/文档类型
                "身份证","护照","驾驶证","营业执照",
                "合同","协议","发票","收据",
                "证明","结清证明","不动产登记证明",
                "报告","申请书","批复","通知"
            };

            if (!string.IsNullOrWhiteSpace(documentType))
            {
                labels.Add(documentType);
                labels.Add(documentType + "类文档");
            }
            if (!string.IsNullOrWhiteSpace(businessDomain))
            {
                labels.Add(businessDomain + "相关文件");
                labels.Add(businessDomain + "证明");
            }

            return [.. labels];
    }

    /// <summary>
        /// 从文本中提取分类信息（备选方案）
    /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="recommendationCount">推荐数量</param>
        /// <returns>推荐分类列表</returns>
        private static List<RecommendedCategory> ExtractCategoriesFromText(string text, int recommendationCount)
        {
            var categories = new List<RecommendedCategory>();

            // 处理阿里云AI返回的特定格式：分类名称,文档类型: None;业务分类: None;
            if (text.Contains("分类") || text.Contains("功能") || text.Contains("主题"))
            {
                // 按分号分割不同的分类字段
                var categoryFields = text.SplitEfficient([';', '；']);
                
                foreach (var field in categoryFields)
                {
                    if (categories.Count >= recommendationCount) break;
                    
                    var trimmedField = field.Trim();
                    if (string.IsNullOrEmpty(trimmedField)) continue;
                    
                    // 按冒号分割字段名和值
                    var colonIndex = trimmedField.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        var fieldName = trimmedField[..colonIndex].Trim();
                        var fieldValue = trimmedField[(colonIndex + 1)..].Trim();
                        
                        // 过滤掉无效值（如 "None"）
                        if (!string.IsNullOrEmpty(fieldValue) && 
                            !fieldValue.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                            !fieldValue.Equals("无", StringComparison.OrdinalIgnoreCase) &&
                            !fieldValue.Equals("空", StringComparison.OrdinalIgnoreCase) &&
                            fieldValue.Length > 1)
                        {
                            // 检查是否已存在相同名称的分类
                            if (!categories.Any(c => c.Name.Equals(fieldValue, StringComparison.OrdinalIgnoreCase)))
                            {
                                categories.Add(new RecommendedCategory
                                {
                                    Name = fieldValue,
                                    Description = $"从{fieldName}字段提取",
                                    Confidence = 0.8,
                                    Level = 1,
                                    Keywords = [fieldName]
                                });
                            }
                        }
                    }
                }
                
                // 如果仍然没有提取到有效分类，尝试更宽松的解析
                if (categories.Count == 0)
                {
                    // 尝试从文本中提取任何看起来像分类名称的内容
                    var words = text.SplitEfficient([' ', '，', ',', ';', '；', ':', '：']);
                    foreach (var word in words)
                    {
                        if (categories.Count >= recommendationCount) break;
                        
                        var trimmedWord = word.Trim();
                        if (trimmedWord.Length > 1 && 
                            !trimmedWord.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                            !trimmedWord.Equals("无", StringComparison.OrdinalIgnoreCase) &&
                            !trimmedWord.Equals("空", StringComparison.OrdinalIgnoreCase) &&
                            !trimmedWord.Contains("分类") &&
                            !trimmedWord.Contains("功能") &&
                            !trimmedWord.Contains("主题"))
                        {
                            if (!categories.Any(c => c.Name.Equals(trimmedWord, StringComparison.OrdinalIgnoreCase)))
                            {
                                categories.Add(new RecommendedCategory
                                {
                                    Name = trimmedWord,
                                    Description = "从文本中提取的分类名称",
                                    Confidence = 0.6,
                                    Level = 1,
                                    Keywords = [trimmedWord]
                                });
                            }
                        }
                    }
                }
            }
            
            // 如果没有提取到有效分类，尝试使用原有的正则表达式模式
            if (categories.Count == 0)
            {
                var patterns = new[]
                {
                    @"分类名称[:：]\s*([^，,;；\n\r]+)",
                    @"文档类型[:：]\s*([^，,；\n\r]+)",
                    @"业务分类[:：]\s*([^，,；\n\r]+)",
                    @"功能分类[:：]\s*([^，,；\n\r]+)",
                    @"主题分类[:：]\s*([^，,；\n\r]+)"
                };

                foreach (var pattern in patterns)
                {
                    if (categories.Count >= recommendationCount) break;

                    var matches = System.Text.RegularExpressions.Regex.Matches(text, pattern);
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        if (categories.Count >= recommendationCount) break;

                        if (match.Groups.Count > 1)
                        {
                            var categoryName = match.Groups[1].Value.Trim();
                            if (!string.IsNullOrEmpty(categoryName) && 
                                !categoryName.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                                !categoryName.Equals("无", StringComparison.OrdinalIgnoreCase) &&
                                !categoryName.Equals("空", StringComparison.OrdinalIgnoreCase) &&
                                !categories.Any(c => c.Name.Equals(categoryName, StringComparison.OrdinalIgnoreCase)))
                            {
                                categories.Add(new RecommendedCategory
                                {
                                    Name = categoryName,
                                    Confidence = 0.7,
                                    Level = 1
                                });
                            }
                        }
                    }
                }
            }

            return categories;
    }

    /// <summary>
        /// 生成默认分类建议
    /// </summary>
        /// <param name="businessDomain">业务领域</param>
        /// <param name="documentType">文档类型</param>
        /// <param name="recommendationCount">推荐数量</param>
        /// <returns>默认分类建议列表</returns>
        private static List<RecommendedCategory> GenerateDefaultCategorySuggestions(
            string? businessDomain, 
            string? documentType, 
            int recommendationCount)
        {
            var suggestions = new List<RecommendedCategory>();
            
            // 基于业务领域生成默认分类
            if (!string.IsNullOrEmpty(businessDomain))
            {
                var domainSuggestions = businessDomain switch
                {
                    "金融服务" => ["金融产品", "风险管理", "投资理财", "信贷服务", "保险业务"],
                    "制造业" => ["生产管理", "质量控制", "供应链", "设备维护", "工艺技术"],
                    "房地产" => ["住宅开发", "商业地产", "物业管理", "土地规划", "建筑设计"],
                    "教育" => ["课程管理", "学生服务", "教师发展", "教学资源", "评估考核"],
                    "医疗健康" => ["临床诊疗", "药品管理", "设备维护", "患者服务", "医疗质量"],
                    _ => new[] { "业务管理", "运营服务", "技术支持", "客户服务", "质量管理" }
                };
                
                foreach (var suggestion in domainSuggestions.Take(recommendationCount))
                {
                    suggestions.Add(new RecommendedCategory
                    {
                        Name = suggestion,
                        Description = $"基于{businessDomain}领域的默认分类建议",
                        Confidence = 0.6,
                        Level = 1,
                        Keywords = [businessDomain, suggestion]
                    });
                }
            }
            
            // 基于文档类型生成默认分类
            if (!string.IsNullOrEmpty(documentType) && suggestions.Count < recommendationCount)
            {
                var typeSuggestions = documentType switch
                {
                    "合同" => ["合同管理", "条款审核", "履约监督", "风险控制"],
                    "发票" => ["发票管理", "财务核算", "税务处理", "报销流程"],
                    "报告" => ["报告分析", "数据统计", "趋势预测", "决策支持"],
                    "申请" => ["申请处理", "审批流程", "状态跟踪", "结果通知"],
                    _ => new[] { "文档管理", "流程控制", "信息记录", "业务处理" }
                };
                
                var remainingCount = recommendationCount - suggestions.Count;
                foreach (var suggestion in typeSuggestions.Take(remainingCount))
                {
                    if (!suggestions.Any(s => s.Name.Equals(suggestion, StringComparison.OrdinalIgnoreCase)))
                    {
                        suggestions.Add(new RecommendedCategory
                        {
                            Name = suggestion,
                            Description = $"基于{documentType}类型的默认分类建议",
                            Confidence = 0.6,
                            Level = 1,
                            Keywords = [documentType, suggestion]
                        });
                    }
                }
            }
            
            // 如果还不够，添加通用分类
            if (suggestions.Count < recommendationCount)
            {
                var generalSuggestions = new[] { "重要文档", "日常管理", "核心业务", "支持服务", "临时文件" };
                var remainingCount = recommendationCount - suggestions.Count;
                
                foreach (var suggestion in generalSuggestions.Take(remainingCount))
                {
                    if (!suggestions.Any(s => s.Name.Equals(suggestion, StringComparison.OrdinalIgnoreCase)))
                    {
                        suggestions.Add(new RecommendedCategory
                        {
                            Name = suggestion,
                            Description = "通用文档分类建议",
                            Confidence = 0.5,
                            Level = 1,
                            Keywords = [suggestion]
                        });
                    }
                }
            }
            
            return suggestions;
        }

        /// <summary>
        /// 调用DashScope API进行文本分析
        /// </summary>
        /// <param name="systemPrompt">系统提示词</param>
        /// <param name="userPrompt">用户提示词</param>
        /// <returns>分析结果</returns>
        public async Task<string> CallDashScopeApiAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                var request = new DashScopeRequest
                {
                    Model = "qwen-doc-turbo",
                    Messages =
                    [
                        new() { Role = "system", Content = "You are a helpful assistant." },
                        new() { Role = "system", Content = systemPrompt },
                        new() { Role = "user", Content = userPrompt }
                    ],
                    Stream = false,
                    StreamOptions = new DashScopeStreamOptions { IncludeUsage = false }
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                _logger.LogDebug("发送DashScope API请求: {RequestJson}", json);

                var response = await _httpClient.PostAsync(_dashScopeUrl, httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogDebug("DashScope API响应: {ResponseContent}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var dashScopeResponse = JsonSerializer.Deserialize<DashScopeResponse>(responseContent, _jsonOptions);
                    if (dashScopeResponse?.Choices?.Count > 0)
                    {
                        return dashScopeResponse.Choices[0].Message.Content;
                    }
                }

                _logger.LogWarning("DashScope API调用失败，状态码: {StatusCode}, 响应: {ResponseContent}", 
                    response.StatusCode, responseContent);
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "调用DashScope API失败");
                return string.Empty;
            }
        }

        /// <summary>
        /// 生成摘要提取系统提示词
        /// </summary>
        /// <returns>系统提示词</returns>
        private static string GenerateSummaryExtractionSystemPrompt()
        {
            return """
                你是一个专业的文档摘要专家，擅长从各种类型的文档中提取核心信息和生成准确摘要。
                
                你的任务是：
                1. 仔细分析文档内容，理解其主题和核心观点
                2. 识别文档中的关键信息、重要观点、核心结论
                3. 生成准确、简洁、具有代表性的摘要
                4. 确保摘要逻辑清晰、语言流畅
                5. 保持原文的核心信息和重要观点
                
                摘要原则：
                - 准确反映原文的核心内容和主要观点
                - 保持逻辑清晰和语言流畅
                - 避免遗漏重要信息
                - 确保摘要的完整性和可读性
                """;
        }

        /// <summary>
        /// 生成摘要提取用户提示词
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>用户提示词</returns>
        private static string GenerateSummaryExtractionUserPrompt(string content, int maxLength)
        {
            return $"""
                请为以下文档生成一个简洁准确的摘要，摘要长度不超过 {maxLength} 个字符：
                
                文档内容：
                {content}
                
                要求：
                1. 摘要应该准确反映文档的核心内容和主要观点
                2. 保持逻辑清晰，语言流畅
                3. 避免遗漏重要信息
                4. 摘要长度不超过 {maxLength} 个字符
                5. 直接返回摘要内容，不需要额外的格式说明
                """;
        }

        /// <summary>
        /// 从响应中提取摘要
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <returns>摘要内容</returns>
        private static string ExtractSummaryFromResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            // 清理响应文本，移除可能的格式标记
            var summary = responseText.Trim();
            
            // 移除可能的引号
            if (summary.StartsWith('"') && summary.EndsWith('"'))
            {
                summary = summary[1..^1];
            }
            
            // 移除可能的"摘要："等前缀
            var prefixes = new[] { "摘要：", "摘要:", "Summary:", "Summary：" };
            foreach (var prefix in prefixes)
            {
                if (summary.StartsWith(prefix))
                {
                    summary = summary[prefix.Length..].Trim();
                    break;
                }
            }

            return summary;
        }

        /// <summary>
        /// 使用简单文本分析生成摘要（降级方案）
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>摘要内容</returns>
        private static string ExtractSummaryWithSimpleAnalysis(string content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content))
                return string.Empty;

            // 简单的摘要生成：取前几段或前几个句子
            var sentences = content
                .Split(['。', '！', '？', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
                .Where(s => !string.IsNullOrWhiteSpace(s.Trim()))
                .Select(s => s.Trim())
                .ToList();

            if (sentences.Count == 0)
                return content.Length > maxLength ? content[..maxLength] : content;

            var summary = string.Empty;
            foreach (var sentence in sentences)
            {
                if (summary.Length + sentence.Length + 1 <= maxLength)
                {
                    summary += (string.IsNullOrEmpty(summary) ? "" : "。") + sentence;
                }
                else
                {
                    break;
                }
            }

            return string.IsNullOrEmpty(summary) ? content[..Math.Min(content.Length, maxLength)] : summary;
        }

        /// <summary>
        /// 生成关键词提取系统提示词
        /// </summary>
        /// <returns>系统提示词</returns>
        private static string GenerateKeywordExtractionSystemPrompt()
        {
            return """
                你是一个专业的文档分析专家，擅长从各种类型的文档中提取关键词。
                
                你的任务是：
                1. 仔细分析文档内容，理解其主题和核心概念
                2. 识别文档中的关键术语、重要概念、专业词汇
                3. 提取最具代表性和重要性的关键词
                4. 确保关键词准确、简洁、具有代表性
                5. 避免提取过于通用或无关的词汇
                
                提取原则：
                - 优先选择专业术语和核心概念
                - 考虑词汇的重要性和代表性
                - 保持关键词的简洁性和准确性
                - 避免重复或近义词
                """;
        }

        /// <summary>
        /// 生成关键词提取用户提示词
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>用户提示词</returns>
        private static string GenerateKeywordExtractionUserPrompt(string content, int keywordCount)
        {
            return $"""
                请从以下文档中提取 {keywordCount} 个最重要的关键词：
                
                文档内容：
                {content}
                
                要求：
                1. 提取 {keywordCount} 个最具代表性的关键词
                2. 关键词应该准确反映文档的核心内容
                3. 优先选择专业术语、重要概念、核心主题
                4. 避免过于通用或无关的词汇
                5. 以JSON数组格式返回，格式：["关键词1", "关键词2", "关键词3"]
                """;
        }

        /// <summary>
        /// 从JSON字符串中解析关键词
        /// </summary>
        /// <param name="jsonContent">JSON内容</param>
        /// <returns>关键词列表</returns>
        private static List<string> ParseKeywordsFromJson(string jsonContent)
        {
            var keywords = new List<string>();
            
            if (string.IsNullOrWhiteSpace(jsonContent))
                return keywords;

            try
            {
                // 尝试直接解析JSON数组
                var jsonArray = JsonSerializer.Deserialize<string[]>(jsonContent);
                if (jsonArray != null)
                {
                    keywords.AddRange(jsonArray.Where(k => !string.IsNullOrWhiteSpace(k)));
                }
            }
            catch
            {
                // 如果JSON解析失败，尝试从文本中提取关键词
                var matches = QuotedStringsRegex().Matches(jsonContent);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var keyword = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        keywords.Add(keyword);
                    }
                }
            }

            return [.. keywords.Distinct()];
        }

        /// <summary>
        /// 使用简单文本分析提取关键词（降级方案）
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>关键词列表</returns>
        private static List<string> ExtractKeywordsWithSimpleAnalysis(string content, int keywordCount)
        {
            var keywords = new List<string>();
            
            if (string.IsNullOrWhiteSpace(content))
                return keywords;

            // 简单的关键词提取：基于词频和长度
            var words = content
                .Split([' ', '\n', '\r', '\t', '，', '。', '！', '？', '；', '：', '、', '（', '）', '【', '】', '《', '》'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 1 && w.Length < 10) // 过滤太短或太长的词
                .GroupBy(w => w.ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Key.Length)
                .Select(g => g.Key)
                .Take(keywordCount)
                .ToList();

            return words;
        }

        // 编译时生成的正则表达式方法
        [System.Text.RegularExpressions.GeneratedRegex(@"\d+")]
        private static partial System.Text.RegularExpressions.Regex NumbersRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"\d{4}[-/]\d{1,2}[-/]\d{1,2}")]
        private static partial System.Text.RegularExpressions.Regex DatesRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b")]
        private static partial System.Text.RegularExpressions.Regex EmailsRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"https?://[^\s]+")]
        private static partial System.Text.RegularExpressions.Regex UrlsRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"[\u4e00-\u9fff]")]
        private static partial System.Text.RegularExpressions.Regex ChineseRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"[a-zA-Z]")]
        private static partial System.Text.RegularExpressions.Regex EnglishRegex();

        [System.Text.RegularExpressions.GeneratedRegex(@"""([^""]+)""")]
        private static partial System.Text.RegularExpressions.Regex QuotedStringsRegex();
    }

    /// <summary>
    /// DashScope API请求模型
    /// </summary>
    public class DashScopeRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("messages")]
        public List<DashScopeMessage> Messages { get; set; } = [];
        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
        [JsonPropertyName("streamOptions")]
        public DashScopeStreamOptions? StreamOptions { get; set; }
    }

    /// <summary>
    /// DashScope流式选项
    /// </summary>
    public class DashScopeStreamOptions
    {
        [JsonPropertyName("includeUsage")]
        public bool IncludeUsage { get; set; } = true;
    }

    /// <summary>
    /// DashScope消息模型
    /// </summary>
    public class DashScopeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DashScope API响应模型
    /// </summary>
    public class DashScopeResponse
    {
        [JsonPropertyName("choices")]
        public List<DashScopeChoice> Choices { get; set; } = [];
    }

    /// <summary>
    /// DashScope选择模型
    /// </summary>
    public class DashScopeChoice
    {
        [JsonPropertyName("message")]
        public DashScopeMessage Message { get; set; } = new();
    }
}
