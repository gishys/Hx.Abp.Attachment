using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 元数据预设推荐服务
    /// 支持基于规则、使用频率、相似度、大数据分析等推荐策略
    /// </summary>
    public class MetaFieldPresetRecommendationService(
        IMetaFieldPresetRepository repository,
        ILogger<MetaFieldPresetRecommendationService> logger) : ITransientDependency
    {
        private readonly IMetaFieldPresetRepository _repository = repository;
        private readonly ILogger<MetaFieldPresetRecommendationService> _logger = logger;

        /// <summary>
        /// 基于规则的推荐
        /// </summary>
        public async Task<List<MetaFieldPreset>> RecommendByRulesAsync(
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int topN = 10)
        {
            try
            {
                var presets = await _repository.GetRecommendedPresetsAsync(
                    businessScenario,
                    facetType,
                    templatePurpose,
                    tags,
                    topN,
                    0.3,
                    true);

                // 应用规则过滤
                var filteredPresets = presets.Where(preset =>
                {
                    // 规则1：必须匹配业务场景（如果提供）
                    if (!string.IsNullOrWhiteSpace(businessScenario) &&
                        (preset.BusinessScenarios == null || !preset.BusinessScenarios.Contains(businessScenario)))
                    {
                        return false;
                    }

                    // 规则2：必须匹配分面类型（如果提供）
                    if (facetType.HasValue && !preset.IsApplicableToFacetType(facetType.Value))
                    {
                        return false;
                    }

                    // 规则3：必须匹配模板用途（如果提供）
                    if (templatePurpose.HasValue && !preset.IsApplicableToTemplatePurpose(templatePurpose.Value))
                    {
                        return false;
                    }

                    // 规则4：至少匹配一个标签（如果提供）
                    if (tags != null && tags.Count > 0 &&
                        (preset.Tags == null || !preset.Tags.Any(t => tags.Contains(t))))
                    {
                        return false;
                    }

                    return true;
                }).ToList();

                return [.. filteredPresets.Take(topN)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于规则的推荐失败");
                return [];
            }
        }

        /// <summary>
        /// 基于使用频率的推荐
        /// </summary>
        public async Task<List<MetaFieldPreset>> RecommendByUsageFrequencyAsync(
            int topN = 10,
            DateTime? since = null)
        {
            try
            {
                var presets = await _repository.GetPopularPresetsAsync(topN, true, since);
                return [.. presets.OrderByDescending(p => p.UsageCount).Take(topN)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于使用频率的推荐失败");
                return [];
            }
        }

        /// <summary>
        /// 基于相似度的推荐（根据元数据字段相似度）
        /// </summary>
        public async Task<List<MetaFieldPreset>> RecommendBySimilarityAsync(
            List<MetaField> referenceFields,
            double similarityThreshold = 0.7,
            int topN = 10)
        {
            try
            {
                var allPresets = await _repository.GetAllEnabledAsync();
                var scoredPresets = new List<(MetaFieldPreset preset, double score)>();

                foreach (var preset in allPresets)
                {
                    var similarity = CalculateFieldSimilarity(referenceFields, [.. preset.MetaFields]);
                    if (similarity >= similarityThreshold)
                    {
                        scoredPresets.Add((preset, similarity));
                    }
                }

                return [.. scoredPresets
                    .OrderByDescending(x => x.score)
                    .Take(topN)
                    .Select(x => x.preset)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于相似度的推荐失败");
                return [];
            }
        }

        /// <summary>
        /// 基于大数据分析的推荐（综合分析多个因素）
        /// </summary>
        public async Task<List<MetaFieldPreset>> RecommendByDataAnalysisAsync(
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            List<MetaField>? referenceFields = null,
            int topN = 10)
        {
            try
            {
                var allPresets = await _repository.GetAllEnabledAsync();
                var scoredPresets = new List<(MetaFieldPreset preset, double score)>();

                foreach (var preset in allPresets)
                {
                    double score = 0.0;

                    // 因素1：推荐权重（30%）
                    score += preset.RecommendationWeight * 0.3;

                    // 因素2：使用频率（25%）
                    var usageScore = Math.Min(preset.UsageCount / 100.0, 1.0);
                    score += usageScore * 0.25;

                    // 因素3：业务场景匹配（15%）
                    if (!string.IsNullOrWhiteSpace(businessScenario) &&
                        preset.BusinessScenarios?.Contains(businessScenario) == true)
                    {
                        score += 0.15;
                    }

                    // 因素4：分面类型匹配（10%）
                    if (facetType.HasValue && preset.IsApplicableToFacetType(facetType.Value))
                    {
                        score += 0.1;
                    }

                    // 因素5：模板用途匹配（10%）
                    if (templatePurpose.HasValue && preset.IsApplicableToTemplatePurpose(templatePurpose.Value))
                    {
                        score += 0.1;
                    }

                    // 因素6：标签匹配（5%）
                    if (tags != null && tags.Count > 0 &&
                        preset.Tags != null && preset.Tags.Any(t => tags.Contains(t)))
                    {
                        score += 0.05;
                    }

                    // 因素7：字段相似度（5%）
                    if (referenceFields != null && referenceFields.Count > 0)
                    {
                        var similarity = CalculateFieldSimilarity(referenceFields, [.. preset.MetaFields]);
                        score += similarity * 0.05;
                    }

                    scoredPresets.Add((preset, score));
                }

                return [.. scoredPresets
                    .OrderByDescending(x => x.score)
                    .Take(topN)
                    .Select(x => x.preset)];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于大数据分析的推荐失败");
                return [];
            }
        }

        /// <summary>
        /// 动态推荐（根据上下文自动选择最佳推荐策略）
        /// </summary>
        public async Task<List<MetaFieldPreset>> RecommendDynamicallyAsync(
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            List<MetaField>? referenceFields = null,
            int topN = 10)
        {
            try
            {
                // 策略选择逻辑
                RecommendationStrategy strategy;

                // 如果有参考字段，优先使用相似度推荐
                if (referenceFields != null && referenceFields.Count > 0)
                {
                    strategy = RecommendationStrategy.Similarity;
                }
                // 如果有明确的业务场景和分面类型，使用规则推荐
                else if (!string.IsNullOrWhiteSpace(businessScenario) && facetType.HasValue)
                {
                    strategy = RecommendationStrategy.Rules;
                }
                // 否则使用综合分析
                else
                {
                    strategy = RecommendationStrategy.DataAnalysis;
                }

                _logger.LogInformation("选择推荐策略：{strategy}", strategy);

                return strategy switch
                {
                    RecommendationStrategy.Rules => await RecommendByRulesAsync(
                        businessScenario, facetType, templatePurpose, tags, topN),
                    RecommendationStrategy.UsageFrequency => await RecommendByUsageFrequencyAsync(topN),
                    RecommendationStrategy.Similarity => await RecommendBySimilarityAsync(
                        referenceFields ?? [], 0.7, topN),
                    RecommendationStrategy.DataAnalysis => await RecommendByDataAnalysisAsync(
                        businessScenario, facetType, templatePurpose, tags, referenceFields, topN),
                    _ => await RecommendByDataAnalysisAsync(
                        businessScenario, facetType, templatePurpose, tags, referenceFields, topN)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "动态推荐失败");
                return [];
            }
        }

        #region 私有方法

        /// <summary>
        /// 计算字段相似度
        /// </summary>
        private static double CalculateFieldSimilarity(List<MetaField> fields1, List<MetaField> fields2)
        {
            if (fields1.Count == 0 && fields2.Count == 0)
                return 1.0;

            if (fields1.Count == 0 || fields2.Count == 0)
                return 0.0;

            var matchedFields = 0;
            var totalFields = Math.Max(fields1.Count, fields2.Count);

            foreach (var field1 in fields1)
            {
                var matched = fields2.Any(field2 =>
                    field2.FieldKey == field1.FieldKey ||
                    (field2.FieldName == field1.FieldName && field2.DataType == field1.DataType));

                if (matched)
                {
                    matchedFields++;
                }
            }

            return (double)matchedFields / totalFields;
        }

        #endregion

        #region 枚举

        private enum RecommendationStrategy
        {
            Rules,
            UsageFrequency,
            Similarity,
            DataAnalysis
        }

        #endregion
    }
}

