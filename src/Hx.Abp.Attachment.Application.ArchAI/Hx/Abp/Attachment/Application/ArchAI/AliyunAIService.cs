using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 阿里云AI服务 - 使用HTTP API调用
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
        /// 生成文本摘要
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
                        Labels = "摘要"
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
        /// 提取关键词
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
                        Labels = "关键词"
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
                summary = System.Text.RegularExpressions.Regex.Replace(summary, @"\s+", " ");
                return summary;
            }

            // 如果没有找到摘要字段，尝试提取整个文本作为摘要
            var textMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"text""\s*:\s*""([^""]+)""");
            if (textMatch.Success)
            {
                var text = textMatch.Groups[1].Value.Trim();
                // 清理制表符和多余空格
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
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
            var keywordMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"关键词:\s*([^;]+)");
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
                var importantMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"重要词汇:\s*([^;]+)");
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
                    var conceptMatch = System.Text.RegularExpressions.Regex.Match(responseText, @"核心概念:\s*([^;]+)");
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

        [System.Text.RegularExpressions.GeneratedRegex(@"摘要:\s*([^;]+)")]
        private static partial System.Text.RegularExpressions.Regex SummaryRegex();
    }

    /// <summary>
    /// 阿里云NLU API请求模型
    /// </summary>
    public class AliyunNLURequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "opennlu-v1";
        [JsonPropertyName("input")]
        public AliyunNLUInput Input { get; set; } = new();
        [JsonPropertyName("parameters")]
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API输入参数
    /// </summary>
    public class AliyunNLUInput
    {
        [JsonPropertyName("sentence")]
        public string Sentence { get; set; } = string.Empty;
        [JsonPropertyName("task")]
        public string Task { get; set; } = "extraction";
        [JsonPropertyName("labels")]
        public string Labels { get; set; } = string.Empty;
    }

    /// <summary>
    /// 阿里云NLU API响应模型
    /// </summary>
    public class AliyunNLUResponse
    {
        [JsonPropertyName("output")]
        public AliyunNLUOutput? Output { get; set; }
        [JsonPropertyName("usage")]
        public AliyunNLUUsage? Usage { get; set; }
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API输出
    /// </summary>
    public class AliyunNLUOutput
    {
        [JsonPropertyName("rt")]
        public decimal? Rt { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API使用量统计
    /// </summary>
    public class AliyunNLUUsage
    {
        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 阿里云错误响应模型
    /// </summary>
    public class AliyunErrorResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }
}
