using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 附件管理权限定义提供者
    /// 基于ABP vNext权限系统的最佳实践实现
    /// </summary>
    public class AttachmentPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            // 创建附件管理权限组
            var attachmentGroup = context.AddGroup(
                "Attachment",
                L("Permission:Attachment")
            );

            // 分类管理权限
            var cataloguePermission = attachmentGroup.AddPermission(
                "Attachment.Catalogue",
                L("Permission:Attachment.Catalogue"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类查看权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.View",
                L("Permission:Attachment.Catalogue.View"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类创建权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Create",
                L("Permission:Attachment.Catalogue.Create"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类编辑权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Edit",
                L("Permission:Attachment.Catalogue.Edit"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类删除权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Delete",
                L("Permission:Attachment.Catalogue.Delete"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类审批权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Approve",
                L("Permission:Attachment.Catalogue.Approve"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类发布权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Publish",
                L("Permission:Attachment.Catalogue.Publish"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类归档权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Archive",
                L("Permission:Attachment.Catalogue.Archive"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类导出权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Export",
                L("Permission:Attachment.Catalogue.Export"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类导入权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.Import",
                L("Permission:Attachment.Catalogue.Import"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类权限管理权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.ManagePermissions",
                L("Permission:Attachment.Catalogue.ManagePermissions"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类配置管理权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.ManageConfiguration",
                L("Permission:Attachment.Catalogue.ManageConfiguration"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 分类审计日志查看权限
            cataloguePermission.AddChild(
                "Attachment.Catalogue.ViewAuditLog",
                L("Permission:Attachment.Catalogue.ViewAuditLog"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 文件管理权限
            var filePermission = attachmentGroup.AddPermission(
                "Attachment.File",
                L("Permission:Attachment.File"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 文件上传权限
            filePermission.AddChild(
                "Attachment.File.Upload",
                L("Permission:Attachment.File.Upload"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 文件下载权限
            filePermission.AddChild(
                "Attachment.File.Download",
                L("Permission:Attachment.File.Download"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );

            // 文件删除权限
            filePermission.AddChild(
                "Attachment.File.Delete",
                L("Permission:Attachment.File.Delete"),
                multiTenancySide: Volo.Abp.MultiTenancy.MultiTenancySides.Both
            );
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AttachmentResource>(name);
        }
    }

    /// <summary>
    /// 附件管理资源（用于本地化）
    /// </summary>
    public class AttachmentResource
    {
    }
}
