namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 权限操作枚举 - 定义系统支持的所有权限操作
    /// </summary>
    public enum PermissionAction
    {
        /// <summary>
        /// 查看
        /// </summary>
        View = 1,

        /// <summary>
        /// 创建
        /// </summary>
        Create = 2,

        /// <summary>
        /// 编辑
        /// </summary>
        Edit = 3,

        /// <summary>
        /// 删除
        /// </summary>
        Delete = 4,

        /// <summary>
        /// 审批
        /// </summary>
        Approve = 5,

        /// <summary>
        /// 发布
        /// </summary>
        Publish = 6,

        /// <summary>
        /// 归档
        /// </summary>
        Archive = 7,

        /// <summary>
        /// 导出
        /// </summary>
        Export = 8,

        /// <summary>
        /// 导入
        /// </summary>
        Import = 9,

        /// <summary>
        /// 管理权限
        /// </summary>
        ManagePermissions = 10,

        /// <summary>
        /// 管理配置
        /// </summary>
        ManageConfiguration = 11,

        /// <summary>
        /// 查看审计日志
        /// </summary>
        ViewAuditLog = 12,

        /// <summary>
        /// 所有权限
        /// </summary>
        All = 99
    }
}
