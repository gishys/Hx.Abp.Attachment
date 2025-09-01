using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    /// <summary>
    /// 分类名称推荐输入DTO
    /// </summary>
    public class CategoryNameRecommendationInputDto
    {
        /// <summary>
        /// 文档内容或描述
        /// </summary>
        [Required]
        [StringLength(10000, MinimumLength = 10, ErrorMessage = "文本长度必须在10-10000字符之间")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 业务领域（如：金融服务、制造业、房地产等）
        /// </summary>
        public string? BusinessDomain { get; set; }

        /// <summary>
        /// 文档类型（如：合同、发票、证明等）
        /// </summary>
        public string? DocumentType { get; set; }

        /// <summary>
        /// 期望的分类名称数量
        /// </summary>
        [Range(1, 10)]
        public int RecommendationCount { get; set; } = 5;

        /// <summary>
        /// 是否包含分类描述
        /// </summary>
        public bool IncludeDescription { get; set; } = true;

        /// <summary>
        /// 是否包含分类关键词
        /// </summary>
        public bool IncludeKeywords { get; set; } = true;

        /// <summary>
        /// 业务上下文信息
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }
    }

    /// <summary>
    /// 分类名称推荐结果DTO
    /// </summary>
    public class CategoryNameRecommendationResultDto
    {
        /// <summary>
        /// 推荐分类名称列表
        /// </summary>
        [Required]
        public List<RecommendedCategory> RecommendedCategories { get; set; } = [];

        /// <summary>
        /// 推荐置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 推荐时间戳
        /// </summary>
        public DateTime RecommendationTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 推荐元数据
        /// </summary>
        public CategoryRecommendationMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// 推荐分类
    /// </summary>
    public class RecommendedCategory
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分类描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分类关键词
        /// </summary>
        public List<string> Keywords { get; set; } = [];

        /// <summary>
        /// 推荐置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 分类层级（如：一级分类、二级分类）
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 父分类名称
        /// </summary>
        public string? ParentCategory { get; set; }

        /// <summary>
        /// 适用场景
        /// </summary>
        public List<string> ApplicableScenarios { get; set; } = [];
    }

    /// <summary>
    /// 分类推荐元数据
    /// </summary>
    public class CategoryRecommendationMetadata
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
        /// 推荐分类数量
        /// </summary>
        public int RecommendedCategoryCount { get; set; }

        /// <summary>
        /// 业务领域识别结果
        /// </summary>
        public string? IdentifiedBusinessDomain { get; set; }

        /// <summary>
        /// 文档类型识别结果
        /// </summary>
        public string? IdentifiedDocumentType { get; set; }
    }
}
