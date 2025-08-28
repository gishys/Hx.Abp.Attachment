using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{

    /// <summary>
    /// AI服务工厂
    /// </summary>
    public class AIServiceFactory(
        ILogger<AIServiceFactory> logger,
        AliyunAIService aliyunAIService,
        DeepSeekTextAnalysisService deepSeekTextAnalysisService) : IScopedDependency
    {
        private readonly ILogger<AIServiceFactory> _logger = logger;
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;
        private readonly DeepSeekTextAnalysisService _deepSeekTextAnalysisService = deepSeekTextAnalysisService;

        /// <summary>
        /// 获取AI服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>AI服务实例</returns>
        public ITextAnalysisProvider GetService(AIServiceType serviceType)
        {
            return serviceType switch
            {
                AIServiceType.DeepSeek => new DeepSeekTextAnalysisProvider(_deepSeekTextAnalysisService),
                AIServiceType.Aliyun => new AliyunTextAnalysisProvider(_aliyunAIService),
                _ => throw new UserFriendlyException($"不支持的AI服务类型: {serviceType}")
            };
        }

        /// <summary>
        /// 根据配置获取默认AI服务
        /// </summary>
        /// <returns>默认AI服务实例</returns>
        public ITextAnalysisProvider GetDefaultService()
        {
            var defaultServiceType = Environment.GetEnvironmentVariable("DEFAULT_AI_SERVICE_TYPE") ?? "Aliyun";

            if (Enum.TryParse<AIServiceType>(defaultServiceType, out var serviceType))
            {
                _logger.LogInformation("使用默认AI服务类型: {ServiceType}", serviceType);
                return GetService(serviceType);
            }

            _logger.LogWarning("无法解析默认AI服务类型配置，使用DeepSeek作为默认服务");
            return GetService(AIServiceType.DeepSeek);
        }
    }

    /// <summary>
    /// 文本分析提供者接口
    /// </summary>
    public interface ITextAnalysisProvider
    {
        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">分析输入参数</param>
        /// <returns>分析结果</returns>
        Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input);
    }

    /// <summary>
    /// DeepSeek文本分析提供者
    /// </summary>
    public class DeepSeekTextAnalysisProvider(DeepSeekTextAnalysisService deepSeekTextAnalysisService) : ITextAnalysisProvider
    {
        private readonly DeepSeekTextAnalysisService _deepSeekTextAnalysisService = deepSeekTextAnalysisService;

        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            var prompt = BuildAnalysisPrompt(input);
            var response = await _deepSeekTextAnalysisService.CallAIApiAsync(prompt, input.Text, 800);
            var result = ParseAnalysisResult(response.Choices[0].Message.Content);
            result.AnalysisTime = DateTime.Now;

            return result;
        }

        private static string BuildAnalysisPrompt(TextAnalysisInputDto input)
        {
            var taskDescription = input.AnalysisType == TextAnalysisType.TextClassification
                ? $"请对输入的多个同类文本样本进行深度分析，提取该类文本（{input.ClassificationName}）的通用特征，生成结构化的分类描述和特征关键词，用于文本分类和模板匹配。"
                : "请对输入的文本进行深度分析，生成结构化的摘要和关键词提取结果，用于后续的语义匹配和模板分类。";

            return BaseTextAnalysisService.BuildGenericPrompt(input.KeywordCount, input.MaxSummaryLength, taskDescription);
        }

        private static TextAnalysisDto ParseAnalysisResult(string content)
        {
            return BaseTextAnalysisService.ParseAnalysisResult(content);
        }
    }

    /// <summary>
    /// 阿里云文本分析提供者
    /// </summary>
    public class AliyunTextAnalysisProvider(AliyunAIService aliyunAIService) : ITextAnalysisProvider
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            try
            {
                // 并行调用阿里云AI服务生成摘要和关键词
                var summaryTask = _aliyunAIService.GenerateSummaryAsync(input.Text);
                var keywordsTask = _aliyunAIService.ExtractKeywordsAsync(input.Text, input.KeywordCount);

                await Task.WhenAll(summaryTask, keywordsTask);

                var result = new TextAnalysisDto
                {
                    Summary = summaryTask.Result.Length > input.MaxSummaryLength ? summaryTask.Result[..input.MaxSummaryLength] : summaryTask.Result,
                    Keywords = keywordsTask.Result,
                    Confidence = 0.9, // 阿里云AI的默认置信度
                    AnalysisTime = DateTime.Now
                };

                return result;
            }
            catch (Exception)
            {
                // 如果并行调用失败，尝试串行调用
                var summary = await _aliyunAIService.GenerateSummaryAsync(input.Text);
                var keywords = await _aliyunAIService.ExtractKeywordsAsync(input.Text, input.KeywordCount);

                var result = new TextAnalysisDto
                {
                    Summary = summary.Length > input.MaxSummaryLength ? summary[..input.MaxSummaryLength] : summary,
                    Keywords = keywords,
                    Confidence = 0.9,
                    AnalysisTime = DateTime.Now
                };

                return result;
            }
        }
    }
}
