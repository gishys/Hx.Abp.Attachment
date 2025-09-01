using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 语义向量服务 - 基于阿里云DashScope API
    /// </summary>
    public class SemanticVectorService(
        ILogger<SemanticVectorService> logger,
        HttpClient httpClient) : IScopedDependency
    {
        private readonly ILogger<SemanticVectorService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DASHSCOPE_API_KEY");
        private readonly string _apiUrl = "https://dashscope.aliyuncs.com/compatible-mode/v1/embeddings";
        
        // 使用配置类中的默认值
        private const string DefaultModel = SemanticVectorConfiguration.DefaultModel;
        private const int DefaultDimension = SemanticVectorConfiguration.DefaultDimension;
        private const string DefaultEncodingFormat = SemanticVectorConfiguration.DefaultEncodingFormat;
        private const int MaxBatchSize = SemanticVectorConfiguration.MaxBatchSize;

        // JSON序列化选项
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// 生成单个文本的语义向量
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <param name="model">模型名称，默认使用text-embedding-v4</param>
        /// <param name="dimension">向量维度，默认1024</param>
        /// <returns>语义向量</returns>
        public async Task<List<double>> GenerateVectorAsync(string text, string model = DefaultModel, int dimension = DefaultDimension)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("输入文本不能为空", nameof(text));
            }

            // 验证参数
            if (!SemanticVectorConfiguration.IsModelSupported(model))
            {
                throw new ArgumentException($"不支持的模型: {model}", nameof(model));
            }

            if (!SemanticVectorConfiguration.IsDimensionSupported(dimension))
            {
                throw new ArgumentException($"不支持的向量维度: {dimension}", nameof(dimension));
            }

            var vectors = await GenerateVectorsAsync([text], model, dimension);
            return vectors.FirstOrDefault() ?? [];
        }

        /// <summary>
        /// 批量生成语义向量 - 优化版本
        /// </summary>
        /// <param name="texts">文本列表</param>
        /// <param name="model">模型名称，默认使用text-embedding-v4</param>
        /// <param name="dimension">向量维度，默认1024</param>
        /// <returns>向量列表</returns>
        public async Task<List<List<double>>> GenerateVectorsAsync(List<string> texts, string model = DefaultModel, int dimension = DefaultDimension)
        {
            if (texts == null || texts.Count == 0)
            {
                return [];
            }

            // 过滤空文本
            var validTexts = texts.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (validTexts.Count == 0)
            {
                return [.. Enumerable.Repeat(new List<double>(), texts.Count)];
            }

            var allVectors = new List<List<double>>();
            
            // 分批处理，避免超出API限制
            for (int i = 0; i < validTexts.Count; i += MaxBatchSize)
            {
                var batch = validTexts.Skip(i).Take(MaxBatchSize).ToList();
                var batchVectors = await GenerateBatchVectorsAsync(batch, model, dimension);
                allVectors.AddRange(batchVectors);
            }

            // 保持与原始输入顺序一致，空文本对应空向量
            var result = new List<List<double>>();
            int vectorIndex = 0;
            
            foreach (var text in texts)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    result.Add([]);
                }
                else
                {
                    result.Add(vectorIndex < allVectors.Count ? allVectors[vectorIndex] : []);
                    vectorIndex++;
                }
            }

            return result;
        }

        /// <summary>
        /// 生成单批向量 - 带重试机制
        /// </summary>
        private async Task<List<List<double>>> GenerateBatchVectorsAsync(List<string> texts, string model, int dimension)
        {
            var retryCount = 0;
            var maxRetries = SemanticVectorConfiguration.MaxRetryCount;

            while (retryCount <= maxRetries)
            {
                try
                {
                    _logger.LogDebug("开始批量生成语义向量，文本数量: {TextCount}, 模型: {Model}, 重试次数: {RetryCount}", 
                        texts.Count, model, retryCount);

                    var requestData = new DashScopeEmbeddingRequest
                    {
                        Model = model,
                        Input = texts,
                        Dimension = dimension,
                        EncodingFormat = DefaultEncodingFormat
                    };

                    var jsonContent = JsonSerializer.Serialize(requestData, JsonOptions);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                    // 设置超时时间
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(SemanticVectorConfiguration.RequestTimeoutSeconds));
                    var response = await _httpClient.PostAsync(_apiUrl, content, cts.Token);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogError("向量生成API调用失败: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                        
                        // 尝试解析错误信息
                        var errorResponse = JsonSerializer.Deserialize<DashScopeErrorResponse>(errorContent, JsonOptions);
                        var errorMessage = errorResponse?.Error?.Message ?? "未知错误";
                        var errorCode = errorResponse?.Error?.Code ?? "";
                        
                        // 如果是认证错误，直接抛出，不需要重试
                        if (errorCode == "invalid_api_key" || errorCode == "authentication_error")
                        {
                            throw new UserFriendlyException($"API认证失败: {errorMessage}");
                        }
                        
                        // 如果是客户端错误（4xx），不重试
                        if (response.StatusCode >= System.Net.HttpStatusCode.BadRequest && 
                            response.StatusCode < System.Net.HttpStatusCode.InternalServerError)
                        {
                            throw new UserFriendlyException($"请求参数错误: {errorMessage}");
                        }
                        
                        // 服务器错误（5xx）才重试
                        if (retryCount < maxRetries)
                        {
                            retryCount++;
                            _logger.LogWarning("服务器错误，准备重试 {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                            await Task.Delay(SemanticVectorConfiguration.RetryDelayMs * retryCount);
                            continue;
                        }
                        
                        throw new UserFriendlyException($"语义向量生成服务暂时不可用: {errorMessage}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonSerializer.Deserialize<DashScopeEmbeddingResponse>(responseContent, JsonOptions);

                    if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
                    {
                        throw new UserFriendlyException("语义向量生成服务返回结果为空");
                    }

                    // 按索引排序确保顺序正确
                    var sortedData = apiResponse.Data.OrderBy(d => d.Index).ToList();
                    var vectors = sortedData.Select(d => d.Embedding).ToList();

                    _logger.LogDebug("批量语义向量生成完成，返回向量数量: {VectorCount}, 维度: {Dimension}", 
                        vectors.Count, vectors.FirstOrDefault()?.Count ?? 0);

                    return vectors;
                }
                catch (UserFriendlyException)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        _logger.LogWarning("请求超时，准备重试 {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await Task.Delay(SemanticVectorConfiguration.RetryDelayMs * retryCount);
                        continue;
                    }
                    throw new UserFriendlyException("语义向量生成服务请求超时，请稍后再试");
                }
                catch (Exception ex)
                {
                    if (retryCount < maxRetries)
                    {
                        retryCount++;
                        _logger.LogWarning(ex, "网络错误，准备重试 {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await Task.Delay(SemanticVectorConfiguration.RetryDelayMs * retryCount);
                        continue;
                    }
                    
                    _logger.LogError(ex, "批量生成语义向量过程中发生错误");
                    throw new UserFriendlyException("语义向量生成服务暂时不可用，请稍后再试");
                }
            }

            throw new UserFriendlyException("语义向量生成服务暂时不可用，请稍后再试");
        }

        /// <summary>
        /// 计算两个向量的余弦相似度
        /// </summary>
        /// <param name="vector1">向量1</param>
        /// <param name="vector2">向量2</param>
        /// <returns>相似度分数 (0-1)</returns>
        public static double CalculateCosineSimilarity(List<double> vector1, List<double> vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                return 0.0;
            }

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
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            {
                return 0.0;
            }

            try
            {
                var vectors = await GenerateVectorsAsync([text1, text2]);
                
                if (vectors.Count < 2 || vectors[0].Count == 0 || vectors[1].Count == 0)
                {
                    return 0.0;
                }

                return CalculateCosineSimilarity(vectors[0], vectors[1]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算文本相似度时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 计算多个文本之间的相似度矩阵
        /// </summary>
        /// <param name="texts">文本列表</param>
        /// <returns>相似度矩阵</returns>
        public async Task<double[,]> CalculateSimilarityMatrixAsync(List<string> texts)
        {
            if (texts == null || texts.Count == 0)
            {
                return new double[0, 0];
            }

            var vectors = await GenerateVectorsAsync(texts);
            var matrix = new double[texts.Count, texts.Count];

            for (int i = 0; i < texts.Count; i++)
            {
                for (int j = 0; j < texts.Count; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 1.0; // 自身相似度为1
                    }
                    else if (i < vectors.Count && j < vectors.Count && 
                             vectors[i].Count > 0 && vectors[j].Count > 0)
                    {
                        matrix[i, j] = CalculateCosineSimilarity(vectors[i], vectors[j]);
                    }
                    else
                    {
                        matrix[i, j] = 0.0;
                    }
                }
            }

            return matrix;
        }
    }

    /// <summary>
    /// 阿里云DashScope嵌入请求模型
    /// </summary>
    public class DashScopeEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];

        [JsonPropertyName("dimension")]
        public int Dimension { get; set; } = 1024;

        [JsonPropertyName("encoding_format")]
        public string EncodingFormat { get; set; } = "float";
    }

    /// <summary>
    /// 阿里云DashScope嵌入响应模型
    /// </summary>
    public class DashScopeEmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<DashScopeEmbeddingData> Data { get; set; } = [];

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("usage")]
        public DashScopeEmbeddingUsage? Usage { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    public class DashScopeEmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("embedding")]
        public List<double> Embedding { get; set; } = [];

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class DashScopeEmbeddingUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 阿里云DashScope错误响应模型
    /// </summary>
    public class DashScopeErrorResponse
    {
        [JsonPropertyName("error")]
        public DashScopeError? Error { get; set; }
    }

    public class DashScopeError
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("param")]
        public string? Param { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
    }
}
