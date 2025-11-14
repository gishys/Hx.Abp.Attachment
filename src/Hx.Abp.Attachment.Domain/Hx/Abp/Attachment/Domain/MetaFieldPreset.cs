using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 预设元数据内容实体
    /// 用于存储和管理预设的元数据字段集合，支持快速创建分类模板
    /// </summary>
    public class MetaFieldPreset : Entity<Guid>
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        [NotNull]
        public virtual string PresetName { get; private set; }

        /// <summary>
        /// 预设描述
        /// </summary>
        [CanBeNull]
        public virtual string? Description { get; private set; }

        /// <summary>
        /// 预设标签（用于分类和搜索）
        /// </summary>
        [CanBeNull]
        public virtual List<string>? Tags { get; private set; }

        /// <summary>
        /// 元数据字段集合（JSONB格式，存储元数据字段信息）
        /// </summary>
        public virtual ICollection<MetaField> MetaFields { get; private set; } = [];

        /// <summary>
        /// 适用业务场景（如：Project、Document、Archive等）
        /// </summary>
        [CanBeNull]
        public virtual List<string>? BusinessScenarios { get; private set; }

        /// <summary>
        /// 适用的分面类型
        /// </summary>
        [CanBeNull]
        public virtual List<FacetType>? ApplicableFacetTypes { get; private set; }

        /// <summary>
        /// 适用的模板用途
        /// </summary>
        [CanBeNull]
        public virtual List<TemplatePurpose>? ApplicableTemplatePurposes { get; private set; }

        /// <summary>
        /// 使用次数（用于推荐算法）
        /// </summary>
        public virtual int UsageCount { get; private set; } = 0;

        /// <summary>
        /// 推荐权重（0.0-1.0，用于智能推荐）
        /// </summary>
        public virtual double RecommendationWeight { get; private set; } = 0.5;

        /// <summary>
        /// 是否启用
        /// </summary>
        public virtual bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 是否系统预设（系统预设不可删除）
        /// </summary>
        public virtual bool IsSystemPreset { get; private set; } = false;

        /// <summary>
        /// 排序号（用于展示顺序）
        /// </summary>
        public virtual int SortOrder { get; private set; } = 0;

        /// <summary>
        /// 最后使用时间（用于推荐算法）
        /// </summary>
        [CanBeNull]
        public virtual DateTime? LastUsedTime { get; private set; }


#pragma warning disable CS8618
        protected MetaFieldPreset() { }
