namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 知识图谱仓储接口
    /// 提供 Apache AGE 图数据库查询功能
    /// </summary>
    public interface IKnowledgeGraphRepository
    {
        /// <summary>
        /// 执行 Cypher 查询并返回结果
        /// </summary>
        /// <param name="cypherQuery">Cypher 查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="commandTimeout">命令超时时间（秒）</param>
        /// <returns>查询结果（agtype JSON 字符串列表）</returns>
        Task<List<string>> ExecuteCypherQueryAsync(
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            int commandTimeout = 30);

        /// <summary>
        /// 执行 Cypher 查询并返回单个结果
        /// </summary>
        /// <param name="cypherQuery">Cypher 查询语句</param>
        /// <param name="parameters">查询参数</param>
        /// <param name="commandTimeout">命令超时时间（秒）</param>
        /// <returns>查询结果（agtype JSON 字符串），如果没有结果返回 null</returns>
        Task<string?> ExecuteCypherQuerySingleAsync(
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            int commandTimeout = 30);
    }
}

