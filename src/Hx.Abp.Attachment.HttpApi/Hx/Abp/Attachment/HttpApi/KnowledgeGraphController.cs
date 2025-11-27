using Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 知识图谱 API 控制器
    /// 提供知识图谱数据查询的 HTTP 接口
    /// </summary>
    [ApiController]
    [Route("api/app/knowledge-graph")]
    public class KnowledgeGraphController(IKnowledgeGraphAppService knowledgeGraphAppService) : AbpControllerBase
    {
        protected IKnowledgeGraphAppService KnowledgeGraphAppService { get; } = knowledgeGraphAppService;

        /// <summary>
        /// 获取图谱数据（GET 方法，通过查询参数）
        /// 支持中心实体查询、实体类型过滤、关系类型过滤等功能
        /// </summary>
        /// <param name="centerEntityId">中心实体ID（可选，以该实体为中心展开查询）</param>
        /// <param name="entityTypes">实体类型过滤（可选，多个类型用逗号分隔，如：Catalogue,Person）</param>
        /// <param name="relationshipTypes">关系类型过滤（可选）</param>
        /// <param name="depth">查询深度（默认 2）</param>
        /// <param name="maxNodes">最大节点数（默认 500）</param>
        /// <param name="status">状态过滤（可选）</param>
        /// <returns>图数据响应，包含节点、边和统计信息</returns>
        /// <remarks>
        /// 示例请求：
        /// GET /api/app/knowledge-graph?centerEntityId=xxx&amp;entityTypes=Catalogue,Person&amp;depth=2&amp;maxNodes=500
        /// </remarks>
        [HttpGet]
        public virtual Task<GraphDataDto> GetGraphDataAsync(
            [FromQuery] Guid? centerEntityId = null,
            [FromQuery] string? entityTypes = null,
            [FromQuery] string? relationshipTypes = null,
            [FromQuery] int? depth = null,
            [FromQuery] int? maxNodes = null,
            [FromQuery] string? status = null)
        {
            var input = new GraphQueryInput
            {
                CenterEntityId = centerEntityId,
                EntityTypes = !string.IsNullOrEmpty(entityTypes) 
                    ? [.. entityTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
                    : null,
                RelationshipTypes = !string.IsNullOrEmpty(relationshipTypes)
                    ? [.. relationshipTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
                    : null,
                Depth = depth,
                MaxNodes = maxNodes,
                Status = status
            };

            return KnowledgeGraphAppService.GetGraphDataAsync(input);
        }

        /// <summary>
        /// 获取图谱数据（POST 方法，通过请求体传递复杂参数）
        /// 支持中心实体查询、实体类型过滤、关系类型过滤等功能
        /// </summary>
        /// <param name="input">图查询输入参数</param>
        /// <returns>图数据响应，包含节点、边和统计信息</returns>
        /// <remarks>
        /// 示例请求：
        /// POST /api/app/knowledge-graph/query
        /// {
        ///   "centerEntityId": "xxx",
        ///   "entityTypes": ["Catalogue", "Person"],
        ///   "depth": 2,
        ///   "maxNodes": 500
        /// }
        /// </remarks>
        [HttpPost("query")]
        public virtual Task<GraphDataDto> QueryGraphDataAsync([FromBody] GraphQueryInput input)
        {
            return KnowledgeGraphAppService.GetGraphDataAsync(input);
        }
    }
}

