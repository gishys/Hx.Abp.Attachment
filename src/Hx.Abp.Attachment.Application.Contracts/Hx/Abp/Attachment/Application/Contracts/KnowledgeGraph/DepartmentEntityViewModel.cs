namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 部门实体视图模型 - 组织维度
    /// 表示分类所属的部门，通过Reference和ReferenceType关联
    /// </summary>
    public class DepartmentEntityViewModel : KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 部门编码
        /// </summary>
        public string DepartmentCode { get; set; } = string.Empty;

        /// <summary>
        /// 父部门ID（用于层级结构）
        /// </summary>
        public Guid? ParentDepartmentId { get; set; }

        /// <summary>
        /// 关联的分类数量（图谱统计字段，可选）
        /// </summary>
        public int CatalogueCount { get; set; }

        /// <summary>
        /// 部门人员数量（图谱统计字段，可选）
        /// </summary>
        public int PersonCount { get; set; }

        // 注意：其他字段（DepartmentName、ManagerId等）通过关联组织系统获取
    }
}

