using Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph;
using Volo.Abp.Domain.Entities.Auditing;

namespace Hx.Abp.Attachment.Domain.KnowledgeGraph
{
    /// <summary>
    /// 知识图谱关系实体
    /// 注意：关系数据存储在kg_relationships表中，通过entity_id关联到现有实体表
    /// 继承 CreationAuditedAggregateRoot 作为聚合根实体，包含创建审计、扩展属性和乐观锁字段
    /// </summary>
    public class KnowledgeGraphRelationship : CreationAuditedAggregateRoot<Guid>
    {

        /// <summary>
        /// 源实体ID（关联到现有实体表，如 AttachCatalogue.Id）
        /// </summary>
        public Guid SourceEntityId { get; set; }

        /// <summary>
        /// 源实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
        /// </summary>
        public string SourceEntityType { get; set; } = string.Empty;

        /// <summary>
        /// 目标实体ID（关联到现有实体表）
        /// </summary>
        public Guid TargetEntityId { get; set; }

        /// <summary>
        /// 目标实体类型
        /// </summary>
        public string TargetEntityType { get; set; } = string.Empty;

        /// <summary>
        /// 关系类型
        /// </summary>
        public RelationshipType Type { get; set; }

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关系语义属性（用于抽象关系类型的具体语义描述）
        /// 角色（用于 PersonRelatesToCatalogue、PersonRelatesToWorkflow 等）
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等）
        /// </summary>
        public string? SemanticType { get; set; }

        /// <summary>
        /// 关系权重（用于影响分析）
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// 辅助方法：获取关系的显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return Type switch
            {
                RelationshipType.PersonRelatesToCatalogue => Role != null
                    ? $"人员-分类关系({Role})"
                    : "人员-分类关系",
                RelationshipType.CatalogueRelatesToCatalogue => SemanticType != null
                    ? $"分类-分类关系({SemanticType})"
                    : "分类-分类关系",
                RelationshipType.PersonRelatesToWorkflow => Role != null
                    ? $"人员-工作流关系({Role})"
                    : "人员-工作流关系",
                RelationshipType.WorkflowRelatesToWorkflow => SemanticType != null
                    ? $"工作流-工作流关系({SemanticType})"
                    : "工作流-工作流关系",
                _ => Type.ToString()
            };
        }
    }
}
