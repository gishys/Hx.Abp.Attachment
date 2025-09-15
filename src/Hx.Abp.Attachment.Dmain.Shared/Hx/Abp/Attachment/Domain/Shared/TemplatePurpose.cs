namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 模板用途枚举 - 标识模板的具体用途
    /// </summary>
    public enum TemplatePurpose
    {
        /// <summary>
        /// 分类管理
        /// </summary>
        Classification = 1,

        /// <summary>
        /// 文档管理
        /// </summary>
        Document = 2,

        /// <summary>
        /// 流程管理
        /// </summary>
        Workflow = 3,

        /// <summary>
        /// 权限管理
        /// </summary>
        Permission = 4,

        /// <summary>
        /// 档案管理
        /// </summary>
        Archive = 5,

        /// <summary>
        /// 其他用途
        /// </summary>
        Other = 99
    }
}
