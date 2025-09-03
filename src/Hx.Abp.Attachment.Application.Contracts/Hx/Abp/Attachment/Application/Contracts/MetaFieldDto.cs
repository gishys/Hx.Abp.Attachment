using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 元数据字段DTO
    /// </summary>
    public class MetaFieldDto
    {
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// 字段键名
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 正则表达式模式
        /// </summary>
        public string? RegexPattern { get; set; }

        /// <summary>
        /// 枚举选项
        /// </summary>
        public string? Options { get; set; }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// 字段顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 字段分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 验证规则
        /// </summary>
        public string? ValidationRules { get; set; }

        /// <summary>
        /// 元数据标签
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; set; }
    }

    /// <summary>
    /// 创建/更新元数据字段DTO
    /// </summary>
    public class CreateUpdateMetaFieldDto
    {
        /// <summary>
        /// 实体类型
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// 字段键名
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 单位
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 正则表达式模式
        /// </summary>
        public string? RegexPattern { get; set; }

        /// <summary>
        /// 枚举选项
        /// </summary>
        public string? Options { get; set; }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// 字段顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 字段分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 验证规则
        /// </summary>
        public string? ValidationRules { get; set; }

        /// <summary>
        /// 元数据标签
        /// </summary>
        public List<string>? Tags { get; set; }
    }

    /// <summary>
    /// 元数据字段查询DTO
    /// </summary>
    public class MetaFieldQueryDto
    {
        /// <summary>
        /// 实体类型
        /// </summary>
        public string? EntityType { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public string? DataType { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool? IsEnabled { get; set; }

        /// <summary>
        /// 字段分组
        /// </summary>
        public string? Group { get; set; }

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// 标签
        /// </summary>
        public List<string>? Tags { get; set; }
    }
}
