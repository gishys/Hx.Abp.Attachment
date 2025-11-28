using Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph;

namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 创建关系输入参数
    /// </summary>
    public class CreateRelationshipInput
    {
        /// <summary>
        /// 源实体ID
        /// </summary>
        public Guid SourceEntityId { get; set; }

        /// <summary>
        /// 源实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
        /// </summary>
        public string SourceEntityType { get; set; } = string.Empty;

        /// <summary>
        /// 目标实体ID
        /// </summary>
        public Guid TargetEntityId { get; set; }

        /// <summary>
        /// 目标实体类型
        /// </summary>
        public string TargetEntityType { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型
        /// </summary>
        public RelationshipType RelationshipType { get; set; }

        /// <summary>
        /// 角色（用于 PersonRelatesToCatalogue、PersonRelatesToWorkflow 等抽象关系类型）
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等抽象关系类型）
        /// </summary>
        public string? SemanticType { get; set; }

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关系权重（默认 1.0）
        /// </summary>
        public double? Weight { get; set; }

        /// <summary>
        /// 扩展属性
        /// </summary>
        public Dictionary<string, object>? Properties { get; set; }
    }
}

