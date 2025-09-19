namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 智能分析结果DTO
    /// </summary>
    public class IntelligentAnalysisResultDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 分析状态
        /// </summary>
        public AnalysisStatus Status { get; set; } = AnalysisStatus.Success;

        /// <summary>
        /// 错误信息（如果分析失败）
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 概要信息分析结果
        /// </summary>
        public SummaryAnalysisResult? SummaryAnalysis { get; set; }

        /// <summary>
        /// 标签分析结果
        /// </summary>
        public TagsAnalysisResult? TagsAnalysis { get; set; }

        /// <summary>
        /// 全文内容分析结果
        /// </summary>
        public FullTextAnalysisResult? FullTextAnalysis { get; set; }

        /// <summary>
        /// 元数据分析结果
        /// </summary>
        public MetaDataAnalysisResult? MetaDataAnalysis { get; set; }

        /// <summary>
        /// 更新的字段列表
        /// </summary>
        public List<string> UpdatedFields { get; set; } = [];

        /// <summary>
        /// 分析统计信息
        /// </summary>
        public AnalysisStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 分析状态枚举
    /// </summary>
    public enum AnalysisStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 部分成功
        /// </summary>
        PartialSuccess = 1,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = 2,

        /// <summary>
        /// 跳过（无需分析）
        /// </summary>
        Skipped = 3
    }

    /// <summary>
    /// 概要信息分析结果
    /// </summary>
    public class SummaryAnalysisResult
    {
        /// <summary>
        /// 原始概要信息
        /// </summary>
        public string? OriginalSummary { get; set; }

        /// <summary>
        /// 生成的概要信息
        /// </summary>
        public string? GeneratedSummary { get; set; }

        /// <summary>
        /// 是否更新
        /// </summary>
        public bool IsUpdated { get; set; }

        /// <summary>
        /// 置信度（0-1）
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 关键字列表
        /// </summary>
        public List<string> Keywords { get; set; } = [];

        /// <summary>
        /// 语义向量（用于相似度计算）
        /// </summary>
        public List<double>? SemanticVector { get; set; }
    }

    /// <summary>
    /// 标签分析结果
    /// </summary>
    public class TagsAnalysisResult
    {
        /// <summary>
        /// 原始标签
        /// </summary>
        public List<string> OriginalTags { get; set; } = [];

        /// <summary>
        /// 生成的标签
        /// </summary>
        public List<string> GeneratedTags { get; set; } = [];

        /// <summary>
        /// 是否更新
        /// </summary>
        public bool IsUpdated { get; set; }

        /// <summary>
        /// 标签置信度
        /// </summary>
        public Dictionary<string, float> TagConfidences { get; set; } = [];
    }

    /// <summary>
    /// 全文内容分析结果
    /// </summary>
    public class FullTextAnalysisResult
    {
        /// <summary>
        /// 处理的文件数量
        /// </summary>
        public int ProcessedFilesCount { get; set; }

        /// <summary>
        /// 成功的文件数量
        /// </summary>
        public int SuccessfulFilesCount { get; set; }

        /// <summary>
        /// 失败的文件数量
        /// </summary>
        public int FailedFilesCount { get; set; }

        /// <summary>
        /// 是否更新
        /// </summary>
        public bool IsUpdated { get; set; }

        /// <summary>
        /// 提取的文本内容长度
        /// </summary>
        public int ExtractedTextLength { get; set; }

        /// <summary>
        /// 处理详情
        /// </summary>
        public List<FileProcessingDetail> ProcessingDetails { get; set; } = [];
    }

    /// <summary>
    /// 文件处理详情
    /// </summary>
    public class FileProcessingDetail
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 处理状态
        /// </summary>
        public FileProcessingStatus Status { get; set; }

        /// <summary>
        /// 提取的文本长度
        /// </summary>
        public int ExtractedTextLength { get; set; }

        /// <summary>
        /// 提取的文本内容（用于AI分析）
        /// </summary>
        public string? ExtractedText { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 文件处理状态
    /// </summary>
    public enum FileProcessingStatus
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,

        /// <summary>
        /// 失败
        /// </summary>
        Failed = 1,

        /// <summary>
        /// 跳过（不支持OCR）
        /// </summary>
        Skipped = 2
    }

    /// <summary>
    /// 元数据分析结果
    /// </summary>
    public class MetaDataAnalysisResult
    {
        /// <summary>
        /// 原始元数据字段数量
        /// </summary>
        public int OriginalMetaFieldsCount { get; set; }

        /// <summary>
        /// 生成的元数据字段数量
        /// </summary>
        public int GeneratedMetaFieldsCount { get; set; }

        /// <summary>
        /// 是否更新
        /// </summary>
        public bool IsUpdated { get; set; }

        /// <summary>
        /// 识别的实体列表
        /// </summary>
        public List<RecognizedEntity> RecognizedEntities { get; set; } = [];

        /// <summary>
        /// 生成的元数据字段
        /// </summary>
        public List<MetaFieldDto> GeneratedMetaFields { get; set; } = [];
    }

    /// <summary>
    /// 识别的实体
    /// </summary>
    public class RecognizedEntity
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
        /// 置信度
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// 出现次数
        /// </summary>
        public int OccurrenceCount { get; set; }
    }

    /// <summary>
    /// 分析统计信息
    /// </summary>
    public class AnalysisStatistics
    {
        /// <summary>
        /// 总处理时间（毫秒）
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// 处理的文件总数
        /// </summary>
        public int TotalFilesProcessed { get; set; }

        /// <summary>
        /// 成功处理的文件数
        /// </summary>
        public int SuccessfulFilesProcessed { get; set; }

        /// <summary>
        /// 提取的文本总长度
        /// </summary>
        public int TotalExtractedTextLength { get; set; }

        /// <summary>
        /// 生成的标签数量
        /// </summary>
        public int GeneratedTagsCount { get; set; }

        /// <summary>
        /// 识别的实体数量
        /// </summary>
        public int RecognizedEntitiesCount { get; set; }

        /// <summary>
        /// 更新的字段数量
        /// </summary>
        public int UpdatedFieldsCount { get; set; }
    }
}
