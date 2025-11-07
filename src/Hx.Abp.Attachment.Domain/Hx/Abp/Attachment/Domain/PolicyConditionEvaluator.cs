using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 策略条件评估器
    /// 用于评估策略权限的条件是否满足
    /// </summary>
    public class PolicyConditionEvaluator
    {
        /// <summary>
        /// 策略条件上下文
        /// </summary>
        public class PolicyContext
        {
            /// <summary>
            /// 用户ID
            /// </summary>
            public Guid? UserId { get; set; }

            /// <summary>
            /// 用户角色列表
            /// </summary>
            public string[]? UserRoles { get; set; }

            /// <summary>
            /// 业务引用
            /// </summary>
            public string? Reference { get; set; }

            /// <summary>
            /// 业务类型
            /// </summary>
            public int? ReferenceType { get; set; }

            /// <summary>
            /// 分类分面类型
            /// </summary>
            public int? CatalogueFacetType { get; set; }

            /// <summary>
            /// 分类用途
            /// </summary>
            public int? CataloguePurpose { get; set; }

            /// <summary>
            /// 分类路径
            /// </summary>
            public string? Path { get; set; }

            /// <summary>
            /// 自定义属性（用于扩展）
            /// </summary>
            public Dictionary<string, object>? CustomAttributes { get; set; }
        }

        /// <summary>
        /// 策略条件定义
        /// </summary>
        public class PolicyCondition
        {
            /// <summary>
            /// 条件类型（equals, contains, in, range, regex等）
            /// </summary>
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            /// <summary>
            /// 属性名称
            /// </summary>
            [JsonPropertyName("property")]
            public string? Property { get; set; }

            /// <summary>
            /// 条件值
            /// </summary>
            [JsonPropertyName("value")]
            public JsonElement? Value { get; set; }

            /// <summary>
            /// 操作符（and, or）
            /// </summary>
            [JsonPropertyName("operator")]
            public string? Operator { get; set; }

            /// <summary>
            /// 子条件列表
            /// </summary>
            [JsonPropertyName("conditions")]
            public List<PolicyCondition>? Conditions { get; set; }
        }

        /// <summary>
        /// 评估策略条件
        /// </summary>
        /// <param name="attributeConditions">属性条件JSON字符串</param>
        /// <param name="context">策略上下文</param>
        /// <returns>是否满足条件</returns>
        public static bool Evaluate(string? attributeConditions, PolicyContext context)
        {
            if (string.IsNullOrWhiteSpace(attributeConditions))
            {
                return true; // 没有条件，默认通过
            }

            try
            {
                var condition = JsonSerializer.Deserialize<PolicyCondition>(attributeConditions);
                if (condition == null)
                {
                    return false;
                }

                return EvaluateCondition(condition, context);
            }
            catch (JsonException)
            {
                // JSON解析失败，返回false
                return false;
            }
        }

        /// <summary>
        /// 评估单个条件
        /// </summary>
        private static bool EvaluateCondition(PolicyCondition condition, PolicyContext context)
        {
            // 如果有子条件，递归评估
            if (condition.Conditions != null && condition.Conditions.Count > 0)
            {
                var results = condition.Conditions.Select(c => EvaluateCondition(c, context)).ToList();
                
                // 根据操作符合并结果
                return condition.Operator?.ToLowerInvariant() switch
                {
                    "or" => results.Any(r => r),
                    "and" => results.All(r => r),
                    _ => results.All(r => r) // 默认使用AND
                };
            }

            // 评估单个条件
            if (string.IsNullOrWhiteSpace(condition.Property) || condition.Value == null)
            {
                return false;
            }

            var propertyValue = GetPropertyValue(condition.Property, context);
            if (propertyValue == null)
            {
                return false;
            }

            return condition.Type?.ToLowerInvariant() switch
            {
                "equals" => EvaluateEquals(propertyValue, condition.Value.Value),
                "contains" => EvaluateContains(propertyValue, condition.Value.Value),
                "in" => EvaluateIn(propertyValue, condition.Value.Value),
                "range" => EvaluateRange(propertyValue, condition.Value.Value),
                "regex" => EvaluateRegex(propertyValue, condition.Value.Value),
                "not_equals" => !EvaluateEquals(propertyValue, condition.Value.Value),
                "not_contains" => !EvaluateContains(propertyValue, condition.Value.Value),
                "not_in" => !EvaluateIn(propertyValue, condition.Value.Value),
                _ => false
            };
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        private static object? GetPropertyValue(string property, PolicyContext context)
        {
            return property.ToLowerInvariant() switch
            {
                "userid" or "user_id" => context.UserId?.ToString(),
                "reference" => context.Reference,
                "referencetype" or "reference_type" => context.ReferenceType,
                "cataloguefacettype" or "catalogue_facet_type" => context.CatalogueFacetType,
                "cataloguepurpose" or "catalogue_purpose" => context.CataloguePurpose,
                "path" => context.Path,
                _ => context.CustomAttributes?.TryGetValue(property, out var value) == true ? value : null
            };
        }

        /// <summary>
        /// 评估等于条件
        /// </summary>
        private static bool EvaluateEquals(object propertyValue, JsonElement conditionValue)
        {
            var propertyStr = propertyValue.ToString();
            var conditionStr = conditionValue.GetString();
            return propertyStr == conditionStr;
        }

        /// <summary>
        /// 评估包含条件
        /// </summary>
        private static bool EvaluateContains(object propertyValue, JsonElement conditionValue)
        {
            var propertyStr = propertyValue.ToString();
            var conditionStr = conditionValue.GetString();
            return propertyStr != null && conditionStr != null && propertyStr.Contains(conditionStr, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 评估在列表中条件
        /// </summary>
        private static bool EvaluateIn(object propertyValue, JsonElement conditionValue)
        {
            if (conditionValue.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var propertyStr = propertyValue.ToString();
            return conditionValue.EnumerateArray().Any(item => item.GetString() == propertyStr);
        }

        /// <summary>
        /// 评估范围条件
        /// </summary>
        private static bool EvaluateRange(object propertyValue, JsonElement conditionValue)
        {
            if (conditionValue.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var min = conditionValue.TryGetProperty("min", out var minElement) ? minElement.GetInt32() : (int?)null;
            var max = conditionValue.TryGetProperty("max", out var maxElement) ? maxElement.GetInt32() : (int?)null;

            if (propertyValue is int intValue)
            {
                if (min.HasValue && intValue < min.Value) return false;
                if (max.HasValue && intValue > max.Value) return false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 评估正则表达式条件
        /// </summary>
        private static bool EvaluateRegex(object propertyValue, JsonElement conditionValue)
        {
            var propertyStr = propertyValue.ToString();
            var pattern = conditionValue.GetString();
            
            if (string.IsNullOrWhiteSpace(propertyStr) || string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(propertyStr, pattern);
            }
            catch
            {
                return false;
            }
        }
    }
}
