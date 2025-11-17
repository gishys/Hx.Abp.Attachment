using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 模板验证服务
    /// 负责验证模板创建和更新时的业务规则
    /// </summary>
    public class AttachCatalogueTemplateValidationService(
        IAttachCatalogueTemplateRepository templateRepository,
        ILogger<AttachCatalogueTemplateValidationService> logger)
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly ILogger<AttachCatalogueTemplateValidationService> _logger = logger;

        /// <summary>
        /// 验证模板创建/更新的业务规则
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="facetType">分面类型</param>
        /// <param name="parentId">父模板ID</param>
        /// <param name="parentVersion">父模板版本</param>
        /// <param name="excludeTemplateId">排除的模板ID（用于更新时排除当前模板）</param>
        /// <param name="operation">操作类型（用于日志）</param>
        public async Task ValidateTemplateRulesAsync(
            string templateName,
            FacetType facetType,
            Guid? parentId,
            int? parentVersion,
            Guid? excludeTemplateId = null,
            string operation = "创建")
        {
            // 验证模板名称不能为空
            if (string.IsNullOrWhiteSpace(templateName))
            {
                throw new UserFriendlyException($"{operation}模板失败：模板名称不能为空");
            }

            // 规则0：验证模板名称在同一父节点下不能重复（根节点下也不能重复）
            var nameExists = await _templateRepository.ExistsByNameAsync(templateName, parentId, parentVersion, excludeTemplateId);
            if (nameExists)
            {
                var scope = parentId.HasValue ? "同级" : "根节点";
                _logger.LogWarning(
                    "规则验证失败：{operation}模板时，在{scope}下已存在名称为 '{templateName}' 的模板，父模板ID={parentId}",
                    operation, scope, templateName, parentId);
                throw new UserFriendlyException($"{operation}模板失败：在{scope}下已存在名称为 '{templateName}' 的模板，请使用其他名称");
            }

            // 计算是否为动态分面
            var isDynamicFacet = !FacetTypePolicies.IsStaticFacet(facetType);

            // 规则1：根分类模板不能是动态分面
            if (parentId == null && isDynamicFacet)
            {
                _logger.LogWarning("规则验证失败：{operation}模板时，根分类模板不能是动态分面，分面类型={facetType}", operation, facetType);
                throw new UserFriendlyException($"{operation}模板失败：根分类模板不能是动态分面，请选择静态分面类型");
            }

            // 如果有父模板，验证同级规则
            if (parentId.HasValue)
            {
                await ValidateSiblingRulesAsync(parentId.Value, parentVersion, isDynamicFacet, excludeTemplateId, operation);
            }
        }

        /// <summary>
        /// 验证同级模板的业务规则
        /// </summary>
        private async Task ValidateSiblingRulesAsync(
            Guid parentId,
            int? parentVersion,
            bool isNewTemplateDynamic,
            Guid? excludeTemplateId,
            string operation)
        {
            // 获取父模板
            AttachCatalogueTemplate? parentTemplate = null;
            if (parentVersion.HasValue)
            {
                parentTemplate = await _templateRepository.GetByVersionAsync(parentId, parentVersion.Value);
            }
            else
            {
                parentTemplate = await _templateRepository.GetLatestVersionAsync(parentId, false);
            }

            if (parentTemplate == null)
            {
                throw new UserFriendlyException($"未找到父模板 {parentId}");
            }

            // 获取同级子模板（只获取最新版本）
            var siblings = await _templateRepository.GetChildrenAsync(parentId, parentTemplate.Version, true);
            
            // 排除当前模板（用于更新场景）
            if (excludeTemplateId.HasValue)
            {
                siblings = [.. siblings.Where(s => s.Id != excludeTemplateId.Value)];
            }

            // 规则2：同一级只能有一个动态分面模板
            var existingDynamicSiblings = siblings.Where(s => !s.IsStatic).ToList();
            if (isNewTemplateDynamic && existingDynamicSiblings.Count > 0)
            {
                var existingDynamicName = existingDynamicSiblings.First().TemplateName;
                _logger.LogWarning(
                    "规则验证失败：{operation}模板时，同一级只能有一个动态分面模板，已存在动态分面模板={existingName}，父模板ID={parentId}",
                    operation, existingDynamicName, parentId);
                throw new UserFriendlyException(
                    $"{operation}模板失败：同一级只能有一个动态分面模板，已存在动态分面模板 '{existingDynamicName}'");
            }

            // 规则3：存在动态分面模板的分类不能在同一级存在静态分类
            // 分析：从业务角度考虑，动态分面和静态分面应该互斥，以保持分类结构清晰
            // 如果同一级既有动态分面又有静态分面，会导致：
            // 1. 分类结构混乱，用户难以理解哪些是动态创建的，哪些是静态的
            // 2. 业务逻辑复杂化，需要区分处理动态和静态分类
            // 3. 用户体验差，容易产生困惑
            // 因此，我们采用互斥策略：同一级不能同时存在动态和静态分面模板
            if (existingDynamicSiblings.Count > 0 && !isNewTemplateDynamic)
            {
                var existingDynamicName = existingDynamicSiblings.First().TemplateName;
                _logger.LogWarning(
                    "规则验证失败：{operation}模板时，存在动态分面模板的分类不能在同一级存在静态分类，已存在动态分面模板={existingName}，父模板ID={parentId}",
                    operation, existingDynamicName, parentId);
                throw new UserFriendlyException(
                    $"{operation}模板失败：存在动态分面模板 '{existingDynamicName}' 的分类不能在同一级创建静态分类模板，请先删除动态分面模板或选择其他父分类");
            }

            // 如果新模板是动态分面，检查是否已有静态分面
            if (isNewTemplateDynamic)
            {
                var existingStaticSiblings = siblings.Where(s => s.IsStatic).ToList();
                if (existingStaticSiblings.Count > 0)
                {
                    var existingStaticNames = string.Join("、", existingStaticSiblings.Select(s => s.TemplateName));
                    _logger.LogWarning(
                        "规则验证失败：{operation}模板时，存在静态分面模板的分类不能在同一级创建动态分面模板，已存在静态分面模板={existingNames}，父模板ID={parentId}",
                        operation, existingStaticNames, parentId);
                    throw new UserFriendlyException(
                        $"{operation}模板失败：存在静态分面模板 '{existingStaticNames}' 的分类不能在同一级创建动态分面模板，请先删除静态分面模板或选择其他父分类");
                }
            }
        }
    }
}

