namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 工作流实体视图模型 - 流程维度
    /// 管理档案的创建、审核、归档等业务流程，定义分类的生命周期和审批流程
    /// </summary>
    public class WorkflowEntityViewModel : KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 工作流编码（唯一标识）
        /// </summary>
        public string WorkflowCode { get; set; } = string.Empty;

        /// <summary>
        /// 工作流类型（创建审批、归档审批、销毁审批等）
        /// </summary>
        public string WorkflowType { get; set; } = string.Empty;

        /// <summary>
        /// 工作流状态（ACTIVE, ARCHIVED, DISABLED）
        /// </summary>
        public new string Status { get; set; } = "ACTIVE";

        /// <summary>
        /// 模板定义Id（关联到工作流模板定义）
        /// </summary>
        public Guid? TemplateDefinitionId { get; set; }

        /// <summary>
        /// 模板定义版本（工作流模板定义的版本号）
        /// </summary>
        public int TemplateDefinitionVersion { get; set; }

        /// <summary>
        /// 拥有部门ID
        /// </summary>
        public Guid? OwnerDepartmentId { get; set; }

        /// <summary>
        /// 管理员人员ID
        /// </summary>
        public Guid? ManagerPersonId { get; set; }

        /// <summary>
        /// 关联的分类数量（使用该工作流的分类数，图谱统计字段，可选）
        /// </summary>
        public int CatalogueCount { get; set; }

        /// <summary>
        /// 工作流实例数量（图谱统计字段，可选）
        /// </summary>
        public int InstanceCount { get; set; }

        /// <summary>
        /// 活跃实例数量（图谱统计字段，可选）
        /// </summary>
        public int ActiveInstanceCount { get; set; }

        // 注意：工作流的详细定义（节点、边、条件等）通过关联工作流引擎获取，不在此处存储
    }
}

