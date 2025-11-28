namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 权限效果枚举 - 定义权限的允许或拒绝效果
    /// </summary>
    public enum PermissionEffect
    {
        /// <summary>
        /// 允许
        /// </summary>
        Allow = 1,

        /// <summary>
        /// 拒绝
        /// </summary>
        Deny = 2,

        /// <summary>
        /// 继承（从父节点继承）
        /// </summary>
        Inherit = 3
    }
}
