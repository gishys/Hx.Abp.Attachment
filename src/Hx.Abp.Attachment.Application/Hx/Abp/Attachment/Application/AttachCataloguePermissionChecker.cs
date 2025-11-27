using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 附件分类权限检查器
    /// 集成ABP vNext权限系统与业务权限的最佳实践实现
    /// </summary>
    public class AttachCataloguePermissionChecker(
        IPermissionChecker permissionChecker,
        ICurrentUser currentUser,
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IAttachCatalogueTemplateRepository templateRepository,
        IServiceProvider serviceProvider,
        ILogger<AttachCataloguePermissionChecker> logger) : ITransientDependency
    {
        private readonly IPermissionChecker _permissionChecker = permissionChecker;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly IEfCoreAttachCatalogueRepository _catalogueRepository = catalogueRepository;
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<AttachCataloguePermissionChecker> _logger = logger;

        /// <summary>
        /// 检查用户是否具有指定分类的指定权限
        /// 权限检查优先级：
        /// 1. ABP系统权限（全局权限）
        /// 2. 分类特定权限（业务权限）
        /// 3. 继承权限（从父分类继承）
        /// </summary>
        /// <param name="catalogue">分类实体</param>
        /// <param name="action">权限操作</param>
        /// <param name="userId">用户ID（可选，如果为空则使用当前用户）</param>
        /// <param name="userRoles">用户角色列表（可选，如果为空则自动获取）</param>
        /// <returns>是否具有权限</returns>
        public async Task<bool> CheckPermissionAsync(
            AttachCatalogue catalogue,
            PermissionAction action,
            Guid? userId = null,
            List<string>? userRoles = null)
        {
            if (catalogue == null)
            {
                _logger.LogWarning("分类实体为空，权限检查失败");
                return false;
            }

            var targetUserId = userId ?? _currentUser.Id;
            if (targetUserId == null)
            {
                _logger.LogWarning("用户ID为空，权限检查失败");
                return false;
            }

            try
            {
                // 获取用户角色（如果未提供）
                var roles = userRoles?.ToArray() ?? await GetUserRolesAsync(targetUserId.Value);

                // 1. 首先检查ABP系统权限（全局权限）
                var abpPermissionName = GetAbpPermissionName(action);
                if (await _permissionChecker.IsGrantedAsync(abpPermissionName))
                {
                    _logger.LogDebug("用户 {UserId} 通过ABP系统权限检查，分类: {CatalogueId}, 操作: {Action}",
                        targetUserId, catalogue.Id, action);
                    return true;
                }

                // 2. 检查分类特定权限（业务权限）
                var hasCataloguePermission = CheckCataloguePermission(catalogue, targetUserId.Value, action, roles);
                if (hasCataloguePermission)
                {
                    _logger.LogDebug("用户 {UserId} 通过分类权限检查，分类: {CatalogueId}, 操作: {Action}",
                        targetUserId, catalogue.Id, action);
                    return true;
                }

                // 3. 检查继承权限（从父分类和模板继承）
                var hasInheritedPermission = await CheckInheritedPermissionAsync(catalogue, targetUserId.Value, action, roles);
                if (hasInheritedPermission)
                {
                    _logger.LogDebug("用户 {UserId} 通过继承权限检查，分类: {CatalogueId}, 操作: {Action}",
                        targetUserId, catalogue.Id, action);
                    return true;
                }

                _logger.LogDebug("用户 {UserId} 权限检查失败，分类: {CatalogueId}, 操作: {Action}",
                    targetUserId, catalogue.Id, action);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "权限检查时发生错误，分类: {CatalogueId}, 操作: {Action}",
                    catalogue.Id, action);
                return false;
            }
        }


        /// <summary>
        /// 检查继承权限
        /// 继承优先级：
        /// 1. 从父分类继承权限（递归向上查找）
        /// 2. 从模板继承权限
        /// </summary>
        private async Task<bool> CheckInheritedPermissionAsync(
            AttachCatalogue catalogue,
            Guid userId,
            PermissionAction action,
            string[]? userRoles = null)
        {
            // 如果未提供用户角色，使用当前用户的角色或获取指定用户的角色
            if (userRoles == null || userRoles.Length == 0)
            {
                if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                {
                    userRoles = _currentUser.Roles?.ToArray() ?? [];
                }
                else
                {
                    // 获取指定用户的角色
                    userRoles = await GetUserRolesAsync(userId);
                }
            }

            var visitedCatalogueIds = new HashSet<Guid>(); // 防止循环引用

            // 1. 从父分类继承权限（递归向上查找）
            if (catalogue.ParentId.HasValue)
            {
                var hasParentPermission = await CheckParentInheritedPermissionAsync(
                    catalogue.ParentId.Value,
                    userId,
                    action,
                    userRoles,
                    visitedCatalogueIds);
                
                if (hasParentPermission)
                {
                    _logger.LogDebug("从父分类继承权限成功，分类: {CatalogueId}, 父分类: {ParentId}, 操作: {Action}",
                        catalogue.Id, catalogue.ParentId, action);
                    return true;
                }
            }

            // 2. 从模板继承权限
            if (catalogue.TemplateId.HasValue)
            {
                var hasTemplatePermission = await CheckTemplateInheritedPermissionAsync(
                    catalogue,
                    catalogue.TemplateId.Value,
                    catalogue.TemplateVersion,
                    userId,
                    action,
                    userRoles ?? []);
                
                if (hasTemplatePermission)
                {
                    _logger.LogDebug("从模板继承权限成功，分类: {CatalogueId}, 模板: {TemplateId}, 操作: {Action}",
                        catalogue.Id, catalogue.TemplateId, action);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 递归检查父分类的继承权限
        /// </summary>
        private async Task<bool> CheckParentInheritedPermissionAsync(
            Guid parentId,
            Guid userId,
            PermissionAction action,
            string[] userRoles,
            HashSet<Guid> visitedCatalogueIds)
        {
            // 防止循环引用
            if (visitedCatalogueIds.Contains(parentId))
            {
                _logger.LogWarning("检测到循环引用，分类ID: {ParentId}", parentId);
                return false;
            }

            visitedCatalogueIds.Add(parentId);

            try
            {
                // 获取父分类
                var parentCatalogue = await _catalogueRepository.GetAsync(parentId);
                if (parentCatalogue == null)
                {
                    return false;
                }

                // 检查父分类的直接权限
                var hasDirectPermission = CheckCataloguePermission(parentCatalogue, userId, action, userRoles);
                if (hasDirectPermission)
                {
                    return true;
                }

                // 检查父分类的继承权限（Effect == Inherit）
                if (parentCatalogue.Permissions != null && parentCatalogue.Permissions.Count > 0)
                {
                    var inheritedPermissions = parentCatalogue.Permissions
                        .Where(p => p.IsEffective() && 
                                   p.Action == action && 
                                   p.Effect == PermissionEffect.Inherit)
                        .ToList();

                    if (inheritedPermissions.Count > 0)
                    {
                        // 检查用户或角色是否匹配
                        var userInheritedPermissions = inheritedPermissions
                            .Where(p => p.PermissionType == "User" && 
                                       p.PermissionTarget == userId.ToString())
                            .ToList();

                        var roleInheritedPermissions = inheritedPermissions
                            .Where(p => p.PermissionType == "Role" && 
                                       userRoles.Contains(p.PermissionTarget))
                            .ToList();

                        // 检查策略权限
                        var policyInheritedPermissions = inheritedPermissions
                            .Where(p => p.PermissionType == "Policy")
                            .ToList();

                        var matchedPolicyPermissions = new List<AttachCatalogueTemplatePermission>();
                        if (policyInheritedPermissions.Count > 0)
                        {
                            var policyContext = CreatePolicyContext(parentCatalogue, userId, userRoles);
                            foreach (var policyPermission in policyInheritedPermissions)
                            {
                                if (policyPermission.EvaluatePolicyCondition(policyContext))
                                {
                                    matchedPolicyPermissions.Add(policyPermission);
                                }
                            }
                        }

                        if (userInheritedPermissions.Count > 0 || 
                            roleInheritedPermissions.Count > 0 || 
                            matchedPolicyPermissions.Count > 0)
                        {
                            // 继续向上查找父分类
                            if (parentCatalogue.ParentId.HasValue)
                            {
                                return await CheckParentInheritedPermissionAsync(
                                    parentCatalogue.ParentId.Value,
                                    userId,
                                    action,
                                    userRoles,
                                    visitedCatalogueIds);
                            }
                        }
                    }
                }

                // 如果父分类没有直接权限，继续向上查找
                if (parentCatalogue.ParentId.HasValue)
                {
                    return await CheckParentInheritedPermissionAsync(
                        parentCatalogue.ParentId.Value,
                        userId,
                        action,
                        userRoles,
                        visitedCatalogueIds);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查父分类继承权限时发生错误，父分类ID: {ParentId}", parentId);
                return false;
            }
        }

        /// <summary>
        /// 检查模板继承权限
        /// </summary>
        private async Task<bool> CheckTemplateInheritedPermissionAsync(
            AttachCatalogue catalogue,
            Guid templateId,
            int? templateVersion,
            Guid userId,
            PermissionAction action,
            string[] userRoles)
        {
            try
            {
                // 获取模板
                AttachCatalogueTemplate? template = null;
                if (templateVersion.HasValue)
                {
                    template = await _templateRepository.GetByVersionAsync(templateId, templateVersion.Value);
                }
                else
                {
                    template = await _templateRepository.GetLatestVersionAsync(templateId);
                }

                if (template == null || template.Permissions == null || template.Permissions.Count == 0)
                {
                    return false;
                }

                // 检查模板权限
                var relevantPermissions = template.Permissions
                    .Where(p => p.IsEffective() && p.Action == action)
                    .ToList();

                if (relevantPermissions.Count == 0)
                {
                    return false;
                }

                // 检查用户权限
                var userPermissions = relevantPermissions
                    .Where(p => p.PermissionType == "User" && 
                               p.PermissionTarget == userId.ToString())
                    .ToList();

                // 检查角色权限
                var rolePermissions = relevantPermissions
                    .Where(p => p.PermissionType == "Role" && 
                               userRoles.Contains(p.PermissionTarget))
                    .ToList();

                // 检查策略权限
                var policyPermissions = relevantPermissions
                    .Where(p => p.PermissionType == "Policy")
                    .ToList();

                var matchedPolicyPermissions = new List<AttachCatalogueTemplatePermission>();
                if (policyPermissions.Count > 0)
                {
                    var policyContext = CreatePolicyContext(catalogue, userId, userRoles);
                    foreach (var policyPermission in policyPermissions)
                    {
                        if (policyPermission.EvaluatePolicyCondition(policyContext))
                        {
                            matchedPolicyPermissions.Add(policyPermission);
                        }
                    }
                }

                // 合并所有相关权限
                var allPermissions = userPermissions.Concat(rolePermissions).Concat(matchedPolicyPermissions).ToList();

                if (allPermissions.Count == 0)
                {
                    return false;
                }

                // 权限优先级：拒绝 > 允许 > 继承
                // 如果有拒绝权限，直接拒绝
                if (allPermissions.Any(p => p.Effect == PermissionEffect.Deny))
                {
                    return false;
                }

                // 如果有允许权限，返回允许
                if (allPermissions.Any(p => p.Effect == PermissionEffect.Allow))
                {
                    return true;
                }

                // 如果有继承权限，需要检查模板的父模板（如果存在）
                if (allPermissions.Any(p => p.Effect == PermissionEffect.Inherit))
                {
                    if (template.ParentId.HasValue)
                    {
                        return await CheckTemplateInheritedPermissionAsync(
                            catalogue,
                            template.ParentId.Value,
                            template.ParentVersion,
                            userId,
                            action,
                            userRoles);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查模板继承权限时发生错误，模板ID: {TemplateId}, 版本: {Version}",
                    templateId, templateVersion);
                return false;
            }
        }

        /// <summary>
        /// 检查分类特定权限（支持用户角色参数和策略权限）
        /// </summary>
        private bool CheckCataloguePermission(
            AttachCatalogue catalogue,
            Guid userId,
            PermissionAction action,
            string[]? userRoles = null)
        {
            if (catalogue.Permissions == null || catalogue.Permissions.Count == 0)
            {
                return false;
            }

            // 获取用户角色
            var roles = userRoles ?? _currentUser.Roles ?? [];

            // 检查直接权限
            var relevantPermissions = catalogue.Permissions
                .Where(p => p.IsEffective() && p.Action == action)
                .ToList();

            if (relevantPermissions.Count == 0)
            {
                return false;
            }

            // 检查用户权限
            var userPermissions = relevantPermissions
                .Where(p => p.PermissionType == "User" && 
                           p.PermissionTarget == userId.ToString() &&
                           p.Effect != PermissionEffect.Inherit) // 排除继承权限，继承权限需要单独处理
                .ToList();

            // 检查角色权限
            var rolePermissions = relevantPermissions
                .Where(p => p.PermissionType == "Role" && 
                           roles.Contains(p.PermissionTarget) &&
                           p.Effect != PermissionEffect.Inherit) // 排除继承权限
                .ToList();

            // 检查策略权限
            var policyPermissions = relevantPermissions
                .Where(p => p.PermissionType == "Policy" &&
                           p.Effect != PermissionEffect.Inherit) // 排除继承权限
                .ToList();

            // 评估策略权限
            var matchedPolicyPermissions = new List<AttachCatalogueTemplatePermission>();
            if (policyPermissions.Count > 0)
            {
                var policyContext = CreatePolicyContext(catalogue, userId, roles);
                foreach (var policyPermission in policyPermissions)
                {
                    if (policyPermission.EvaluatePolicyCondition(policyContext))
                    {
                        matchedPolicyPermissions.Add(policyPermission);
                    }
                }
            }

            // 合并所有相关权限
            var allPermissions = userPermissions.Concat(rolePermissions).Concat(matchedPolicyPermissions).ToList();

            if (allPermissions.Count == 0)
            {
                return false;
            }

            // 权限优先级：拒绝 > 允许
            // 如果有拒绝权限，直接拒绝
            if (allPermissions.Any(p => p.Effect == PermissionEffect.Deny))
            {
                return false;
            }

            // 如果有允许权限，返回允许
            if (allPermissions.Any(p => p.Effect == PermissionEffect.Allow))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 创建策略上下文
        /// </summary>
        private PolicyConditionEvaluator.PolicyContext CreatePolicyContext(
            AttachCatalogue catalogue,
            Guid userId,
            string[]? userRoles = null)
        {
            return new PolicyConditionEvaluator.PolicyContext
            {
                UserId = userId,
                UserRoles = userRoles ?? _currentUser.Roles?.ToArray(),
                Reference = catalogue.Reference,
                ReferenceType = catalogue.ReferenceType,
                CatalogueFacetType = (int?)catalogue.CatalogueFacetType,
                CataloguePurpose = (int?)catalogue.CataloguePurpose,
                Path = catalogue.Path,
                CustomAttributes = new Dictionary<string, object>
                {
                    { "CatalogueId", catalogue.Id },
                    { "CatalogueName", catalogue.CatalogueName },
                    { "ParentId", catalogue.ParentId ?? Guid.Empty },
                    { "TemplateId", catalogue.TemplateId ?? Guid.Empty },
                    { "TemplateVersion", catalogue.TemplateVersion ?? 0 },
                    { "IsArchived", catalogue.IsArchived },
                    { "IsStatic", catalogue.IsStatic }
                }
            };
        }

        /// <summary>
        /// 将业务权限操作转换为ABP权限名称
        /// </summary>
        private static string GetAbpPermissionName(PermissionAction action)
        {
            return action switch
            {
                PermissionAction.View => "Attachment.Catalogue.View",
                PermissionAction.Create => "Attachment.Catalogue.Create",
                PermissionAction.Edit => "Attachment.Catalogue.Edit",
                PermissionAction.Delete => "Attachment.Catalogue.Delete",
                PermissionAction.Approve => "Attachment.Catalogue.Approve",
                PermissionAction.Publish => "Attachment.Catalogue.Publish",
                PermissionAction.Archive => "Attachment.Catalogue.Archive",
                PermissionAction.Export => "Attachment.Catalogue.Export",
                PermissionAction.Import => "Attachment.Catalogue.Import",
                PermissionAction.ManagePermissions => "Attachment.Catalogue.ManagePermissions",
                PermissionAction.ManageConfiguration => "Attachment.Catalogue.ManageConfiguration",
                PermissionAction.ViewAuditLog => "Attachment.Catalogue.ViewAuditLog",
                PermissionAction.All => "Attachment.Catalogue", // 所有权限
                _ => "Attachment.Catalogue.View" // 默认权限
            };
        }

        /// <summary>
        /// 批量检查权限（支持继承权限检查）
        /// </summary>
        public async Task<Dictionary<Guid, bool>> CheckPermissionsAsync(
            List<AttachCatalogue> catalogues,
            PermissionAction action,
            Guid? userId = null,
            List<string>? userRoles = null)
        {
            var results = new Dictionary<Guid, bool>();
            var targetUserId = userId ?? _currentUser.Id;

            if (targetUserId == null)
            {
                foreach (var catalogue in catalogues)
                {
                    results[catalogue.Id] = false;
                }
                return results;
            }

            // 获取用户角色（如果未提供）
            var roles = userRoles?.ToArray() ?? await GetUserRolesAsync(targetUserId.Value);

            // 批量检查ABP系统权限
            var abpPermissionName = GetAbpPermissionName(action);
            var hasAbpPermission = await _permissionChecker.IsGrantedAsync(abpPermissionName);

            foreach (var catalogue in catalogues)
            {
                if (hasAbpPermission)
                {
                    results[catalogue.Id] = true;
                }
                else
                {
                    // 检查分类直接权限
                    var hasDirectPermission = CheckCataloguePermission(catalogue, targetUserId.Value, action, roles);
                    if (hasDirectPermission)
                    {
                        results[catalogue.Id] = true;
                    }
                    else
                    {
                        // 检查继承权限
                        results[catalogue.Id] = await CheckInheritedPermissionAsync(catalogue, targetUserId.Value, action, roles);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 获取用户角色
        /// 使用 ABP 框架的 IIdentityUserRepository 服务获取用户角色信息（可选依赖）
        /// </summary>
        private async Task<string[]> GetUserRolesAsync(Guid userId)
        {
            try
            {
                // 如果查询的是当前用户且已认证，优先使用 ICurrentUser.Roles（性能更好）
                if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                {
                    var roles = _currentUser.Roles?.ToArray() ?? [];
                    
                    // 如果 ICurrentUser.Roles 有值，直接返回
                    if (roles.Length > 0)
                    {
                        return roles;
                    }
                    
                    // 如果 ICurrentUser.Roles 为空，尝试从数据库获取完整角色信息
                }

                // 使用服务定位器模式获取 IIdentityUserRepository（可选依赖）
                var identityUserRepository = _serviceProvider.GetService<IIdentityUserRepository>();
                
                if (identityUserRepository == null)
                {
                    _logger.LogWarning(
                        "[IIdentityUserRepository]未注册服务！Identity 模块可能未安装或未配置。UserId={UserId}，将使用 ICurrentUser.Roles 作为回退方案。",
                        userId);
                    
                    // 回退到 ICurrentUser.Roles
                    if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                    {
                        return _currentUser.Roles?.ToArray() ?? [];
                    }
                    
                    return [];
                }

                // 使用 IIdentityUserRepository 获取完整的用户角色信息
                var user = await identityUserRepository.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("用户不存在: UserId={UserId}", userId);
                    return [];
                }

                var rolesList = await identityUserRepository.GetRolesAsync(userId);
                return [.. rolesList.Select(r => r.Name)];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取用户角色失败: UserId={UserId}", userId);
                
                // 发生异常时，尝试使用 ICurrentUser.Roles 作为后备方案
                if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                {
                    return _currentUser.Roles?.ToArray() ?? [];
                }
                
                return [];
            }
        }
    }
}
