using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 基础文本分析服务 - 提取公共逻辑
    /// </summary>
    public abstract class BaseTextAnalysisService(
        ILogger<BaseTextAnalysisService> logger,
        HttpClient httpClient,
        SemanticVectorService semanticVectorService) : IScopedDependency
    {
        protected readonly ILogger _logger = logger;
        protected readonly HttpClient _httpClient = httpClient;
        protected readonly SemanticVectorService _semanticVectorService = semanticVectorService;
        protected readonly string _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DEEPSEEK_API_KEY");
        protected readonly string _apiUrl = "https://api.deepseek.com/chat/completions";
        
        protected static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        protected async Task<DeepSeekResponse> CallAIApiAsync(string prompt, string userContent, int maxTokens = 50000)
        {
            var requestData = new DeepSeekRequest
            {
                Model = "deepseek-chat",
                Messages =
                [
                    new() { Role = "system", Content = prompt },
                    new() { Role = "user", Content = userContent }
                ],
                Temperature = 1,
                MaxTokens = maxTokens
            };

            var jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync(_apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API调用失败: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                throw new UserFriendlyException($"AI服务暂时不可用，请稍后再试。错误代码: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, JsonOptions);

            if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                throw new UserFriendlyException("AI服务返回结果为空");
            }

            return apiResponse;
        }

        public static TextAnalysisDto ParseAnalysisResult(string content)
        {
            try
            {
                var result = JsonSerializer.Deserialize<TextAnalysisDto>(content, JsonOptions);
                if (result != null && !string.IsNullOrEmpty(result.Summary))
                {
                    return result;
                }
            }
            catch (JsonException) { }

            var jsonContent = ExtractJsonFromText(content);
            if (!string.IsNullOrEmpty(jsonContent))
            {
                try
                {
                    var result = JsonSerializer.Deserialize<TextAnalysisDto>(jsonContent, JsonOptions);
                    if (result != null && !string.IsNullOrEmpty(result.Summary))
                    {
                        return result;
                    }
                }
                catch (JsonException) { }
            }

            return ParseAnalysisResultManually(content);
        }

        private static string ExtractJsonFromText(string content)
        {
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return content.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            return string.Empty;
        }

        private static TextAnalysisDto ParseAnalysisResultManually(string content)
        {
            var result = new TextAnalysisDto
            {
                Summary = "文本分析完成",
                Keywords = [],
                Confidence = 0.8
            };

            var words = content.Split([' ', ',', '.', ';', ':', '!', '?', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Select(w => w.Trim('"', '\'', '(', ')', '[', ']', '{', '}'))
                .Where(w => w.Length > 2)
                .GroupBy(w => w.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            result.Keywords.AddRange(words);
            return result;
        }

        public static string BuildGenericPrompt(int keywordCount, int maxSummaryLength, string taskDescription)
        {
            return $@"
# 通用文本分析专家指令

## 任务要求
{taskDescription}

## 输出格式要求
请严格按照以下JSON格式返回结果，不要包含任何其他内容：

{{
  ""summary"": ""文本摘要内容，控制在{maxSummaryLength}字符以内，突出核心信息和主要观点"",
  ""keywords"": [""关键词1"", ""关键词2"", ""关键词3"", ""关键词4"", ""关键词5""],
  ""confidence"": 0.95
}}

## 分析指导原则
1. **摘要生成**：
   - 提取文本的核心信息和主要观点
   - 保持逻辑清晰，语言简洁
   - 确保摘要完整表达原文主旨
   - 重点关注实体名称、时间、金额、地点等关键信息

2. **关键词提取**：
   - 提取{keywordCount}个最重要的关键词
   - 关键词应具有代表性，能体现文本主题
   - 包含实体名词、专业术语、核心概念等
   - 按重要性排序，优先提取：
     * 实体名称（公司、机构、人名、地名等）
     * 文档类型标识
     * 关键业务术语
     * 重要时间节点
     * 数值信息（金额、数量等）

3. **置信度评估**：
   - 基于文本清晰度、信息完整性评估
   - 范围0.0-1.0，0.9以上表示高置信度
   - 考虑文本结构、信息密度、专业术语使用等因素

## 注意事项
- 只返回JSON格式结果，不要包含解释文字
- 确保JSON格式正确，可以被直接解析
- 关键词应该是单个词或短语，不要包含标点符号
- 摘要应该客观准确，避免主观判断
- 重点关注对后续语义匹配有用的信息";
        }

        protected static void AddMetadata(TextAnalysisDto result, DeepSeekResponse apiResponse, int textLength, long processingTimeMs)
        {
            result.Metadata = new AnalysisMetadata
            {
                TextLength = textLength,
                ProcessingTimeMs = processingTimeMs,
                Model = apiResponse.Model,
                ApiUsage = apiResponse.Usage != null ? new ApiUsageInfo
                {
                    PromptTokens = apiResponse.Usage.PromptTokens,
                    CompletionTokens = apiResponse.Usage.CompletionTokens,
                    TotalTokens = apiResponse.Usage.TotalTokens
                } : null
            };
        }

        /// <summary>
        /// 添加基础元数据（用于非DeepSeek服务）
        /// </summary>
        protected static void AddBasicMetadata(TextAnalysisDto result, int textLength, long processingTimeMs)
        {
            result.Metadata = new AnalysisMetadata
            {
                TextLength = textLength,
                ProcessingTimeMs = processingTimeMs,
                Model = "AI-Service",
                ApiUsage = null
            };
        }

        protected async Task<List<double>?> GenerateSemanticVectorAsync(string summary, List<string> keywords)
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
        /// 识别业务领域
        /// </summary>
        protected static string IdentifyBusinessDomain(string summary, List<string> keywords)
        {
            var text = (summary + " " + string.Join(" ", keywords)).ToLower();
            
            foreach (var rule in TextAnalysisConfiguration.BusinessDomainRules)
            {
                if (rule.Value.Any(keyword => text.Contains(keyword)))
                {
                    return rule.Key;
                }
            }
            
            return "其他领域";
        }
    }

    public class DeepSeekRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    public class DeepSeekResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("created")]
        public long Created { get; set; }

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = [];

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }

        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; } = new();

        [JsonPropertyName("logprobs")]
        public object? Logprobs { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_tokens_details")]
        public PromptTokensDetails? PromptTokensDetails { get; set; }

        [JsonPropertyName("prompt_cache_hit_tokens")]
        public int? PromptCacheHitTokens { get; set; }

        [JsonPropertyName("prompt_cache_miss_tokens")]
        public int? PromptCacheMissTokens { get; set; }
    }

    public class PromptTokensDetails
    {
        [JsonPropertyName("cached_tokens")]
        public int CachedTokens { get; set; }
    }
}
