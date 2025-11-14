using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 预设元数据内容DTO
    /// </summary>
    public class MetaFieldPresetDto
    {
        /// <summary>
        /// 预设ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 预设名称
        /// </summary>
        public string PresetName { get; set; } = string.Empty;

        /// <summary>
        /// 预设描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 预设标签
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<MetaFieldDto> MetaFields { get; set; } = [];

        /// <summary>
        /// 适用业务场景
        /// </summary>
        public List<string>? BusinessScenarios { get; set; }

        /// <summary>
        /// 适用的分面类型
        /// </summary>
        public List<FacetType>? ApplicableFacetTypes { get; set; }

        /// <summary>
        /// 适用的模板用途
        /// </summary>
        public List<TemplatePurpose>? ApplicableTemplatePurposes { get; set; }

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 推荐权重
        /// </summary>
        public double RecommendationWeight { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 是否系统预设
        /// </summary>
        public bool IsSystemPreset { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedTime { get; set; }
    }

    /// <summary>
    /// 创建/更新预设元数据内容DTO
    /// </summary>
    public class CreateUpdateMetaFieldPresetDto
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        public string PresetName { get; set; } = string.Empty;

        /// <summary>
        /// 预设描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 预设标签
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<CreateUpdateMetaFieldDto>? MetaFields { get; set; }

        /// <summary>
        /// 适用业务场景
        /// </summary>
        public List<string>? BusinessScenarios { get; set; }

        /// <summary>
        /// 适用的分面类型
        /// </summary>
        public List<FacetType>? ApplicableFacetTypes { get; set; }

        /// <summary>
        /// 适用的模板用途
        /// </summary>
        public List<TemplatePurpose>? ApplicableTemplatePurposes { get; set; }

        /// <summary>
        /// 排序号
        /// </summary>
        public int SortOrder { get; set; }
    }

    /// <summary>
    /// 预设推荐请求DTO
    /// </summary>
    public class PresetRecommendationRequestDto
    {
        /// <summary>
        /// 业务场景
        /// </summary>
        public string? BusinessScenario { get; set; }

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType? FacetType { get; set; }

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 返回数量
        /// </summary>
        public int TopN { get; set; } = 10;

        /// <summary>
        /// 最小推荐权重
        /// </summary>
        public double MinWeight { get; set; } = 0.3;

        /// <summary>
        /// 是否只返回启用的预设
        /// </summary>
        public bool OnlyEnabled { get; set; } = true;
    }

    /// <summary>
    /// 预设推荐结果DTO
    /// </summary>
    public class PresetRecommendationDto
    {
        /// <summary>
        /// 预设信息
        /// </summary>
        public MetaFieldPresetDto Preset { get; set; } = null!;

        /// <summary>
        /// 推荐分数（0.0-1.0）
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 推荐原因
        /// </summary>
        public List<string> Reasons { get; set; } = [];
    }

    /// <summary>
    /// 预设搜索请求DTO
    /// </summary>
    public class PresetSearchRequestDto
    {
        /// <summary>
        /// 关键词
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 业务场景
        /// </summary>
        public string? BusinessScenario { get; set; }

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType? FacetType { get; set; }

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }

        /// <summary>
        /// 是否只返回启用的预设
        /// </summary>
        public bool OnlyEnabled { get; set; } = true;

        /// <summary>
        /// 最大返回数量
        /// </summary>
        public int MaxResults { get; set; } = 50;

        /// <summary>
        /// 跳过数量（用于分页）
        /// </summary>
        public int SkipCount { get; set; } = 0;
    }

    /// <summary>
    /// 预设统计信息DTO
    /// </summary>
    public class PresetStatisticsDto
    {
        /// <summary>
        /// 总预设数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 启用预设数
        /// </summary>
        public int EnabledCount { get; set; }

        /// <summary>
        /// 系统预设数
        /// </summary>
        public int SystemPresetCount { get; set; }

        /// <summary>
        /// 总使用次数
        /// </summary>
        public long TotalUsageCount { get; set; }

        /// <summary>
        /// 最热门预设（Top 10）
        /// </summary>
        public List<MetaFieldPresetDto> TopPresets { get; set; } = [];

        /// <summary>
        /// 业务场景统计
        /// </summary>
        public Dictionary<string, int> BusinessScenarioStats { get; set; } = [];

        /// <summary>
        /// 标签统计
        /// </summary>
        public Dictionary<string, int> TagStats { get; set; } = [];
    }

    /// <summary>
    /// 批量应用预设到模板的请求DTO
    /// </summary>
    public class ApplyPresetsToTemplateRequestDto
    {
        /// <summary>
        /// 预设ID列表
        /// </summary>
        public List<Guid> PresetIds { get; set; } = [];

        /// <summary>
        /// 合并策略：Skip（跳过重复字段）或 Overwrite（覆盖重复字段）
        /// </summary>
        public MergeStrategy MergeStrategy { get; set; } = MergeStrategy.Skip;

        /// <summary>
        /// 是否保留模板中已存在但不在预设中的字段
        /// </summary>
        public bool KeepExistingFields { get; set; } = true;
    }

    /// <summary>
    /// 合并策略枚举
    /// </summary>
    public enum MergeStrategy
    {
        /// <summary>
        /// 跳过重复字段（保留模板中的原有字段）
        /// </summary>
        Skip = 0,

        /// <summary>
        /// 覆盖重复字段（用预设中的字段替换模板中的字段）
        /// </summary>
        Overwrite = 1
    }

    /// <summary>
    /// 批量应用预设到模板的响应DTO
    /// </summary>
    public class ApplyPresetsToTemplateResponseDto
    {
        /// <summary>
        /// 成功应用的元数据字段列表
        /// </summary>
        public List<MetaFieldDto> AppliedFields { get; set; } = [];

        /// <summary>
        /// 跳过的字段信息（字段键名和原因）
        /// </summary>
        public List<SkippedFieldInfo> SkippedFields { get; set; } = [];

        /// <summary>
        /// 应用的预设数量
        /// </summary>
        public int AppliedPresetCount { get; set; }

        /// <summary>
        /// 总字段数量
        /// </summary>
        public int TotalFieldCount { get; set; }

        /// <summary>
        /// 成功应用的字段数量
        /// </summary>
        public int AppliedFieldCount { get; set; }

        /// <summary>
        /// 跳过的字段数量
        /// </summary>
        public int SkippedFieldCount { get; set; }
    }

    /// <summary>
    /// 跳过的字段信息
    /// </summary>
    public class SkippedFieldInfo
    {
        /// <summary>
        /// 字段键名
        /// </summary>
        public string FieldKey { get; set; } = string.Empty;

        /// <summary>
        /// 字段显示名称
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 跳过的原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 来源预设ID
        /// </summary>
        public Guid? PresetId { get; set; }

        /// <summary>
        /// 来源预设名称
        /// </summary>
        public string? PresetName { get; set; }
    }
}

