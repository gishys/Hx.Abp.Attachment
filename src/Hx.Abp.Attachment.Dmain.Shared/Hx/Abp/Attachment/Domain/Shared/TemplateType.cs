namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 模板类型枚举 - 标识模板的层级和用途
    /// </summary>
    public enum TemplateType
    {
        /// <summary>
        /// 项目级模板
        /// </summary>
        Project = 1,

        /// <summary>
        /// 阶段级模板
        /// </summary>
        Phase = 2,

        /// <summary>
        /// 业务分类模板
        /// </summary>
        BusinessCategory = 3,

        /// <summary>
        /// 专业领域模板
        /// </summary>
        Professional = 4,

        /// <summary>
        /// 通用模板
        /// </summary>
        General = 99
    }
}
