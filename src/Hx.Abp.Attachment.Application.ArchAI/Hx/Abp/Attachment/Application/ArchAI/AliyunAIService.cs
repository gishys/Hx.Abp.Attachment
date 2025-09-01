using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 阿里云AI服务 - 使用OpenNLU进行智能文本分析
    /// </summary>
    public partial class AliyunAIService(ILogger<AliyunAIService> logger, HttpClient httpClient) : IScopedDependency
    {
        private readonly ILogger<AliyunAIService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DASHSCOPE_API_KEY");
        private readonly string _workspaceId = Environment.GetEnvironmentVariable("ALIYUN_WORKSPACE_ID")
                ?? throw new UserFriendlyException("缺少环境变量 ALIYUN_WORKSPACE_ID");
        private readonly string _baseUrl = "https://dashscope.aliyuncs.com/api/v1/services/nlp/nlu/understanding";
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        /// <summary>
        /// 生成文本摘要 - 适用于AttachCatalogue智能查询
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="maxLength">最大长度</param>
        /// <returns>摘要内容</returns>
        public async Task<string> GenerateSummaryAsync(string content, int maxLength = 500)
        {
            try
            {
                _logger.LogInformation("开始调用阿里云AI生成摘要，文本长度: {TextLength}", content.Length);

                var request = new AliyunNLURequest
                {
                    Model = "opennlu-v1",
                    Input = new AliyunNLUInput
                    {
                        Sentence = content,
                        Task = "extraction",
                        Labels = "摘要,核心内容,主要信息"
                    }
                };

                var response = await CallAliyunNLUApiAsync(request);
                
                if (!string.IsNullOrEmpty(response.Output?.Text))
                {
                    var summary = ExtractSummaryFromResponse(response.Output.Text);
                    if (!string.IsNullOrEmpty(summary))
                    {
                        _logger.LogInformation("阿里云AI摘要生成成功，长度: {SummaryLength}", summary.Length);
                        return summary.Length > maxLength ? summary[..maxLength] : summary;
                    }
                }

                _logger.LogWarning("阿里云AI摘要生成响应为空或解析失败");
                throw new UserFriendlyException("阿里云AI返回的摘要内容为空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云AI摘要生成失败");
                throw new UserFriendlyException("阿里云AI摘要生成服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 提取关键词 - 适用于AttachCatalogue智能查询
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>关键词列表</returns>
        public async Task<List<string>> ExtractKeywordsAsync(string content, int keywordCount = 5)
        {
            try
            {
                _logger.LogInformation("开始调用阿里云AI提取关键词，文本长度: {TextLength}, 关键词数量: {KeywordCount}",
                    content.Length, keywordCount);

                var request = new AliyunNLURequest
                {
                    Model = "opennlu-v1",
                    Input = new AliyunNLUInput
                    {
                        Sentence = content,
                        Task = "extraction",
                        Labels = "关键词,重要词汇,核心概念,实体名称"
                    }
                };

                var response = await CallAliyunNLUApiAsync(request);
                
                if (!string.IsNullOrEmpty(response.Output?.Text))
                {
                    var keywords = ExtractKeywordsFromResponse(response.Output.Text, keywordCount);
                    if (keywords.Count > 0)
                    {
                        _logger.LogInformation("阿里云AI关键词提取成功，提取数量: {KeywordCount}", keywords.Count);
                        return keywords;
                    }
                }

                _logger.LogWarning("阿里云AI关键词提取响应为空或解析失败");
                throw new UserFriendlyException("阿里云AI返回的关键词内容为空");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "阿里云AI关键词提取失败");
                throw new UserFriendlyException("阿里云AI关键词提取服务暂时不可用，请稍后再试");
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
        /// <returns>综合分析结果</returns>
        public async Task<ComprehensiveAnalysisResult> AnalyzeComprehensivelyAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5)
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
        /// 从响应中提取摘要内容
        /// </summary>
        /// <param name="responseText">API响应文本</param>
        /// <returns>提取的摘要</returns>
        private static string ExtractSummaryFromResponse(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return string.Empty;

            // 尝试提取摘要字段
            var summaryMatch = AliyunAIService.SummaryRegex().Match(responseText);
            if (summaryMatch.Success)
            {
                var summary = summaryMatch.Groups[1].Value.Trim();
                // 清理制表符和多余空格
                summary = AliyunAIService.WhitespaceRegex().Replace(summary, " ");
                return summary;
            }

            // 如果没有找到摘要字段，尝试提取整个文本作为摘要
            var textMatch = AliyunAIService.TextFieldRegex().Match(responseText);
            if (textMatch.Success)
            {
                var text = textMatch.Groups[1].Value.Trim();
                // 清理制表符和多余空格
                text = AliyunAIService.WhitespaceRegex().Replace(text, " ");
                return text;
            }

            return string.Empty;
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
                    .Split(['\t', ',', '，', ';', '；', ' '], StringSplitOptions.RemoveEmptyEntries)
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
                        .Split(['\t', ',', '，', ';', '；', ' '], StringSplitOptions.RemoveEmptyEntries)
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
                            .Split(['\t', ',', '，', ';', '；', ' '], StringSplitOptions.RemoveEmptyEntries)
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
                        .Split(['\t', ',', '，', ';', '；', ' '], StringSplitOptions.RemoveEmptyEntries)
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
    }
}
