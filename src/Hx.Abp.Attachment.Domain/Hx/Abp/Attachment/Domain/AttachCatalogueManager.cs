using Newtonsoft.Json;
using RulesEngine.Interfaces;
using RulesEngine.Models;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.Domain
{
    public class AttachCatalogueManager(
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        IAttachCatalogueTemplateRepository templateRepository,
        IRulesEngine rulesEngine,
        ISemanticMatcher semanticMatcher,
        IGuidGenerator guidGenerator) : DomainService, IAttachCatalogueManager
    {
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository = catalogueRepository;
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IRulesEngine _rulesEngine = rulesEngine;
        private readonly ISemanticMatcher _semanticMatcher = semanticMatcher;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

        public async Task<AttachCatalogue> GenerateFromTemplateAsync(
            AttachCatalogueTemplate template,
            string reference,
            int referenceType,
            Dictionary<string, object>? contextData = null)
        {
            // 检查是否为最新版本
            if (!template.IsLatest)
            {
                var latest = await _templateRepository.GetLatestVersionAsync(template.TemplateName);
                if (latest != null && latest.Id != template.Id)
                {
                    throw new BusinessException("Template:NotLatestVersion")
                        .WithData("TemplateName", template.TemplateName)
                        .WithData("LatestVersion", latest.Version);
                }
            }

            var rootCatalogue = await CreateCatalogueFromTemplate(template, null, reference, referenceType, contextData);
            await CreateChildCatalogues(template, rootCatalogue, reference, referenceType, contextData);
            return rootCatalogue;
        }

        public async Task<AttachCatalogueTemplate> CreateTemplateVersionAsync(
            AttachCatalogueTemplate baseTemplate,
            Guid? newParentId = null)
        {
            var allVersions = await _templateRepository.GetTemplateHistoryAsync(baseTemplate.Id);
            var nextVersion = allVersions.Count > 0 ? allVersions.Max(t => t.Version) + 1 : 1;

            var newTemplate = new AttachCatalogueTemplate(
                id: _guidGenerator.Create(),
                templateName: baseTemplate.TemplateName,
                attachReceiveType: baseTemplate.AttachReceiveType,
                sequenceNumber: baseTemplate.SequenceNumber,
                isRequired: baseTemplate.IsRequired,
                isStatic: baseTemplate.IsStatic,
                parentId: newParentId ?? baseTemplate.ParentId,
                namePattern: baseTemplate.NamePattern,
                ruleExpression: baseTemplate.RuleExpression,
                semanticModel: baseTemplate.SemanticModel,
                version: nextVersion,
                isLatest: false
            );

            await _templateRepository.InsertAsync(newTemplate);
            await CopyTemplateChildrenAsync(baseTemplate, newTemplate);

            return newTemplate;
        }

        /// <summary>
        /// 更新分类的语义向量
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="embedding">语义向量</param>
        /// <returns>更新后的分类</returns>
        public async Task<AttachCatalogue> UpdateCatalogueEmbeddingAsync(Guid catalogueId, float[] embedding)
        {
            var catalogue = await _catalogueRepository.FindAsync(catalogueId) ?? throw new BusinessException("分类不存在");
            catalogue.SetEmbedding(embedding);
            await _catalogueRepository.UpdateAsync(catalogue);
            return catalogue;
        }

        private async Task CopyTemplateChildrenAsync(AttachCatalogueTemplate source, AttachCatalogueTemplate target)
        {
            var children = await _templateRepository.GetChildrenAsync(source.Id, false);
            foreach (var child in children)
            {
                await CreateTemplateVersionAsync(child, target.Id);
            }
        }

        private async Task<AttachCatalogue> CreateCatalogueFromTemplate(
            AttachCatalogueTemplate template,
            Guid? parentId,
            string reference,
            int referenceType,
            Dictionary<string, object>? contextData)
        {
            var catalogue = new AttachCatalogue(
                id: GuidGenerator.Create(),
                attachReceiveType: template.AttachReceiveType,
                catologueName: await ResolveCatalogueName(template, contextData),
                sequenceNumber: template.SequenceNumber,
                reference: reference,
                referenceType: referenceType,
                parentId: parentId,
                isRequired: template.IsRequired,
                isStatic: template.IsStatic
            );

            await _catalogueRepository.InsertAsync(catalogue);
            return catalogue;
        }

        private async Task<string> ResolveCatalogueName(
            AttachCatalogueTemplate template,
            Dictionary<string, object>? contextData)
        {
            // 优先使用AI语义匹配
            if (!string.IsNullOrEmpty(template.SemanticModel) && contextData != null)
            {
                return await _semanticMatcher.GenerateNameAsync(template.SemanticModel, contextData);
            }

            // 其次使用规则引擎
            if (!string.IsNullOrEmpty(template.RuleExpression) && contextData != null)
            {
                var workflow = JsonConvert.DeserializeObject<Workflow>(template.RuleExpression);
                if (workflow != null)
                {
                    var ruleParameters = contextData.Select(kvp => new RuleParameter(kvp.Key, kvp.Value)).ToArray();
                    var result = await _rulesEngine.ExecuteActionWorkflowAsync(
                        workflow.WorkflowName,
                        "GenerateName",
                        ruleParameters);

                    if (result.Output != null)
                    {
#pragma warning disable CS8603 // 可能返回 null 引用。
                        return result.Output.ToString();
#pragma warning restore CS8603 // 可能返回 null 引用。
                    }
                }
            }

            // 最后使用静态名称
            return template.TemplateName;
        }

        private async Task CreateChildCatalogues(
            AttachCatalogueTemplate parentTemplate,
            AttachCatalogue parentCatalogue,
            string reference,
            int referenceType,
            Dictionary<string, object>? contextData)
        {
            var children = await _templateRepository.GetChildrenAsync(parentTemplate.Id);

            foreach (var childTemplate in children)
            {
                var childCatalogue = await CreateCatalogueFromTemplate(
                    childTemplate,
                    parentCatalogue.Id,
                    reference,
                    referenceType,
                    contextData);

                await CreateChildCatalogues(childTemplate, childCatalogue, reference, referenceType, contextData);
            }
        }
    }
}
