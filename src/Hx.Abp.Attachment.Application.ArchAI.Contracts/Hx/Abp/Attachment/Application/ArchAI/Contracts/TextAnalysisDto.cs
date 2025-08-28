using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    /// <summary>
    /// 文本分析结果DTO
    /// </summary>
    public class TextAnalysisDto
    {
        /// <summary>
        /// 文本摘要
        /// </summary>
        [Required]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// 关键词列表
        /// </summary>
        [Required]
        public List<string> Keywords { get; set; } = [];

        /// <summary>
        /// 分析置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 分析时间戳
        /// </summary>
        public DateTime AnalysisTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 语义向量（用于相似度计算）
        /// </summary>
        public List<double>? SemanticVector { get; set; }

        /// <summary>
        /// 文档类型识别结果
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// 业务领域分类
        /// </summary>
        public string? BusinessDomain { get; set; }

        /// <summary>
        /// 提取的实体信息
        /// </summary>
        public List<EntityInfo> Entities { get; set; } = [];

        /// <summary>
        /// 分析元数据
        /// </summary>
        public AnalysisMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// 实体信息
    /// </summary>
    public class EntityInfo
    {
        /// <summary>
        /// 实体名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 实体类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 实体值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 分析元数据
    /// </summary>
    public class AnalysisMetadata
    {
        /// <summary>
        /// 文本长度
        /// </summary>
        public int TextLength { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 使用的模型
        /// </summary>
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// API调用统计
        /// </summary>
        public ApiUsageInfo? ApiUsage { get; set; }
    }

    /// <summary>
    /// API使用信息
    /// </summary>
    public class ApiUsageInfo
    {
        /// <summary>
        /// 提示词token数
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// 完成token数
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总token数
        /// </summary>
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 文本分析输入DTO
    /// </summary>
    public class TextAnalysisInputDto
    {
        /// <summary>
        /// 待分析的文本内容
        /// </summary>
        [Required]
        [StringLength(10000, MinimumLength = 10, ErrorMessage = "文本长度必须在10-10000字符之间")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 关键词提取数量 (默认5个)
        /// </summary>
        [Range(1, 20)]
        public int KeywordCount { get; set; } = 5;

        /// <summary>
        /// 摘要最大长度 (默认200字符)
        /// </summary>
        [Range(50, 500)]
        public int MaxSummaryLength { get; set; } = 200;

        /// <summary>
        /// 是否生成语义向量
        /// </summary>
        public bool GenerateSemanticVector { get; set; } = true;

        /// <summary>
        /// 是否提取实体信息
        /// </summary>
        public bool ExtractEntities { get; set; } = true;

        /// <summary>
        /// 业务上下文信息
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }

        /// <summary>
        /// 分析类型
        /// </summary>
        public TextAnalysisType AnalysisType { get; set; } = TextAnalysisType.SingleDocument;

        /// <summary>
        /// 文本分类名称（当AnalysisType为TextClassification时使用）
        /// </summary>
        public string? ClassificationName { get; set; }

        /// <summary>
        /// 指定使用的AI服务类型（可选，默认使用配置的默认服务）
        /// </summary>
        public AIServiceType? PreferredAIService { get; set; }
    }

    /// <summary>
    /// 文本分析类型
    /// </summary>
    public enum TextAnalysisType
    {
        /// <summary>
        /// 单个文档分析：提取具体文档的摘要和关键词
        /// </summary>
        SingleDocument = 1,

        /// <summary>
        /// 文本分类分析：提取一类文本的通用特征，用于分类匹配
        /// </summary>
        TextClassification = 2
    }
}
