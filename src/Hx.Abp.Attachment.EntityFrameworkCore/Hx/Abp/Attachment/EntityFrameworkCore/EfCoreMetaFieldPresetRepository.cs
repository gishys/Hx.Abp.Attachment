using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// 预设元数据内容仓储实现
    /// </summary>
    public class EfCoreMetaFieldPresetRepository(
        IDbContextProvider<AttachmentDbContext> dbContextProvider)
        : EfCoreRepository<AttachmentDbContext, MetaFieldPreset, Guid>(dbContextProvider),
        IMetaFieldPresetRepository
    {
        public async Task<MetaFieldPreset?> FindByNameAsync(string presetName, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(p => p.PresetName == presetName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> FindByBusinessScenarioAsync(string businessScenario, bool onlyEnabled = true, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(p => p.BusinessScenarios != null &&
                           p.BusinessScenarios.Contains(businessScenario));

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> FindByFacetTypeAsync(FacetType facetType, bool onlyEnabled = true, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(p => (p.ApplicableFacetTypes == null || p.ApplicableFacetTypes.Count == 0 ||
                           p.ApplicableFacetTypes.Contains(facetType)));

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> FindByTemplatePurposeAsync(TemplatePurpose templatePurpose, bool onlyEnabled = true, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(p => (p.ApplicableTemplatePurposes == null || p.ApplicableTemplatePurposes.Count == 0 ||
                           p.ApplicableTemplatePurposes.Contains(templatePurpose)));

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> SearchAsync(
            string? keyword = null,
            List<string>? tags = null,
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool onlyEnabled = true,
            int maxResults = 50,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsQueryable();

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.PresetName.Contains(keyword) ||
                                       (p.Description != null && p.Description.Contains(keyword)));
            }

            if (tags != null && tags.Count > 0)
            {
                query = query.Where(p => p.Tags != null && p.Tags.Any(t => tags.Contains(t)));
            }

            if (!string.IsNullOrWhiteSpace(businessScenario))
            {
                query = query.Where(p => p.BusinessScenarios != null &&
                                       p.BusinessScenarios.Contains(businessScenario));
            }

            if (facetType.HasValue)
            {
                query = query.Where(p => p.ApplicableFacetTypes == null ||
                                       p.ApplicableFacetTypes.Count == 0 ||
                                       p.ApplicableFacetTypes.Contains(facetType.Value));
            }

            if (templatePurpose.HasValue)
            {
                query = query.Where(p => p.ApplicableTemplatePurposes == null ||
                                       p.ApplicableTemplatePurposes.Count == 0 ||
                                       p.ApplicableTemplatePurposes.Contains(templatePurpose.Value));
            }

            return await query
                .OrderByDescending(p => p.RecommendationWeight)
                .ThenByDescending(p => p.UsageCount)
                .Take(maxResults)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> GetPopularPresetsAsync(
            int topN = 10,
            bool onlyEnabled = true,
            DateTime? since = null,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsQueryable();

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            if (since.HasValue)
            {
                query = query.Where(p => p.LastUsedTime.HasValue && p.LastUsedTime >= since.Value);
            }

            return await query
                .OrderByDescending(p => p.UsageCount)
                .ThenByDescending(p => p.LastUsedTime)
                .Take(topN)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> GetRecommendedPresetsAsync(
            string? businessScenario = null,
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            List<string>? tags = null,
            int topN = 10,
            double minWeight = 0.3,
            bool onlyEnabled = true,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(p => p.RecommendationWeight >= minWeight);

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            if (!string.IsNullOrWhiteSpace(businessScenario))
            {
                query = query.Where(p => p.BusinessScenarios != null &&
                                       p.BusinessScenarios.Contains(businessScenario));
            }

            if (facetType.HasValue)
            {
                query = query.Where(p => p.ApplicableFacetTypes == null ||
                                       p.ApplicableFacetTypes.Count == 0 ||
                                       p.ApplicableFacetTypes.Contains(facetType.Value));
            }

            if (templatePurpose.HasValue)
            {
                query = query.Where(p => p.ApplicableTemplatePurposes == null ||
                                       p.ApplicableTemplatePurposes.Count == 0 ||
                                       p.ApplicableTemplatePurposes.Contains(templatePurpose.Value));
            }

            if (tags != null && tags.Count > 0)
            {
                query = query.Where(p => p.Tags != null && p.Tags.Any(t => tags.Contains(t)));
            }

            return await query
                .OrderByDescending(p => p.RecommendationWeight)
                .ThenByDescending(p => p.UsageCount)
                .Take(topN)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> FindByTagsAsync(
            List<string> tags,
            bool matchAll = false,
            bool onlyEnabled = true,
            CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet.AsQueryable();

            if (onlyEnabled)
            {
                query = query.Where(p => p.IsEnabled);
            }

            if (matchAll)
            {
                query = query.Where(p => p.Tags != null && tags.All(t => p.Tags.Contains(t)));
            }
            else
            {
                query = query.Where(p => p.Tags != null && p.Tags.Any(t => tags.Contains(t)));
            }

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<List<MetaFieldPreset>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            return await dbSet
                .Where(p => p.IsEnabled)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.PresetName)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string presetName, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var query = dbSet
                .Where(p => p.PresetName == presetName);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync(cancellationToken);
        }


        public async Task BatchUpdateRecommendationWeightsAsync(Dictionary<Guid, double> weights, CancellationToken cancellationToken = default)
        {
            var dbSet = await GetDbSetAsync();
            var presets = await dbSet
                .Where(p => weights.Keys.Contains(p.Id))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (var preset in presets)
            {
                if (weights.TryGetValue(preset.Id, out var weight))
                {
                    preset.SetRecommendationWeight(weight);
                }
            }

            await SaveChangesAsync(cancellationToken);
        }
    }
}

