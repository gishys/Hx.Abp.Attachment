namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 附件目录模板权限常量
    /// </summary>
    public static class AttachmentPermissions
    {
        public const string GroupName = "Attachment";

        public static class Templates
        {
            public const string Default = GroupName + ".Templates";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string View = Default + ".View";
            public const string Approve = Default + ".Approve";
            public const string Publish = Default + ".Publish";
            public const string Archive = Default + ".Archive";
            public const string Export = Default + ".Export";
            public const string Import = Default + ".Import";
            public const string ManagePermissions = Default + ".ManagePermissions";
            public const string ManageConfiguration = Default + ".ManageConfiguration";
            public const string ViewAuditLog = Default + ".ViewAuditLog";
        }

        public static class PermissionManagement
        {
            public const string Default = GroupName + ".PermissionManagement";
            public const string ManageRoles = Default + ".ManageRoles";
            public const string ManageUsers = Default + ".ManageUsers";
            public const string ManagePolicies = Default + ".ManagePolicies";
        }
    }
}
