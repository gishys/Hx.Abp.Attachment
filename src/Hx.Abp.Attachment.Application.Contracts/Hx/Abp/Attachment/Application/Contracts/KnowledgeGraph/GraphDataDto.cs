namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 图数据响应
    /// </summary>
    public class GraphDataDto
    {
        /// <summary>
        /// 节点列表
        /// </summary>
        public List<NodeDto> Nodes { get; set; } = [];

        /// <summary>
        /// 边列表
        /// </summary>
        public List<EdgeDto> Edges { get; set; } = [];

        /// <summary>
        /// 统计信息
        /// </summary>
        public GraphStatisticsDto? Statistics { get; set; }

        /// <summary>
        /// 查询执行时间（毫秒）
        /// </summary>
        public int QueryTimeMs { get; set; }
    }
}

