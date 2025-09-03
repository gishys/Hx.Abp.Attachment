namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 分面类型枚举 - 标识模板的层级和用途
    /// 基于行业最佳实践，采用分面分类体系
    /// </summary>
    public enum FacetType
    {
        /// <summary>
        /// 通用分面 - 默认类型
        /// </summary>
        General = 0,

        /// <summary>
        /// 组织维度 - 公司、部门、团队等组织架构
        /// </summary>
        Organization = 1,

        /// <summary>
        /// 项目类型 - 项目分类、项目性质等
        /// </summary>
        ProjectType = 2,

        /// <summary>
        /// 阶段分面 - 项目阶段、生命周期等
        /// </summary>
        Phase = 3,

        /// <summary>
        /// 专业领域 - 技术专业、业务领域等
        /// </summary>
        Discipline = 4,

        /// <summary>
        /// 文档类型 - 文档分类、文件格式等
        /// </summary>
        DocumentType = 5,

        /// <summary>
        /// 时间切片 - 时间维度、时间周期等
        /// </summary>
        TimeSlice = 6,

        /// <summary>
        /// 业务自定义 - 特定业务场景的扩展分面
        /// </summary>
        Custom = 99
    }
}