#pragma warning restore CS8618

        public MetaFieldPreset(
            Guid id,
            [NotNull] string presetName,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null,
            [CanBeNull] List<string>? businessScenarios = null,
            [CanBeNull] List<FacetType>? applicableFacetTypes = null,
            [CanBeNull] List<TemplatePurpose>? applicableTemplatePurposes = null,
            bool isSystemPreset = false,
            int sortOrder = 0)
        {
            Id = id;
            PresetName = Check.NotNullOrWhiteSpace(presetName, nameof(presetName));
            Description = description;
            Tags = tags ?? [];
            MetaFields = metaFields ?? [];
            BusinessScenarios = businessScenarios ?? [];
            ApplicableFacetTypes = applicableFacetTypes ?? [];
            ApplicableTemplatePurposes = applicableTemplatePurposes ?? [];
            IsSystemPreset = isSystemPreset;
            SortOrder = sortOrder;
            UsageCount = 0;
            RecommendationWeight = 0.5;
            IsEnabled = true;
        }

        /// <summary>
        /// 更新预设信息
        /// </summary>
        public virtual void Update(
            [NotNull] string presetName,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null,
            [CanBeNull] List<string>? businessScenarios = null,
            [CanBeNull] List<FacetType>? applicableFacetTypes = null,
            [CanBeNull] List<TemplatePurpose>? applicableTemplatePurposes = null,
            int? sortOrder = null)
        {
            PresetName = Check.NotNullOrWhiteSpace(presetName, nameof(presetName));

            if (description != null)
                Description = description;

            if (tags != null)
                Tags = tags;

            if (metaFields != null)
            {
                ValidateMetaFields(metaFields);
                MetaFields = metaFields;
            }

            if (businessScenarios != null)
                BusinessScenarios = businessScenarios;

            if (applicableFacetTypes != null)
                ApplicableFacetTypes = applicableFacetTypes;

            if (applicableTemplatePurposes != null)
                ApplicableTemplatePurposes = applicableTemplatePurposes;

            if (sortOrder.HasValue)
                SortOrder = sortOrder.Value;
        }

        /// <summary>
        /// 添加元数据字段
        /// </summary>
        public virtual void AddMetaField(MetaField metaField)
        {
            ArgumentNullException.ThrowIfNull(metaField);

            MetaFields ??= [];

            if (MetaFields.Any(f => f.FieldKey == metaField.FieldKey))
            {
                throw new ArgumentException($"字段键名 '{metaField.FieldKey}' 已存在", nameof(metaField));
            }

            MetaFields.Add(metaField);
        }

        /// <summary>
        /// 移除元数据字段
        /// </summary>
        public virtual void RemoveMetaField(string fieldKey)
        {
            if (!string.IsNullOrWhiteSpace(fieldKey))
            {
                var field = MetaFields?.FirstOrDefault(f => f.FieldKey == fieldKey);
                if (field != null)
                {
                    MetaFields?.Remove(field);
                }
            }
        }

        /// <summary>
        /// 更新元数据字段
        /// </summary>
        public virtual void UpdateMetaField(string fieldKey, MetaField metaField)
        {
            ArgumentNullException.ThrowIfNull(metaField);

            if (string.IsNullOrWhiteSpace(fieldKey))
                throw new ArgumentException("字段键名不能为空", nameof(fieldKey));

            var existingField = MetaFields?.FirstOrDefault(f => f.FieldKey == fieldKey)
                ?? throw new ArgumentException($"字段键名 '{fieldKey}' 不存在", nameof(fieldKey));

            if (fieldKey != metaField.FieldKey &&
                MetaFields?.Any(f => f.FieldKey == metaField.FieldKey) == true)
            {
                throw new ArgumentException($"字段键名 '{metaField.FieldKey}' 已存在", nameof(metaField));
            }

            MetaFields?.Remove(existingField);
            MetaFields?.Add(metaField);
        }

        /// <summary>
        /// 设置元数据字段集合
        /// </summary>
        public virtual void SetMetaFields([CanBeNull] List<MetaField>? metaFields)
        {
            if (metaFields != null)
            {
                ValidateMetaFields(metaFields);
            }
            MetaFields = metaFields ?? [];
        }

        /// <summary>
        /// 验证元数据字段配置
        /// </summary>
        private static void ValidateMetaFields(List<MetaField> metaFields)
        {
            foreach (var field in metaFields)
            {
                try
                {
                    field.Validate();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"元数据字段 '{field.FieldKey}' 验证失败: {ex.Message}", nameof(metaFields));
                }
            }

            var duplicateKeys = metaFields
                .GroupBy(f => f.FieldKey)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateKeys.Count > 0)
            {
                throw new ArgumentException($"存在重复的字段键名: {string.Join(", ", duplicateKeys)}", nameof(metaFields));
            }
        }

        /// <summary>
        /// 增加使用次数
        /// </summary>
        public virtual void IncrementUsageCount()
        {
            UsageCount++;
            LastUsedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 设置推荐权重
        /// </summary>
        public virtual void SetRecommendationWeight(double weight)
        {
            if (weight < 0.0 || weight > 1.0)
                throw new ArgumentException("推荐权重必须在0.0到1.0之间", nameof(weight));

            RecommendationWeight = weight;
        }

        /// <summary>
        /// 启用预设
        /// </summary>
        public virtual void Enable()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// 禁用预设
        /// </summary>
        public virtual void Disable()
        {
            if (IsSystemPreset)
                throw new InvalidOperationException("系统预设不能禁用");

            IsEnabled = false;
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public virtual void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                Tags ??= [];
                if (!Tags.Contains(tag))
                {
                    Tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public virtual void RemoveTag(string tag)
        {
            Tags?.Remove(tag);
        }

        /// <summary>
        /// 添加业务场景
        /// </summary>
        public virtual void AddBusinessScenario(string scenario)
        {
            if (!string.IsNullOrWhiteSpace(scenario))
            {
                BusinessScenarios ??= [];
                if (!BusinessScenarios.Contains(scenario))
                {
                    BusinessScenarios.Add(scenario);
                }
            }
        }

        /// <summary>
        /// 移除业务场景
        /// </summary>
        public virtual void RemoveBusinessScenario(string scenario)
        {
            BusinessScenarios?.Remove(scenario);
        }

        /// <summary>
        /// 检查是否适用于指定的分面类型
        /// </summary>
        public virtual bool IsApplicableToFacetType(FacetType facetType)
        {
            return ApplicableFacetTypes == null ||
                   ApplicableFacetTypes.Count == 0 ||
                   ApplicableFacetTypes.Contains(facetType);
        }

        /// <summary>
        /// 检查是否适用于指定的模板用途
        /// </summary>
        public virtual bool IsApplicableToTemplatePurpose(TemplatePurpose templatePurpose)
        {
            return ApplicableTemplatePurposes == null ||
                   ApplicableTemplatePurposes.Count == 0 ||
                   ApplicableTemplatePurposes.Contains(templatePurpose);
        }

        /// <summary>
        /// 检查是否匹配搜索条件
        /// </summary>
        public virtual bool MatchesSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return true;

            var lowerTerm = searchTerm.ToLowerInvariant();
            return PresetName.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase) ||
                   (Description?.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase) == true) ||
                   (Tags?.Any(tag => tag.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase)) == true) ||
                   (BusinessScenarios?.Any(scenario => scenario.Contains(lowerTerm, StringComparison.InvariantCultureIgnoreCase)) == true) ||
                   (MetaFields?.Any(field => field.MatchesSearch(searchTerm)) == true);
        }

        /// <summary>
        /// 验证预设配置
        /// </summary>
        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(PresetName))
                throw new ArgumentException("预设名称不能为空", nameof(PresetName));

            if (MetaFields != null && MetaFields.Count > 0)
            {
                ValidateMetaFields([.. MetaFields]);
            }
        }
    }
}
