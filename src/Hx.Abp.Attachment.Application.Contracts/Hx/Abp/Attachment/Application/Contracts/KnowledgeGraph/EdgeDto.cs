namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 边数据
    /// </summary>
    public class EdgeDto
    {
        /// <summary>
        /// 源节点ID
        /// </summary>
        public Guid Source { get; set; }

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public Guid Target { get; set; }

        /// <summary>
        /// 关系类型
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 角色（用于抽象关系类型）
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// 语义类型（用于抽象关系类型）
        /// </summary>
        public string? SemanticType { get; set; }

        /// <summary>
        /// 关系权重
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// 关系属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = [];
    }
}

