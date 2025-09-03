using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 元数据字段值对象
    /// 用于描述模板的元数据字段信息，支持命名实体识别(NER)和前端展示
    /// </summary>
    public class MetaField : ValueObject
    {
        /// <summary>
        /// 实体类型（如：Project、ProjectPhase、Document等）
        /// </summary>
        public string EntityType { get; private set; }

        /// <summary>
        /// 字段键名（JSON key）
        /// </summary>
        public string FieldKey { get; private set; }

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string FieldName { get; private set; }

        /// <summary>
        /// 数据类型（string/number/date/boolean/array/object）
        /// </summary>
        public string DataType { get; private set; }

        /// <summary>
        /// 单位（如：kV、万元、m³等）
        /// </summary>
        public string? Unit { get; private set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; private set; }

        /// <summary>
        /// 正则表达式模式（用于前端校验）
        /// </summary>
        public string? RegexPattern { get; private set; }

        /// <summary>
        /// 枚举选项（JSON数组格式）
        /// </summary>
        public string? Options { get; private set; }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string? Description { get; private set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string? DefaultValue { get; private set; }

        /// <summary>
        /// 字段顺序
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 字段分组
        /// </summary>
        public string? Group { get; private set; }

        /// <summary>
        /// 验证规则（JSON格式，存储复杂的验证逻辑）
        /// </summary>
        public string? ValidationRules { get; private set; }

        /// <summary>
        /// 元数据标签（用于分类和搜索）
        /// </summary>
        public List<string>? Tags { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; private set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        protected MetaField() { }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

        public MetaField(
            string entityType,
            string fieldKey,
            string fieldName,
            string dataType,
            bool isRequired = false,
            string? unit = null,
            string? regexPattern = null,
            string? options = null,
            string? description = null,
            string? defaultValue = null,
            int order = 0,
            bool isEnabled = true,
            string? group = null,
            string? validationRules = null,
            List<string>? tags = null)
        {
            EntityType = Check.NotNullOrWhiteSpace(entityType, nameof(entityType));
            FieldKey = Check.NotNullOrWhiteSpace(fieldKey, nameof(fieldKey));
            FieldName = Check.NotNullOrWhiteSpace(fieldName, nameof(fieldName));
            DataType = Check.NotNullOrWhiteSpace(dataType, nameof(dataType));
            IsRequired = isRequired;
            Unit = unit;
            RegexPattern = regexPattern;
            Options = options;
            Description = description;
            DefaultValue = defaultValue;
            Order = order;
            IsEnabled = isEnabled;
            Group = group;
            ValidationRules = validationRules;
            Tags = tags ?? [];
            CreationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新字段信息
        /// </summary>
        public void Update(
            string? fieldName = null,
            string? dataType = null,
            bool? isRequired = null,
            string? unit = null,
            string? regexPattern = null,
            string? options = null,
            string? description = null,
            string? defaultValue = null,
            int? order = null,
            bool? isEnabled = null,
            string? group = null,
            string? validationRules = null,
            List<string>? tags = null)
        {
            if (fieldName != null) FieldName = Check.NotNullOrWhiteSpace(fieldName, nameof(fieldName));
            if (dataType != null) DataType = Check.NotNullOrWhiteSpace(dataType, nameof(dataType));
            if (isRequired.HasValue) IsRequired = isRequired.Value;
            if (unit != null) Unit = unit;
            if (regexPattern != null) RegexPattern = regexPattern;
            if (options != null) Options = options;
            if (description != null) Description = description;
            if (defaultValue != null) DefaultValue = defaultValue;
            if (order.HasValue) Order = order.Value;
            if (isEnabled.HasValue) IsEnabled = isEnabled.Value;
            if (group != null) Group = group;
            if (validationRules != null) ValidationRules = validationRules;
            if (tags != null) Tags = tags;
            
            LastModificationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                Tags ??= [];
                if (!Tags.Contains(tag))
                {
                    Tags.Add(tag);
                    LastModificationTime = DateTime.UtcNow;
                }
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public void RemoveTag(string tag)
        {
            if (Tags?.Remove(tag) == true)
            {
                LastModificationTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 启用字段
        /// </summary>
        public void Enable()
        {
            IsEnabled = true;
            LastModificationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 禁用字段
        /// </summary>
        public void Disable()
        {
            IsEnabled = false;
            LastModificationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 验证字段配置
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(EntityType))
                throw new ArgumentException("实体类型不能为空", nameof(EntityType));

            if (string.IsNullOrWhiteSpace(FieldKey))
                throw new ArgumentException("字段键名不能为空", nameof(FieldKey));

            if (string.IsNullOrWhiteSpace(FieldName))
                throw new ArgumentException("字段名称不能为空", nameof(FieldName));

            if (string.IsNullOrWhiteSpace(DataType))
                throw new ArgumentException("数据类型不能为空", nameof(DataType));

            // 验证数据类型
            var validDataTypes = new[] { "string", "number", "date", "boolean", "array", "object" };
            if (!validDataTypes.Contains(DataType.ToLowerInvariant()))
                throw new ArgumentException($"不支持的数据类型: {DataType}", nameof(DataType));

            // 验证正则表达式（如果提供）
            if (!string.IsNullOrWhiteSpace(RegexPattern))
            {
                try
                {
                    _ = new System.Text.RegularExpressions.Regex(RegexPattern);
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"正则表达式格式错误: {ex.Message}", nameof(RegexPattern));
                }
            }

            // 验证枚举选项（如果提供）
            if (!string.IsNullOrWhiteSpace(Options))
            {
                try
                {
                    var options = JsonSerializer.Deserialize<string[]>(Options);
                    if (options == null || options.Length == 0)
                        throw new ArgumentException("枚举选项不能为空数组", nameof(Options));
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"枚举选项JSON格式错误: {ex.Message}", nameof(Options));
                }
            }

            // 验证验证规则（如果提供）
            if (!string.IsNullOrWhiteSpace(ValidationRules))
            {
                try
                {
                    var rules = JsonSerializer.Deserialize<object>(ValidationRules) ?? throw new ArgumentException("验证规则不能为空", nameof(ValidationRules));
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"验证规则JSON格式错误: {ex.Message}", nameof(ValidationRules));
                }
            }
        }

        /// <summary>
        /// 获取字段摘要信息
        /// </summary>
        public string GetSummary()
        {
            var summary = $"{FieldName}({FieldKey}) - {DataType}";
            if (!string.IsNullOrWhiteSpace(Unit))
                summary += $" [{Unit}]";
            if (IsRequired)
                summary += " [必填]";
            if (!IsEnabled)
                summary += " [已禁用]";
            return summary;
        }

        /// <summary>
        /// 检查是否匹配搜索条件
        /// </summary>
        public bool MatchesSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var lowerTerm = searchTerm.ToLowerInvariant();
            return FieldName.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase) ||
                   FieldKey.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase) ||
                   (Description?.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase) == true) ||
                   (Tags?.Any(tag => tag.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase)) == true);
        }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return EntityType;
            yield return FieldKey;
            yield return FieldName;
            yield return DataType;
            yield return IsRequired;
            yield return Order;
            yield return CreationTime;
        }
    }
}
