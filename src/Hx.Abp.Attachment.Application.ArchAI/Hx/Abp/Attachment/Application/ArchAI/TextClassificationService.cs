using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using System.Text;
using Volo.Abp;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 文本分类服务
    /// </summary>
    public class TextClassificationService(
        ILogger<TextClassificationService> logger,
        HttpClient httpClient,
        SemanticVectorService semanticVectorService) : BaseTextAnalysisService(logger, httpClient, semanticVectorService)
    {
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

                var taskDescription = $"请对输入的多个同类文本样本进行深度分析，提取该类文本（{input.ClassificationName}）的通用特征，生成结构化的分类描述和特征关键词，用于文本分类和模板匹配。";
                var prompt = BuildGenericPrompt(input.KeywordCount, input.MaxSummaryLength, taskDescription);
                var userContent = BuildSampleText(input.TextSamples);

                var apiResponse = await CallAIApiAsync(prompt, userContent, 1000);
                var result = ParseAnalysisResult(apiResponse.Choices[0].Message.Content);
                result.AnalysisTime = DateTime.Now;
                // 添加元数据
                stopwatch.Stop();
                AddMetadata(result, apiResponse, input.TextSamples.Sum(s => s.Length), stopwatch.ElapsedMilliseconds);
                // 识别文档类型和业务领域
                result.DocumentType = input.ClassificationName;
                result.BusinessDomain = IdentifyBusinessDomain(result.Summary, result.Keywords);
                // 生成语义向量
                if (input.GenerateSemanticVector)
                {
                    result.SemanticVector = await GenerateSemanticVectorAsync(result.Summary, result.Keywords);
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
    }
}
