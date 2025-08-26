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
    /// 文本分类服务
    /// </summary>
    public class TextClassificationService(
        ILogger<TextClassificationService> logger,
        HttpClient httpClient,
        SemanticVectorService semanticVectorService) : IScopedDependency
    {
        private readonly ILogger<TextClassificationService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly SemanticVectorService _semanticVectorService = semanticVectorService;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DEEPSEEK_API_KEY");
        private readonly string _apiUrl = "https://api.deepseek.com/chat/completions";
        
        // JSON序列化选项
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// 提取文本分类特征
        /// </summary>
        /// <param name="input">文本分类输入参数</param>
        /// <returns>文本分类特征结果</returns>
        public async Task<TextAnalysisDto> ExtractClassificationFeaturesAsync(TextClassificationInputDto input)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("开始提取文本分类特征，分类名称: {ClassificationName}, 样本数量: {SampleCount}", 
                    input.ClassificationName, input.TextSamples.Count);

                var prompt = BuildClassificationPrompt(input);
                var requestData = new DeepSeekRequest
                {
                    Model = "deepseek-chat",
                    Messages =
                    [
                        new() { Role = "system", Content = prompt },
                        new() { Role = "user", Content = BuildSampleText(input.TextSamples) }
                    ],
                    Temperature = 0.3,
                    MaxTokens = 1000
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
                    throw new UserFriendlyException($"文本分类特征提取服务暂时不可用，请稍后再试。错误代码: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent, JsonOptions);

                if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
                {
                    throw new UserFriendlyException("文本分类特征提取服务返回结果为空");
                }

                var result = ParseAnalysisResult(apiResponse.Choices[0].Message.Content);
                result.AnalysisTime = DateTime.Now;

                // 添加元数据
                stopwatch.Stop();
                result.Metadata = new AnalysisMetadata
                {
                    TextLength = input.TextSamples.Sum(s => s.Length),
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Model = apiResponse.Model,
                    ApiUsage = apiResponse.Usage != null ? new ApiUsageInfo
                    {
                        PromptTokens = apiResponse.Usage.PromptTokens,
                        CompletionTokens = apiResponse.Usage.CompletionTokens,
                        TotalTokens = apiResponse.Usage.TotalTokens
                    } : null
                };

                // 识别文档类型和业务领域
                result.DocumentType = input.ClassificationName;
                result.BusinessDomain = IdentifyBusinessDomain(result.Summary, result.Keywords);

                // 生成语义向量
                if (input.GenerateSemanticVector)
                {
                    try
                    {
                        var vectorText = $"{result.Summary} {string.Join(" ", result.Keywords)}";
                        result.SemanticVector = await _semanticVectorService.GenerateVectorAsync(vectorText);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "生成语义向量失败，继续处理其他功能");
                    }
                }

                _logger.LogInformation("文本分类特征提取完成，分类名称: {ClassificationName}, 提取关键词数量: {KeywordCount}, 置信度: {Confidence}, 处理时间: {ProcessingTime}ms", 
                    input.ClassificationName, result.Keywords.Count, result.Confidence, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本分类特征提取过程中发生错误");
                throw new UserFriendlyException("文本分类特征提取服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 构建分类提示词
        /// </summary>
        private static string BuildClassificationPrompt(TextClassificationInputDto input)
        {
            return $@"
# 文本分类特征提取专家指令

## 任务要求
请对输入的多个同类文本样本进行深度分析，提取该类文本的通用特征，生成结构化的分类描述和特征关键词，用于文本分类和模板匹配。

## 输出格式要求
请严格按照以下JSON格式返回结果，不要包含任何其他内容：

{{
  ""summary"": ""该类文本的通用特征描述，控制在{input.MaxSummaryLength}字符以内，突出该类文本的核心特征和识别要点"",
  ""keywords"": [""特征关键词1"", ""特征关键词2"", ""特征关键词3"", ""特征关键词4"", ""特征关键词5""],
  ""confidence"": 0.95
}}

## 分析指导原则
1. **分类特征描述**：
   - 提取该类文本的通用特征和识别要点
   - 描述该类文本的典型内容、结构、格式特征
   - 突出该类文本区别于其他类型文本的关键特征
   - 重点关注：
     * 文档类型特征（如：结清证明、合同、报告等）
     * 业务领域特征（如：金融服务、制造业、房地产等）
     * 内容结构特征（如：包含哪些典型信息块）
     * 语言表达特征（如：常用术语、表达方式）

2. **特征关键词提取**：
   - 提取{input.KeywordCount}个最能代表该类文本的特征关键词
   - 关键词应具有通用性，能代表该类文本的典型特征
   - 按重要性排序，优先提取：
     * 文档类型标识词（如：证明、合同、报告、证书等）
     * 业务领域术语（如：贷款、结清、抵押、登记等）
     * 典型实体类型（如：公司、银行、金额、日期等）
     * 关键动作词（如：办理、结清、注销、登记等）
     * 特征性表达（如：同意、确认、证明等）

3. **置信度评估**：
   - 基于样本的代表性、特征的一致性评估
   - 范围0.0-1.0，0.9以上表示高置信度
   - 考虑样本数量、特征清晰度、分类准确性等因素

## 分类特征提取策略
- **通用性优先**：提取的特征应适用于该类文本的所有实例
- **区分性优先**：优先提取能区分该类文本与其他类型文本的特征
- **稳定性优先**：优先提取在该类文本中稳定出现的特征
- **可识别性优先**：优先提取便于自动识别和匹配的特征

## 注意事项
- 只返回JSON格式结果，不要包含解释文字
- 确保JSON格式正确，可以被直接解析
- 关键词应该是通用特征词，不要包含具体的人名、地名、金额等
- 摘要应该描述该类文本的通用特征，而不是具体内容
- 重点关注对文本分类和模板匹配有用的特征
- 避免过于具体或过于抽象的描述，保持适度的通用性";
        }

        /// <summary>
        /// 构建样本文本
        /// </summary>
        private static string BuildSampleText(List<string> textSamples)
        {
            var sampleText = new StringBuilder();
            sampleText.AppendLine("以下是该类文本的样本：");
            sampleText.AppendLine();

            for (int i = 0; i < textSamples.Count; i++)
            {
                sampleText.AppendLine($"样本{i + 1}：");
                sampleText.AppendLine(textSamples[i]);
                sampleText.AppendLine();
            }

            sampleText.AppendLine("请基于以上样本，提取该类文本的通用特征。");
            return sampleText.ToString();
        }

        /// <summary>
        /// 解析分析结果
        /// </summary>
        private static TextAnalysisDto ParseAnalysisResult(string content)
        {
            try
            {
                // 尝试直接解析JSON
                var result = JsonSerializer.Deserialize<TextAnalysisDto>(content, JsonOptions);
                if (result != null && !string.IsNullOrEmpty(result.Summary))
                {
                    return result;
                }
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"直接JSON解析失败: {ex.Message}");
            }

            // 尝试从文本中提取JSON
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
                catch (JsonException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"提取JSON解析失败: {ex.Message}");
                }
            }

            // 备用方案：手动解析
            return ParseAnalysisResultManually(content);
        }

        /// <summary>
        /// 从文本中提取JSON内容
        /// </summary>
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

        /// <summary>
        /// 手动解析分析结果（备用方案）
        /// </summary>
        private static TextAnalysisDto ParseAnalysisResultManually(string content)
        {
            var result = new TextAnalysisDto
            {
                Summary = "文本分类特征提取完成",
                Keywords = [],
                Confidence = 0.8
            };

            // 简单的关键词提取逻辑
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

        /// <summary>
        /// 识别业务领域
        /// </summary>
        private static string IdentifyBusinessDomain(string summary, List<string> keywords)
        {
            var text = (summary + " " + string.Join(" ", keywords)).ToLower();
            
            if (text.Contains("银行") || text.Contains("贷款") || text.Contains("金融"))
                return "金融服务";
            
            if (text.Contains("工程") || text.Contains("机械") || text.Contains("制造"))
                return "制造业";
            
            if (text.Contains("房地产") || text.Contains("房产") || text.Contains("不动产"))
                return "房地产";
            
            if (text.Contains("政府") || text.Contains("政务") || text.Contains("行政"))
                return "政务服务";
            
            return "其他领域";
        }
    }
}
