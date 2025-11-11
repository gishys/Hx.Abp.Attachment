namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 分面类型枚举 - 标识模板的层级和用途
    /// 基于行业最佳实践，采用分面分类体系
    /// </summary>
    public enum FacetType
    {
        /// <summary>
        /// 通用分面 - 默认类型（默认静态）
        /// </summary>
        General = 0,

        /// <summary>
        /// 组织维度 - 公司、部门、团队等组织架构（默认动态）
        /// </summary>
        Organization = 1,

        /// <summary>
        /// 项目类型 - 项目分类、项目性质等（默认动态）
        /// </summary>
        ProjectType = 2,

        /// <summary>
        /// 阶段分面 - 项目阶段、生命周期等（默认动态）
        /// </summary>
        Phase = 3,

        /// <summary>
        /// 专业领域 - 技术专业、业务领域等（默认静态）
        /// </summary>
        Discipline = 4,

        /// <summary>
        /// 文档类型 - 文档分类、文件格式等（默认静态）
        /// </summary>
        DocumentType = 5,

        /// <summary>
        /// 时间切片 - 时间维度、时间周期等（默认动态）
        /// </summary>
        TimeSlice = 6,

        /// <summary>
        /// 业务自定义 - 特定业务场景的扩展分面（默认动态）
        /// </summary>
        Custom = 99
    }

    /// <summary>
    /// 分面默认实例化策略（静态/动态）的辅助类
    /// </summary>
    public static class FacetTypePolicies
    {
        /// <summary>
        /// 是否为默认静态分面（模板中的分类在实例中自动创建）
        /// </summary>
        public static bool IsStaticFacet(FacetType facetType)
        {
            return facetType switch
            {
                FacetType.General => true,
                FacetType.Discipline => true,
                FacetType.DocumentType => true,
                // 以下为默认动态
                FacetType.Organization => false,
                FacetType.ProjectType => false,
                FacetType.Phase => false,
                FacetType.TimeSlice => false,
                FacetType.Custom => false,
                _ => true
            };
        }
    }
}
