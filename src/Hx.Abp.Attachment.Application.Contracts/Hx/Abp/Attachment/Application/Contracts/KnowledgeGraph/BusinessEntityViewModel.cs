namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 业务实体视图模型 - 通过Reference和ReferenceType关联的外部业务实体
    /// 支持项目、流程、合同、任务等多种业务类型（不包括部门，部门单独作为维度）
    /// </summary>
    public class BusinessEntityViewModel : KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 对应AttachCatalogue.Reference
        /// </summary>
        public string ReferenceId { get; set; } = string.Empty;

        /// <summary>
        /// 对应AttachCatalogue.ReferenceType
        /// </summary>
        public int ReferenceType { get; set; }

        /// <summary>
        /// 业务类型名称（如"Project"、"Process"、"Contract"等）
        /// </summary>
        public string BusinessType { get; set; } = string.Empty;

        /// <summary>
        /// 关联的分类数量（图谱统计字段，可选）
        /// </summary>
        public int CatalogueCount { get; set; }

        // 注意：业务专属属性通过关联外部业务系统获取，不在此处存储
    }
}

