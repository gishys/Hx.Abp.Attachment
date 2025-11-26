namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 人员实体视图模型 - 通过审计字段关联（CreatorId等）
    /// 简化定义，只包含图谱查询需要的字段
    /// </summary>
    public class PersonEntityViewModel : KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 员工ID（关联到用户系统）
        /// </summary>
        public string EmployeeId { get; set; } = string.Empty;

        /// <summary>
        /// 关联部门ID
        /// </summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>
        /// 创建的分类数量（图谱统计字段，可选）
        /// </summary>
        public int CreatedCatalogueCount { get; set; }

        // 注意：其他字段（Position、Email、Phone等）通过关联用户系统获取
    }
}

