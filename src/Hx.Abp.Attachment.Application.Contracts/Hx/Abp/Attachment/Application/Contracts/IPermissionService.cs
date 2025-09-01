using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 权限服务接口 - 基于ABP vNext权限系统简化实现
    /// </summary>
    public interface IPermissionService : IApplicationService
    {
        /// <summary>
        /// 检查用户是否具有指定模板的指定权限
        /// </summary>
        Task<bool> HasPermissionAsync(Guid userId, Guid templateId, PermissionAction action);

        /// <summary>
        /// 批量检查用户权限
        /// </summary>
        Task<Dictionary<Guid, bool>> HasPermissionsAsync(Guid userId, List<Guid> templateIds, PermissionAction action);

        /// <summary>
        /// 获取模板的权限配置
        /// </summary>
        Task<List<AttachCatalogueTemplatePermissionResultDto>> GetTemplatePermissionsAsync(Guid templateId);

        /// <summary>
        /// 设置模板权限
        /// </summary>
        Task SetTemplatePermissionsAsync(Guid templateId, List<CreateAttachCatalogueTemplatePermissionDto> permissions);

        /// <summary>
        /// 添加模板权限
        /// </summary>
        Task AddTemplatePermissionAsync(Guid templateId, CreateAttachCatalogueTemplatePermissionDto permission);

        /// <summary>
        /// 更新模板权限
        /// </summary>
        Task UpdateTemplatePermissionAsync(Guid templateId, Guid permissionId, UpdateAttachCatalogueTemplatePermissionDto permission);

        /// <summary>
        /// 移除模板权限
        /// </summary>
        Task RemoveTemplatePermissionAsync(Guid templateId, Guid permissionId);

        /// <summary>
        /// 获取模板权限摘要
        /// </summary>
        Task<string> GetTemplatePermissionSummaryAsync(Guid templateId);

        /// <summary>
        /// 检查权限冲突
        /// </summary>
        Task<List<string>> CheckPermissionConflictsAsync(Guid templateId);
    }
}
