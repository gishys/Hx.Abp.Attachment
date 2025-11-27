namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 图查询输入参数
    /// </summary>
    public class GraphQueryInput
    {
        /// <summary>
        /// 中心实体ID（可选，以该实体为中心展开查询）
        /// </summary>
        public Guid? CenterEntityId { get; set; }

        /// <summary>
        /// 实体类型过滤（可选）
        /// </summary>
        public List<string>? EntityTypes { get; set; }

        /// <summary>
        /// 关系类型过滤（可选）
        /// </summary>
        public List<string>? RelationshipTypes { get; set; }

        /// <summary>
        /// 查询深度（默认 2）
        /// </summary>
        public int? Depth { get; set; } = 2;

        /// <summary>
        /// 最大节点数（默认 500）
        /// </summary>
        public int? MaxNodes { get; set; } = 500;

        /// <summary>
        /// 状态过滤（可选）
        /// </summary>
        public string? Status { get; set; }
    }
}

