using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 简化版默认语义匹配服务实现
    /// 基于数据库驱动的智能推荐，移除复杂的本地算法
    /// </summary>
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ISemanticMatcher))]
    public partial class DefaultSemanticMatcher(
        ILogger<DefaultSemanticMatcher> logger,
        IAttachCatalogueTemplateRepository templateRepository) : ISemanticMatcher, ITransientDependency
    {
        private readonly ILogger<DefaultSemanticMatcher> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));

        /// <summary>
        /// 基于语义生成分类名称
        /// </summary>
        public Task<string> GenerateNameAsync(string modelName, Dictionary<string, object> context)
        {
            try
            {
                _logger.LogInformation("开始生成分类名称，模型：{modelName}", modelName);

                // 基于上下文生成智能名称
                var contextSummary = ExtractContextSummary(context);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var generatedName = $"Generated_{contextSummary}_{timestamp}";

                _logger.LogInformation("生成分类名称：{generatedName}", generatedName);
                return Task.FromResult(generatedName.Trim('_'));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成分类名称失败，模型：{modelName}", modelName);
                return Task.FromResult($"Generated_{DateTime.Now:yyyyMMdd}");
            }
        }

        /// <summary>
        /// 匹配与查询语义相似的模板
        /// </summary>
        public async Task<List<AttachCatalogueTemplate>> MatchTemplatesAsync(
            string query,
            List<AttachCatalogueTemplate> templates,
            double threshold = 0.6,
            int topN = 5)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("查询参数无效：{query}", query);
                return [];
            }

            try
            {
                _logger.LogInformation("开始数据库驱动的模板匹配，查询：{query}，阈值：{threshold}，TopN：{topN}", 
                    query, threshold, topN);

                // 直接使用数据库仓储进行智能推荐
                var matchedTemplates = await _templateRepository.GetIntelligentRecommendationsAsync(
                    query, threshold, topN, true, false);

                _logger.LogInformation("模板匹配完成，找到 {matchCount} 个匹配结果", matchedTemplates.Count);
                return matchedTemplates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模板匹配失败，查询：{query}", query);
                return [];
            }
        }

        /// <summary>
        /// 计算两个文本的语义相似度
        /// </summary>
        public async Task<double> CalculateSimilarityAsync(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            try
            {
                // 使用数据库仓储进行相似度计算
                var matchedTemplates = await _templateRepository.GetIntelligentRecommendationsAsync(
                    text1, 0.1, 1, true, false);

                if (matchedTemplates.Count > 0)
                {
                    var template = matchedTemplates.First();
                    // 基于模板名称与text2的简单相似度计算
                    var similarity = CalculateSimpleSimilarity(template.TemplateName, text2);
                    
                    _logger.LogDebug("相似度计算完成：{text1} vs {text2} = {score}", 
                        text1[..Math.Min(20, text1.Length)], 
                        text2[..Math.Min(20, text2.Length)], 
                        similarity);

                    return similarity;
                }

                return 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "相似度计算失败：{text1} vs {text2}", text1, text2);
                return 0.0;
            }
        }

        /// <summary>
        /// 训练自定义语义模型（简化版本）
        /// </summary>
        public Task TrainModelAsync(string modelName, Dictionary<string, string> trainingData)
        {
            try
            {
                _logger.LogInformation("语义模型训练（简化版本）：{modelName}，训练数据量：{dataCount}", 
                    modelName, trainingData?.Count ?? 0);

                // 简化实现：仅记录日志，实际训练由数据库驱动
                _logger.LogInformation("语义模型训练完成（数据库驱动）：{modelName}", modelName);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "训练语义模型失败：{modelName}", modelName);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 提取文本的关键特征（简化版本）
        /// </summary>
        public Task<float[]> ExtractFeaturesAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(new float[64]);

            try
            {
                // 简化特征提取：基于文本长度和基本统计
                var features = new float[64];
                
                // 文本长度特征
                features[0] = Math.Min(text.Length / 1000.0f, 1.0f);
                
                // 词汇数量特征
                var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                features[1] = Math.Min(wordCount / 100.0f, 1.0f);
                
                // 数字密度特征
                var digitCount = text.Count(char.IsDigit);
                features[2] = Math.Min(digitCount / 100.0f, 1.0f);

                return Task.FromResult(features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "特征提取失败：{text}", text);
                return Task.FromResult(new float[64]);
            }
        }

        #region 私有方法

        /// <summary>
        /// 提取上下文摘要
        /// </summary>
        private string ExtractContextSummary(Dictionary<string, object> context)
        {
            try
            {
                var summaryParts = new List<string>();

                foreach (var (key, value) in context.Take(3))
                {
                    if (value != null)
                    {
                        var valueStr = value.ToString();
                        if (!string.IsNullOrWhiteSpace(valueStr))
                        {
                            // 提取关键信息
                            var words = valueStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var keyWords = words.Take(2).Where(w => w.Length > 1);
                            summaryParts.AddRange(keyWords);
                        }
                    }
                }

                return string.Join("_", summaryParts.Take(5));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "特征提取失败：{text}", ex.Message);
                return "Context";
            }
        }

        /// <summary>
        /// 计算简单相似度
        /// </summary>
        private static double CalculateSimpleSimilarity(string text1, string text2)
        {
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return 0.0;

            var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words1.Length == 0 || words2.Length == 0)
                return 0.0;

            var intersection = words1.Intersect(words2, StringComparer.OrdinalIgnoreCase).Count();
            var union = words1.Union(words2, StringComparer.OrdinalIgnoreCase).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        #endregion
    }
}
