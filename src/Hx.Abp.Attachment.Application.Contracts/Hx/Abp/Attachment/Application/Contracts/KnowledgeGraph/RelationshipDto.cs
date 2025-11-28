using Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph;

namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 关系数据传输对象
    /// </summary>
    public class RelationshipDto
    {
        /// <summary>
        /// 关系ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 源实体ID
        /// </summary>
        public Guid SourceEntityId { get; set; }

        /// <summary>
        /// 源实体类型
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
        /// 角色（用于抽象关系类型）
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 语义类型（用于抽象关系类型）
        /// </summary>
        public string? SemanticType { get; set; }

        /// <summary>
        /// 关系描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 关系权重
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// 扩展属性
        /// </summary>
        public Dictionary<string, object?>? Properties { get; set; } = [];

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    }
}

