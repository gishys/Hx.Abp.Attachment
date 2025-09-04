using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.Application
{
    public class AttachCatalogueTemplateAppService(
        IAttachCatalogueTemplateRepository repository,
        IAttachCatalogueManager catalogueManager,
        IGuidGenerator guidGenerator,
        ILogger<AttachCatalogueTemplateAppService> logger) :
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
        private readonly ILogger<AttachCatalogueTemplateAppService> _logger = logger;

        /// <summary>
        /// 创建模板（支持自动路径维护）
        /// </summary>
        public override async Task<AttachCatalogueTemplateDto> CreateAsync(CreateUpdateAttachCatalogueTemplateDto input)
        {
            try
            {
                // 自动计算模板路径
                string? templatePath = null;
                if (input.ParentId.HasValue)
                {
                    // 有父级：获取父级路径，然后查找同级最大路径
                    var parentTemplate = await _templateRepository.GetAsync(input.ParentId.Value);
                    var maxPathAtSameLevel = await _templateRepository.GetMaxTemplatePathAtSameLevelAsync(parentTemplate.TemplatePath);

                    if (string.IsNullOrEmpty(maxPathAtSameLevel))
                    {
                        // 没有同级，创建第一个子路径
                        templatePath = AttachCatalogueTemplate.AppendTemplatePathCode(parentTemplate.TemplatePath, "00001");
                    }
                    else
                    {
                        // 有同级，获取最大路径的最后一个单元代码并+1
                        var lastUnitCode = AttachCatalogueTemplate.GetLastUnitTemplatePathCode(maxPathAtSameLevel);
                        var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                        var nextUnitCode = nextNumber.ToString("D5");
                        templatePath = AttachCatalogueTemplate.AppendTemplatePathCode(parentTemplate.TemplatePath, nextUnitCode);
                    }
                }
                else
                {
                    // 没有父级：查找根级别最大路径
                    var maxPathAtRootLevel = await _templateRepository.GetMaxTemplatePathAtSameLevelAsync(null);
                    if (string.IsNullOrEmpty(maxPathAtRootLevel))
                    {
                        // 没有根级别模板，创建第一个
                        templatePath = AttachCatalogueTemplate.CreateTemplatePathCode(1);
                    }
                    else
                    {
                        // 有根级别模板，获取最大路径并+1
                        var nextNumber = Convert.ToInt32(maxPathAtRootLevel) + 1;
                        templatePath = AttachCatalogueTemplate.CreateTemplatePathCode(nextNumber);
                    }
                    // 验证路径格式
                    if (!AttachCatalogueTemplate.IsValidTemplatePath(templatePath))
                    {
                        throw new UserFriendlyException("模板路径格式不正确");
                    }
                }
                // 创建实体
                var template = new AttachCatalogueTemplate(
                    id: input.Id != Guid.Empty ? input.Id : _guidGenerator.Create(),
                    templateName: input.TemplateName,
                    attachReceiveType: input.AttachReceiveType,
                    sequenceNumber: input.SequenceNumber,
                    isRequired: input.IsRequired,
                    isStatic: input.IsStatic,
                    parentId: input.ParentId,
                    workflowConfig: input.WorkflowConfig,
                    version: 1,
                    isLatest: true,
                    facetType: input.FacetType,
                    templatePurpose: input.TemplatePurpose,
                    textVector: input.TextVector,
                    description: input.Description,
                    tags: input.Tags,
                    metaFields: input.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    templatePath: templatePath
                );

                // 验证配置
                template.ValidateConfiguration();

                // 保存实体
                await _templateRepository.InsertAsync(template);

                _logger.LogInformation("创建模板成功：{templateName}，路径：{templatePath}", input.TemplateName, templatePath);

                return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建模板失败：{templateName}", input.TemplateName);
                throw new UserFriendlyException("创建模板失败，请稍后重试");
            }
        }

        /// <summary>
        /// 更新模板（支持路径维护）
        /// </summary>
        public override async Task<AttachCatalogueTemplateDto> UpdateAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            try
            {
                var template = await _templateRepository.GetAsync(id);

                // 如果父模板发生变化，需要重新计算路径
                string? newTemplatePath = input.TemplatePath;
                if (template.ParentId != input.ParentId)
                {
                    if (input.ParentId.HasValue)
                    {
                        // 有父级：获取父级路径，然后查找同级最大路径
                        var parentTemplate = await _templateRepository.GetAsync(input.ParentId.Value);
                        var maxPathAtSameLevel = await _templateRepository.GetMaxTemplatePathAtSameLevelAsync(parentTemplate.TemplatePath);
                        
                        if (string.IsNullOrEmpty(maxPathAtSameLevel))
                        {
                            // 没有同级，创建第一个子路径
                            newTemplatePath = AttachCatalogueTemplate.AppendTemplatePathCode(parentTemplate.TemplatePath, "00001");
                        }
                        else
                        {
                            // 有同级，获取最大路径的最后一个单元代码并+1
                            var lastUnitCode = AttachCatalogueTemplate.GetLastUnitTemplatePathCode(maxPathAtSameLevel);
                            var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                            var nextUnitCode = nextNumber.ToString("D5");
                            newTemplatePath = AttachCatalogueTemplate.AppendTemplatePathCode(parentTemplate.TemplatePath, nextUnitCode);
                        }
                    }
                    else
                    {
                        // 成为根节点：查找根级别最大路径
                        var maxPathAtRootLevel = await _templateRepository.GetMaxTemplatePathAtSameLevelAsync(null);
                        
                        if (string.IsNullOrEmpty(maxPathAtRootLevel))
                        {
                            // 没有根级别模板，创建第一个
                            newTemplatePath = AttachCatalogueTemplate.CreateTemplatePathCode(1);
                        }
                        else
                        {
                            // 有根级别模板，获取最大路径并+1
                            var nextNumber = Convert.ToInt32(maxPathAtRootLevel) + 1;
                            newTemplatePath = AttachCatalogueTemplate.CreateTemplatePathCode(nextNumber);
                        }
                    }
                }

                // 验证路径格式
                if (!AttachCatalogueTemplate.IsValidTemplatePath(newTemplatePath))
                {
                    throw new UserFriendlyException("模板路径格式不正确");
                }

                // 更新实体
                template.Update(
                    input.TemplateName,
                    input.AttachReceiveType,
                    input.SequenceNumber,
                    input.IsRequired,
                    input.IsStatic,
                    input.WorkflowConfig,
                    input.FacetType,
                    input.TemplatePurpose,
                    input.Description,
                    input.Tags,
                    input.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    newTemplatePath
                );

                // 如果父模板发生变化，更新父模板ID
                if (template.ParentId != input.ParentId)
                {
                    template.ChangeParent(input.ParentId, newTemplatePath);
                }

                // 验证配置
                template.ValidateConfiguration();

                // 保存实体
                await _templateRepository.UpdateAsync(template);

                _logger.LogInformation("更新模板成功：{templateName}，路径：{templatePath}", input.TemplateName, newTemplatePath);

                return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板失败：{templateName}", input.TemplateName);
                throw new UserFriendlyException("更新模板失败，请稍后重试");
            }
        }

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

            // 使用基于路径的优化方法构建树形结构
            await BuildTemplateTreeOptimized(structure, rootTemplate);
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

        /// <summary>
        /// 基于路径优化的树形结构构建方法
        /// 使用TemplatePath进行高效查询，避免递归调用
        /// </summary>
        private async Task BuildTemplateTreeOptimized(AttachCatalogueStructureDto parent, AttachCatalogueTemplate template)
        {
            try
            {
                // 使用基于路径的子树查询，一次性获取所有子节点
                var allChildren = await _templateRepository.GetTemplateSubtreeAsync(template.Id, true, 10);

                // 过滤掉根节点本身，只保留子节点
                var children = allChildren.Where(t => t.Id != template.Id).ToList();

                // 构建树形结构
                var childStructures = new List<AttachCatalogueStructureDto>();
                var childDict = children.ToDictionary(c => c.Id, c => c);

                // 为每个子节点创建结构对象
                foreach (var child in children)
                {
                    var childStructure = new AttachCatalogueStructureDto
                    {
                        Root = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(child),
                        Children = []
                    };
                    childStructures.Add(childStructure);
                }

                // 构建父子关系
                foreach (var childStructure in childStructures)
                {
                    if (childStructure.Root != null && childDict.TryGetValue(childStructure.Root.Id, out var childTemplate))
                    {
                        if (childTemplate.ParentId == template.Id)
                        {
                            parent.Children?.Add(childStructure);
                        }
                        else if (childTemplate.ParentId.HasValue && childDict.TryGetValue(childTemplate.ParentId.Value, out var parentChild))
                        {
                            // 找到父级子节点
                            var parentChildStructure = childStructures.FirstOrDefault(cs => cs.Root != null && cs.Root.Id == parentChild.Id);
                            parentChildStructure?.Children?.Add(childStructure);
                        }
                    }
                }

                _logger.LogInformation("构建模板树形结构完成，根模板：{templateName}，子节点数量：{childCount}",
                    template.TemplateName, children.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建模板树形结构失败，根模板：{templateName}", template.TemplateName);
                // 如果优化方法失败，回退到原来的递归方法
                await BuildTemplateTree(parent, template);
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
                workflowConfig: input.WorkflowConfig,
                version: nextVersion,
                isLatest: false,
                facetType: input.FacetType,
                templatePurpose: input.TemplatePurpose,
                textVector: input.TextVector,
                description: input.Description,
                tags: input.Tags,
                metaFields: input.MetaFields?.Select(mf => new MetaField(
                    mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                    mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                    mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                )).ToList(),
                templatePath: input.TemplatePath
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
                    workflowConfig: child.WorkflowConfig,
                    version: child.Version,
                    isLatest: false,
                    facetType: child.FacetType,
                    templatePurpose: child.TemplatePurpose,
                    textVector: child.TextVector,
                    description: child.Description,
                    tags: child.Tags,
                    metaFields: child.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    templatePath: child.TemplatePath
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
                workflowConfig: versionToRollback.WorkflowConfig,
                version: latestVersion.Version + 1,
                isLatest: false,
                facetType: versionToRollback.FacetType,
                templatePurpose: versionToRollback.TemplatePurpose,
                textVector: versionToRollback.TextVector,
                description: versionToRollback.Description,
                tags: versionToRollback.Tags,
                metaFields: versionToRollback.MetaFields?.Select(mf => new MetaField(
                    mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                    mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                    mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                )).ToList(),
                templatePath: versionToRollback.TemplatePath
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

        // ============= 混合检索方法 =============

        public async Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesHybridAsync(TemplateSearchInputDto input)
        {
            try
            {
                var templates = await _templateRepository.SearchTemplatesHybridAsync(
                    input.Keyword,
                    input.SemanticQuery,
                    input.FacetType,
                    input.TemplatePurpose,
                    input.Tags,
                    input.MaxResults,
                    input.SimilarityThreshold,
                    input.Weights?.TextWeight ?? 0.4,
                    input.Weights?.SemanticWeight ?? 0.6);

                var results = templates.Select(t => new TemplateSearchResultDto
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    Description = t.Description,
                    Tags = t.Tags ?? [],
                    FacetType = t.FacetType,
                    TemplatePurpose = t.TemplatePurpose,
                    IsLatest = t.IsLatest,
                    Version = t.Version,
                    // 这里可以根据实际检索逻辑计算评分
                    TotalScore = 1.0,
                    TextScore = 0.8,
                    SemanticScore = 0.9,
                    TagScore = 0.7,
                    MatchReasons = ["关键词匹配", "语义相似"]
                }).ToList();

                return new ListResultDto<TemplateSearchResultDto>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "混合检索模板失败");
                throw new UserFriendlyException("检索失败，请稍后重试");
            }
        }

        public async Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTextAsync(
            string keyword, 
            FacetType? facetType = null, 
            TemplatePurpose? templatePurpose = null, 
            List<string>? tags = null, 
            int maxResults = 20)
        {
            try
            {
                var templates = await _templateRepository.SearchTemplatesByTextAsync(
                    keyword, facetType, templatePurpose, tags, maxResults);

                var results = templates.Select(t => new TemplateSearchResultDto
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    Description = t.Description,
                    Tags = t.Tags ?? [],
                    FacetType = t.FacetType,
                    TemplatePurpose = t.TemplatePurpose,
                    IsLatest = t.IsLatest,
                    Version = t.Version,
                    TotalScore = 1.0,
                    TextScore = 1.0,
                    SemanticScore = 0.0,
                    TagScore = 0.0,
                    MatchReasons = ["文本匹配"]
                }).ToList();

                return new ListResultDto<TemplateSearchResultDto>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文本检索模板失败");
                throw new UserFriendlyException("检索失败，请稍后重试");
            }
        }

        public async Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTagsAsync(
            List<string> tags, 
            FacetType? facetType = null, 
            TemplatePurpose? templatePurpose = null, 
            int maxResults = 20)
        {
            try
            {
                var templates = await _templateRepository.SearchTemplatesByTagsAsync(
                    tags, facetType, templatePurpose, maxResults);

                var results = templates.Select(t => new TemplateSearchResultDto
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    Description = t.Description,
                    Tags = t.Tags ?? [],
                    FacetType = t.FacetType,
                    TemplatePurpose = t.TemplatePurpose,
                    IsLatest = t.IsLatest,
                    Version = t.Version,
                    TotalScore = 1.0,
                    TextScore = 0.0,
                    SemanticScore = 0.0,
                    TagScore = 1.0,
                    MatchReasons = ["标签匹配"]
                }).ToList();

                return new ListResultDto<TemplateSearchResultDto>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标签检索模板失败");
                throw new UserFriendlyException("检索失败，请稍后重试");
            }
        }

        public async Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesBySemanticAsync(
            string semanticQuery, 
            FacetType? facetType = null, 
            TemplatePurpose? templatePurpose = null, 
            double similarityThreshold = 0.7, 
            int maxResults = 20)
        {
            try
            {
                var templates = await _templateRepository.SearchTemplatesBySemanticAsync(
                    semanticQuery, facetType, templatePurpose, similarityThreshold, maxResults);

                var results = templates.Select(t => new TemplateSearchResultDto
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    Description = t.Description,
                    Tags = t.Tags ?? [],
                    FacetType = t.FacetType,
                    TemplatePurpose = t.TemplatePurpose,
                    IsLatest = t.IsLatest,
                    Version = t.Version,
                    TotalScore = 1.0,
                    TextScore = 0.0,
                    SemanticScore = 1.0,
                    TagScore = 0.0,
                    MatchReasons = ["语义相似"]
                }).ToList();

                return new ListResultDto<TemplateSearchResultDto>(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "语义检索模板失败");
                throw new UserFriendlyException("检索失败，请稍后重试");
            }
        }

        public async Task<ListResultDto<string>> GetPopularTagsAsync(int topN = 20)
        {
            try
            {
                var tags = await _templateRepository.GetPopularTagsAsync(topN);
                return new ListResultDto<string>(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门标签失败");
                throw new UserFriendlyException("获取标签失败，请稍后重试");
            }
        }

        public async Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            try
            {
                return await _templateRepository.GetTagStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取标签统计失败");
                throw new UserFriendlyException("获取统计失败，请稍后重试");
            }
        }

        // ============= 树状结构查询方法 =============

        /// <summary>
        /// 获取根节点模板（用于树状展示）
        /// 基于TemplatePath优化，提高性能
        /// </summary>
        public async Task<ListResultDto<AttachCatalogueTemplateTreeDto>> GetRootTemplatesAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool includeChildren = true,
            bool onlyLatest = true)
        {
            try
            {
                // 使用优化的基于路径的查询方法
                var rootTemplates = await _templateRepository.GetRootTemplatesAsync(
                    facetType, templatePurpose, includeChildren, onlyLatest);

                var treeDtos = rootTemplates.Select(t => 
                    ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateTreeDto>(t)).ToList();

                _logger.LogInformation("获取根节点模板完成，数量：{count}，包含子节点：{includeChildren}，使用路径优化：{optimized}", 
                    treeDtos.Count, includeChildren, includeChildren);

                return new ListResultDto<AttachCatalogueTemplateTreeDto>(treeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取根节点模板失败");
                throw new UserFriendlyException("获取模板树失败，请稍后重试");
            }
        }

        #region 元数据字段管理

        public async Task<ListResultDto<MetaFieldDto>> GetTemplateMetaFieldsAsync(Guid templateId)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                var metaFields = template.GetEnabledMetaFields().ToList();
                var metaFieldDtos = ObjectMapper.Map<List<MetaField>, List<MetaFieldDto>>(metaFields);
                
                _logger.LogInformation("获取模板 {templateId} 的元数据字段完成，数量：{count}", templateId, metaFieldDtos.Count);
                
                return new ListResultDto<MetaFieldDto>(metaFieldDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板 {templateId} 的元数据字段失败", templateId);
                throw new UserFriendlyException("获取元数据字段失败，请稍后重试");
            }
        }

        public async Task<MetaFieldDto> AddMetaFieldToTemplateAsync(Guid templateId, CreateUpdateMetaFieldDto input)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                
                var metaField = new MetaField(
                    input.EntityType,
                    input.FieldKey,
                    input.FieldName,
                    input.DataType,
                    input.IsRequired,
                    input.Unit,
                    input.RegexPattern,
                    input.Options,
                    input.Description,
                    input.DefaultValue,
                    input.Order,
                    input.IsEnabled,
                    input.Group,
                    input.ValidationRules,
                    input.Tags
                );
                
                template.AddMetaField(metaField);
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("向模板 {templateId} 添加元数据字段成功：{fieldKey}", templateId, input.FieldKey);
                
                return ObjectMapper.Map<MetaField, MetaFieldDto>(metaField);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向模板 {templateId} 添加元数据字段失败", templateId);
                throw new UserFriendlyException("添加元数据字段失败，请稍后重试");
            }
        }

        public async Task<MetaFieldDto> UpdateTemplateMetaFieldAsync(Guid templateId, string fieldKey, CreateUpdateMetaFieldDto input)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                
                var updatedMetaField = new MetaField(
                    input.EntityType,
                    input.FieldKey,
                    input.FieldName,
                    input.DataType,
                    input.IsRequired,
                    input.Unit,
                    input.RegexPattern,
                    input.Options,
                    input.Description,
                    input.DefaultValue,
                    input.Order,
                    input.IsEnabled,
                    input.Group,
                    input.ValidationRules,
                    input.Tags
                );
                
                template.UpdateMetaField(fieldKey, updatedMetaField);
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("更新模板 {templateId} 的元数据字段成功：{fieldKey}", templateId, fieldKey);
                
                return ObjectMapper.Map<MetaField, MetaFieldDto>(updatedMetaField);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板 {templateId} 的元数据字段失败：{fieldKey}", templateId, fieldKey);
                throw new UserFriendlyException("更新元数据字段失败，请稍后重试");
            }
        }

        public async Task RemoveMetaFieldFromTemplateAsync(Guid templateId, string fieldKey)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                template.RemoveMetaField(fieldKey);
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("从模板 {templateId} 移除元数据字段成功：{fieldKey}", templateId, fieldKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从模板 {templateId} 移除元数据字段失败：{fieldKey}", templateId, fieldKey);
                throw new UserFriendlyException("移除元数据字段失败，请稍后重试");
            }
        }

        public async Task<MetaFieldDto?> GetTemplateMetaFieldAsync(Guid templateId, string fieldKey)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                var metaField = template.GetMetaField(fieldKey);
                
                if (metaField == null)
                    return null;
                    
                return ObjectMapper.Map<MetaField, MetaFieldDto>(metaField);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板 {templateId} 的元数据字段失败：{fieldKey}", templateId, fieldKey);
                throw new UserFriendlyException("获取元数据字段失败，请稍后重试");
            }
        }

        public async Task<ListResultDto<MetaFieldDto>> QueryTemplateMetaFieldsAsync(Guid templateId, MetaFieldQueryDto input)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                var metaFields = template.GetEnabledMetaFields().ToList();
                
                // 应用查询条件
                var filteredFields = metaFields.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(input.EntityType))
                    filteredFields = filteredFields.Where(f => f.EntityType == input.EntityType);
                    
                if (!string.IsNullOrWhiteSpace(input.DataType))
                    filteredFields = filteredFields.Where(f => f.DataType == input.DataType);
                    
                if (input.IsRequired.HasValue)
                    filteredFields = filteredFields.Where(f => f.IsRequired == input.IsRequired.Value);
                    
                if (input.IsEnabled.HasValue)
                    filteredFields = filteredFields.Where(f => f.IsEnabled == input.IsEnabled.Value);
                    
                if (!string.IsNullOrWhiteSpace(input.Group))
                    filteredFields = filteredFields.Where(f => f.Group == input.Group);
                    
                if (!string.IsNullOrWhiteSpace(input.SearchTerm))
                    filteredFields = filteredFields.Where(f => f.MatchesSearch(input.SearchTerm));
                    
                if (input.Tags != null && input.Tags.Count > 0)
                    filteredFields = filteredFields.Where(f => f.Tags != null && input.Tags.Any(tag => f.Tags.Contains(tag)));
                
                var result = filteredFields.OrderBy(f => f.Order).ToList();
                var metaFieldDtos = ObjectMapper.Map<List<MetaField>, List<MetaFieldDto>>(result);
                
                _logger.LogInformation("查询模板 {templateId} 的元数据字段完成，数量：{count}", templateId, metaFieldDtos.Count);
                
                return new ListResultDto<MetaFieldDto>(metaFieldDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询模板 {templateId} 的元数据字段失败", templateId);
                throw new UserFriendlyException("查询元数据字段失败，请稍后重试");
            }
        }

        public async Task UpdateMetaFieldsOrderAsync(Guid templateId, List<string> fieldKeys)
        {
            try
            {
                var template = await _templateRepository.GetAsync(templateId);
                
                for (int i = 0; i < fieldKeys.Count; i++)
                {
                    var field = template.GetMetaField(fieldKeys[i]);
                    field?.Update(order: i);
                }
                
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("更新模板 {templateId} 的元数据字段顺序成功，字段数量：{count}", templateId, fieldKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板 {templateId} 的元数据字段顺序失败", templateId);
                throw new UserFriendlyException("更新字段顺序失败，请稍后重试");
            }
        }

        #endregion

        #region 模板路径相关方法

        /// <summary>
        /// 根据路径获取模板
        /// </summary>
        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathAsync(string? templatePath, bool includeChildren = false)
        {
            try
            {
                var templates = await _templateRepository.GetTemplatesByPathAsync(templatePath, includeChildren);
                var templateDtos = ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates);
                
                _logger.LogInformation("根据路径获取模板完成，路径：{templatePath}，数量：{count}", templatePath, templateDtos.Count);
                
                return new ListResultDto<AttachCatalogueTemplateDto>(templateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据路径获取模板失败，路径：{templatePath}", templatePath);
                throw new UserFriendlyException("获取模板失败，请稍后重试");
            }
        }

        /// <summary>
        /// 根据路径深度获取模板
        /// </summary>
        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathDepthAsync(int depth, bool onlyLatest = true)
        {
            try
            {
                var templates = await _templateRepository.GetTemplatesByPathDepthAsync(depth, onlyLatest);
                var templateDtos = ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates);
                
                _logger.LogInformation("根据路径深度获取模板完成，深度：{depth}，数量：{count}", depth, templateDtos.Count);
                
                return new ListResultDto<AttachCatalogueTemplateDto>(templateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据路径深度获取模板失败，深度：{depth}", depth);
                throw new UserFriendlyException("获取模板失败，请稍后重试");
            }
        }

        /// <summary>
        /// 计算下一个模板路径
        /// </summary>
        public Task<string> CalculateNextTemplatePathAsync(string? parentPath)
        {
            try
            {
                var nextPath = AttachCatalogueTemplate.CalculateNextTemplatePath(parentPath);
                
                _logger.LogInformation("计算下一个模板路径完成，父路径：{parentPath}，下一个路径：{nextPath}", parentPath, nextPath);
                
                return Task.FromResult(nextPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "计算下一个模板路径失败，父路径：{parentPath}", parentPath);
                throw new UserFriendlyException("计算路径失败，请稍后重试");
            }
        }

        /// <summary>
        /// 验证模板路径格式
        /// </summary>
        public Task<bool> ValidateTemplatePathAsync(string? templatePath)
        {
            try
            {
                var isValid = AttachCatalogueTemplate.IsValidTemplatePath(templatePath);
                
                _logger.LogInformation("验证模板路径完成，路径：{templatePath}，是否有效：{isValid}", templatePath, isValid);
                
                return Task.FromResult(isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证模板路径失败，路径：{templatePath}", templatePath);
                throw new UserFriendlyException("验证路径失败，请稍后重试");
            }
        }

        /// <summary>
        /// 根据路径范围获取模板
        /// </summary>
        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathRangeAsync(string? startPath, string? endPath, bool onlyLatest = true)
        {
            try
            {
                var templates = await _templateRepository.GetTemplatesByPathRangeAsync(startPath, endPath, onlyLatest);
                var templateDtos = ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates);
                
                _logger.LogInformation("根据路径范围获取模板完成，起始路径：{startPath}，结束路径：{endPath}，数量：{count}", startPath, endPath, templateDtos.Count);
                
                return new ListResultDto<AttachCatalogueTemplateDto>(templateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据路径范围获取模板失败，起始路径：{startPath}，结束路径：{endPath}", startPath, endPath);
                throw new UserFriendlyException("获取模板失败，请稍后重试");
            }
        }

        #endregion
    }
}
