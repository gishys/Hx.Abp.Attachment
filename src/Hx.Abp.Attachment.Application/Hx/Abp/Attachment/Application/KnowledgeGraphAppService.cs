using Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.KnowledgeGraph;
using Hx.Abp.Attachment.Domain.Shared;
using Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Data;
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
        /// 注意：使用具体的实体类型标签（Catalogue, Person, Department等），而不是通用的 Entity
        /// </summary>
        private static string BuildCypherQuery(GraphQueryInput input)
        {
            var query = new StringBuilder();

            // 构建实体类型标签列表（用于多标签查询）
            var entityLabels = input.EntityTypes?.Count > 0
                ? string.Join("|", input.EntityTypes.Select(EntityType.Normalize).Where(l => l != null))
                : "Catalogue|Person|Department|BusinessEntity|Workflow"; // 默认查询所有类型

            if (input.CenterEntityId.HasValue)
            {
                // 以中心实体为中心展开查询
                // 注意：中心实体类型未知，需要查询所有可能的标签
                var depth = input.Depth ?? 2;
                query.AppendLine($"MATCH path = (center)-[*1..{depth}]-(related)");
                query.AppendLine($"WHERE (center:Catalogue OR center:Person OR center:Department OR center:BusinessEntity OR center:Workflow)");
                query.AppendLine("AND center.id = $centerId");

                // 实体类型过滤
                if (input.EntityTypes?.Count > 0)
                {
                    var typeConditions = input.EntityTypes
                        .Select(EntityType.Normalize)
                        .Where(t => t != null)
                        .Select(t => $"related:{t}")
                        .ToList();
                    if (typeConditions.Count > 0)
                    {
                        query.AppendLine($"AND ({string.Join(" OR ", typeConditions)})");
                    }
                }

                query.AppendLine("WITH DISTINCT related as n, relationships(path) as rels");
                query.AppendLine("UNWIND rels as r");
                query.AppendLine("MATCH (source) WHERE source.id = startNode(r).id");
                query.AppendLine("MATCH (target) WHERE target.id = endNode(r).id");
                query.AppendLine("RETURN n, collect(DISTINCT {rel: r, source: source, target: target}) as relationships");
            }
            else
            {
                // 全局查询
                // 使用多标签匹配：支持 Catalogue, Person, Department, BusinessEntity, Workflow
                var labelConditions = input.EntityTypes?.Count > 0
                    ? input.EntityTypes
                        .Select(EntityType.Normalize)
                        .Where(t => t != null)
                        .Select(t => $"n:{t}")
                        .ToList()
                    : ["n:Catalogue", "n:Person", "n:Department", "n:BusinessEntity", "n:Workflow"];

                query.AppendLine($"MATCH (n) WHERE ({string.Join(" OR ", labelConditions)})");

                var conditions = new List<string>();

                // 状态过滤（如果有，仅适用于 Catalogue）
                if (!string.IsNullOrEmpty(input.Status))
                {
                    conditions.Add("(NOT (n:Catalogue) OR n.status = $status)");
                }

                if (conditions.Count != 0)
                {
                    query.AppendLine($"AND {string.Join(" AND ", conditions)}");
                }

                query.AppendLine("OPTIONAL MATCH (n)-[r]->(m)");
                query.AppendLine("WHERE (m:Catalogue OR m:Person OR m:Department OR m:BusinessEntity OR m:Workflow)");
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
        /// 注意：使用多标签查询，支持所有实体类型（Catalogue, Person, Department等）
        /// </summary>
        private async Task<NodeDto?> LoadEntityNodeAsync(Guid entityId, Guid userId, List<string> userRoles)
        {
            try
            {
                // 从图数据库查询实体节点（使用多标签查询，支持所有实体类型）
                // 注意：节点标签使用具体的实体类型（Catalogue, Person, Department等），而不是通用的 Entity
                var cypherQuery = @"
                    MATCH (n) 
                    WHERE (n:Catalogue OR n:Person OR n:Department OR n:BusinessEntity OR n:Workflow)
                    AND n.id = $id 
                    RETURN n";
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
            // 规范化实体类型（确保大小写一致）
            var normalizedType = EntityType.Normalize(entityType);
            if (normalizedType == null)
            {
                Logger.LogWarning("无效的实体类型：{EntityType}", entityType);
                return false;
            }

            if (EntityType.Equals(normalizedType, EntityType.Catalogue))
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

        // ========== 关系管理方法实现 ==========

        /// <summary>
        /// 创建关系
        /// 支持抽象关系类型（通过 Role 和 SemanticType 属性）
        /// </summary>
        public async Task<RelationshipDto> CreateRelationshipAsync(CreateRelationshipInput input)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. 验证源实体和目标实体是否存在
                await ValidateEntitiesExistAsync(input.SourceEntityId, input.SourceEntityType);
                await ValidateEntitiesExistAsync(input.TargetEntityId, input.TargetEntityType);

                // 2. 验证关系类型是否有效
                ValidateRelationshipType(input.RelationshipType, input.SourceEntityType, input.TargetEntityType);

                // 3. 规范化 Role（用于后续检查）
                var normalizedRoleForCheck = Role.Normalize(input.Role, input.RelationshipType);
                
                // 准备 Role 和 SemanticType 的显示字符串（用于错误消息和日志）
                var rolePart = Role.IsNullOrEmpty(normalizedRoleForCheck) ? "" : $":{normalizedRoleForCheck}";
                var semanticTypePart = string.IsNullOrEmpty(input.SemanticType) ? "" : $":{input.SemanticType}";
                
                // 4. 检查关系是否已存在（根据业务规则，某些关系类型不允许重复）
                // 对于抽象关系类型，需要考虑 role 或 semanticType 的组合唯一性
                var exists = await CheckRelationshipExistsAsync(
                    input.SourceEntityId,
                    input.TargetEntityId,
                    input.RelationshipType,
                    normalizedRoleForCheck,
                    input.SemanticType);

                if (exists)
                {
                    throw new UserFriendlyException(
                        $"关系已存在：{input.SourceEntityType}({input.SourceEntityId}) -[{input.RelationshipType}{rolePart}{semanticTypePart}]-> " +
                        $"{input.TargetEntityType}({input.TargetEntityId})");
                }

                // 5. 验证权限：检查用户是否有权限创建该关系
                var userId = _currentUser.Id ?? Guid.Empty;
                var userRoles = await GetUserRolesAsync(userId);
                await ValidateRelationshipCreationPermissionAsync(
                    input.SourceEntityId,
                    input.SourceEntityType,
                    input.TargetEntityId,
                    input.TargetEntityType,
                    userId,
                    userRoles);

                // 6. 验证业务规则（循环关系检查等）
                await ValidateBusinessRulesAsync(input);

                // 7. 规范化 Role（确保大小写一致，已在步骤3中完成）
                // 验证 Role（如果提供了 Role，必须是有效的）
                if (!Role.IsNullOrEmpty(input.Role) && normalizedRoleForCheck == null)
                {
                    throw new UserFriendlyException(
                        $"无效的角色值：{input.Role}。关系类型 {input.RelationshipType} 不支持该角色。");
                }

                // 8. 创建关系实体（支持抽象关系类型）
                var relationship = new KnowledgeGraphRelationship
                {
                    SourceEntityId = input.SourceEntityId,
                    SourceEntityType = input.SourceEntityType,
                    TargetEntityId = input.TargetEntityId,
                    TargetEntityType = input.TargetEntityType,
                    Type = input.RelationshipType,
                    Role = normalizedRoleForCheck, // 使用规范化后的角色（确保大小写一致）
                    SemanticType = input.SemanticType, // 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等）
                    Description = input.Description,
                    Weight = input.Weight ?? 1.0
                };

                // 设置扩展属性（使用 ABP 的 ExtraProperties）
                if (input.Properties != null)
                {
                    foreach (var prop in input.Properties)
                    {
                        relationship.SetProperty(prop.Key, prop.Value);
                    }
                }

                await _relationshipRepository.InsertAsync(relationship);

                // 7. 同步到 Apache AGE 图数据库（异步，使用后台作业或直接同步）
                await SyncRelationshipToAgeGraphAsync(relationship);

                // 9. 记录审计日志
                stopwatch.Stop();
                // rolePart 和 semanticTypePart 已在步骤3中定义，直接使用
                Logger.LogInformation(
                    "创建关系成功：{SourceEntityType}({SourceEntityId}) -[{RelationshipType}{RolePart}{SemanticTypePart}]-> {TargetEntityType}({TargetEntityId}), RelationshipId={RelationshipId}, Time={Time}ms",
                    input.SourceEntityType,
                    input.SourceEntityId,
                    input.RelationshipType,
                    rolePart,
                    semanticTypePart,
                    input.TargetEntityType,
                    input.TargetEntityId,
                    relationship.Id,
                    stopwatch.ElapsedMilliseconds);

                // 10. 返回DTO
                return MapToRelationshipDto(relationship);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "创建关系失败：{SourceEntityType}({SourceEntityId}) -[{RelationshipType}]-> {TargetEntityType}({TargetEntityId})",
                    input.SourceEntityType,
                    input.SourceEntityId,
                    input.RelationshipType,
                    input.TargetEntityType,
                    input.TargetEntityId);
                throw new UserFriendlyException($"创建关系失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 验证实体是否存在
        /// 支持 Catalogue、Person（IdentityUser）、Department（OrganizationUnit）等实体类型
        /// </summary>
        private async Task ValidateEntitiesExistAsync(Guid entityId, string entityType)
        {
            // 规范化实体类型（确保大小写一致）
            var normalizedType = EntityType.Normalize(entityType) ?? throw new UserFriendlyException($"无效的实体类型：{entityType}");
            var exists = normalizedType switch
            {
                EntityType.Catalogue => await _catalogueRepository.AnyAsync(e => e.Id == entityId),
                
                // Person 实体：使用 ABP Identity 模块的 IIdentityUserRepository
                EntityType.Person => await ValidatePersonExistsAsync(entityId),
                
                // Department 实体：使用 ABP Identity 模块的 IOrganizationUnitRepository
                EntityType.Department => await ValidateDepartmentExistsAsync(entityId),
                
                // 其他实体类型（BusinessEntity、Workflow）可以根据需要扩展
                _ => false
            };

            if (!exists)
            {
                throw new UserFriendlyException($"实体不存在：{normalizedType}({entityId})");
            }
        }

        /// <summary>
        /// 验证人员（Person/IdentityUser）是否存在
        /// 使用 ABP Identity 模块的 IIdentityUserRepository（可选依赖）
        /// </summary>
        private async Task<bool> ValidatePersonExistsAsync(Guid personId)
        {
            try
            {
                // 使用服务定位器模式获取 IIdentityUserRepository（可选依赖）
                var identityUserRepository = _serviceProvider.GetService<IIdentityUserRepository>();
                
                if (identityUserRepository == null)
                {
                    Logger.LogWarning(
                        "[IIdentityUserRepository]未注册服务！Identity 模块可能未安装或未配置。PersonId={PersonId}，无法验证人员是否存在。",
                        personId);
                    
                    // 如果 Identity 模块未注册，无法验证，返回 false 或根据业务需求处理
                    // 这里返回 false，让调用方抛出异常
                    return false;
                }

                // 使用 IIdentityUserRepository 验证用户是否存在
                var user = await identityUserRepository.FindAsync(personId);
                return user != null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "验证人员存在性失败：PersonId={PersonId}", personId);
                return false;
            }
        }

        /// <summary>
        /// 验证部门（Department/OrganizationUnit）是否存在
        /// 使用 ABP Identity 模块的 IOrganizationUnitRepository（可选依赖）
        /// </summary>
        private async Task<bool> ValidateDepartmentExistsAsync(Guid departmentId)
        {
            try
            {
                // 使用服务定位器模式获取 IOrganizationUnitRepository（可选依赖）
                // 注意：IOrganizationUnitRepository 位于 Volo.Abp.Identity 命名空间
                var organizationUnitRepository = _serviceProvider.GetService<IOrganizationUnitRepository>();
                
                if (organizationUnitRepository == null)
                {
                    Logger.LogWarning(
                        "[IOrganizationUnitRepository]未注册服务！Identity 模块可能未安装或未配置。DepartmentId={DepartmentId}，无法验证部门是否存在。",
                        departmentId);
                    
                    // 如果 Identity 模块未注册，无法验证，返回 false 或根据业务需求处理
                    // 这里返回 false，让调用方抛出异常
                    return false;
                }

                // 使用 IOrganizationUnitRepository 验证组织单元是否存在
                var organizationUnit = await organizationUnitRepository.FindAsync(departmentId);
                return organizationUnit != null;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "验证部门存在性失败：DepartmentId={DepartmentId}", departmentId);
                return false;
            }
        }

        /// <summary>
        /// 验证关系类型是否有效
        /// </summary>
        private static void ValidateRelationshipType(RelationshipType relationshipType, string sourceEntityType, string targetEntityType)
        {
            // 规范化实体类型（确保大小写一致）
            var normalizedSourceType = EntityType.Normalize(sourceEntityType);
            var normalizedTargetType = EntityType.Normalize(targetEntityType);

            if (normalizedSourceType == null || normalizedTargetType == null)
            {
                throw new UserFriendlyException($"无效的实体类型：源={sourceEntityType}, 目标={targetEntityType}");
            }

            // 定义有效的关系类型组合（基于优化后的抽象关系类型）
            var validCombinations = new Dictionary<RelationshipType, (string source, string target)[]>
            {
                { RelationshipType.CatalogueHasChild, new[] { (EntityType.Catalogue, EntityType.Catalogue) } },
                { RelationshipType.CatalogueRelatesToCatalogue, new[] { (EntityType.Catalogue, EntityType.Catalogue) } },
                { RelationshipType.CatalogueReferencesBusiness, new[] { (EntityType.Catalogue, EntityType.BusinessEntity) } },
                { RelationshipType.PersonRelatesToCatalogue, new[] { (EntityType.Person, EntityType.Catalogue) } },
                { RelationshipType.PersonBelongsToDepartment, new[] { (EntityType.Person, EntityType.Department) } },
                { RelationshipType.DepartmentOwnsCatalogue, new[] { (EntityType.Department, EntityType.Catalogue) } },
                { RelationshipType.DepartmentManagesCatalogue, new[] { (EntityType.Department, EntityType.Catalogue) } },
                { RelationshipType.DepartmentHasParent, new[] { (EntityType.Department, EntityType.Department) } },
                { RelationshipType.BusinessEntityHasCatalogue, new[] { (EntityType.BusinessEntity, EntityType.Catalogue) } },
                { RelationshipType.BusinessEntityManagesCatalogue, new[] { (EntityType.BusinessEntity, EntityType.Catalogue) } },
                { RelationshipType.CatalogueUsesWorkflow, new[] { (EntityType.Catalogue, EntityType.Workflow) } },
                { RelationshipType.WorkflowManagesCatalogue, new[] { (EntityType.Workflow, EntityType.Catalogue) } },
                { RelationshipType.WorkflowInstanceBelongsToCatalogue, new[] { (EntityType.Workflow, EntityType.Catalogue) } },
                { RelationshipType.PersonRelatesToWorkflow, new[] { (EntityType.Person, EntityType.Workflow) } },
                { RelationshipType.DepartmentOwnsWorkflow, new[] { (EntityType.Department, EntityType.Workflow) } },
                { RelationshipType.WorkflowRelatesToWorkflow, new[] { (EntityType.Workflow, EntityType.Workflow) } }
            };

            if (!validCombinations.TryGetValue(relationshipType, out (string source, string target)[]? validCombos))
            {
                throw new UserFriendlyException($"无效的关系类型：{relationshipType}");
            }

            var isValid = validCombos.Any(c => 
                EntityType.Equals(c.source, normalizedSourceType) && 
                EntityType.Equals(c.target, normalizedTargetType));

            if (!isValid)
            {
                throw new UserFriendlyException(
                    $"关系类型 {relationshipType} 不支持 {normalizedSourceType} -> {normalizedTargetType} 的组合");
            }
        }

        /// <summary>
        /// 检查关系是否已存在（考虑 role 和 semanticType）
        /// </summary>
        private async Task<bool> CheckRelationshipExistsAsync(
            Guid sourceId,
            Guid targetId,
            RelationshipType relationshipType,
            string? role = null,
            string? semanticType = null)
        {
            // 对于抽象关系类型，需要检查 role 或 semanticType
            if (relationshipType == RelationshipType.PersonRelatesToCatalogue ||
                relationshipType == RelationshipType.PersonRelatesToWorkflow)
            {
                // 规范化角色（确保大小写一致）
                var normalizedRole = Role.Normalize(role, relationshipType);
                
                if (!Role.IsNullOrEmpty(normalizedRole))
                {
                    // 检查是否存在相同 role 的关系（使用规范化后的角色进行比较）
                    return await _relationshipRepository.AnyAsync(r =>
                        r.SourceEntityId == sourceId &&
                        r.TargetEntityId == targetId &&
                        r.Type == relationshipType &&
                        Role.Equals(r.Role, normalizedRole));
                }
                else
                {
                    // 如果未指定 role，检查是否存在任何 role 的关系
                    return await _relationshipRepository.AnyAsync(r =>
                        r.SourceEntityId == sourceId &&
                        r.TargetEntityId == targetId &&
                        r.Type == relationshipType &&
                        Role.IsNullOrEmpty(r.Role));
                }
            }

            if (relationshipType == RelationshipType.CatalogueRelatesToCatalogue ||
                relationshipType == RelationshipType.WorkflowRelatesToWorkflow)
            {
                if (!string.IsNullOrEmpty(semanticType))
                {
                    // 检查是否存在相同 semanticType 的关系
                    return await _relationshipRepository.AnyAsync(r =>
                        r.SourceEntityId == sourceId &&
                        r.TargetEntityId == targetId &&
                        r.Type == relationshipType &&
                        r.SemanticType == semanticType);
                }
                else
                {
                    // 如果未指定 semanticType，检查是否存在任何 semanticType 的关系
                    return await _relationshipRepository.AnyAsync(r =>
                        r.SourceEntityId == sourceId &&
                        r.TargetEntityId == targetId &&
                        r.Type == relationshipType &&
                        (r.SemanticType == null || r.SemanticType == ""));
                }
            }

            // 对于非抽象关系类型，只检查基本条件
            return await _relationshipRepository.AnyAsync(r =>
                r.SourceEntityId == sourceId &&
                r.TargetEntityId == targetId &&
                r.Type == relationshipType);
        }

        /// <summary>
        /// 验证关系创建权限
        /// </summary>
        private async Task ValidateRelationshipCreationPermissionAsync(
            Guid sourceEntityId,
            string sourceEntityType,
            Guid targetEntityId,
            string targetEntityType,
            Guid userId,
            List<string> userRoles)
        {
            // 规范化实体类型（确保大小写一致）
            var normalizedSourceType = EntityType.Normalize(sourceEntityType);
            var normalizedTargetType = EntityType.Normalize(targetEntityType);

            // 检查源实体的写权限
            if (EntityType.Equals(normalizedSourceType, EntityType.Catalogue))
            {
                var catalogue = await _catalogueRepository.GetAsync(sourceEntityId);
                if (!await _permissionChecker.CheckPermissionAsync(catalogue, PermissionAction.Edit, userId, userRoles))
                {
                    throw new UserFriendlyException("没有权限创建关系：源实体权限不足");
                }
            }

            // 检查目标实体的读权限（至少需要读权限才能关联）
            if (EntityType.Equals(normalizedTargetType, EntityType.Catalogue))
            {
                var catalogue = await _catalogueRepository.GetAsync(targetEntityId);
                if (!await _permissionChecker.CheckPermissionAsync(catalogue, PermissionAction.View, userId, userRoles))
                {
                    throw new UserFriendlyException("没有权限创建关系：目标实体权限不足");
                }
            }
        }

        /// <summary>
        /// 验证业务规则（循环关系检查等）
        /// </summary>
        private async Task ValidateBusinessRulesAsync(CreateRelationshipInput input)
        {
            // 检查循环关系（例如：分类A包含分类B，分类B不能包含分类A）
            if (input.RelationshipType == RelationshipType.CatalogueHasChild ||
                (input.RelationshipType == RelationshipType.CatalogueRelatesToCatalogue &&
                 input.SemanticType == "DependsOn"))
            {
                // 检查是否会导致循环：目标分类是否是源分类的祖先
                var hasCycle = await CheckCycleAsync(input.TargetEntityId, input.SourceEntityId);
                if (hasCycle)
                {
                    throw new UserFriendlyException("不能创建循环关系：目标分类是源分类的祖先");
                }
            }

            // 其他业务规则验证...
        }

        /// <summary>
        /// 检查是否存在循环关系
        /// 注意：用于 CatalogueHasChild 关系，节点类型为 Catalogue
        /// </summary>
        private async Task<bool> CheckCycleAsync(Guid ancestorId, Guid descendantId)
        {
            try
            {
                // 使用 Apache AGE 查询检查是否存在从 descendantId 到 ancestorId 的路径
                // 注意：节点标签使用具体的实体类型 Catalogue，而不是通用的 Entity
                var cypherQuery = @"
                    MATCH path = (descendant:Catalogue {id: $descendantId})-[*1..10]->(ancestor:Catalogue {id: $ancestorId})
                    WHERE descendant.id = $descendantId AND ancestor.id = $ancestorId
                    RETURN count(path) as pathCount";

                var parameters = new Dictionary<string, object>
                {
                    { "descendantId", descendantId.ToString() },
                    { "ancestorId", ancestorId.ToString() }
                };

                var results = await _knowledgeGraphRepository.ExecuteCypherQueryAsync(cypherQuery, parameters);
                if (results.Count == 0)
                {
                    return false;
                }

                // 解析结果
                var resultJson = JsonDocument.Parse(results[0]);
                if (resultJson.RootElement.TryGetProperty("pathCount", out var pathCountElement))
                {
                    var pathCount = pathCountElement.GetInt64();
                    return pathCount > 0;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "检查循环关系时发生错误，假设不存在循环");
                return false; // 发生错误时，假设不存在循环，允许创建关系
            }
        }

        /// <summary>
        /// 同步关系到 Apache AGE 图数据库
        /// </summary>
        private async Task SyncRelationshipToAgeGraphAsync(KnowledgeGraphRelationship relationship)
        {
            try
            {
                // 映射关系类型到 Cypher 关系名称（与 SQL 脚本保持一致）
                var cypherRelType = relationship.Type switch
                {
                    RelationshipType.CatalogueHasChild => "HAS_CHILD",
                    RelationshipType.CatalogueRelatesToCatalogue => "RELATES_TO",
                    RelationshipType.PersonRelatesToCatalogue => "RELATES_TO",
                    RelationshipType.PersonBelongsToDepartment => "BELONGS_TO",
                    RelationshipType.DepartmentOwnsCatalogue => "OWNS",
                    RelationshipType.DepartmentManagesCatalogue => "MANAGES",
                    RelationshipType.DepartmentHasParent => "HAS_PARENT",
                    RelationshipType.BusinessEntityHasCatalogue => "HAS",
                    RelationshipType.BusinessEntityManagesCatalogue => "MANAGES",
                    RelationshipType.CatalogueReferencesBusiness => "REFERENCES",
                    RelationshipType.CatalogueUsesWorkflow => "USES",
                    RelationshipType.WorkflowManagesCatalogue => "MANAGES",
                    RelationshipType.WorkflowInstanceBelongsToCatalogue => "INSTANCE_OF",
                    RelationshipType.PersonRelatesToWorkflow => "RELATES_TO",
                    RelationshipType.DepartmentOwnsWorkflow => "OWNS",
                    RelationshipType.WorkflowRelatesToWorkflow => "RELATES_TO",
                    _ => "RELATES_TO"
                };

                // 使用实际的实体类型作为节点标签（使用 EntityType 常量确保一致性）
                var sourceEntityType = EntityType.Normalize(relationship.SourceEntityType) ?? relationship.SourceEntityType;
                var targetEntityType = EntityType.Normalize(relationship.TargetEntityType) ?? relationship.TargetEntityType;

                // 构建 Cypher 查询，创建或更新关系
                // 注意：节点标签使用实际的实体类型（Catalogue, Person, Department等），而不是通用的 Entity
                var cypherQuery = $@"
                    MATCH (source:{sourceEntityType} {{id: $sourceId}})
                    MATCH (target:{targetEntityType} {{id: $targetId}})
                    MERGE (source)-[r:{cypherRelType} {{relationshipId: $relationshipId}}]->(target)
                    SET r.type = $type,
                        r.role = $role,
                        r.semanticType = $semanticType,
                        r.description = $description,
                        r.weight = $weight,
                        r.creationTime = $creationTime,
                        r.updatedTime = $updatedTime";

                var parameters = new Dictionary<string, object>
                {
                    { "sourceId", relationship.SourceEntityId.ToString() },
                    { "targetId", relationship.TargetEntityId.ToString() },
                    { "relationshipId", relationship.Id.ToString() },
                    { "type", relationship.Type.ToString() },
                    { "role", relationship.Role ?? "" },
                    { "semanticType", relationship.SemanticType ?? "" },
                    { "description", relationship.Description ?? "" },
                    { "weight", relationship.Weight },
                    { "creationTime", relationship.CreationTime.ToString("O") },
                    { "updatedTime", DateTime.UtcNow.ToString("O") }
                };

                await _knowledgeGraphRepository.ExecuteCypherQueryAsync(cypherQuery, parameters);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "同步关系到 Apache AGE 图数据库失败，RelationshipId={RelationshipId}", relationship.Id);
                // 不抛出异常，允许关系创建成功，图数据库同步失败不影响主流程
            }
        }

        /// <summary>
        /// 映射关系实体到DTO
        /// </summary>
        private static RelationshipDto MapToRelationshipDto(KnowledgeGraphRelationship relationship)
        {
            return new RelationshipDto
            {
                Id = relationship.Id,
                SourceEntityId = relationship.SourceEntityId,
                SourceEntityType = relationship.SourceEntityType,
                TargetEntityId = relationship.TargetEntityId,
                TargetEntityType = relationship.TargetEntityType,
                RelationshipType = relationship.Type,
                Role = relationship.Role,
                SemanticType = relationship.SemanticType,
                Description = relationship.Description,
                Weight = relationship.Weight,
                Properties = relationship.ExtraProperties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                CreationTime = relationship.CreationTime
            };
        }
    }
}

