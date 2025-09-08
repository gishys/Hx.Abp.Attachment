using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization.Permissions;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 权限服务实现 - 基于ABP vNext权限系统简化实现
    /// </summary>
    public class PermissionService(
        IAttachCatalogueTemplateRepository templateRepository,
        IPermissionChecker permissionChecker,
        ILogger<PermissionService> logger) : ApplicationService, IPermissionService
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IPermissionChecker _permissionChecker = permissionChecker;
        private readonly ILogger<PermissionService> _logger = logger;

        public async Task<bool> HasPermissionAsync(Guid userId, Guid templateId, PermissionAction action)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId);
                if (template == null) return false;

                // 首先检查ABP内置权限
                var permissionName = GetPermissionName(action);
                if (await _permissionChecker.IsGrantedAsync(permissionName))
                {
                    return true;
                }

                // 然后检查模板特定的权限配置
                return template.HasPermission(userId, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查权限时发生错误");
                return false;
            }
        }

        public async Task<Dictionary<Guid, bool>> HasPermissionsAsync(Guid userId, List<Guid> templateIds, PermissionAction action)
        {
            var results = new Dictionary<Guid, bool>();
            try
            {
                var templates = await _templateRepository.GetListAsync(t => templateIds.Contains(t.Id));
                var permissionName = GetPermissionName(action);

                foreach (var templateId in templateIds)
                {
                    var template = templates.FirstOrDefault(t => t.Id == templateId);
                    if (template != null)
                    {
                        // 检查ABP内置权限
                        var hasBuiltInPermission = await _permissionChecker.IsGrantedAsync(permissionName);
                        if (hasBuiltInPermission)
                        {
                            results[templateId] = true;
                            continue;
                        }

                        // 检查模板特定权限
                        results[templateId] = template.HasPermission(userId, action);
                    }
                    else
                    {
                        results[templateId] = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量检查权限时发生错误");
                foreach (var templateId in templateIds)
                {
                    results[templateId] = false;
                }
            }
            return results;
        }

        public async Task<List<AttachCatalogueTemplatePermissionResultDto>> GetTemplatePermissionsAsync(Guid templateId)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"模板不存在: {templateId}");
                var result = new List<AttachCatalogueTemplatePermissionResultDto>();
                
                if (template.Permissions != null)
                {
                    foreach (var permission in template.Permissions)
                    {
                        result.Add(MapToResultDto(permission));
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板权限时发生错误");
                throw;
            }
        }

        public async Task SetTemplatePermissionsAsync(Guid templateId, List<CreateAttachCatalogueTemplatePermissionDto> permissions)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"模板不存在: {templateId}");

                // 清空现有权限
                template.Permissions?.Clear();
                
                // 添加新权限
                if (permissions != null)
                {
                    foreach (var permissionDto in permissions)
                    {
                        var permission = MapToDomainEntity(permissionDto);
                        template.AddPermission(permission);
                    }
                }

                await _templateRepository.UpdateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置模板权限时发生错误");
                throw;
            }
        }

        public async Task AddTemplatePermissionAsync(Guid templateId, CreateAttachCatalogueTemplatePermissionDto permissionDto)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"模板不存在: {templateId}");
                var permission = MapToDomainEntity(permissionDto);
                template.AddPermission(permission);
                await _templateRepository.UpdateAsync(template);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加模板权限时发生错误");
                throw;
            }
        }

        public async Task UpdateTemplatePermissionAsync(Guid templateId, Guid permissionId, UpdateAttachCatalogueTemplatePermissionDto permissionDto)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"模板不存在: {templateId}");

                // 查找并更新权限
                var existingPermission = template.Permissions?.FirstOrDefault(p => p.GetHashCode() == permissionId.GetHashCode());
                if (existingPermission != null)
                {
                    // 移除旧权限
                    template.RemovePermission(existingPermission);
                    
                    // 添加新权限
                    var updatedPermission = MapToDomainEntity(permissionDto);
                    template.AddPermission(updatedPermission);
                    
                    await _templateRepository.UpdateAsync(template);
                }
                else
                {
                    throw new UserFriendlyException($"权限不存在: {permissionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板权限时发生错误");
                throw;
            }
        }

        public async Task RemoveTemplatePermissionAsync(Guid templateId, Guid permissionId)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId) ?? throw new UserFriendlyException($"模板不存在: {templateId}");

                // 查找并移除权限
                var permission = template.Permissions?.FirstOrDefault(p => p.GetHashCode() == permissionId.GetHashCode());
                if (permission != null)
                {
                    template.RemovePermission(permission);
                    await _templateRepository.UpdateAsync(template);
                }
                else
                {
                    throw new UserFriendlyException($"权限不存在: {permissionId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移除模板权限时发生错误");
                throw;
            }
        }

        public async Task<string> GetTemplatePermissionSummaryAsync(Guid templateId)
        {
            try
            {
                var template = await _templateRepository.GetLatestVersionAsync(templateId);
                return template == null ? throw new UserFriendlyException($"模板不存在: {templateId}") : template.GetPermissionSummary();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板权限摘要时发生错误");
                throw;
            }
        }

        public async Task<List<string>> CheckPermissionConflictsAsync(Guid templateId)
        {
            try
            {
                var conflicts = new List<string>();
                var permissions = await GetTemplatePermissionsAsync(templateId);

                // 检查权限冲突逻辑
                var groupedPermissions = permissions
                    .Where(p => p.IsEffective)
                    .GroupBy(p => new { p.PermissionType, p.Action, p.PermissionTarget })
                    .Where(g => g.Count() > 1);

                foreach (var group in groupedPermissions)
                {
                    conflicts.Add($"发现权限冲突: {group.Key.PermissionType} - {group.Key.Action} - {group.Key.PermissionTarget}");
                }

                return conflicts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查权限冲突时发生错误");
                return [$"检查权限冲突时发生错误: {ex.Message}"];
            }
        }

        /// <summary>
        /// 根据权限操作获取ABP权限名称
        /// </summary>
        private static string GetPermissionName(PermissionAction action)
        {
            return action switch
            {
                PermissionAction.View => "Attachment.Templates.View",
                PermissionAction.Create => "Attachment.Templates.Create",
                PermissionAction.Edit => "Attachment.Templates.Edit",
                PermissionAction.Delete => "Attachment.Templates.Delete",
                PermissionAction.Approve => "Attachment.Templates.Approve",
                PermissionAction.Publish => "Attachment.Templates.Publish",
                PermissionAction.Archive => "Attachment.Templates.Archive",
                PermissionAction.Export => "Attachment.Templates.Export",
                PermissionAction.Import => "Attachment.Templates.Import",
                PermissionAction.ManagePermissions => "Attachment.Templates.ManagePermissions",
                PermissionAction.ManageConfiguration => "Attachment.Templates.ManageConfiguration",
                PermissionAction.ViewAuditLog => "Attachment.Templates.ViewAuditLog",
                _ => "Attachment.Templates.View"
            };
        }

        /// <summary>
        /// 将DTO映射到领域实体
        /// </summary>
        private static AttachCatalogueTemplatePermission MapToDomainEntity(CreateAttachCatalogueTemplatePermissionDto dto)
        {
            return new AttachCatalogueTemplatePermission(
                dto.PermissionType,
                dto.PermissionTarget,
                dto.Action,
                dto.Effect,
                dto.AttributeConditions,
                dto.EffectiveTime,
                dto.ExpirationTime,
                dto.Description
            );
        }

        /// <summary>
        /// 将DTO映射到领域实体
        /// </summary>
        private static AttachCatalogueTemplatePermission MapToDomainEntity(UpdateAttachCatalogueTemplatePermissionDto dto)
        {
            return new AttachCatalogueTemplatePermission(
                dto.PermissionType,
                dto.PermissionTarget,
                dto.Action,
                dto.Effect,
                dto.AttributeConditions,
                dto.EffectiveTime,
                dto.ExpirationTime,
                dto.Description
            );
        }

        /// <summary>
        /// 将领域实体映射到结果DTO
        /// </summary>
        private static AttachCatalogueTemplatePermissionResultDto MapToResultDto(AttachCatalogueTemplatePermission permission)
        {
            return new AttachCatalogueTemplatePermissionResultDto
            {
                PermissionType = permission.PermissionType,
                PermissionTarget = permission.PermissionTarget,
                Action = permission.Action,
                Effect = permission.Effect,
                AttributeConditions = permission.AttributeConditions,
                IsEnabled = permission.IsEnabled,
                EffectiveTime = permission.EffectiveTime,
                ExpirationTime = permission.ExpirationTime,
                Description = permission.Description,
                IsEffective = permission.IsEffective(),
                PermissionIdentifier = permission.GetPermissionIdentifier(),
                PermissionSummary = permission.GetPermissionSummary()
            };
        }
    }
}
