using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 知识图谱应用服务接口
    /// 提供知识图谱数据查询功能，支持权限控制和性能优化
    /// </summary>
    public interface IKnowledgeGraphAppService : IApplicationService
    {
        /// <summary>
        /// 获取图谱数据（Apache AGE 实现）
        /// 基于项目实体和关系设计，支持权限控制和性能优化
        /// </summary>
        /// <param name="input">图查询输入参数</param>
        /// <returns>图数据响应，包含节点、边和统计信息</returns>
        /// <remarks>
        /// 功能特性：
        /// - 支持中心实体查询：以指定实体为中心展开查询
        /// - 支持实体类型过滤：按类型过滤节点
        /// - 支持关系类型过滤：按类型过滤关系
        /// - 支持查询深度限制：可配置查询深度
        /// - 支持结果数量限制：防止返回过多数据
        /// - 权限控制：自动过滤无权限访问的实体和关系
        /// - 性能优化：节点和关系去重、查询超时控制
        /// </remarks>
        Task<GraphDataDto> GetGraphDataAsync(GraphQueryInput input);
    }
}

