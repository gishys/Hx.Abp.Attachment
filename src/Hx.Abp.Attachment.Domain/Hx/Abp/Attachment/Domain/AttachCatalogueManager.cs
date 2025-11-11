using Hx.Abp.Attachment.Domain.Shared;
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
        IGuidGenerator guidGenerator,
        IEfCoreAttachFileRepository fileRepository) : DomainService, IAttachCatalogueManager
    {
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository = catalogueRepository;
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IRulesEngine _rulesEngine = rulesEngine;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly IEfCoreAttachFileRepository _fileRepository = fileRepository;

        // 用于在事务中跟踪已创建的分类，避免重复查询数据库
        private readonly Dictionary<string, int> _sequenceNumberCache = [];
        private readonly Dictionary<string, string> _pathCache = [];
        private readonly Dictionary<Guid, AttachCatalogue> _createdCataloguesCache = [];

        public async Task<AttachCatalogue> GenerateFromTemplateAsync(
            AttachCatalogueTemplate template,
            string reference,
            int referenceType,
            Dictionary<string, object>? contextData = null,
            string? customTemplateName = null,
            List<MetaField>? customMetaFields = null)
        {
            // 清空缓存，开始新的模板生成过程
            _sequenceNumberCache.Clear();
            _pathCache.Clear();
            _createdCataloguesCache.Clear();

            // 检查是否为最新版本
            if (!template.IsLatest)
            {
                var latest = await _templateRepository.GetLatestVersionAsync(template.Id, true);
                if (latest != null && latest.Id != template.Id)
                {
                    throw new BusinessException("Template:NotLatestVersion")
                        .WithData("TemplateName", template.TemplateName)
                        .WithData("LatestVersion", latest.Version);
                }
            }

            var rootCatalogue = await CreateCatalogueFromTemplate(template, null, reference, referenceType, contextData, customTemplateName, customMetaFields);
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
                templateId: _guidGenerator.Create(),
                templateName: baseTemplate.TemplateName,
                attachReceiveType: baseTemplate.AttachReceiveType,
                sequenceNumber: baseTemplate.SequenceNumber,
                isRequired: baseTemplate.IsRequired,
                isStatic: baseTemplate.IsStatic,
                parentId: newParentId ?? baseTemplate.ParentId,
                workflowConfig: baseTemplate.WorkflowConfig,
                version: nextVersion,
                isLatest: false,
                facetType: baseTemplate.FacetType,
                templatePurpose: baseTemplate.TemplatePurpose,
                templateRole: baseTemplate.TemplateRole,
                textVector: baseTemplate.TextVector
            );

            await _templateRepository.InsertAsync(newTemplate);
            await CopyTemplateChildrenAsync(baseTemplate, newTemplate);

            return newTemplate;
        }


        private async Task CopyTemplateChildrenAsync(AttachCatalogueTemplate source, AttachCatalogueTemplate target)
        {
            var children = await _templateRepository.GetChildrenAsync(source.Id, source.Version, false);
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
            Dictionary<string, object>? contextData,
            string? customTemplateName = null,
            List<MetaField>? customMetaFields = null)
        {
            // 动态分面：模板创建实例时不直接创建分类
            if (!template.IsStatic)
            {
                throw new BusinessException("Catalogue:DynamicFacetInstantiationNotAllowed")
                    .WithData("TemplateId", template.Id)
                    .WithData("TemplateName", template.TemplateName);
            }

            // 1. 计算正确的 sequenceNumber：基于相同父模板的最大序号+1
            int sequenceNumber = await CalculateNextSequenceNumberAsync(template.Id, template.Version, parentId);

            // 2. 计算 path：参考 GetEntitys 中的逻辑
            string? path = await CalculatePathAsync(parentId);

            // 使用自定义名称或模板名称
            var catalogueName = !string.IsNullOrWhiteSpace(customTemplateName)
                ? customTemplateName
                : await ResolveCatalogueName(template, contextData);

            // 使用自定义元数据或模板元数据
            var metaFields = customMetaFields ?? template.MetaFields?.ToList();

            var catalogue = new AttachCatalogue(
                id: GuidGenerator.Create(),
                attachReceiveType: template.AttachReceiveType,
                catologueName: catalogueName,
                sequenceNumber: sequenceNumber,
                reference: reference,
                referenceType: referenceType,
                parentId: parentId,
                isRequired: template.IsRequired,
                isStatic: template.IsStatic,
                templateId: template.Id,
                templateVersion: template.Version,
                catalogueFacetType: template.FacetType,
                cataloguePurpose: template.TemplatePurpose,
                templateRole: template.TemplateRole,
                tags: template.Tags,
                textVector: template.TextVector,
                metaFields: metaFields,
                path: path
            );

            // 复制权限集合
            if (template.Permissions != null && template.Permissions.Count != 0)
            {
                foreach (var permission in template.Permissions)
                {
                    catalogue.AddPermission(permission);
                }
            }

            await _catalogueRepository.InsertAsync(catalogue);

            // 将创建的分类添加到缓存中，供后续子分类查询使用
            _createdCataloguesCache[catalogue.Id] = catalogue;

            return catalogue;
        }

        /// <summary>
        /// 计算下一个序号：基于相同父模板的最大序号+1
        /// </summary>
        private async Task<int> CalculateNextSequenceNumberAsync(Guid templateId, int templateVersion, Guid? parentId)
        {
            // 创建缓存键
            var cacheKey = $"{parentId}";

            // 如果缓存中已有该键，直接返回下一个序号
            if (_sequenceNumberCache.TryGetValue(cacheKey, out int value))
            {
                _sequenceNumberCache[cacheKey] = ++value;
                return value;
            }

            // 查询数据库中相同父模板下已存在的最大序号
            var existingCatalogues = await _catalogueRepository.GetQueryableAsync();
            var maxSequenceNumber = existingCatalogues
                .Where(c => c.TemplateId == templateId &&
                           c.TemplateVersion == templateVersion &&
                           c.ParentId == parentId)
                .Max(c => (int?)c.SequenceNumber);

            // 计算下一个序号并缓存
            var nextSequenceNumber = (maxSequenceNumber ?? 0) + 1;
            _sequenceNumberCache[cacheKey] = nextSequenceNumber;

            return nextSequenceNumber;
        }

        /// <summary>
        /// 计算路径：参考 GetEntitys 中的逻辑
        /// </summary>
        private async Task<string?> CalculatePathAsync(Guid? parentId)
        {
            // 创建缓存键
            var cacheKey = parentId?.ToString() ?? "ROOT";

            if (parentId.HasValue)
            {
                // 有父级：优先从缓存中获取父级分类，如果缓存中没有则从数据库查询
                AttachCatalogue? parentCatalogue = null;

                if (_createdCataloguesCache.TryGetValue(parentId.Value, out AttachCatalogue? value))
                {
                    parentCatalogue = value;
                }
                else
                {
                    parentCatalogue = await _catalogueRepository.GetAsync(parentId.Value);
                }

                if (parentCatalogue != null)
                {
                    // 检查缓存中是否已有该父级下的路径
                    if (_pathCache.TryGetValue(cacheKey, out string? lastPath))
                    {
                        var lastUnitCode = AttachCatalogue.GetLastUnitPathCode(lastPath);
                        var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                        var nextUnitCode = nextNumber.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}");
                        var nextChildPath = AttachCatalogue.AppendPathCode(parentCatalogue.Path, nextUnitCode);
                        _pathCache[cacheKey] = nextChildPath;
                        return nextChildPath;
                    }

                    // 查询数据库中同级最大路径
                    var existingCatalogues = await _catalogueRepository.GetQueryableAsync();
                    var maxPathAtSameLevel = existingCatalogues
                        .Where(c => c.ParentId == parentId && !string.IsNullOrEmpty(c.Path))
                        .Select(c => c.Path)
                        .OrderByDescending(path => path)
                        .FirstOrDefault();

                    string nextPath;
                    if (string.IsNullOrEmpty(maxPathAtSameLevel))
                    {
                        // 没有同级，创建第一个子路径
                        nextPath = AttachCatalogue.AppendPathCode(parentCatalogue.Path, "0000001");
                    }
                    else
                    {
                        // 有同级，获取最大路径的最后一个单元代码并+1
                        var lastUnitCode = AttachCatalogue.GetLastUnitPathCode(maxPathAtSameLevel);
                        var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                        var nextUnitCode = nextNumber.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}");
                        nextPath = AttachCatalogue.AppendPathCode(parentCatalogue.Path, nextUnitCode);
                    }

                    // 缓存路径
                    _pathCache[cacheKey] = nextPath;
                    return nextPath;
                }
            }
            else
            {
                // 检查缓存中是否已有根级别的路径
                if (_pathCache.TryGetValue(cacheKey, out string? lastPath))
                {
                    var nextNumber = Convert.ToInt32(lastPath) + 1;
                    var newRootPath = AttachCatalogue.CreatePathCode(nextNumber);
                    _pathCache[cacheKey] = newRootPath;
                    return newRootPath;
                }

                // 没有父级：查找根级别最大路径
                var existingCatalogues = await _catalogueRepository.GetQueryableAsync();
                var maxPathAtRootLevel = existingCatalogues
                    .Where(c => c.ParentId == null && !string.IsNullOrEmpty(c.Path))
                    .Select(c => c.Path)
                    .OrderByDescending(path => path)
                    .FirstOrDefault();

                string nextRootPath;
                if (string.IsNullOrEmpty(maxPathAtRootLevel))
                {
                    // 没有根级别分类，创建第一个
                    nextRootPath = AttachCatalogue.CreatePathCode(1);
                }
                else
                {
                    // 有根级别分类，获取最大路径并+1
                    var nextNumber = Convert.ToInt32(maxPathAtRootLevel) + 1;
                    nextRootPath = AttachCatalogue.CreatePathCode(nextNumber);
                }

                // 缓存路径
                _pathCache[cacheKey] = nextRootPath;
                return nextRootPath;
            }

            return null;
        }

        private async Task<string> ResolveCatalogueName(
            AttachCatalogueTemplate template,
            Dictionary<string, object>? contextData)
        {
            // 优先使用规则引擎
            if (!string.IsNullOrEmpty(template.WorkflowConfig) && contextData != null)
            {
                var workflow = JsonConvert.DeserializeObject<Workflow>(template.WorkflowConfig);
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
            var children = await _templateRepository.GetChildrenAsync(parentTemplate.Id, parentTemplate.Version);

            foreach (var childTemplate in children)
            {
                // 动态分面子节点不自动创建，由业务逻辑/人工创建
                if (!childTemplate.IsStatic)
                {
                    continue;
                }

                var childCatalogue = await CreateCatalogueFromTemplate(
                    childTemplate,
                    parentCatalogue.Id,
                    reference,
                    referenceType,
                    contextData);

                await CreateChildCatalogues(childTemplate, childCatalogue, reference, referenceType, contextData);
            }
        }

        /// <summary>
        /// 软删除分类及其所有子分类
        /// </summary>
        /// <param name="catalogue">要删除的分类</param>
        /// <param name="cancellationToken">取消令牌</param>
        public virtual async Task SoftDeleteCatalogueWithChildrenAsync(
            AttachCatalogue catalogue,
            CancellationToken cancellationToken = default)
        {
            if (catalogue == null)
                return;

            var catalogueIds = new List<Guid>();
            var fileIds = new List<Guid>();

            // 递归收集所有子分类和文件的ID
            CollectChildrenIds(catalogue, catalogueIds, fileIds);

            // 软删除所有关联的文件
            if (fileIds.Count > 0)
            {
                await _fileRepository.DeleteManyAsync(fileIds, false, cancellationToken);
            }

            // 软删除所有分类（包括父分类和子分类）
            await _catalogueRepository.DeleteManyAsync(catalogueIds, true, cancellationToken);
        }

        /// <summary>
        /// 硬删除分类及其所有子分类
        /// </summary>
        /// <param name="catalogue">要删除的分类</param>
        /// <param name="cancellationToken">取消令牌</param>
        public virtual async Task HardDeleteCatalogueWithChildrenAsync(
            AttachCatalogue catalogue,
            CancellationToken cancellationToken = default)
        {
            if (catalogue == null)
                return;

            var catalogueIds = new List<Guid>();
            var fileIds = new List<Guid>();

            // 递归收集所有子分类和文件的ID
            CollectChildrenIds(catalogue, catalogueIds, fileIds);

            // 硬删除所有关联的文件
            if (fileIds.Count > 0)
            {
                await _fileRepository.DeleteManyAsync(fileIds, true, cancellationToken);
            }

            // 硬删除所有分类（包括父分类和子分类）
            if (catalogueIds.Count > 0)
            {
                foreach (var id in catalogueIds)
                {
                    await _catalogueRepository.HardDeleteAsync(c => c.Id == id, true, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 递归收集子分类和文件的ID
        /// </summary>
        /// <param name="catalogue">分类</param>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="fileIds">文件ID列表（可选）</param>
        private static void CollectChildrenIds(AttachCatalogue catalogue, List<Guid> catalogueIds, List<Guid>? fileIds = null)
        {
            // 添加当前分类ID
            catalogueIds.Add(catalogue.Id);

            // 添加当前分类下的所有文件ID（如果提供了文件ID列表）
            if (fileIds != null && catalogue.AttachFiles != null && catalogue.AttachFiles.Count > 0)
            {
                fileIds.AddRange(catalogue.AttachFiles.Select(f => f.Id));
            }

            // 递归处理子分类
            if (catalogue.Children != null && catalogue.Children.Count > 0)
            {
                foreach (var child in catalogue.Children)
                {
                    CollectChildrenIds(child, catalogueIds, fileIds);
                }
            }
        }
    }
}
