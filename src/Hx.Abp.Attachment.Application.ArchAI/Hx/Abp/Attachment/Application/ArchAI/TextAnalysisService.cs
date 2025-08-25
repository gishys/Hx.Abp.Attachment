using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 文本分析服务
    /// </summary>
    public class TextAnalysisService(
        ILogger<TextAnalysisService> logger,
        HttpClient httpClient) : IScopedDependency
    {
        private readonly ILogger<TextAnalysisService> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")
                ?? throw new UserFriendlyException("缺少环境变量 DEEPSEEK_API_KEY");
        private readonly string _apiUrl = "https://api.deepseek.com/chat/completions";

        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">文本分析输入参数</param>
        /// <returns>文本分析结果</returns>
        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            try
            {
                _logger.LogInformation("开始分析文本，长度: {TextLength}", input.Text.Length);

                var prompt = BuildAnalysisPrompt(input);
                var requestData = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new { role = "system", content = prompt },
                        new { role = "user", content = input.Text }
                    },
                    temperature = 0.3,
                    max_tokens = 800
                };

                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("API调用失败: {StatusCode}, {ErrorContent}", response.StatusCode, errorContent);
                    throw new UserFriendlyException($"文本分析服务暂时不可用，请稍后再试。错误代码: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<DeepSeekResponse>(responseContent);

                if (apiResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
                {
                    throw new UserFriendlyException("文本分析服务返回结果为空");
                }

                var result = ParseAnalysisResult(apiResponse.Choices[0].Message.Content);
                result.AnalysisTime = DateTime.Now;

                _logger.LogInformation("文本分析完成，提取关键词数量: {KeywordCount}", result.Keywords.Count);
                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本分析过程中发生错误");
                throw new UserFriendlyException("文本分析服务暂时不可用，请稍后再试");
            }
        }

        /// <summary>
        /// 构建分析提示词
        /// </summary>
        private static string BuildAnalysisPrompt(TextAnalysisInputDto input)
        {
            return $@"
# 文本分析专家指令

## 任务要求
请对输入的文本进行深度分析，生成结构化的摘要和关键词提取结果。

## 输出格式要求
请严格按照以下JSON格式返回结果，不要包含任何其他内容：

{{
  ""summary"": ""文本摘要内容，控制在{input.MaxSummaryLength}字符以内，突出核心信息和主要观点"",
  ""keywords"": [""关键词1"", ""关键词2"", ""关键词3"", ""关键词4"", ""关键词5""],
  ""confidence"": 0.95
}}

## 分析指导原则
1. **摘要生成**：
   - 提取文本的核心信息和主要观点
   - 保持逻辑清晰，语言简洁
   - 确保摘要完整表达原文主旨

2. **关键词提取**：
   - 提取{input.KeywordCount}个最重要的关键词
   - 关键词应具有代表性，能体现文本主题
   - 包含实体名词、专业术语、核心概念等
   - 按重要性排序

3. **置信度评估**：
   - 基于文本清晰度、信息完整性评估
   - 范围0.0-1.0，0.9以上表示高置信度

## 注意事项
- 只返回JSON格式结果，不要包含解释文字
- 确保JSON格式正确，可以被直接解析
- 关键词应该是单个词或短语，不要包含标点符号
- 摘要应该客观准确，避免主观判断";
        }

        /// <summary>
        /// 解析分析结果
        /// </summary>
        private static TextAnalysisDto ParseAnalysisResult(string content)
        {
            try
            {
                // 尝试直接解析JSON
                var result = JsonSerializer.Deserialize<TextAnalysisDto>(content);
                if (result != null)
                {
                    return result;
                }
            }
            catch
            {
                // 如果直接解析失败，尝试提取JSON部分
            }

            // 尝试从文本中提取JSON
            var jsonStart = content.IndexOf('{');
            var jsonEnd = content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                try
                {
                    var result = JsonSerializer.Deserialize<TextAnalysisDto>(jsonContent);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch
                {
                    // 继续使用备用方案
                }
            }

            // 备用方案：手动解析
            return ParseAnalysisResultManually(content);
        }

        /// <summary>
        /// 手动解析分析结果（备用方案）
        /// </summary>
        private static TextAnalysisDto ParseAnalysisResultManually(string content)
        {
            var result = new TextAnalysisDto
            {
                Summary = "文本分析完成",
                Keywords = [],
                Confidence = 0.8
            };

            // 简单的关键词提取逻辑
            var words = content.Split([' ', ',', '.', ';', ':', '!', '?'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .GroupBy(w => w.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key);

            result.Keywords.AddRange(words);

            return result;
        }
    }

    /// <summary>
    /// DeepSeek API响应模型
    /// </summary>
    public class DeepSeekResponse
    {
        public List<Choice> Choices { get; set; } = [];
    }

    public class Choice
    {
        public Message Message { get; set; } = new();
    }

    public class Message
    {
        public string Content { get; set; } = string.Empty;
    }
}
