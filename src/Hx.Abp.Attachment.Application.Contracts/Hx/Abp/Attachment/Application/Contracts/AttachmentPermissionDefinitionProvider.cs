using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

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

            // 分类管理权限
            var cataloguePermission = attachmentGroup.AddPermission(
                "Attachment.Catalogue",
                L("Permission:Attachment.Catalogue"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.View",
                L("Permission:Attachment.Catalogue.View"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Create",
                L("Permission:Attachment.Catalogue.Create"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Edit",
                L("Permission:Attachment.Catalogue.Edit"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Delete",
                L("Permission:Attachment.Catalogue.Delete"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Approve",
                L("Permission:Attachment.Catalogue.Approve"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Publish",
                L("Permission:Attachment.Catalogue.Publish"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Archive",
                L("Permission:Attachment.Catalogue.Archive"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Export",
                L("Permission:Attachment.Catalogue.Export"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.Import",
                L("Permission:Attachment.Catalogue.Import"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.ManagePermissions",
                L("Permission:Attachment.Catalogue.ManagePermissions"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.ManageConfiguration",
                L("Permission:Attachment.Catalogue.ManageConfiguration"),
                multiTenancySide: MultiTenancySides.Both
            );

            cataloguePermission.AddChild(
                "Attachment.Catalogue.ViewAuditLog",
                L("Permission:Attachment.Catalogue.ViewAuditLog"),
                multiTenancySide: MultiTenancySides.Both
            );

            // 文件管理权限
            var filePermission = attachmentGroup.AddPermission(
                "Attachment.File",
                L("Permission:Attachment.File"),
                multiTenancySide: MultiTenancySides.Both
            );

            filePermission.AddChild(
                "Attachment.File.Upload",
                L("Permission:Attachment.File.Upload"),
                multiTenancySide: MultiTenancySides.Both
            );

            filePermission.AddChild(
                "Attachment.File.Download",
                L("Permission:Attachment.File.Download"),
                multiTenancySide: MultiTenancySides.Both
            );

            filePermission.AddChild(
                "Attachment.File.Delete",
                L("Permission:Attachment.File.Delete"),
                multiTenancySide: MultiTenancySides.Both
            );

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

            // 知识图谱权限
            var knowledgeGraphPermission = attachmentGroup.AddPermission(AttachmentPermissions.KnowledgeGraph.Default, L("Permission:KnowledgeGraph"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.View, L("Permission:KnowledgeGraph.View"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Create, L("Permission:KnowledgeGraph.Create"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Edit, L("Permission:KnowledgeGraph.Edit"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Delete, L("Permission:KnowledgeGraph.Delete"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Query, L("Permission:KnowledgeGraph.Query"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Analyze, L("Permission:KnowledgeGraph.Analyze"), multiTenancySide: MultiTenancySides.Both);
            knowledgeGraphPermission.AddChild(AttachmentPermissions.KnowledgeGraph.Export, L("Permission:KnowledgeGraph.Export"), multiTenancySide: MultiTenancySides.Both);

            // 智能采集权限
            var intelligentCollectionPermission = attachmentGroup.AddPermission(AttachmentPermissions.IntelligentCollection.Default, L("Permission:IntelligentCollection"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.View, L("Permission:IntelligentCollection.View"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Create, L("Permission:IntelligentCollection.Create"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Edit, L("Permission:IntelligentCollection.Edit"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Delete, L("Permission:IntelligentCollection.Delete"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Execute, L("Permission:IntelligentCollection.Execute"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Configure, L("Permission:IntelligentCollection.Configure"), multiTenancySide: MultiTenancySides.Both);
            intelligentCollectionPermission.AddChild(AttachmentPermissions.IntelligentCollection.Monitor, L("Permission:IntelligentCollection.Monitor"), multiTenancySide: MultiTenancySides.Both);

            // 档案查询权限
            var archiveQueryPermission = attachmentGroup.AddPermission(AttachmentPermissions.ArchiveQuery.Default, L("Permission:ArchiveQuery"), multiTenancySide: MultiTenancySides.Both);
            archiveQueryPermission.AddChild(AttachmentPermissions.ArchiveQuery.View, L("Permission:ArchiveQuery.View"), multiTenancySide: MultiTenancySides.Both);
            archiveQueryPermission.AddChild(AttachmentPermissions.ArchiveQuery.Query, L("Permission:ArchiveQuery.Query"), multiTenancySide: MultiTenancySides.Both);
            archiveQueryPermission.AddChild(AttachmentPermissions.ArchiveQuery.AdvancedQuery, L("Permission:ArchiveQuery.AdvancedQuery"), multiTenancySide: MultiTenancySides.Both);
            archiveQueryPermission.AddChild(AttachmentPermissions.ArchiveQuery.Export, L("Permission:ArchiveQuery.Export"), multiTenancySide: MultiTenancySides.Both);
            archiveQueryPermission.AddChild(AttachmentPermissions.ArchiveQuery.Statistics, L("Permission:ArchiveQuery.Statistics"), multiTenancySide: MultiTenancySides.Both);

            // 数据驾驶舱权限
            var dataDashboardPermission = attachmentGroup.AddPermission(AttachmentPermissions.DataDashboard.Default, L("Permission:DataDashboard"), multiTenancySide: MultiTenancySides.Both);
            dataDashboardPermission.AddChild(AttachmentPermissions.DataDashboard.View, L("Permission:DataDashboard.View"), multiTenancySide: MultiTenancySides.Both);
            dataDashboardPermission.AddChild(AttachmentPermissions.DataDashboard.Configure, L("Permission:DataDashboard.Configure"), multiTenancySide: MultiTenancySides.Both);
            dataDashboardPermission.AddChild(AttachmentPermissions.DataDashboard.Export, L("Permission:DataDashboard.Export"), multiTenancySide: MultiTenancySides.Both);
            dataDashboardPermission.AddChild(AttachmentPermissions.DataDashboard.ManageWidgets, L("Permission:DataDashboard.ManageWidgets"), multiTenancySide: MultiTenancySides.Both);

            // 人工校验台权限
            var manualVerificationPermission = attachmentGroup.AddPermission(AttachmentPermissions.ManualVerification.Default, L("Permission:ManualVerification"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.View, L("Permission:ManualVerification.View"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.Verify, L("Permission:ManualVerification.Verify"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.Approve, L("Permission:ManualVerification.Approve"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.Reject, L("Permission:ManualVerification.Reject"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.BatchProcess, L("Permission:ManualVerification.BatchProcess"), multiTenancySide: MultiTenancySides.Both);
            manualVerificationPermission.AddChild(AttachmentPermissions.ManualVerification.Statistics, L("Permission:ManualVerification.Statistics"), multiTenancySide: MultiTenancySides.Both);

            // 系统设置权限
            var systemSettingsPermission = attachmentGroup.AddPermission(AttachmentPermissions.SystemSettings.Default, L("Permission:SystemSettings"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.View, L("Permission:SystemSettings.View"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.Configure, L("Permission:SystemSettings.Configure"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.ManageUsers, L("Permission:SystemSettings.ManageUsers"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.ManageRoles, L("Permission:SystemSettings.ManageRoles"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.ManagePermissions, L("Permission:SystemSettings.ManagePermissions"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.SystemMaintenance, L("Permission:SystemSettings.SystemMaintenance"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.ViewLogs, L("Permission:SystemSettings.ViewLogs"), multiTenancySide: MultiTenancySides.Both);
            systemSettingsPermission.AddChild(AttachmentPermissions.SystemSettings.BackupRestore, L("Permission:SystemSettings.BackupRestore"), multiTenancySide: MultiTenancySides.Both);
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<AttachmentLocalizationResource>(name);
        }
    }
}
