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

        /// <summary>
        /// 知识图谱权限
        /// </summary>
        public static class KnowledgeGraph
        {
            public const string Default = GroupName + ".KnowledgeGraph";
            public const string View = Default + ".View";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string Query = Default + ".Query";
            public const string Analyze = Default + ".Analyze";
            public const string Export = Default + ".Export";
        }

        /// <summary>
        /// 智能采集权限
        /// </summary>
        public static class IntelligentCollection
        {
            public const string Default = GroupName + ".IntelligentCollection";
            public const string View = Default + ".View";
            public const string Create = Default + ".Create";
            public const string Edit = Default + ".Edit";
            public const string Delete = Default + ".Delete";
            public const string Execute = Default + ".Execute";
            public const string Configure = Default + ".Configure";
            public const string Monitor = Default + ".Monitor";
        }

        /// <summary>
        /// 档案查询权限
        /// </summary>
        public static class ArchiveQuery
        {
            public const string Default = GroupName + ".ArchiveQuery";
            public const string View = Default + ".View";
            public const string Query = Default + ".Query";
            public const string AdvancedQuery = Default + ".AdvancedQuery";
            public const string Export = Default + ".Export";
            public const string Statistics = Default + ".Statistics";
        }

        /// <summary>
        /// 数据驾驶舱权限
        /// </summary>
        public static class DataDashboard
        {
            public const string Default = GroupName + ".DataDashboard";
            public const string View = Default + ".View";
            public const string Configure = Default + ".Configure";
            public const string Export = Default + ".Export";
            public const string ManageWidgets = Default + ".ManageWidgets";
        }

        /// <summary>
        /// 人工校验台权限
        /// </summary>
        public static class ManualVerification
        {
            public const string Default = GroupName + ".ManualVerification";
            public const string View = Default + ".View";
            public const string Verify = Default + ".Verify";
            public const string Approve = Default + ".Approve";
            public const string Reject = Default + ".Reject";
            public const string BatchProcess = Default + ".BatchProcess";
            public const string Statistics = Default + ".Statistics";
        }

        /// <summary>
        /// 系统设置权限
        /// </summary>
        public static class SystemSettings
        {
            public const string Default = GroupName + ".SystemSettings";
            public const string View = Default + ".View";
            public const string Configure = Default + ".Configure";
            public const string ManageUsers = Default + ".ManageUsers";
            public const string ManageRoles = Default + ".ManageRoles";
            public const string ManagePermissions = Default + ".ManagePermissions";
            public const string SystemMaintenance = Default + ".SystemMaintenance";
            public const string ViewLogs = Default + ".ViewLogs";
            public const string BackupRestore = Default + ".BackupRestore";
        }
    }
}
