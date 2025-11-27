using Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.KnowledgeGraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 知识图谱应用服务实现（Apache AGE 版本）
    /// 基于项目实体和关系设计，支持权限控制和性能优化
    /// </summary>
    public class KnowledgeGraphAppService(
        IKnowledgeGraphRepository knowledgeGraphRepository,
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        IRepository<KnowledgeGraphRelationship, Guid> relationshipRepository,
        AttachCataloguePermissionChecker permissionChecker,
        ICurrentUser currentUser,
        IServiceProvider serviceProvider) : AttachmentService, IKnowledgeGraphAppService
    {
        private readonly IKnowledgeGraphRepository _knowledgeGraphRepository = knowledgeGraphRepository;
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository = catalogueRepository;
        private readonly IRepository<KnowledgeGraphRelationship, Guid> _relationshipRepository = relationshipRepository;
        private readonly AttachCataloguePermissionChecker _permissionChecker = permissionChecker;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        /// <summary>
        /// 获取图谱数据（Apache AGE 实现）
        /// 基于项目实体和关系设计，支持权限控制和性能优化
        /// </summary>
        public async Task<GraphDataDto> GetGraphDataAsync(GraphQueryInput input)
        {
            var stopwatch = Stopwatch.StartNew();

            var nodes = new List<NodeDto>();
            var edges = new List<EdgeDto>();
            var nodeMap = new Dictionary<Guid, NodeDto>(); // 用于去重和快速查找
            var edgeSet = new HashSet<string>(); // 用于关系去重

            var userId = _currentUser.Id ?? Guid.Empty;
            var userRoles = await GetUserRolesAsync(userId);

            try
            {
                // 构建 Cypher 查询
                var cypherQuery = BuildCypherQuery(input);
                var parameters = BuildParameters(input);

                // 使用仓储执行查询
                var agtypeResults = await _knowledgeGraphRepository.ExecuteCypherQueryAsync(
                    cypherQuery,
                    parameters,
                    commandTimeout: 30);

                // 解析查询结果
                foreach (var agtypeValue in agtypeResults)
                {
                    try
                    {
                        // Apache AGE 返回 agtype，需要解析为 JSON
                        var resultJson = JsonDocument.Parse(agtypeValue);
                        var resultElement = resultJson.RootElement;

                        // 解析节点数据
                        if (resultElement.TryGetProperty("n", out var nodeElement))
                        {
                            var nodeDto = await MapToNodeDtoAsync(nodeElement);

                            if (nodeDto != null && !nodeMap.ContainsKey(nodeDto.EntityId))
                            {
                                // 权限检查：利用 AttachCatalogue.Permissions 过滤无权限的实体
                                if (await CheckEntityAccessAsync(
                                    nodeDto.EntityId,
                                    nodeDto.Type,
                                    userId,
                                    Domain.Shared.PermissionAction.View,
                                    userRoles))
                                {
                                    nodes.Add(nodeDto);
                                    nodeMap[nodeDto.EntityId] = nodeDto;
                                }
                            }
                        }

                        // 解析关系数据
                        if (resultElement.TryGetProperty("relationships", out var relsElement))
                        {
                            if (relsElement.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var relItem in relsElement.EnumerateArray())
                                {
                                    await ParseRelationshipAsync(relItem, nodeMap, edges, edgeSet, userId, userRoles);
                                }
                            }
                        }
                        // 如果关系在单独的字段中
                        else if (resultElement.TryGetProperty("r", out var relElement))
                        {
                            await ParseRelationshipAsync(relElement, nodeMap, edges, edgeSet, userId, userRoles);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "解析图数据时发生错误，跳过该记录");
                        // 继续处理下一条记录
                    }
                }

                // 如果指定了中心实体，确保中心节点在结果中
                if (input.CenterEntityId.HasValue && !nodeMap.ContainsKey(input.CenterEntityId.Value))
                {
                    var centerNode = await LoadEntityNodeAsync(input.CenterEntityId.Value, userId, userRoles);
                    if (centerNode != null)
                    {
                        nodes.Add(centerNode);
                        nodeMap[centerNode.EntityId] = centerNode;
                    }
                }

                stopwatch.Stop();

                // 计算统计信息
                var statistics = CalculateStatistics(nodes, edges);

                // 记录审计日志
                await LogGraphQueryAsync(input, nodes.Count, edges.Count, stopwatch.ElapsedMilliseconds);

                return new GraphDataDto
                {
                    Nodes = nodes,
                    Edges = edges,
                    Statistics = statistics,
                    QueryTimeMs = (int)stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取图数据失败: {Input}", JsonSerializer.Serialize(input));
                throw;
            }
        }

        /// <summary>
        /// 构建 Cypher 查询语句
        /// </summary>
        private static string BuildCypherQuery(GraphQueryInput input)
        {
            var query = new StringBuilder();

            if (input.CenterEntityId.HasValue)
            {
                // 以中心实体为中心展开查询
                var depth = input.Depth ?? 2;
                query.AppendLine($"MATCH path = (center:Entity {{id: $centerId}})-[*1..{depth}]-(related:Entity)");
                query.AppendLine("WHERE center.id = $centerId");

                // 实体类型过滤
                if (input.EntityTypes?.Count > 0)
                {
                    query.AppendLine("AND related.type IN $entityTypes");
                }

                query.AppendLine("WITH DISTINCT related as n, relationships(path) as rels");
                query.AppendLine("UNWIND rels as r");
                query.AppendLine("MATCH (source:Entity {id: startNode(r).id})");
                query.AppendLine("MATCH (target:Entity {id: endNode(r).id})");
                query.AppendLine("RETURN n, collect(DISTINCT {rel: r, source: source, target: target}) as relationships");
            }
            else
            {
                // 全局查询
                query.AppendLine("MATCH (n:Entity)");

                var conditions = new List<string>();

                // 实体类型过滤
                if (input.EntityTypes?.Count > 0)
                {
                    conditions.Add("n.type IN $entityTypes");
                }

                // 状态过滤（如果有）
                if (!string.IsNullOrEmpty(input.Status))
                {
                    conditions.Add("n.status = $status");
                }

                if (conditions.Count != 0)
                {
                    query.AppendLine($"WHERE {string.Join(" AND ", conditions)}");
                }

                query.AppendLine("OPTIONAL MATCH (n)-[r]->(m:Entity)");
                query.AppendLine("RETURN n, collect(DISTINCT {rel: r, target: m}) as relationships");
            }

            // 限制结果数量
            query.AppendLine($"LIMIT {input.MaxNodes ?? 500}");

            return query.ToString();
        }

        /// <summary>
        /// 构建查询参数
        /// </summary>
        private static Dictionary<string, object> BuildParameters(GraphQueryInput input)
        {
            var parameters = new Dictionary<string, object>();

            if (input.CenterEntityId.HasValue)
            {
                parameters["centerId"] = input.CenterEntityId.Value.ToString();
            }

            if (input.EntityTypes?.Count > 0)
            {
                parameters["entityTypes"] = input.EntityTypes;
            }

            if (!string.IsNullOrEmpty(input.Status))
            {
                parameters["status"] = input.Status;
            }

            return parameters;
        }

        /// <summary>
        /// 从 AGE 结果映射到 NodeDto
        /// </summary>
        private Task<NodeDto?> MapToNodeDtoAsync(JsonElement nodeElement)
        {
            try
            {
                if (nodeElement.ValueKind != JsonValueKind.Object)
                    return Task.FromResult<NodeDto?>(null);

                // 获取节点 ID（agtype 中的 id 字段）
                if (!nodeElement.TryGetProperty("id", out var idElement))
                    return Task.FromResult<NodeDto?>(null);

                var idString = idElement.GetString();
                if (string.IsNullOrEmpty(idString) || !Guid.TryParse(idString, out var entityId))
                    return Task.FromResult<NodeDto?>(null);

                // 获取节点类型
                var entityType = nodeElement.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString() ?? "Unknown"
                    : "Unknown";

                // 获取节点名称
                var name = nodeElement.TryGetProperty("name", out var nameElement)
                    ? nameElement.GetString() ?? ""
                    : "";

                // 获取标签
                var tags = new List<string>();
                if (nodeElement.TryGetProperty("tags", out var tagsElement))
                {
                    if (tagsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tag in tagsElement.EnumerateArray())
                        {
                            if (tag.ValueKind == JsonValueKind.String)
                                tags.Add(tag.GetString() ?? "");
                        }
                    }
                }

                // 获取其他属性
                var properties = new Dictionary<string, object>();
                foreach (var prop in nodeElement.EnumerateObject())
                {
                    if (prop.Name != "id" && prop.Name != "type" && prop.Name != "name" && prop.Name != "tags")
                    {
                        properties[prop.Name] = ExtractJsonValue(prop.Value);
                    }
                }

                return Task.FromResult<NodeDto?>(new NodeDto
                {
                    EntityId = entityId,
                    Type = entityType,
                    Name = name,
                    Tags = tags,
                    Properties = properties,
                    SecurityLevel = properties.GetValueOrDefault("securityLevel")?.ToString(),
                    UpdatedTime = properties.TryGetValue("updatedTime", out var updatedTime)
                        ? DateTime.TryParse(updatedTime.ToString(), out var dt) ? dt : DateTime.UtcNow
                        : DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "映射节点数据失败");
                return Task.FromResult<NodeDto?>(null);
            }
        }

        /// <summary>
        /// 解析关系数据
        /// </summary>
        private async Task ParseRelationshipAsync(
            JsonElement relElement,
            Dictionary<Guid, NodeDto> nodeMap,
            List<EdgeDto> edges,
            HashSet<string> edgeSet,
            Guid userId,
            List<string> userRoles)
        {
            try
            {
                // 解析关系对象（可能包含 rel, source, target）
                JsonElement relData;
                JsonElement sourceElement;
                JsonElement targetElement;

                if (relElement.TryGetProperty("rel", out var relProp))
                {
                    relData = relProp;
                }
                else
                {
                    relData = relElement;
                }

                if (relElement.TryGetProperty("source", out var sourceProp))
                {
                    sourceElement = sourceProp;
                }
                else if (relData.TryGetProperty("start", out var startProp))
                {
                    sourceElement = startProp;
                }
                else
                {
                    return; // 无法解析源节点
                }

                if (relElement.TryGetProperty("target", out var targetProp))
                {
                    targetElement = targetProp;
                }
                else if (relData.TryGetProperty("end", out var endProp))
                {
                    targetElement = endProp;
                }
                else
                {
                    return; // 无法解析目标节点
                }

                // 获取源和目标节点 ID
                var sourceIdString = sourceElement.TryGetProperty("id", out var sourceIdProp)
                    ? sourceIdProp.GetString()
                    : null;
                var targetIdString = targetElement.TryGetProperty("id", out var targetIdProp)
                    ? targetIdProp.GetString()
                    : null;

                if (string.IsNullOrEmpty(sourceIdString) || string.IsNullOrEmpty(targetIdString))
                    return;

                if (!Guid.TryParse(sourceIdString, out var sourceId) ||
                    !Guid.TryParse(targetIdString, out var targetId))
                    return;

                // 权限检查：确保源和目标节点都有权限访问
                var sourceType = sourceElement.TryGetProperty("type", out var sourceTypeProp)
                    ? sourceTypeProp.GetString() ?? "Unknown"
                    : "Unknown";
                var targetType = targetElement.TryGetProperty("type", out var targetTypeProp)
                    ? targetTypeProp.GetString() ?? "Unknown"
                    : "Unknown";

                if (!await CheckEntityAccessAsync(sourceId, sourceType, userId, Domain.Shared.PermissionAction.View, userRoles) ||
                    !await CheckEntityAccessAsync(targetId, targetType, userId, Domain.Shared.PermissionAction.View, userRoles))
                {
                    return; // 无权限访问，跳过该关系
                }

                // 确保节点在节点列表中
                if (!nodeMap.ContainsKey(sourceId))
                {
                    var sourceNode = await MapToNodeDtoAsync(sourceElement);
                    if (sourceNode != null)
                    {
                        nodeMap[sourceId] = sourceNode;
                    }
                }

                if (!nodeMap.ContainsKey(targetId))
                {
                    var targetNode = await MapToNodeDtoAsync(targetElement);
                    if (targetNode != null)
                    {
                        nodeMap[targetId] = targetNode;
                    }
                }

                // 获取关系类型
                var relType = relData.TryGetProperty("type", out var typeProp)
                    ? typeProp.GetString() ?? "RELATES_TO"
                    : "RELATES_TO";

                // 获取关系属性
                var relProperties = new Dictionary<string, object>();
                var role = "";
                var semanticType = "";
                var weight = 1.0;

                foreach (var prop in relData.EnumerateObject())
                {
                    if (prop.Name == "type")
                        continue;

                    if (prop.Name == "role")
                    {
                        role = prop.Value.GetString() ?? "";
                        relProperties["role"] = role;
                    }
                    else if (prop.Name == "semanticType")
                    {
                        semanticType = prop.Value.GetString() ?? "";
                        relProperties["semanticType"] = semanticType;
                    }
                    else if (prop.Name == "weight")
                    {
                        weight = prop.Value.GetDouble();
                        relProperties["weight"] = weight;
                    }
                    else
                    {
                        relProperties[prop.Name] = ExtractJsonValue(prop.Value);
                    }
                }

                // 创建关系唯一标识（用于去重）
                var edgeKey = $"{sourceId}_{targetId}_{relType}_{role}_{semanticType}";
                if (edgeSet.Contains(edgeKey))
                    return; // 关系已存在，跳过

                edgeSet.Add(edgeKey);

                // 创建关系 DTO
                var edge = new EdgeDto
                {
                    Source = sourceId,
                    Target = targetId,
                    Type = relType,
                    Role = string.IsNullOrEmpty(role) ? null : role,
                    SemanticType = string.IsNullOrEmpty(semanticType) ? null : semanticType,
                    Weight = weight,
                    Properties = relProperties
                };

                edges.Add(edge);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "解析关系数据失败");
            }
        }

        /// <summary>
        /// 加载实体节点（用于中心节点）
        /// 包含权限检查，确保用户有权限访问该实体
        /// </summary>
        private async Task<NodeDto?> LoadEntityNodeAsync(Guid entityId, Guid userId, List<string> userRoles)
        {
            try
            {
                // 从图数据库查询实体节点
                var cypherQuery = "MATCH (n:Entity {id: $id}) RETURN n";
                var parameters = new Dictionary<string, object>
                {
                    { "id", entityId.ToString() }
                };

                var agtypeValue = await _knowledgeGraphRepository.ExecuteCypherQuerySingleAsync(
                    cypherQuery,
                    parameters);

                if (string.IsNullOrEmpty(agtypeValue))
                {
                    Logger.LogDebug("实体节点不存在于图数据库中: EntityId={EntityId}", entityId);
                    return null;
                }

                var resultJson = JsonDocument.Parse(agtypeValue);
                if (!resultJson.RootElement.TryGetProperty("n", out var nodeElement))
                {
                    Logger.LogDebug("图查询结果中未找到节点数据: EntityId={EntityId}", entityId);
                    return null;
                }

                // 映射为 NodeDto
                var nodeDto = await MapToNodeDtoAsync(nodeElement);
                if (nodeDto == null)
                {
                    Logger.LogWarning("映射节点数据失败: EntityId={EntityId}", entityId);
                    return null;
                }

                // 权限检查：确保用户有权限访问该实体
                // 这是关键的安全检查，防止用户访问无权限的实体节点
                if (!await CheckEntityAccessAsync(
                    nodeDto.EntityId,
                    nodeDto.Type,
                    userId,
                    Domain.Shared.PermissionAction.View,
                    userRoles))
                {
                    Logger.LogInformation(
                        "用户无权限访问实体节点: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}",
                        userId,
                        entityId,
                        nodeDto.Type);
                    return null; // 无权限，返回 null，调用方不会添加该节点
                }

                // 权限检查通过，返回节点数据
                return nodeDto;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "加载实体节点失败: EntityId={EntityId}, UserId={UserId}", entityId, userId);
                return null;
            }
        }

        /// <summary>
        /// 提取 JSON 值
        /// </summary>
        private object ExtractJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? "",
                JsonValueKind.Number => element.TryGetInt64(out var i64) ? i64 : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Array => element.EnumerateArray().Select(ExtractJsonValue).ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ExtractJsonValue(p.Value)),
                _ => element.ToString()
            };
        }

        /// <summary>
        /// 计算统计信息
        /// </summary>
        private static GraphStatisticsDto CalculateStatistics(List<NodeDto> nodes, List<EdgeDto> edges)
        {
            var nodeTypeCounts = nodes
                .GroupBy(n => n.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            var edgeTypeCounts = edges
                .GroupBy(e => e.Type)
                .ToDictionary(g => g.Key, g => g.Count());

            return new GraphStatisticsDto
            {
                TotalNodes = nodes.Count,
                TotalEdges = edges.Count,
                NodeTypes = nodeTypeCounts,
                EdgeTypes = edgeTypeCounts
            };
        }

        /// <summary>
        /// 记录图查询审计日志
        /// </summary>
        private Task LogGraphQueryAsync(GraphQueryInput input, int nodeCount, int edgeCount, long executionTimeMs)
        {
            try
            {
                Logger.LogInformation(
                    "图查询完成: CenterEntityId={CenterEntityId}, EntityTypes={EntityTypes}, Nodes={NodeCount}, Edges={EdgeCount}, Time={Time}ms",
                    input.CenterEntityId,
                    string.Join(",", input.EntityTypes ?? []),
                    nodeCount,
                    edgeCount,
                    executionTimeMs
                );
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "记录图查询审计日志失败");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 获取用户角色
        /// 使用 ABP 框架的 IIdentityUserRepository 服务获取用户角色信息（可选依赖）
        /// 
        /// 实现说明：
        /// - 优先使用 ICurrentUser.Roles（当前用户，性能更好，从 JWT token 中获取）
        /// - 如果查询的是其他用户或需要完整角色信息，使用 IIdentityUserRepository.GetRolesAsync
        /// - IIdentityUserRepository 会返回完整的角色信息（包括从组织单元继承的角色）
        /// - 使用服务定位器模式，如果 Identity 模块未注册，不会导致服务启动失败
        /// </summary>
        private async Task<List<string>> GetUserRolesAsync(Guid userId)
        {
            try
            {
                // 如果查询的是当前用户且已认证，优先使用 ICurrentUser.Roles（性能更好）
                if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                {
                    var roles = _currentUser.Roles?.ToList() ?? [];
                    
                    // 如果 ICurrentUser.Roles 有值，直接返回
                    if (roles.Count != 0)
                    {
                        return roles;
                    }
                    
                    // 如果 ICurrentUser.Roles 为空，尝试从数据库获取完整角色信息
                    // 这可以获取到从组织单元继承的角色等完整信息
                }

                // 使用服务定位器模式获取 IIdentityUserRepository（可选依赖）
                // 如果 Identity 模块未注册，服务不可用，使用回退方案
                var identityUserRepository = _serviceProvider.GetService<IIdentityUserRepository>();
                
                if (identityUserRepository == null)
                {
                    Logger.LogWarning(
                        "[IIdentityUserRepository]未注册服务！Identity 模块可能未安装或未配置。UserId={UserId}，将使用 ICurrentUser.Roles 作为回退方案。",
                        userId);
                    
                    // 回退到 ICurrentUser.Roles
                    if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                    {
                        return _currentUser.Roles?.ToList() ?? [];
                    }
                    
                    return [];
                }

                // 使用 IIdentityUserRepository 获取完整的用户角色信息
                // 这会返回用户的所有角色，包括：
                // 1. 直接分配的角色
                // 2. 通过组织单元继承的角色
                // 3. 其他方式分配的角色
                var user = await identityUserRepository.FindAsync(userId);
                if (user == null)
                {
                    Logger.LogWarning("用户不存在: UserId={UserId}", userId);
                    return [];
                }

                var rolesList = await identityUserRepository.GetRolesAsync(userId);
                return [.. rolesList.Select(r => r.Name)];
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "获取用户角色失败: UserId={UserId}", userId);
                
                // 发生异常时，尝试使用 ICurrentUser.Roles 作为后备方案
                if (_currentUser.Id == userId && _currentUser.IsAuthenticated)
                {
                    return _currentUser.Roles?.ToList() ?? [];
                }
                
                return [];
            }
        }

        /// <summary>
        /// 检查实体访问权限（利用AttachCatalogue.Permissions）
        /// 注意：entityId 参数对应现有实体的 Id（如 AttachCatalogue.Id），
        /// 视图模型通过 EntityId 属性关联到现有实体
        /// </summary>
        private async Task<bool> CheckEntityAccessAsync(
            Guid entityId, // 现有实体的 Id（如 AttachCatalogue.Id）
            string entityType,
            Guid userId,
            Domain.Shared.PermissionAction action,
            List<string> userRoles)
        {
            if (entityType == "Catalogue")
            {
                try
                {
                    var catalogue = await _catalogueRepository.GetAsync(entityId);
                    if (catalogue == null) return false;

                    // 使用 AttachCataloguePermissionChecker 进行权限检查
                    // 传递 userRoles 参数，确保权限检查使用正确的用户角色信息
                    return await _permissionChecker.CheckPermissionAsync(catalogue, action, userId, userRoles);
                }
                catch
                {
                    return false;
                }
            }

            // 其他实体类型（Person、Department、BusinessEntity、Workflow）默认允许访问
            // 可根据需要扩展权限检查逻辑
            return true;
        }
    }
}

