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
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// 分析置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 分析时间戳
        /// </summary>
        public DateTime AnalysisTime { get; set; } = DateTime.Now;
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
    }
}
