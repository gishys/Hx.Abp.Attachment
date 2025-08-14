using System.Text.RegularExpressions;
using YourNamespace.AttachCatalogues;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 默认语义匹配服务实现
    /// 使用本地算法提供基本功能
    /// 可替换为真正的AI服务
    /// </summary>
    public class DefaultSemanticMatcher : ISemanticMatcher
    {
        // 简单缓存训练过的模型
        private readonly Dictionary<string, Dictionary<string, string>> _trainedModels =
            new Dictionary<string, Dictionary<string, string>>();

        public Task<string> GenerateNameAsync(string modelName, Dictionary<string, object> context)
        {
            // 默认实现：基于上下文生成简单名称
            string contextSummary = string.Join("_", context.Values.Take(3));
            string generatedName = $"Generated_{DateTime.Now:yyyyMMdd}_{contextSummary}";
            return Task.FromResult(generatedName.Trim('_'));
        }

        public async Task<List<AttachCatalogueTemplate>> MatchTemplatesAsync(
            string query,
            List<AttachCatalogueTemplate> templates,
            double threshold = 0.6,
            int topN = 5)
        {
            var matches = new List<(AttachCatalogueTemplate Template, double Score)>();

            foreach (var template in templates)
            {
                double score = await CalculateSimilarityAsync(query, template.TemplateName);
                if (score >= threshold)
                {
                    matches.Add((template, score));
                }
            }

            return matches
                .OrderByDescending(m => m.Score)
                .Take(topN)
                .Select(m => m.Template)
                .ToList();
        }

        public Task<double> CalculateSimilarityAsync(string text1, string text2)
        {
            // 默认实现：使用Jaccard相似度算法
            if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
                return Task.FromResult(0.0);

            // 预处理文本
            text1 = PreprocessText(text1);
            text2 = PreprocessText(text2);

            // 分词
            var tokens1 = Tokenize(text1);
            var tokens2 = Tokenize(text2);

            if (tokens1.Count == 0 || tokens2.Count == 0)
                return Task.FromResult(0.0);

            // 计算Jaccard相似度
            var intersection = tokens1.Intersect(tokens2).Count();
            var union = tokens1.Union(tokens2).Count();

            double similarity = (double)intersection / union;
            return Task.FromResult(similarity);
        }

        public Task TrainModelAsync(string modelName, Dictionary<string, string> trainingData)
        {
            // 默认实现：简单存储训练数据
            _trainedModels[modelName] = trainingData;
            return Task.CompletedTask;
        }

        public Task<float[]> ExtractFeaturesAsync(string text)
        {
            // 默认实现：返回简单的二进制特征向量
            // 实际应用中应替换为真正的特征提取
            var tokens = Tokenize(PreprocessText(text));
            var uniqueTokens = tokens.Distinct().ToList();

            // 创建固定大小的特征向量
            var features = new float[256];
            for (int i = 0; i < Math.Min(uniqueTokens.Count, 256); i++)
            {
                features[i] = 1.0f;
            }

            return Task.FromResult(features);
        }

        #region Helper Methods

        private string PreprocessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 转换为小写
            text = text.ToLowerInvariant();

            // 移除非字母数字字符（保留中文等）
            text = Regex.Replace(text, @"[^\p{L}\p{N}\s]", "");

            return text.Trim();
        }

        private List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // 简单分词：按空格分割
            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                       .Where(t => t.Length > 1) // 过滤掉单字符
                       .ToList();
        }

        #endregion
    }
}
