namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// 语义向量服务配置
    /// </summary>
    public static class SemanticVectorConfiguration
    {
        /// <summary>
        /// 默认模型名称
        /// </summary>
        public const string DefaultModel = "text-embedding-v4";

        /// <summary>
        /// 默认向量维度
        /// </summary>
        public const int DefaultDimension = 1024;

        /// <summary>
        /// 默认编码格式
        /// </summary>
        public const string DefaultEncodingFormat = "float";

        /// <summary>
        /// 最大批量处理大小
        /// </summary>
        public const int MaxBatchSize = 10;

        /// <summary>
        /// API请求超时时间（秒）
        /// </summary>
        public const int RequestTimeoutSeconds = 30;

        /// <summary>
        /// 重试次数
        /// </summary>
        public const int MaxRetryCount = 3;

        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        public const int RetryDelayMs = 1000;

        /// <summary>
        /// 支持的模型列表
        /// </summary>
        public static readonly string[] SupportedModels = [
            "text-embedding-v1",
            "text-embedding-v2", 
            "text-embedding-v3",
            "text-embedding-v4"
        ];

        /// <summary>
        /// 支持的向量维度
        /// </summary>
        public static readonly int[] SupportedDimensions = [512, 1024, 1536];

        /// <summary>
        /// 验证模型名称是否支持
        /// </summary>
        /// <param name="model">模型名称</param>
        /// <returns>是否支持</returns>
        public static bool IsModelSupported(string model)
        {
            return !string.IsNullOrWhiteSpace(model) && SupportedModels.Contains(model);
        }

        /// <summary>
        /// 验证向量维度是否支持
        /// </summary>
        /// <param name="dimension">向量维度</param>
        /// <returns>是否支持</returns>
        public static bool IsDimensionSupported(int dimension)
        {
            return SupportedDimensions.Contains(dimension);
        }

        /// <summary>
        /// 获取默认模型名称
        /// </summary>
        /// <returns>默认模型名称</returns>
        public static string GetDefaultModel()
        {
            return DefaultModel;
        }

        /// <summary>
        /// 获取默认向量维度
        /// </summary>
        /// <returns>默认向量维度</returns>
        public static int GetDefaultDimension()
        {
            return DefaultDimension;
        }
    }
}
