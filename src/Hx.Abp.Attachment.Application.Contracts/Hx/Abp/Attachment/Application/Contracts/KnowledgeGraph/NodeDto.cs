namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 节点数据
    /// </summary>
    public class NodeDto
    {
        /// <summary>
        /// 实体ID（关联到现有实体表）
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// 实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 实体名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签列表
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 节点属性
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = [];

        /// <summary>
        /// 安全级别
        /// </summary>
        public string? SecurityLevel { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedTime { get; set; }
    }
}

