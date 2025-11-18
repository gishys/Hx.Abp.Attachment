using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 动态分面信息DTO
    /// 用于传递动态分面分类的创建信息（如案卷信息）
    /// </summary>
    public class DynamicFacetInfoDto
    {
        /// <summary>
        /// 动态分面分类名称（如案卷名称）
        /// </summary>
        [Required(ErrorMessage = "动态分面分类名称不能为空")]
        [MaxLength(500, ErrorMessage = "动态分面分类名称长度不能超过500个字符")]
        [JsonPropertyName("catalogueName")]
        public required string CatalogueName { get; set; }

        /// <summary>
        /// 动态分面分类描述（可选）
        /// </summary>
        [MaxLength(2000, ErrorMessage = "描述长度不能超过2000个字符")]
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 序号（可选，如果不提供则自动分配）
        /// </summary>
        [JsonPropertyName("sequenceNumber")]
        public int? SequenceNumber { get; set; }

        /// <summary>
        /// 标签列表（可选）
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 元数据字段（可选，用于存储额外的业务信息）
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }
}

