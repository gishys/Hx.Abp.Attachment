using YourNamespace.AttachCatalogues;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 语义匹配服务接口
    /// </summary>
    public interface ISemanticMatcher
    {
        /// <summary>
        /// 基于语义生成分类名称
        /// </summary>
        /// <param name="modelName">使用的AI模型名称</param>
        /// <param name="context">上下文数据</param>
        /// <returns>生成的分类名称</returns>
        Task<string> GenerateNameAsync(string modelName, Dictionary<string, object> context);

        /// <summary>
        /// 匹配与查询语义相似的模板
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="templates">模板列表</param>
        /// <param name="threshold">相似度阈值 (0-1)</param>
        /// <param name="topN">返回最匹配的前N个结果</param>
        /// <returns>匹配的模板列表</returns>
        Task<List<AttachCatalogueTemplate>> MatchTemplatesAsync(
            string query,
            List<AttachCatalogueTemplate> templates,
            double threshold = 0.6,
            int topN = 5);

        /// <summary>
        /// 计算两个文本的语义相似度
        /// </summary>
        /// <param name="text1">文本1</param>
        /// <param name="text2">文本2</param>
        /// <returns>相似度分数 (0-1)</returns>
        Task<double> CalculateSimilarityAsync(string text1, string text2);

        /// <summary>
        /// 训练自定义语义模型
        /// </summary>
        /// <param name="modelName">模型名称</param>
        /// <param name="trainingData">训练数据 (文本, 标签)</param>
        Task TrainModelAsync(string modelName, Dictionary<string, string> trainingData);

        /// <summary>
        /// 提取文本的关键特征
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>特征向量</returns>
        Task<float[]> ExtractFeaturesAsync(string text);
    }
}
