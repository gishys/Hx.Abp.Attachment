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
        ApplicationService,
        IAttachCatalogueTemplateAppService
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = repository;
        private readonly IAttachCatalogueManager _catalogueManager = catalogueManager;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly ILogger<AttachCatalogueTemplateAppService> _logger = logger;

        #region 基本 CRUD 方法

        /// <summary>
        /// 获取模板（最新版本，支持树形结构）
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <param name="includeTreeStructure">是否包含树形结构（默认false，保持向后兼容）</param>
        /// <returns>模板信息，如果包含树形结构则返回完整的树</returns>
        public async Task<AttachCatalogueTemplateDto> GetAsync(Guid id, bool includeTreeStructure = false)
        {
            try
            {
                _logger.LogInformation("获取模板：ID={id}, 包含树形结构={includeTreeStructure}", id, includeTreeStructure);
                
                var template = await _templateRepository.GetLatestVersionAsync(id, includeTreeStructure);
                
                if (template == null)
                {
                    _logger.LogWarning("未找到模板：ID={id}", id);
                    throw new UserFriendlyException($"未找到模板 {id}");
                }

                var result = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
                
                _logger.LogInformation("获取模板成功：ID={id}, Version={version}, 包含树形结构={includeTreeStructure}", 
                    id, template.Version, includeTreeStructure);
                
                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板失败：ID={id}, 包含树形结构={includeTreeStructure}", id, includeTreeStructure);
                throw new UserFriendlyException("获取模板失败，请稍后重试");
            }
        }


        /// <summary>
        /// 获取模板列表
        /// </summary>
        public async Task<PagedResultDto<AttachCatalogueTemplateDto>> GetListAsync(GetAttachCatalogueTemplateListDto input)
        {
            var query = await _templateRepository.GetQueryableAsync();
            
            // 应用过滤条件
            if (input.FacetType.HasValue)
            {
                query = query.Where(t => t.FacetType == input.FacetType.Value);
            }
            
            if (input.TemplatePurpose.HasValue)
            {
                query = query.Where(t => t.TemplatePurpose == input.TemplatePurpose.Value);
            }
            
            if (input.IsLatest.HasValue && input.IsLatest.Value)
            {
                query = query.Where(t => t.IsLatest);
            }
            
            if (!string.IsNullOrEmpty(input.Name))
            {
                query = query.Where(t => t.TemplateName.Contains(input.Name) || 
                                       (t.Description != null && t.Description.Contains(input.Name)));
            }

            var totalCount = await AsyncExecuter.CountAsync(query);
            var templates = await AsyncExecuter.ToListAsync(
                query.OrderBy(t => t.TemplateName)
                     .Skip(input.SkipCount)
                     .Take(input.MaxResultCount));

            var templateDtos = ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(templates);
            
            return new PagedResultDto<AttachCatalogueTemplateDto>(totalCount, templateDtos);
        }

        /// <summary>
        /// 创建模板（支持自动路径维护）
        /// </summary>
        public async Task<AttachCatalogueTemplateDto> CreateAsync(CreateUpdateAttachCatalogueTemplateDto input)
        {
            try
            {
                // 验证输入参数
                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    throw new UserFriendlyException("模板名称不能为空");
                }

                // 检查模板名称是否已存在（同级比较）
                var nameExists = await _templateRepository.ExistsByNameAsync(input.Name, input.ParentId);
                if (nameExists)
                {
                    var scope = input.ParentId.HasValue ? "同级" : "根节点";
                    throw new UserFriendlyException($"在{scope}下已存在名称为 '{input.Name}' 的模板，请使用其他名称");
                }

                // 自动计算模板路径
                string? templatePath = null;
                if (input.ParentId.HasValue)
                {
                    // 有父级：获取父级路径，然后查找同级最大路径
                    AttachCatalogueTemplate parentTemplate;
                    if (input.ParentVersion.HasValue)
                    {
                        // 指定了父版本，获取特定版本
                        parentTemplate = await _templateRepository.GetByVersionAsync(input.ParentId.Value, input.ParentVersion.Value) ?? throw new UserFriendlyException($"未找到父模板 {input.ParentId.Value} 版本 {input.ParentVersion.Value}");
                    }
                    else
                    {
                        // 未指定父版本，获取最新版本
                        parentTemplate = await _templateRepository.GetLatestVersionAsync(input.ParentId.Value) ?? throw new UserFriendlyException($"未找到父模板 {input.ParentId.Value}");
                    }
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
                var templateId = input.Id ?? _guidGenerator.Create();
                var template = new AttachCatalogueTemplate(
                    templateId: templateId,
                    version: 1,
                    templateName: input.Name,
                    attachReceiveType: input.AttachReceiveType,
                    sequenceNumber: input.SequenceNumber,
                    isRequired: input.IsRequired,
                    isStatic: input.IsStatic,
                    parentId: input.ParentId,
                    parentVersion: input.ParentVersion,
                    workflowConfig: input.WorkflowConfig,
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

                _logger.LogInformation("创建模板成功：{templateName}，路径：{templatePath}", input.Name, templatePath);

                return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
            }
            catch (UserFriendlyException)
            {
                // 重新抛出用户友好的异常，保持原始错误信息
                throw;
            }
            catch (ArgumentException argEx)
            {
                // 参数异常，返回具体的参数错误信息
                _logger.LogError(argEx, "创建模板参数验证失败：{templateName}", input.Name);
                throw new UserFriendlyException($"参数验证失败：{argEx.Message}");
            }
            catch (Exception ex)
            {
                // 其他异常，记录详细日志但返回通用错误信息
                _logger.LogError(ex, "创建模板失败：{templateName}，错误详情：{errorMessage}", input.Name, ex.Message);
                throw new UserFriendlyException($"创建模板失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新模板（支持路径维护）
        /// </summary>
        public async Task<AttachCatalogueTemplateDto> UpdateAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            try
            {
                // 验证输入参数
                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    throw new UserFriendlyException("模板名称不能为空");
                }

                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");

                // 检查模板名称是否已存在（同级比较，排除当前模板）
                if (template.TemplateName != input.Name)
                {
                    var nameExists = await _templateRepository.ExistsByNameAsync(input.Name, input.ParentId, id);
                    if (nameExists)
                    {
                        var scope = input.ParentId.HasValue ? "同级" : "根节点";
                        throw new UserFriendlyException($"在{scope}下已存在名称为 '{input.Name}' 的模板，请使用其他名称");
                    }
                }

                // 如果父模板发生变化，需要重新计算路径
                string? newTemplatePath = input.TemplatePath;
                if (template.ParentId != input.ParentId)
                {
                    if (input.ParentId.HasValue)
                    {
                        // 有父级：获取父级路径，然后查找同级最大路径
                        AttachCatalogueTemplate parentTemplate;
                        if (input.ParentVersion.HasValue)
                        {
                            // 指定了父版本，获取特定版本
                            parentTemplate = await _templateRepository.GetByVersionAsync(input.ParentId.Value, input.ParentVersion.Value) ?? throw new UserFriendlyException($"未找到父模板 {input.ParentId.Value} 版本 {input.ParentVersion.Value}");
                        }
                        else
                        {
                            // 未指定父版本，获取最新版本
                            parentTemplate = await _templateRepository.GetLatestVersionAsync(input.ParentId.Value) ?? throw new UserFriendlyException($"未找到父模板 {input.ParentId.Value}");
                        }
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
                if(string.IsNullOrEmpty(newTemplatePath))
                {
                    newTemplatePath = template.TemplatePath;
                }

                // 更新实体
                template.Update(
                    input.Name,
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

                // 如果父模板发生变化，更新父模板ID和版本
                if (template.ParentId != input.ParentId || template.ParentVersion != input.ParentVersion)
                {
                    template.ChangeParent(input.ParentId, input.ParentVersion, newTemplatePath);
                }

                // 验证配置
                template.ValidateConfiguration();

                // 保存实体
                await _templateRepository.UpdateAsync(template);

                _logger.LogInformation("更新模板成功：{templateName}，路径：{templatePath}", input.Name, newTemplatePath);

                return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
            }
            catch (UserFriendlyException)
            {
                // 重新抛出用户友好的异常，保持原始错误信息
                throw;
            }
            catch (ArgumentException argEx)
            {
                // 参数异常，返回具体的参数错误信息
                _logger.LogError(argEx, "更新模板参数验证失败：{templateName}", input.Name);
                throw new UserFriendlyException($"参数验证失败：{argEx.Message}");
            }
            catch (Exception ex)
            {
                // 其他异常，记录详细日志但返回通用错误信息
                _logger.LogError(ex, "更新模板失败：{templateName}，错误详情：{errorMessage}", input.Name, ex.Message);
                throw new UserFriendlyException($"更新模板失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 删除模板（软删除）
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");

                // 使用仓储的软删除方法
                await _templateRepository.DeleteAsync(template);

                _logger.LogInformation("删除模板成功：{templateName}", template.TemplateName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除模板失败：{templateId}", id);
                throw new UserFriendlyException("删除模板失败，请稍后重试");
            }
        }

        #endregion

        public async Task<ListResultDto<AttachCatalogueTemplateDto>> FindMatchingTemplatesAsync(TemplateMatchInput input)
        {
            List<AttachCatalogueTemplate> templates;

            if (!string.IsNullOrWhiteSpace(input.SemanticQuery))
            {
                templates = await _templateRepository.GetIntelligentRecommendationsAsync(
                    input.SemanticQuery, 
                    input.Threshold, 
                    input.TopN, 
                    input.OnlyLatest, 
                    input.IncludeHistory);
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

        public async Task<TemplateStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false)
        {
            // 获取当前模板
            var currentTemplate = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");

            // 构建版本列表
            var versions = new List<AttachCatalogueTemplateDto>();
            
            // 添加当前版本
            var currentVersionDto = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(currentTemplate);
            
            // 使用基于路径的优化方法构建树形结构
            await BuildTemplateTreeByPath(currentVersionDto, currentTemplate);
            versions.Add(currentVersionDto);
            
            // 如果需要包含历史版本
            if (includeHistory)
            {
                var historyTemplates = await _templateRepository.GetTemplateHistoryAsync(id);
                foreach (var historyTemplate in historyTemplates.Where(t => t.Id != id))
                {
                    var historyDto = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(historyTemplate);
                    
                    // 为历史版本也构建树形结构
                    await BuildTemplateTreeByPath(historyDto, historyTemplate);
                    versions.Add(historyDto);
                }
            }
            
            // 按版本号降序排列
            versions = [.. versions.OrderByDescending(v => v.Version)];
            
            return new TemplateStructureDto
            {
                Versions = versions
            };
        }

        /// <summary>
        /// 基于路径构建模板树形结构（优化版本）
        /// 使用TemplatePath进行高效查询，避免递归调用，提升性能
        /// </summary>
        private async Task BuildTemplateTreeByPath(AttachCatalogueTemplateDto templateDto, AttachCatalogueTemplate template)
        {
            try
            {
                // 使用基于路径的子树查询，一次性获取所有子节点
                var allChildren = await _templateRepository.GetTemplateSubtreeAsync(template.Id, true, 10);

                // 过滤掉根节点本身，只保留子节点
                var children = allChildren.Where(t => t.Id != template.Id).ToList();

                if (children.Count != 0)
                {
                    // 转换为DTO并构建树形结构
                    var childDtos = children.Select(c => ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(c)).ToList();
                    
                    // 构建父子关系映射
                    var childDict = childDtos.ToDictionary(c => c.Id, c => c);
                    var childTemplateDict = children.ToDictionary(c => c.Id, c => c);

                    // 构建树形结构
                    templateDto.Children = [];
                    
                    foreach (var childDto in childDtos)
                    {
                        if (childTemplateDict.TryGetValue(childDto.Id, out var childTemplate))
                        {
                            if (childTemplate.ParentId == template.Id)
                            {
                                // 直接子节点
                                templateDto.Children.Add(childDto);
                            }
                            else if (childTemplate.ParentId.HasValue && childDict.TryGetValue(childTemplate.ParentId.Value, out var parentChildDto))
                            {
                                // 间接子节点，添加到对应的父节点
                                parentChildDto.Children ??= [];
                                
                                parentChildDto.Children.Add(childDto);
                            }
                        }
                    }

                    // 按顺序号排序
                    SortChildrenBySequence(templateDto.Children);
                }

                _logger.LogInformation("基于路径构建模板树形结构完成，根模板：{templateName}，子节点数量：{childCount}",
                    template.TemplateName, children.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "基于路径构建模板树形结构失败，根模板：{templateName}", template.TemplateName);
                // 如果优化方法失败，回退到原来的递归方法
                await BuildTemplateTreeWithChildren(templateDto, template);
            }
        }

        /// <summary>
        /// 递归排序子节点
        /// </summary>
        private static void SortChildrenBySequence(List<AttachCatalogueTemplateDto> children)
        {
            if (children == null || children.Count == 0) return;

            // 按顺序号排序
            children.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));

            // 递归排序每个子节点的子节点
            foreach (var child in children)
            {
                SortChildrenBySequence(child.Children);
            }
        }

        /// <summary>
        /// 构建模板树形结构（包含子模板）- 备用方法
        /// 基于行业最佳实践，直接在模板DTO中构建Children属性
        /// </summary>
        private async Task BuildTemplateTreeWithChildren(AttachCatalogueTemplateDto templateDto, AttachCatalogueTemplate template)
        {
            // 获取子模板
            var children = await _templateRepository.GetChildrenAsync(template.Id);
            
            if (children.Count != 0)
            {
                templateDto.Children = [];
                
                foreach (var child in children.OrderBy(c => c.SequenceNumber))
                {
                    var childDto = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(child);
                    
                    // 递归构建子模板的树形结构
                    await BuildTemplateTreeWithChildren(childDto, child);
                    
                    templateDto.Children.Add(childDto);
                }
            }
        }


        public async Task GenerateCatalogueFromTemplateAsync(GenerateCatalogueInput input)
        {
            var template = await _templateRepository.GetLatestVersionAsync(input.TemplateId) ?? throw new UserFriendlyException($"未找到模板 {input.TemplateId}");
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
            Guid baseId,
            CreateUpdateAttachCatalogueTemplateDto input)
        {
            // 获取基础模板的最新版本
            var baseTemplate = await _templateRepository.GetLatestVersionAsync(baseId) 
                ?? throw new UserFriendlyException("未找到基础模板");

            // 获取下一个版本号
            var allVersions = await _templateRepository.GetTemplateHistoryAsync(baseId);
            var nextVersion = allVersions.Max(t => t.Version) + 1;

            // 创建新版本实体
            var newTemplate = new AttachCatalogueTemplate(
                templateId: baseTemplate.Id, // 使用 Id 获取模板ID
                version: nextVersion,
                templateName: input.Name,
                attachReceiveType: input.AttachReceiveType,
                sequenceNumber: input.SequenceNumber,
                isRequired: input.IsRequired,
                isStatic: input.IsStatic,
                parentId: baseTemplate.ParentId,
                workflowConfig: input.WorkflowConfig,
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
            await _templateRepository.SetAsLatestVersionAsync(newTemplate.Id, newTemplate.Version);

            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(newTemplate);
        }

        private async Task CopyChildrenStructureAsync(AttachCatalogueTemplate source, AttachCatalogueTemplate target)
        {
            var children = await _templateRepository.GetChildrenAsync(source.Id, false);
            foreach (var child in children)
            {
                var newChild = new AttachCatalogueTemplate(
                    templateId: child.Id, // 使用 Id 获取模板ID
                    version: child.Version,
                    templateName: child.TemplateName,
                    attachReceiveType: child.AttachReceiveType,
                    sequenceNumber: child.SequenceNumber,
                    isRequired: child.IsRequired,
                    isStatic: child.IsStatic,
                    parentId: target.Id, // 使用 Id 获取模板ID
                    workflowConfig: child.WorkflowConfig,
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

        public async Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid id, int version)
        {
            // 获取指定版本
            var targetVersion = await _templateRepository.GetByVersionAsync(id, version) ?? throw new UserFriendlyException($"未找到模板 {id} 的版本 {version}");
            await _templateRepository.SetAsLatestVersionAsync(id, version);
            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(targetVersion);
        }

        public async Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplateHistoryAsync(Guid id)
        {
            var history = await _templateRepository.GetTemplateHistoryAsync(id);
            return new ListResultDto<AttachCatalogueTemplateDto>(
                ObjectMapper.Map<List<AttachCatalogueTemplate>, List<AttachCatalogueTemplateDto>>(history));
        }

        public async Task<AttachCatalogueTemplateDto> GetByVersionAsync(Guid id, int version)
        {
            var template = await _templateRepository.GetByVersionAsync(id, version);
            return template == null
                ? throw new UserFriendlyException($"未找到模板 {id} 的版本 {version}")
                : ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
        }

        public async Task<AttachCatalogueTemplateDto> UpdateVersionAsync(Guid id, int version, CreateUpdateAttachCatalogueTemplateDto input)
        {
            var template = await _templateRepository.GetByVersionAsync(id, version) ?? throw new UserFriendlyException($"未找到模板 {id} 的版本 {version}");

            // 更新模板属性 - 直接通过AutoMapper映射
            ObjectMapper.Map(input, template);

            await _templateRepository.UpdateAsync(template);
            return ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template);
        }

        public async Task DeleteVersionAsync(Guid id, int version)
        {
            var template = await _templateRepository.GetByVersionAsync(id, version) ?? throw new UserFriendlyException($"未找到模板 {id} 的版本 {version}");
            await _templateRepository.DeleteAsync(template);
        }

        public async Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid id, int version)
        {
            var versionToRollback = await _templateRepository.GetByVersionAsync(id, version) ?? throw new UserFriendlyException($"未找到模板 {id} 的版本 {version}");
            var latestVersion = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException("未找到最新版本，无法回滚");
            if (latestVersion.Version == version)
            {
                throw new UserFriendlyException("此版本已是最新版本");
            }

            // 创建回滚版本（作为新版本）
            var rollbackTemplate = new AttachCatalogueTemplate(
                templateId: versionToRollback.Id, // 使用 Id 获取模板ID
                version: latestVersion.Version + 1,
                templateName: versionToRollback.TemplateName,
                attachReceiveType: versionToRollback.AttachReceiveType,
                sequenceNumber: versionToRollback.SequenceNumber,
                isRequired: versionToRollback.IsRequired,
                isStatic: versionToRollback.IsStatic,
                parentId: versionToRollback.ParentId,
                workflowConfig: versionToRollback.WorkflowConfig,
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
            await _templateRepository.SetAsLatestVersionAsync(rollbackTemplate.Id, rollbackTemplate.Version);

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
            try
            {
                _logger.LogInformation("开始获取模板统计信息");
                var domainStatistics = await _templateRepository.GetTemplateStatisticsAsync();
                
                // 映射Domain值对象到Application DTO
                var dto = new AttachCatalogueTemplateStatisticsDto
                {
                    TotalCount = domainStatistics.TotalCount,
                    RootTemplateCount = domainStatistics.RootTemplateCount,
                    ChildTemplateCount = domainStatistics.ChildTemplateCount,
                    LatestVersionCount = domainStatistics.LatestVersionCount,
                    HistoryVersionCount = domainStatistics.HistoryVersionCount,
                    GeneralFacetCount = domainStatistics.GeneralFacetCount,
                    DisciplineFacetCount = domainStatistics.DisciplineFacetCount,
                    ClassificationPurposeCount = domainStatistics.ClassificationPurposeCount,
                    DocumentPurposeCount = domainStatistics.DocumentPurposeCount,
                    WorkflowPurposeCount = domainStatistics.WorkflowPurposeCount,
                    TemplatesWithVector = domainStatistics.TemplatesWithVector,
                    AverageVectorDimension = domainStatistics.AverageVectorDimension,
                    MaxTreeDepth = domainStatistics.MaxTreeDepth,
                    AverageChildrenCount = domainStatistics.AverageChildrenCount,
                    LatestCreationTime = domainStatistics.LatestCreationTime?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    LatestModificationTime = domainStatistics.LatestModificationTime?.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
                
                _logger.LogInformation("获取模板统计信息完成，总数量：{totalCount}", dto.TotalCount);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板统计信息失败");
                throw new UserFriendlyException("获取统计信息失败，请稍后重试");
            }
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
                    input.Weights?.SemanticWeight ?? 0.6,
                    input.OnlyLatest);

                var results = templates.Select(t => new TemplateSearchResultDto
                {
                    Id = t.Id,
                    Name = t.TemplateName,
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
                    Name = t.TemplateName,
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
                    Name = t.TemplateName,
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
                    Name = t.TemplateName,
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
            bool onlyLatest = true,
            string? fulltextQuery = null)
        {
            try
            {
                // 使用优化的基于路径的查询方法
                var rootTemplates = await _templateRepository.GetRootTemplatesAsync(
                    facetType, templatePurpose, includeChildren, onlyLatest, fulltextQuery);

                var treeDtos = rootTemplates.Select(t => 
                    ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateTreeDto>(t)).ToList();

                _logger.LogInformation("获取根节点模板完成，数量：{count}，包含子节点：{includeChildren}，使用路径优化：{optimized}，全文检索：{fulltextQuery}", 
                    treeDtos.Count, includeChildren, includeChildren, fulltextQuery ?? "无");

                return new ListResultDto<AttachCatalogueTemplateTreeDto>(treeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取根节点模板失败");
                throw new UserFriendlyException("获取模板树失败，请稍后重试");
            }
        }

        #region 元数据字段管理

        public async Task<ListResultDto<MetaFieldDto>> GetTemplateMetaFieldsAsync(Guid id)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");
                var metaFields = template.GetEnabledMetaFields().ToList();
                var metaFieldDtos = ObjectMapper.Map<List<MetaField>, List<MetaFieldDto>>(metaFields);
                
                _logger.LogInformation("获取模板 {id} 的元数据字段完成，数量：{count}", id, metaFieldDtos.Count);
                
                return new ListResultDto<MetaFieldDto>(metaFieldDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板 {id} 的元数据字段失败", id);
                throw new UserFriendlyException("获取元数据字段失败，请稍后重试");
            }
        }

        /// <summary>
        /// 批量设置元数据字段（创建、更新、删除）
        /// 基于行业最佳实践，使用批量操作提高性能和确保数据一致性
        /// </summary>
        /// <param name="id">模板ID</param>
        /// <param name="metaFields">元数据字段列表</param>
        /// <returns></returns>
        public virtual async Task SetTemplateMetaFieldsAsync(Guid id, List<CreateUpdateMetaFieldDto> metaFields)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");
                
                // 验证输入参数
                if (metaFields == null)
                {
                    throw new UserFriendlyException("元数据字段列表不能为空");
                }

                // 验证字段键名唯一性
                var fieldKeys = metaFields.Select(m => m.FieldKey).ToList();
                if (fieldKeys.Count != fieldKeys.Distinct().Count())
                {
                    throw new UserFriendlyException("元数据字段键名必须唯一");
                }

                // 转换为领域对象
                var domainMetaFields = metaFields.Select(dto => new MetaField(
                    dto.EntityType,
                    dto.FieldKey,
                    dto.FieldName,
                    dto.DataType,
                    dto.IsRequired,
                    dto.Unit,
                    dto.RegexPattern,
                    dto.Options,
                    dto.Description,
                    dto.DefaultValue,
                    dto.Order,
                    dto.IsEnabled,
                    dto.Group,
                    dto.ValidationRules,
                    dto.Tags
                )).ToList();

                // 使用领域对象的批量设置方法
                template.SetMetaFields(domainMetaFields);
                
                // 保存到数据库
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("批量设置模板 {id} 的元数据字段成功，字段数量：{count}", id, metaFields.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量设置模板 {id} 的元数据字段失败", id);
                throw new UserFriendlyException("批量设置元数据字段失败，请稍后重试");
            }
        }


        public async Task<MetaFieldDto?> GetTemplateMetaFieldAsync(Guid templateId, string fieldKey)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"未找到模板 {templateId}");
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

        public async Task<ListResultDto<MetaFieldDto>> QueryTemplateMetaFieldsAsync(Guid id, MetaFieldQueryDto input)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");
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
                
                _logger.LogInformation("查询模板 {id} 的元数据字段完成，数量：{count}", id, metaFieldDtos.Count);
                
                return new ListResultDto<MetaFieldDto>(metaFieldDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询模板 {id} 的元数据字段失败", id);
                throw new UserFriendlyException("查询元数据字段失败，请稍后重试");
            }
        }

        public async Task UpdateMetaFieldsOrderAsync(Guid id, List<string> fieldKeys)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(id) ?? throw new UserFriendlyException($"未找到模板 {id}");
                for (int i = 0; i < fieldKeys.Count; i++)
                {
                    var field = template.GetMetaField(fieldKeys[i]);
                    field?.Update(order: i);
                }
                
                await _templateRepository.UpdateAsync(template);
                
                _logger.LogInformation("更新模板 {id} 的元数据字段顺序成功，字段数量：{count}", id, fieldKeys.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板 {id} 的元数据字段顺序失败", id);
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
