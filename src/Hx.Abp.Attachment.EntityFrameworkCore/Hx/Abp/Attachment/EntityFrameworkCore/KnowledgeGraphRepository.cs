using System.Text.Json;
using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// 知识图谱仓储实现（Apache AGE）
    /// 封装 Apache AGE 图数据库查询逻辑，避免 Application 层直接依赖 EntityFrameworkCore
    /// </summary>
    public class KnowledgeGraphRepository(
        IDbContextProvider<AttachmentDbContext> dbContextProvider,
        ILogger<KnowledgeGraphRepository> logger) : IKnowledgeGraphRepository
    {
        private readonly IDbContextProvider<AttachmentDbContext> _dbContextProvider = dbContextProvider;
        private readonly ILogger<KnowledgeGraphRepository> _logger = logger;
        private const string GraphName = "kg_graph";

        /// <summary>
        /// 执行 Cypher 查询并返回结果
        /// </summary>
        public async Task<List<string>> ExecuteCypherQueryAsync(
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            int commandTimeout = 30)
        {
            if (string.IsNullOrWhiteSpace(cypherQuery))
                throw new ArgumentException("Cypher query cannot be null or empty", nameof(cypherQuery));

            var dbContext = await _dbContextProvider.GetDbContextAsync();
            var connection = dbContext.Database.GetDbConnection() as NpgsqlConnection ?? throw new InvalidOperationException("Database connection is not NpgsqlConnection");

            // 确保连接已打开
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            var results = new List<string>();
            parameters ??= [];

            try
            {
                // 构建 Apache AGE SQL 查询
                // 语法：cypher('graph_name', $$cypher_query$$, '{"param": "value"}')
                // 使用参数化查询避免 SQL 注入
                var paramsJson = JsonSerializer.Serialize(parameters);

                var sqlQuery = @"
                    SELECT * FROM cypher(@graphName, @cypherQuery, @params::jsonb) AS (result agtype)";

                await using var command = new NpgsqlCommand(sqlQuery, connection);
                command.Parameters.AddWithValue("graphName", GraphName);
                command.Parameters.AddWithValue("cypherQuery", cypherQuery);
                command.Parameters.AddWithValue("params", NpgsqlTypes.NpgsqlDbType.Jsonb, paramsJson);
                command.CommandTimeout = commandTimeout;

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    try
                    {
                        var agtypeValue = reader.GetFieldValue<string>(0);
                        results.Add(agtypeValue);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "读取图查询结果时发生错误，跳过该记录");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "执行 Cypher 查询失败: {CypherQuery}", cypherQuery);
                throw;
            }

            return results;
        }

        /// <summary>
        /// 执行 Cypher 查询并返回单个结果
        /// </summary>
        public async Task<string?> ExecuteCypherQuerySingleAsync(
            string cypherQuery,
            Dictionary<string, object>? parameters = null,
            int commandTimeout = 30)
        {
            var results = await ExecuteCypherQueryAsync(cypherQuery, parameters, commandTimeout);
            return results.FirstOrDefault();
        }
    }
}

