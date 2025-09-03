using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 模板搜索输入 DTO
    /// </summary>
    public class TemplateSearchInputDto
    {
        /// <summary>
        /// 搜索关键词（用于字面检索）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 语义查询（用于向量检索）
        /// </summary>
        public string? SemanticQuery { get; set; }

        /// <summary>
        /// 分面类型过滤
        /// </summary>
        public FacetType? FacetType { get; set; }

        /// <summary>
        /// 模板用途过滤
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }

        /// <summary>
        /// 标签过滤（精确匹配）
        /// </summary>
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 是否只搜索最新版本
        /// </summary>
        public bool OnlyLatest { get; set; } = true;

        /// <summary>
        /// 最大返回结果数
        /// </summary>
        [Range(1, 100, ErrorMessage = "最大结果数必须在1-100之间")]
        public int MaxResults { get; set; } = 20;

        /// <summary>
        /// 向量相似度阈值
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "相似度阈值必须在0.0-1.0之间")]
        public double SimilarityThreshold { get; set; } = 0.7;

        /// <summary>
        /// 混合检索权重配置
        /// </summary>
        public HybridSearchWeights? Weights { get; set; }
    }

    /// <summary>
    /// 混合检索权重配置
    /// </summary>
    public class HybridSearchWeights
    {
        /// <summary>
        /// 字面检索权重（0.0-1.0）
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "字面检索权重必须在0.0-1.0之间")]
        public double TextWeight { get; set; } = 0.4;

        /// <summary>
        /// 语义检索权重（0.0-1.0）
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "语义检索权重必须在0.0-1.0之间")]
        public double SemanticWeight { get; set; } = 0.6;

        /// <summary>
        /// 标签匹配权重（0.0-1.0）
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "标签匹配权重必须在0.0-1.0之间")]
        public double TagWeight { get; set; } = 0.3;

        /// <summary>
        /// 名称匹配权重（0.0-1.0）
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "名称匹配权重必须在0.0-1.0之间")]
        public double NameWeight { get; set; } = 0.5;
    }

    /// <summary>
    /// 模板搜索结果 DTO
    /// </summary>
    public class TemplateSearchResultDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 模板标签
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType FacetType { get; set; }

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; }

        /// <summary>
        /// 综合评分
        /// </summary>
        public double TotalScore { get; set; }

        /// <summary>
        /// 字面检索评分
        /// </summary>
        public double TextScore { get; set; }

        /// <summary>
        /// 语义检索评分
        /// </summary>
        public double SemanticScore { get; set; }

        /// <summary>
        /// 标签匹配评分
        /// </summary>
        public double TagScore { get; set; }

        /// <summary>
        /// 匹配原因
        /// </summary>
        public List<string> MatchReasons { get; set; } = [];

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }
    }

    /// <summary>
    /// 高级搜索选项
    /// </summary>
    public class AdvancedSearchOptions
    {
        /// <summary>
        /// 是否启用前缀搜索
        /// </summary>
        public bool EnablePrefixSearch { get; set; } = true;

        /// <summary>
        /// 是否启用模糊搜索
        /// </summary>
        public bool EnableFuzzySearch { get; set; } = true;

        /// <summary>
        /// 模糊搜索编辑距离
        /// </summary>
        [Range(1, 3, ErrorMessage = "编辑距离必须在1-3之间")]
        public int FuzzyDistance { get; set; } = 2;

        /// <summary>
        /// 是否启用布尔搜索
        /// </summary>
        public bool EnableBooleanSearch { get; set; } = false;

        /// <summary>
        /// 是否启用权重搜索
        /// </summary>
        public bool EnableWeightedSearch { get; set; } = true;

        /// <summary>
        /// 是否启用同义词搜索
        /// </summary>
        public bool EnableSynonymSearch { get; set; } = false;
    }
}
