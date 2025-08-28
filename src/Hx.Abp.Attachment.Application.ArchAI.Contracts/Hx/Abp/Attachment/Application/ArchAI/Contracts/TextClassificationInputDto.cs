using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    /// <summary>
    /// 文本分类输入DTO
    /// </summary>
    public class TextClassificationInputDto
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        [Required]
        [StringLength(100, ErrorMessage = "分类名称长度不能超过100字符")]
        public string ClassificationName { get; set; } = string.Empty;

        /// <summary>
        /// 文本样本列表
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "至少需要提供一个文本样本")]
        [MaxLength(50, ErrorMessage = "文本样本数量不能超过50个")]
        public List<string> TextSamples { get; set; } = [];

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
        /// 业务上下文信息
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }

        /// <summary>
        /// 指定使用的AI服务类型（可选，默认使用配置的默认服务）
        /// </summary>
        public AIServiceType? PreferredAIService { get; set; }
    }
}
