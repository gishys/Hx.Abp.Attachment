using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    /// <summary>
    /// 实体识别输入DTO
    /// </summary>
    public class EntityRecognitionInputDto
    {
        /// <summary>
        /// 待识别的文本内容
        /// </summary>
        [Required]
        [StringLength(10000, MinimumLength = 10, ErrorMessage = "文本长度必须在10-10000字符之间")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 要识别的实体类型列表
        /// </summary>
        [Required]
        public List<string> EntityTypes { get; set; } = [];

        /// <summary>
        /// 是否包含实体位置信息
        /// </summary>
        public bool IncludePosition { get; set; } = false;

        /// <summary>
        /// 是否包含实体置信度
        /// </summary>
        public bool IncludeConfidence { get; set; } = true;

        /// <summary>
        /// 业务上下文信息
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }
    }

    /// <summary>
    /// 实体识别结果DTO
    /// </summary>
    public class EntityRecognitionResultDto
    {
        /// <summary>
        /// 识别到的实体列表
        /// </summary>
        [Required]
        public List<RecognizedEntity> Entities { get; set; } = [];

        /// <summary>
        /// 识别置信度 (0-1)
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 识别时间戳
        /// </summary>
        public DateTime RecognitionTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 识别的实体类型统计
        /// </summary>
        public Dictionary<string, int> EntityTypeCounts { get; set; } = [];

        /// <summary>
        /// 识别元数据
        /// </summary>
        public EntityRecognitionMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// 识别到的实体
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
        /// 实体值
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// 置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 实体在文本中的起始位置
        /// </summary>
        public int? StartPosition { get; set; }

        /// <summary>
        /// 实体在文本中的结束位置
        /// </summary>
        public int? EndPosition { get; set; }

        /// <summary>
        /// 实体属性（如金额、日期等）
        /// </summary>
        public Dictionary<string, object>? Properties { get; set; }
    }

    /// <summary>
    /// 实体识别元数据
    /// </summary>
    public class EntityRecognitionMetadata
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
        /// 识别的实体类型数量
        /// </summary>
        public int RecognizedEntityTypeCount { get; set; }

        /// <summary>
        /// 总实体数量
        /// </summary>
        public int TotalEntityCount { get; set; }
    }
}
