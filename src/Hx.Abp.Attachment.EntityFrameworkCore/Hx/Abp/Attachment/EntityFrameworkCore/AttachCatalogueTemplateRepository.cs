using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using System.Linq.Dynamic.Core;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachCatalogueTemplateRepository(
        IDbContextProvider<AttachmentDbContext> dbContextProvider,
        ISemanticMatcher semanticMatcher,
        IRulesEngine rulesEngine,
        IGuidGenerator guidGenerator) :
        EfCoreRepository<AttachmentDbContext, AttachCatalogueTemplate, Guid>(dbContextProvider),
        IAttachCatalogueTemplateRepository
    {
        private readonly ISemanticMatcher _semanticMatcher = semanticMatcher;
        private readonly IRulesEngine _rulesEngine = rulesEngine;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

        public async Task<List<AttachCatalogueTemplate>> FindBySemanticMatchAsync(string query, bool onlyLatest = true)
        {
            var dbSet = await GetDbSetAsync();
            var queryable = dbSet.AsQueryable();
            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            var templates = await queryable.ToListAsync();
            return await _semanticMatcher.MatchTemplatesAsync(query, templates);
        }

        public async Task<List<AttachCatalogueTemplate>> FindByRuleMatchAsync(Dictionary<string, object> context, bool onlyLatest = true)
        {
            var dbSet = await GetDbSetAsync();
            var queryable = dbSet.AsQueryable();
            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            var templates = await queryable.ToListAsync();
            var matchedTemplates = new List<AttachCatalogueTemplate>();

            foreach (var template in templates.Where(t => !string.IsNullOrEmpty(t.RuleExpression)))
            {
                try
                {
                    if (string.IsNullOrEmpty(template.RuleExpression)) continue;
                    var workflow = JsonConvert.DeserializeObject<Workflow>(template.RuleExpression);
                    if (workflow == null) continue;
                    var resultList = await _rulesEngine.ExecuteAllRulesAsync(workflow.WorkflowName, context);

                    if (resultList.Any(r => r.IsSuccess))
                    {
                        matchedTemplates.Add(template);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error executing rule for template {TemplateId}", template.Id);
                }
            }
            return matchedTemplates;
        }

        public async Task<List<AttachCatalogueTemplate>> GetChildrenAsync(Guid parentId, bool onlyLatest = true)
        {
            var queryable = (await GetDbSetAsync())
                .Where(t => t.ParentId == parentId);

            if (onlyLatest)
            {
                queryable = queryable.Where(t => t.IsLatest);
            }

            return await queryable
                .OrderBy(t => t.SequenceNumber)
                .ToListAsync();
        }

        // 新增版本管理方法
        public async Task<AttachCatalogueTemplate?> GetLatestVersionAsync(string templateName)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName && t.IsLatest)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(string templateName)
        {
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId)
        {
            var template = await GetAsync(templateId);
            return await (await GetDbSetAsync())
                .Where(t => t.TemplateName == template.TemplateName)
                .OrderByDescending(t => t.Version)
                .ToListAsync();
        }

        public async Task SetAsLatestVersionAsync(Guid templateId)
        {
            var template = await GetAsync(templateId);
            await SetAllOtherVersionsAsNotLatestAsync(template.TemplateName, templateId);

            template.SetVersion(template.Version, true);
            await UpdateAsync(template, autoSave: true);
        }

        public async Task SetAllOtherVersionsAsNotLatestAsync(string templateName, Guid excludeId)
        {
            var templates = await (await GetDbSetAsync())
                .Where(t => t.TemplateName == templateName && t.Id != excludeId)
                .ToListAsync();

            foreach (var template in templates)
            {
                template.SetVersion(template.Version, false);
                await UpdateAsync(template);
            }
        }
    }
}
