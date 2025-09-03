using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.Application
{
    public class AttachCatalogueTemplateAppService(
        IAttachCatalogueTemplateRepository repository,
        IAttachCatalogueManager catalogueManager,
        IGuidGenerator guidGenerator) :
        CrudAppService<
            AttachCatalogueTemplate,
            AttachCatalogueTemplateDto,
            Guid,
            GetAttachCatalogueTemplateListDto,
            CreateUpdateAttachCatalogueTemplateDto>(repository),
        IAttachCatalogueTemplateAppService
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = repository;
        private readonly IAttachCatalogueManager _catalogueManager = catalogueManager;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;

        public async Task<ListResultDto<AttachCatalogueTemplateDto>> FindMatchingTemplatesAsync(TemplateMatchInput input)
        {
            List<AttachCatalogueTemplate> templates;

            if (!string.IsNullOrWhiteSpace(input.SemanticQuery))
            {
                templates = await _templateRepository.FindBySemanticMatchAsync(input.SemanticQuery, input.OnlyLatest);
            }
            else if (input.ContextData != null && input.ContextData.Count > 0)
            {
                templates = await _templateRepository.FindByRuleMatchAsync(input.ContextData, input.OnlyLatest);
            }
            else
            {
                templates = input.OnlyLatest
                    ? await _templateRepository.GetListAsync(t => t.IsLatest)
                    : await _templateRepository.GetListAsync();
            }

            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates));
        }

        public async Task<AttachCatalogueStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false)
        {
            var rootTemplate = await _templateRepository.GetAsync(id);
            var structure = new AttachCatalogueStructureDto
            {
                Root = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(rootTemplate),
                Children = [],
                History = includeHistory
                    ? ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(
                        await _templateRepository.GetTemplateHistoryAsync(id))
                    : []
            };

            await BuildTemplateTree(structure, rootTemplate);
            return structure;
        }

        private async Task BuildTemplateTree(AttachCatalogueStructureDto parent, AttachCatalogueTemplate template)
        {
            var children = await _templateRepository.GetChildrenAsync(template.Id);
            foreach (var child in children)
            {
                var childDto = new AttachCatalogueStructureDto
                {
                    Root = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(child),
                    Children = []
                };
                parent.Children?.Add(childDto);
                await BuildTemplateTree(childDto, child);
            }
        }

        public async Task GenerateCatalogueFromTemplateAsync(GenerateCatalogueInput input)
        {
            var template = await _templateRepository.GetAsync(input.TemplateId);
            if (!template.IsLatest)
            {
                throw new UserFriendlyException("只能使用最新版本的模板生成分类");
            }

            await _catalogueManager.GenerateFromTemplateAsync(
                template,
                input.Reference,
                input.ReferenceType,
                input.ContextData);
        }

        // ============= 版本管理方法 =============
        public async Task<AttachCatalogueTemplateDto> CreateNewVersionAsync(
            Guid baseTemplateId,
            CreateUpdateAttachCatalogueTemplateDto input)
        {
            var baseTemplate = await _templateRepository.GetAsync(baseTemplateId);

            // 获取下一个版本号
            var allVersions = await _templateRepository.GetTemplateHistoryAsync(baseTemplateId);
            var nextVersion = allVersions.Max(t => t.Version) + 1;

            // 创建新版本实体
            var newTemplate = new AttachCatalogueTemplate(
                id: _guidGenerator.Create(),
                templateName: baseTemplate.TemplateName,
                attachReceiveType: input.AttachReceiveType,
                sequenceNumber: input.SequenceNumber,
                isRequired: input.IsRequired,
                isStatic: input.IsStatic,
                parentId: baseTemplate.ParentId,
                ruleExpression: input.RuleExpression,
                version: nextVersion,
                isLatest: false,
                facetType: input.FacetType,
                templatePurpose: input.TemplatePurpose,
                textVector: input.TextVector
            );

            // 复制子结构
            await CopyChildrenStructureAsync(baseTemplate, newTemplate);

            // 保存新版本
            await _templateRepository.InsertAsync(newTemplate);

            // 设为最新版本
            await _templateRepository.SetAsLatestVersionAsync(newTemplate.Id);

            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(newTemplate);
        }

        private async Task CopyChildrenStructureAsync(AttachCatalogueTemplate source, AttachCatalogueTemplate target)
        {
            var children = await _templateRepository.GetChildrenAsync(source.Id, false);
            foreach (var child in children)
            {
                var newChild = new AttachCatalogueTemplate(
                    id: _guidGenerator.Create(),
                    templateName: child.TemplateName,
                    attachReceiveType: child.AttachReceiveType,
                    sequenceNumber: child.SequenceNumber,
                    isRequired: child.IsRequired,
                    isStatic: child.IsStatic,
                    parentId: target.Id,
                    ruleExpression: child.RuleExpression,
                    version: child.Version,
                    isLatest: false,
                    facetType: child.FacetType,
                    templatePurpose: child.TemplatePurpose,
                    textVector: child.TextVector
                );

                await _templateRepository.InsertAsync(newChild);
                await CopyChildrenStructureAsync(newChild, child);
            }
        }

        public async Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid templateId)
        {
            await _templateRepository.SetAsLatestVersionAsync(templateId);
            var template = await _templateRepository.GetAsync(templateId);
            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
        }

        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplateHistoryAsync(Guid templateId)
        {
            var history = await _templateRepository.GetTemplateHistoryAsync(templateId);
            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(history));
        }

        public async Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid templateId)
        {
            var versionToRollback = await _templateRepository.GetAsync(templateId);
            var latestVersion = await _templateRepository.GetLatestVersionAsync(versionToRollback.TemplateName) ?? throw new UserFriendlyException("未找到最新版本，无法回滚");
            if (latestVersion.Id == templateId)
            {
                throw new UserFriendlyException("此版本已是最新版本");
            }

            // 创建回滚版本（作为新版本）
            var rollbackTemplate = new AttachCatalogueTemplate(
                id: _guidGenerator.Create(),
                templateName: versionToRollback.TemplateName,
                attachReceiveType: versionToRollback.AttachReceiveType,
                sequenceNumber: versionToRollback.SequenceNumber,
                isRequired: versionToRollback.IsRequired,
                isStatic: versionToRollback.IsStatic,
                parentId: versionToRollback.ParentId,
                ruleExpression: versionToRollback.RuleExpression,
                version: latestVersion.Version + 1,
                isLatest: false,
                facetType: versionToRollback.FacetType,
                templatePurpose: versionToRollback.TemplatePurpose,
                textVector: versionToRollback.TextVector
            );

            // 复制子结构
            await CopyChildrenStructureAsync(versionToRollback, rollbackTemplate);

            // 保存回滚版本
            await _templateRepository.InsertAsync(rollbackTemplate);

            // 设为最新版本
            await _templateRepository.SetAsLatestVersionAsync(rollbackTemplate.Id);

            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(rollbackTemplate);
        }

        // ============= 新增模板标识查询方法 =============
        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByIdentifierAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool onlyLatest = true)
        {
            var templates = await _templateRepository.GetTemplatesByIdentifierAsync(
                facetType.HasValue ? (int)facetType.Value : null,
                templatePurpose.HasValue ? (int)templatePurpose.Value : null,
                onlyLatest);

            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates));
        }

        // ============= 新增向量相关方法 =============
        public async Task<ListResultDto<AttachCatalogueTemplateDto>> FindSimilarTemplatesAsync(
            string semanticQuery, 
            double similarityThreshold = 0.7,
            int maxResults = 10)
        {
            var templates = await _templateRepository.FindSimilarTemplatesAsync(
                semanticQuery, similarityThreshold, maxResults);

            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates));
        }

        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByVectorDimensionAsync(
            int minDimension, 
            int maxDimension, 
            bool onlyLatest = true)
        {
            var templates = await _templateRepository.GetTemplatesByVectorDimensionAsync(
                minDimension, maxDimension, onlyLatest);

            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates));
        }

        // ============= 新增统计方法 =============
        public async Task<AttachCatalogueTemplateStatisticsDto> GetTemplateStatisticsAsync()
        {
            var statistics = await _templateRepository.GetTemplateStatisticsAsync();
            return ObjectMapper.Map<object, AttachCatalogueTemplateStatisticsDto>(statistics);
        }
    }
}
