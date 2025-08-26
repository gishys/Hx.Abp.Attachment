using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 语义向量服务
    /// </summary>
    public class SemanticVectorService(
        ILogger<SemanticVectorService> logger,
        HttpClient httpClient) : IScopedDependency
    {
        private readonly ILogger<SemanticVectorService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DEEPSEEK_API_KEY");
        private readonly string _apiUrl = "https://api.deepseek.com/embeddings";

        // JSON序列化选项
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// 生成文本的语义向量
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>语义向量</returns>
        public async Task<List<double>> GenerateVectorAsync(string text)
        {
            try
            {
                _logger.LogDebug("开始生成语义向量，文本长度: {TextLength}", text.Length);

                var requestData = new EmbeddingRequest
                {
                    Model = "deepseek-embedding",
                    Input = text
                };

                var jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("向量生成API调用失败: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                    throw new UserFriendlyException($"语义向量生成服务暂时不可用，请稍后再试。错误代码: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, JsonOptions);

                if (apiResponse?.Data?.FirstOrDefault()?.Embedding == null)
                {
                    throw new UserFriendlyException("语义向量生成服务返回结果为空");
                }

                var vector = apiResponse.Data[0].Embedding;
                _logger.LogDebug("语义向量生成完成，维度: {Dimension}", vector.Count);
                return vector;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "语义向量生成过程中发生错误");
                throw new UserFriendlyException("语义向量生成服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 计算两个向量的余弦相似度
        /// </summary>
        /// <param name="vector1">向量1</param>
        /// <param name="vector2">向量2</param>
        /// <returns>相似度分数 (0-1)</returns>
        public static double CalculateCosineSimilarity(List<double> vector1, List<double> vector2)
        {
            if (vector1.Count != vector2.Count)
            {
                throw new ArgumentException("向量维度不匹配");
            }

            if (vector1.Count == 0)
            {
                return 0.0;
            }

            double dotProduct = 0.0;
            double norm1 = 0.0;
            double norm2 = 0.0;

            for (int i = 0; i < vector1.Count; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                norm1 += vector1[i] * vector1[i];
                norm2 += vector2[i] * vector2[i];
            }

            if (norm1 == 0 || norm2 == 0)
            {
                return 0.0;
            }

            return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        /// <summary>
        /// 计算两个文本的语义相似度
        /// </summary>
        /// <param name="text1">文本1</param>
        /// <param name="text2">文本2</param>
        /// <returns>相似度分数 (0-1)</returns>
        public async Task<double> CalculateTextSimilarityAsync(string text1, string text2)
        {
            try
            {
                var vector1 = await GenerateVectorAsync(text1);
                var vector2 = await GenerateVectorAsync(text2);

                return CalculateCosineSimilarity(vector1, vector2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算文本相似度时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 批量生成语义向量
        /// </summary>
        /// <param name="texts">文本列表</param>
        /// <returns>向量列表</returns>
        public async Task<List<List<double>>> GenerateVectorsAsync(List<string> texts)
        {
            var vectors = new List<List<double>>();
            
            foreach (var text in texts)
            {
                try
                {
                    var vector = await GenerateVectorAsync(text);
                    vectors.Add(vector);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "生成向量失败，文本: {Text}", text);
                    // 添加零向量作为占位符
                    vectors.Add([]);
                }
            }

            return vectors;
        }
    }

    /// <summary>
    /// 嵌入请求模型
    /// </summary>
    public class EmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    /// <summary>
    /// 嵌入响应模型
    /// </summary>
    public class EmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<EmbeddingData> Data { get; set; } = [];

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public EmbeddingUsage? Usage { get; set; }
    }

    public class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("embedding")]
        public List<double> Embedding { get; set; } = [];

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class EmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
