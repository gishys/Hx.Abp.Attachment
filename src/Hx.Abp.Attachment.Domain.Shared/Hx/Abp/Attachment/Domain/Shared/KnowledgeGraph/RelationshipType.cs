namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 关系类型枚举（优化后 - 采用抽象、可扩展的设计）
    /// </summary>
    public enum RelationshipType
    {
        // ========== 分类与分类的关系（抽象化设计） ==========
        // 核心关系：通过 semanticType 属性描述关系的具体语义（时间、业务、版本等）
        CatalogueRelatesToCatalogue,     // 分类关联分类（通用关系，通过 semanticType 区分：时间、业务、版本、依赖等）
        CatalogueHasChild,                // 分类有子分类（树形结构，语义明确，保留独立类型）
        CatalogueReferencesBusiness,     // 分类引用业务实体

        // ========== 人员与分类的关系（抽象化设计） ==========
        // 核心关系：通过 role 属性描述人员的具体角色（项目经理、审核人、专家等）
        PersonRelatesToCatalogue,        // 人员关联分类（通用关系，通过 role 属性区分：创建、管理、项目经理、审核、专家、责任人、联系人、参与等）
        PersonBelongsToDepartment,       // 人员属于部门

        // ========== 部门相关 ==========
        DepartmentOwnsCatalogue,         // 部门拥有分类
        DepartmentManagesCatalogue,      // 部门管理分类
        DepartmentHasParent,             // 部门层级关系

        // ========== 业务实体相关 ==========
        BusinessEntityHasCatalogue,      // 业务实体有分类
        BusinessEntityManagesCatalogue,  // 业务实体管理分类

        // ========== 工作流相关 ==========
        CatalogueUsesWorkflow,           // 分类使用工作流（分类关联工作流模板）
        WorkflowManagesCatalogue,        // 工作流管理分类（工作流实例管理分类的生命周期）
        WorkflowInstanceBelongsToCatalogue, // 工作流实例属于分类（具体的工作流执行实例）
        PersonRelatesToWorkflow,         // 人员关联工作流（通用关系，通过 role 属性区分：管理、执行等）
        DepartmentOwnsWorkflow,          // 部门拥有工作流
        WorkflowRelatesToWorkflow        // 工作流关联工作流（通用关系，通过 semanticType 区分：版本、替换等）
    }
}

