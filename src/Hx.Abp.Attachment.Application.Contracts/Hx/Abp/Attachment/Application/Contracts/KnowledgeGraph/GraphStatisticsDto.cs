namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 统计信息
    /// </summary>
    public class GraphStatisticsDto
    {
        /// <summary>
        /// 总节点数
        /// </summary>
        public int TotalNodes { get; set; }

        /// <summary>
        /// 总边数
        /// </summary>
        public int TotalEdges { get; set; }

        /// <summary>
        /// 节点类型统计
        /// </summary>
        public Dictionary<string, int> NodeTypes { get; set; } = [];

        /// <summary>
        /// 边类型统计
        /// </summary>
        public Dictionary<string, int> EdgeTypes { get; set; } = [];
    }
}

