using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class ClassificationExtentResult
    {
        [JsonPropertyName("recommendedCategory")]
        public required string RecommendedCategory { get; set; }
        [JsonPropertyName("recommendedCategoryId")]
        public required Guid RecommendedCategoryId { get; set; }
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
    /// <summary>
    /// 智能分类推荐结果DTO
    /// </summary>
    public class SmartClassificationResultDto
    {
        /// <summary>
        /// 文件基本信息
        /// </summary>
        public AttachFileDto? FileInfo { get; set; }

        /// <summary>
        /// 推荐分类结果
        /// </summary>
        public ClassificationExtentResult? Classification { get; set; }

        /// <summary>
        /// 可选的分类列表
        /// </summary>
        public List<CategoryOptionDto> AvailableCategories { get; set; } = [];

        /// <summary>
        /// OCR提取的文本内容
        /// </summary>
        public string? OcrContent { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 处理状态
        /// </summary>
        public SmartClassificationStatus Status { get; set; } = SmartClassificationStatus.Success;

        /// <summary>
        /// 错误信息（如果处理失败）
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 分类选项DTO
    /// </summary>
    public class CategoryOptionDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分类路径
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// 父分类ID
        /// </summary>
        public Guid? ParentId { get; set; }
    }

    /// <summary>
    /// 智能分类状态枚举
    /// </summary>
    public enum SmartClassificationStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// OCR处理失败
        /// </summary>
        OcrFailed = 1,

        /// <summary>
        /// 分类推荐失败
        /// </summary>
        ClassificationFailed = 2,

        /// <summary>
        /// 没有可用的分类选项
        /// </summary>
        NoCategoriesAvailable = 3,

        /// <summary>
        /// 文件不支持OCR
        /// </summary>
        FileNotSupportedForOcr = 4,

        /// <summary>
        /// 系统错误
        /// </summary>
        SystemError = 99
    }

    /// <summary>
    /// 智能分类输入DTO
    /// </summary>
    public class SmartClassificationInputDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// 文件列表
        /// </summary>
        public List<AttachFileCreateDto> Files { get; set; } = [];

        /// <summary>
        /// 文件前缀
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// 是否强制重新OCR处理
        /// </summary>
        public bool ForceOcrReprocessing { get; set; } = false;
    }
}
