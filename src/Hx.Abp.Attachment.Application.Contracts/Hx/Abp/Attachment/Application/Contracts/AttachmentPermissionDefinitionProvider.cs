using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 附件目录模板权限定义提供者
    /// </summary>
    public class AttachmentPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var attachmentGroup = context.AddGroup(AttachmentPermissions.GroupName, L("Permission:Attachment"));

            // 模板管理权限
            var templatePermission = attachmentGroup.AddPermission(AttachmentPermissions.Templates.Default, L("Permission:Templates"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Create, L("Permission:Templates.Create"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Edit, L("Permission:Templates.Edit"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Delete, L("Permission:Templates.Delete"));
            templatePermission.AddChild(AttachmentPermissions.Templates.View, L("Permission:Templates.View"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Approve, L("Permission:Templates.Approve"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Publish, L("Permission:Templates.Publish"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Archive, L("Permission:Templates.Archive"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Export, L("Permission:Templates.Export"));
            templatePermission.AddChild(AttachmentPermissions.Templates.Import, L("Permission:Templates.Import"));
            templatePermission.AddChild(AttachmentPermissions.Templates.ManagePermissions, L("Permission:Templates.ManagePermissions"));
            templatePermission.AddChild(AttachmentPermissions.Templates.ManageConfiguration, L("Permission:Templates.ManageConfiguration"));
            templatePermission.AddChild(AttachmentPermissions.Templates.ViewAuditLog, L("Permission:Templates.ViewAuditLog"));

            // 权限管理权限
            var permissionManagement = attachmentGroup.AddPermission(AttachmentPermissions.PermissionManagement.Default, L("Permission:PermissionManagement"));
            permissionManagement.AddChild(AttachmentPermissions.PermissionManagement.ManageRoles, L("Permission:PermissionManagement.ManageRoles"));
            permissionManagement.AddChild(AttachmentPermissions.PermissionManagement.ManageUsers, L("Permission:PermissionManagement.ManageUsers"));
            permissionManagement.AddChild(AttachmentPermissions.PermissionManagement.ManagePolicies, L("Permission:PermissionManagement.ManagePolicies"));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AttachmentResource>(name);
        }
    }
}
