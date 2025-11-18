using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 动态子分类创建服务
    /// 用于根据文件夹结构动态创建子分类，隔离复杂逻辑
    /// </summary>
    public class DynamicSubCatalogueCreationService(
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IAttachCatalogueTemplateRepository templateRepository,
        ILogger<DynamicSubCatalogueCreationService> logger,
        IUnitOfWorkManager unitOfWorkManager)
    {
        private readonly IEfCoreAttachCatalogueRepository _catalogueRepository = catalogueRepository;
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly ILogger<DynamicSubCatalogueCreationService> _logger = logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager = unitOfWorkManager;

        /// <summary>
        /// 根据子文件夹路径递归创建子分类
        /// </summary>
        /// <param name="dynamicFacetCatalogue">动态分面分类（父分类）</param>
        /// <param name="subFolderPath">子文件夹路径（如 "材料类型/正本"）</param>
        /// <param name="dynamicFacetTemplate">动态分面模板</param>
        /// <param name="reference">引用标识</param>
        /// <param name="referenceType">引用类型</param>
        /// <param name="guidGenerator">GUID生成器</param>
        /// <returns>最底层的分类（文件应该归属的分类）</returns>
        public async Task<AttachCatalogue> CreateSubCataloguesFromFolderPathAsync(
            AttachCatalogue dynamicFacetCatalogue,
            string subFolderPath,
            AttachCatalogueTemplate dynamicFacetTemplate,
            string reference,
            int referenceType,
            Volo.Abp.Guids.IGuidGenerator guidGenerator)
        {
            if (string.IsNullOrWhiteSpace(subFolderPath))
            {
                return dynamicFacetCatalogue;
            }

            // 分割路径为文件夹名称数组
            var folderNames = subFolderPath.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            if (folderNames.Length == 0)
            {
                return dynamicFacetCatalogue;
            }

            // 获取动态分面模板的子模板
            var childTemplates = await _templateRepository.GetChildrenAsync(
                dynamicFacetTemplate.Id, dynamicFacetTemplate.Version);

            // 递归创建子分类
            AttachCatalogue currentCatalogue = dynamicFacetCatalogue;
            AttachCatalogueTemplate? currentTemplate = dynamicFacetTemplate;

            foreach (var folderName in folderNames)
            {
                currentCatalogue = await CreateOrFindSubCatalogueAsync(
                    currentCatalogue,
                    folderName,
                    currentTemplate,
                    childTemplates,
                    reference,
                    referenceType,
                    guidGenerator);

                // 更新当前模板为刚创建的分类对应的模板（如果有）
                if (currentCatalogue.TemplateId.HasValue)
                {
                    currentTemplate = await _templateRepository.GetByVersionAsync(
                        currentCatalogue.TemplateId.Value, currentCatalogue.TemplateVersion ?? 1);
                    if (currentTemplate != null)
                    {
                        childTemplates = await _templateRepository.GetChildrenAsync(
                            currentTemplate.Id, currentTemplate.Version);
                    }
                }
                else
                {
                    // 动态创建的分类没有模板，继续使用父模板的子模板
                    childTemplates = await _templateRepository.GetChildrenAsync(
                        dynamicFacetTemplate.Id, dynamicFacetTemplate.Version);
                }
            }

            return currentCatalogue;
        }

        /// <summary>
        /// 创建或查找子分类
        /// </summary>
        private async Task<AttachCatalogue> CreateOrFindSubCatalogueAsync(
            AttachCatalogue parentCatalogue,
            string folderName,
            AttachCatalogueTemplate? parentTemplate,
            List<AttachCatalogueTemplate> childTemplates,
            string reference,
            int referenceType,
            Volo.Abp.Guids.IGuidGenerator guidGenerator)
        {
            // 1. 检查是否已存在同名的子分类（在指定父分类下）
            var existingCatalogue = await _catalogueRepository.GetAsync(
                parentCatalogue.Id, folderName, reference, referenceType);

            if (existingCatalogue != null)
            {
                _logger.LogInformation(
                    "使用已存在的子分类: {CatalogueName}, ID: {CatalogueId}, 父分类ID: {ParentId}",
                    existingCatalogue.CatalogueName, existingCatalogue.Id, parentCatalogue.Id);
                return existingCatalogue;
            }

            // 2. 查找是否有匹配的模板（按名称匹配）
            AttachCatalogueTemplate? matchedTemplate = null;
            if (childTemplates != null && childTemplates.Count > 0)
            {
                matchedTemplate = childTemplates.FirstOrDefault(t =>
                    string.Equals(t.TemplateName, folderName, StringComparison.OrdinalIgnoreCase));
            }

            // 3. 创建新的子分类
            var subCatalogueId = guidGenerator.Create();
            var maxSequenceNumber = await _catalogueRepository.GetMaxSequenceNumberByReferenceAsync(
                parentCatalogue.Id, reference, referenceType);

            // 计算路径
            var maxPathAtSameLevel = await _catalogueRepository.GetMaxPathAtSameLevelAsync(
                parentPath: parentCatalogue.Path);
            string cataloguePath;
            if (string.IsNullOrEmpty(maxPathAtSameLevel))
            {
                cataloguePath = AttachCatalogue.AppendPathCode(parentCatalogue.Path, "0000001");
            }
            else
            {
                var lastUnitCode = AttachCatalogue.GetLastUnitPathCode(maxPathAtSameLevel);
                var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                var nextUnitCode = nextNumber.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}");
                cataloguePath = AttachCatalogue.AppendPathCode(parentCatalogue.Path, nextUnitCode);
            }

            // 4. 根据是否有匹配的模板决定使用哪些属性
            AttachCatalogue subCatalogue;

            if (matchedTemplate != null)
            {
                // 使用模板属性创建分类
                subCatalogue = new AttachCatalogue(
                    subCatalogueId,
                    parentCatalogue.AttachReceiveType,
                    folderName, // 使用文件夹名称
                    maxSequenceNumber + 1,
                    reference,
                    referenceType,
                    parentCatalogue.Id,
                    matchedTemplate.IsRequired,
                    false, // IsVerification
                    false, // VerificationPassed
                    true,
                    0, // AttachCount
                    0, // PageCount
                    matchedTemplate.Id, // TemplateId
                    matchedTemplate.Version, // TemplateVersion
                    matchedTemplate.FacetType, // CatalogueFacetType
                    matchedTemplate.TemplatePurpose, // CataloguePurpose
                    matchedTemplate.TemplateRole, // TemplateRole
                    matchedTemplate.Tags, // Tags
                    matchedTemplate.TextVector, // TextVector
                    matchedTemplate.MetaFields?.ToList(), // MetaFields
                    cataloguePath, // Path
                    false, // IsArchived
                    matchedTemplate.Description // Summary
                );

                // 复制权限
                if (matchedTemplate.Permissions != null && matchedTemplate.Permissions.Count > 0)
                {
                    foreach (var permission in matchedTemplate.Permissions)
                    {
                        subCatalogue.AddPermission(permission);
                    }
                }

                _logger.LogInformation(
                    "使用模板创建子分类: {CatalogueName}, 模板ID: {TemplateId}, 模板名称: {TemplateName}",
                    folderName, matchedTemplate.Id, matchedTemplate.TemplateName);
            }
            else
            {
                // 动态创建子分类，使用父模板的属性（如果可用）或默认值
                var facetType = parentTemplate?.FacetType ?? FacetType.General;
                var templatePurpose = parentTemplate?.TemplatePurpose ?? TemplatePurpose.Classification;
                var templateRole = parentTemplate?.TemplateRole ?? TemplateRole.Branch;

                subCatalogue = new AttachCatalogue(
                    subCatalogueId,
                    parentCatalogue.AttachReceiveType,
                    folderName, // 使用文件夹名称
                    maxSequenceNumber + 1,
                    reference,
                    referenceType,
                    parentCatalogue.Id,
                    false, // IsRequired（默认值）
                    false, // IsVerification
                    false, // VerificationPassed
                    true,
                    0, // AttachCount
                    0, // PageCount
                    null, // TemplateId（动态创建，没有模板）
                    null, // TemplateVersion
                    facetType, // 使用父模板的分面类型
                    templatePurpose, // 使用父模板的用途
                    templateRole, // 使用父模板的角色
                    parentTemplate?.Tags, // Tags（使用父模板的标签）
                    null, // TextVector（动态创建，没有向量）
                    null, // MetaFields（动态创建，没有元数据字段）
                    cataloguePath, // Path
                    false, // IsArchived
                    null // Summary（动态创建，没有描述）
                );

                _logger.LogInformation(
                    "动态创建子分类: {CatalogueName}, 父分类ID: {ParentId}",
                    folderName, parentCatalogue.Id);
            }

            await _catalogueRepository.InsertAsync(subCatalogue);

            // 确保数据已提交，以便后续查询
            var uow = _unitOfWorkManager.Current;
            if (uow != null)
            {
                await uow.SaveChangesAsync();
            }

            return subCatalogue;
        }
    }
}

