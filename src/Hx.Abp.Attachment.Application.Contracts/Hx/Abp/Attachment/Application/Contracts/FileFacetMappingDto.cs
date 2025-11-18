using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 文件与动态分面映射DTO
    /// 用于标识文件所属的动态分面分类，支持文件索引以避免文件名重复问题
    /// </summary>
    public class FileFacetMappingDto
    {
        /// <summary>
        /// 文件名（仅文件名，不包含路径）
        /// 用于向后兼容和显示
        /// </summary>
        [JsonPropertyName("fileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// 文件索引（在文件列表中的位置，从0开始）
        /// 这是最可靠的匹配方式，因为 IFormFile.FileName 只包含文件名，不包含路径
        /// 前端按顺序上传文件，后端按索引位置匹配，即使文件名重复也能准确匹配
        /// </summary>
        [JsonPropertyName("fileIndex")]
        public int? FileIndex { get; set; }

        /// <summary>
        /// 文件大小（字节数，可选）
        /// 可用于辅助匹配，提高匹配准确性（文件名+大小组合）
        /// </summary>
        [JsonPropertyName("fileSize")]
        public long? FileSize { get; set; }

        /// <summary>
        /// 动态分面分类名称（如案卷名称）
        /// </summary>
        [Required(ErrorMessage = "动态分面分类名称不能为空")]
        [MaxLength(500, ErrorMessage = "动态分面分类名称长度不能超过500个字符")]
        [JsonPropertyName("dynamicFacetCatalogueName")]
        public required string DynamicFacetCatalogueName { get; set; }

        /// <summary>
        /// 子文件夹路径（相对于动态分面文件夹的路径，如 "子文件夹1/子文件夹2"）
        /// 用于动态创建子分类，支持嵌套文件夹结构
        /// 例如：如果文件在 "案卷A/材料类型/正本" 路径下，则此值为 "材料类型/正本"
        /// </summary>
        [MaxLength(1000, ErrorMessage = "子文件夹路径长度不能超过1000个字符")]
        [JsonPropertyName("subFolderPath")]
        public string? SubFolderPath { get; set; }
    }
}


