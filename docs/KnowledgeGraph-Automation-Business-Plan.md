# 知识图谱关系自动化创建业务方案

## 1. 方案概述

### 1.1 背景与目标

知识图谱关系自动化创建业务方案旨在通过事件驱动、智能发现和工作流集成等技术手段，实现知识图谱关系的自动构建和维护，减少人工干预，提升数据质量和业务效率。

**核心目标**：
- **自动化率**：实现80%以上的关系自动创建，减少人工维护成本
- **准确率**：关系创建准确率达到95%以上，降低错误率
- **实时性**：关系创建延迟控制在秒级，支持实时业务需求
- **可追溯性**：完整记录关系创建来源和依据，支持审计和合规要求

### 1.2 业务价值

- **提升效率**：自动化关系创建，减少90%的人工维护工作量
- **保证质量**：基于规则和AI的关系发现，确保关系准确性和完整性
- **增强洞察**：通过行为模式分析，发现隐藏的业务关系
- **合规保障**：完整的审计追踪，满足合规性要求

### 1.3 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                    业务应用层                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 分类管理      │  │ 文件管理      │  │ 工作流管理    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                            ↓ (领域事件)
┌─────────────────────────────────────────────────────────────┐
│                  事件驱动引擎                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 事件监听器    │  │ 规则引擎      │  │ 事件分发器    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              关系自动化创建服务层                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 规则驱动创建  │  │ 智能关系发现  │  │ 工作流集成    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  知识图谱服务层                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 关系创建      │  │ 关系验证      │  │ 审计记录      │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. 事件驱动引擎

### 2.1 架构设计

事件驱动引擎基于ABP框架的领域事件机制，监听业务实体的创建、更新、删除等事件，自动触发关系创建流程。

#### 2.1.1 事件类型定义

```csharp
// 实体变更事件（ABP框架提供）
EntityCreatedEventData<TEntity>
EntityUpdatedEventData<TEntity>
EntityDeletedEventData<TEntity>

// 业务自定义事件
public class FileContentAnalyzedEvent : ILocalEvent
{
    public Guid FileId { get; set; }
    public Guid CatalogueId { get; set; }
    public string ExtractedText { get; set; }
    public Dictionary<string, object> ExtractedEntities { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class WorkflowApprovalCompletedEvent : ILocalEvent
{
    public Guid WorkflowInstanceId { get; set; }
    public Guid CatalogueId { get; set; }
    public Guid ApproverId { get; set; }
    public string ApprovalResult { get; set; }
    public DateTime ApprovedAt { get; set; }
}

public class CatalogueMetadataUpdatedEvent : ILocalEvent
{
    public Guid CatalogueId { get; set; }
    public Dictionary<string, object> UpdatedMetaFields { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### 2.1.2 事件监听器实现

```csharp
// CatalogueCreatedEventHandler.cs
public class CatalogueCreatedEventHandler : 
    ILocalEventHandler<EntityCreatedEventData<AttachCatalogue>>,
    ITransientDependency
{
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly IRelationshipAutoCreationService _autoCreationService;
    private readonly ILogger<CatalogueCreatedEventHandler> _logger;

    public CatalogueCreatedEventHandler(
        IKnowledgeGraphService knowledgeGraphService,
        IRelationshipAutoCreationService autoCreationService,
        ILogger<CatalogueCreatedEventHandler> logger)
    {
        _knowledgeGraphService = knowledgeGraphService;
        _autoCreationService = autoCreationService;
        _logger = logger;
    }

    public async Task HandleEventAsync(EntityCreatedEventData<AttachCatalogue> eventData)
    {
        var catalogue = eventData.Entity;
        
        try
        {
            // 1. 创建父子关系
            if (catalogue.ParentId.HasValue)
            {
                await _autoCreationService.CreateParentChildRelationshipAsync(
                    catalogue.ParentId.Value, 
                    catalogue.Id
                );
            }

            // 2. 创建创建者关系
            if (catalogue.CreatorId.HasValue)
            {
                await _autoCreationService.CreateCreatorRelationshipAsync(
                    catalogue.CreatorId.Value,
                    catalogue.Id
                );
            }

            // 3. 创建业务实体关系
            if (!string.IsNullOrEmpty(catalogue.Reference))
            {
                await _autoCreationService.CreateBusinessEntityRelationshipAsync(
                    catalogue.Id,
                    catalogue.Reference,
                    catalogue.ReferenceType
                );
            }

            // 4. 创建部门关系
            await _autoCreationService.CreateDepartmentRelationshipAsync(catalogue);

            // 5. 创建时间关系（与同一业务实体的其他分类）
            await _autoCreationService.CreateTemporalRelationshipsAsync(catalogue);

            // 6. 从元数据提取人员角色关系
            await _autoCreationService.ExtractPersonRoleRelationshipsFromMetadataAsync(catalogue);

            _logger.LogInformation(
                "分类创建事件处理完成: CatalogueId={CatalogueId}",
                catalogue.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "分类创建事件处理失败: CatalogueId={CatalogueId}",
                catalogue.Id
            );
            // 不抛出异常，避免影响主业务流程
        }
    }
}

// CatalogueUpdatedEventHandler.cs
public class CatalogueUpdatedEventHandler :
    ILocalEventHandler<EntityUpdatedEventData<AttachCatalogue>>,
    ITransientDependency
{
    private readonly IRelationshipAutoCreationService _autoCreationService;
    private readonly ILogger<CatalogueUpdatedEventHandler> _logger;

    public async Task HandleEventAsync(EntityUpdatedEventData<AttachCatalogue> eventData)
    {
        var catalogue = eventData.Entity;
        var oldCatalogue = eventData.OldEntity;

        try
        {
            // 1. 如果父分类变更，更新父子关系
            if (oldCatalogue.ParentId != catalogue.ParentId)
            {
                if (oldCatalogue.ParentId.HasValue)
                {
                    await _autoCreationService.RemoveParentChildRelationshipAsync(
                        oldCatalogue.ParentId.Value,
                        catalogue.Id
                    );
                }
                if (catalogue.ParentId.HasValue)
                {
                    await _autoCreationService.CreateParentChildRelationshipAsync(
                        catalogue.ParentId.Value,
                        catalogue.Id
                    );
                }
            }

            // 2. 如果业务引用变更，更新业务实体关系
            if (oldCatalogue.Reference != catalogue.Reference || 
                oldCatalogue.ReferenceType != catalogue.ReferenceType)
            {
                await _autoCreationService.UpdateBusinessEntityRelationshipsAsync(catalogue);
            }

            // 3. 如果元数据变更，重新提取人员角色关系
            if (HasMetadataChanged(oldCatalogue, catalogue))
            {
                await _autoCreationService.RefreshPersonRoleRelationshipsAsync(catalogue);
            }

            // 4. 创建业务关系（与业务相关的其他分类）
            await _autoCreationService.CreateBusinessRelationshipsAsync(catalogue);

            _logger.LogInformation(
                "分类更新事件处理完成: CatalogueId={CatalogueId}",
                catalogue.Id
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "分类更新事件处理失败: CatalogueId={CatalogueId}",
                catalogue.Id
            );
        }
    }

    private bool HasMetadataChanged(AttachCatalogue oldCatalogue, AttachCatalogue newCatalogue)
    {
        // 比较元数据字段是否变更
        var oldMetaFields = oldCatalogue.MetaFields?.ToDictionary(m => m.FieldName, m => m.FieldValue) ?? new Dictionary<string, object>();
        var newMetaFields = newCatalogue.MetaFields?.ToDictionary(m => m.FieldName, m => m.FieldValue) ?? new Dictionary<string, object>();

        return !oldMetaFields.SequenceEqual(newMetaFields);
    }
}
```

### 2.2 事件处理流程

```
业务实体变更
    ↓
ABP领域事件发布
    ↓
事件监听器接收
    ↓
规则引擎匹配
    ↓
关系创建服务执行
    ↓
关系验证
    ↓
关系持久化（PostgreSQL）
    ↓
异步同步到Neo4j
    ↓
审计日志记录
```

### 2.3 事件优先级和重试机制

```csharp
// 事件优先级配置
public enum EventPriority
{
    Critical = 1,    // 关键事件（如分类创建），立即处理
    High = 2,        // 高优先级事件（如分类更新），延迟<1秒
    Normal = 3,      // 普通事件（如文件上传），延迟<5秒
    Low = 4          // 低优先级事件（如元数据更新），延迟<30秒
}

// 事件重试配置
public class EventRetryPolicy
{
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(5);
    public double BackoffMultiplier { get; set; } = 2.0;
}
```

---

## 3. 业务场景自动化规则

### 3.1 规则引擎设计

规则引擎基于配置化的规则定义，支持动态规则加载和热更新，无需重启服务即可调整规则。

#### 3.1.1 规则定义模型

```csharp
// RelationshipCreationRule.cs
public class RelationshipCreationRule
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public RuleTrigger Trigger { get; set; } // 触发条件
    public RuleCondition Condition { get; set; } // 条件判断
    public RelationshipCreationAction Action { get; set; } // 创建动作
    public int Priority { get; set; } // 优先级（数字越小优先级越高）
    public bool Enabled { get; set; } = true;
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

// 触发条件
public class RuleTrigger
{
    public TriggerType Type { get; set; } // EntityCreated, EntityUpdated, EntityDeleted, CustomEvent
    public string EntityType { get; set; } // Catalogue, File, Workflow等
    public Dictionary<string, object> Properties { get; set; } // 触发属性条件
}

// 条件判断
public class RuleCondition
{
    public List<ConditionExpression> Expressions { get; set; } // 条件表达式列表
    public LogicalOperator Operator { get; set; } // AND, OR
}

public class ConditionExpression
{
    public string Field { get; set; } // 字段名
    public ComparisonOperator Operator { get; set; } // Equals, Contains, GreaterThan等
    public object Value { get; set; } // 比较值
}

// 创建动作
public class RelationshipCreationAction
{
    public RelationshipType RelationshipType { get; set; }
    public string? Role { get; set; } // 用于PersonRelatesToCatalogue等
    public string? SemanticType { get; set; } // 用于CatalogueRelatesToCatalogue等
    public Dictionary<string, object> SourceEntityMapping { get; set; } // 源实体映射规则
    public Dictionary<string, object> TargetEntityMapping { get; set; } // 目标实体映射规则
    public Dictionary<string, object> Properties { get; set; } // 关系属性
    public double? Weight { get; set; } // 关系权重
}
```

#### 3.1.2 规则引擎实现

```csharp
// IRelationshipRuleEngine.cs
public interface IRelationshipRuleEngine
{
    /// <summary>
    /// 评估规则并返回匹配的规则
    /// </summary>
    Task<List<RelationshipCreationRule>> EvaluateRulesAsync(
        RuleTrigger trigger,
        Dictionary<string, object> context
    );

    /// <summary>
    /// 执行规则创建关系
    /// </summary>
    Task<List<RelationshipDto>> ExecuteRulesAsync(
        List<RelationshipCreationRule> rules,
        Dictionary<string, object> context
    );

    /// <summary>
    /// 加载规则配置
    /// </summary>
    Task LoadRulesAsync();

    /// <summary>
    /// 重新加载规则配置（热更新）
    /// </summary>
    Task ReloadRulesAsync();
}

// RelationshipRuleEngine.cs
public class RelationshipRuleEngine : IRelationshipRuleEngine, ITransientDependency
{
    private readonly IRepository<RelationshipCreationRule, Guid> _ruleRepository;
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly ILogger<RelationshipRuleEngine> _logger;
    private List<RelationshipCreationRule> _cachedRules = new List<RelationshipCreationRule>();
    private readonly SemaphoreSlim _reloadLock = new SemaphoreSlim(1, 1);

    public RelationshipRuleEngine(
        IRepository<RelationshipCreationRule, Guid> ruleRepository,
        IKnowledgeGraphService knowledgeGraphService,
        ILogger<RelationshipRuleEngine> logger)
    {
        _ruleRepository = ruleRepository;
        _knowledgeGraphService = knowledgeGraphService;
        _logger = logger;
    }

    public async Task<List<RelationshipCreationRule>> EvaluateRulesAsync(
        RuleTrigger trigger,
        Dictionary<string, object> context)
    {
        // 确保规则已加载
        if (_cachedRules.Count == 0)
        {
            await LoadRulesAsync();
        }

        var matchedRules = new List<RelationshipCreationRule>();

        foreach (var rule in _cachedRules.Where(r => r.Enabled))
        {
            if (await MatchTriggerAsync(rule.Trigger, trigger) &&
                await EvaluateConditionAsync(rule.Condition, context))
            {
                matchedRules.Add(rule);
            }
        }

        // 按优先级排序
        return matchedRules.OrderBy(r => r.Priority).ToList();
    }

    public async Task<List<RelationshipDto>> ExecuteRulesAsync(
        List<RelationshipCreationRule> rules,
        Dictionary<string, object> context)
    {
        var createdRelationships = new List<RelationshipDto>();

        foreach (var rule in rules)
        {
            try
            {
                var relationship = await ExecuteRuleAsync(rule, context);
                if (relationship != null)
                {
                    createdRelationships.Add(relationship);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "执行规则失败: RuleId={RuleId}, RuleName={RuleName}",
                    rule.Id, rule.Name
                );
                // 继续执行下一个规则，不中断流程
            }
        }

        return createdRelationships;
    }

    private async Task<bool> MatchTriggerAsync(RuleTrigger ruleTrigger, RuleTrigger eventTrigger)
    {
        if (ruleTrigger.Type != eventTrigger.Type)
            return false;

        if (ruleTrigger.EntityType != eventTrigger.EntityType)
            return false;

        // 检查属性条件
        if (ruleTrigger.Properties != null && ruleTrigger.Properties.Any())
        {
            foreach (var prop in ruleTrigger.Properties)
            {
                if (!eventTrigger.Properties.ContainsKey(prop.Key) ||
                    !Equals(eventTrigger.Properties[prop.Key], prop.Value))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private async Task<bool> EvaluateConditionAsync(
        RuleCondition condition,
        Dictionary<string, object> context)
    {
        if (condition?.Expressions == null || !condition.Expressions.Any())
            return true;

        var results = new List<bool>();

        foreach (var expression in condition.Expressions)
        {
            var fieldValue = GetFieldValue(context, expression.Field);
            var result = EvaluateExpression(expression, fieldValue);
            results.Add(result);
        }

        return condition.Operator == LogicalOperator.And
            ? results.All(r => r)
            : results.Any(r => r);
    }

    private object? GetFieldValue(Dictionary<string, object> context, string field)
    {
        // 支持嵌套字段访问，如 "Entity.ParentId"
        var parts = field.Split('.');
        object? current = context;

        foreach (var part in parts)
        {
            if (current is Dictionary<string, object> dict)
            {
                current = dict.GetValueOrDefault(part);
            }
            else if (current != null)
            {
                var prop = current.GetType().GetProperty(part);
                current = prop?.GetValue(current);
            }
            else
            {
                return null;
            }
        }

        return current;
    }

    private bool EvaluateExpression(ConditionExpression expression, object? fieldValue)
    {
        return expression.Operator switch
        {
            ComparisonOperator.Equals => Equals(fieldValue, expression.Value),
            ComparisonOperator.NotEquals => !Equals(fieldValue, expression.Value),
            ComparisonOperator.Contains => fieldValue?.ToString()?.Contains(expression.Value?.ToString() ?? "") ?? false,
            ComparisonOperator.GreaterThan => CompareValues(fieldValue, expression.Value) > 0,
            ComparisonOperator.LessThan => CompareValues(fieldValue, expression.Value) < 0,
            ComparisonOperator.IsNull => fieldValue == null,
            ComparisonOperator.IsNotNull => fieldValue != null,
            _ => false
        };
    }

    private int CompareValues(object? value1, object? value2)
    {
        if (value1 == null && value2 == null) return 0;
        if (value1 == null) return -1;
        if (value2 == null) return 1;

        if (value1 is IComparable comp1 && value2 is IComparable comp2)
        {
            return comp1.CompareTo(comp2);
        }

        return string.Compare(value1.ToString(), value2?.ToString(), StringComparison.Ordinal);
    }

    private async Task<RelationshipDto?> ExecuteRuleAsync(
        RelationshipCreationRule rule,
        Dictionary<string, object> context)
    {
        var action = rule.Action;

        // 解析源实体ID
        var sourceEntityId = ResolveEntityId(action.SourceEntityMapping, context);
        if (!sourceEntityId.HasValue)
            return null;

        // 解析目标实体ID
        var targetEntityId = ResolveEntityId(action.TargetEntityMapping, context);
        if (!targetEntityId.HasValue)
            return null;

        // 解析实体类型
        var sourceEntityType = ResolveEntityType(action.SourceEntityMapping, context);
        var targetEntityType = ResolveEntityType(action.TargetEntityMapping, context);

        // 检查关系是否已存在
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            sourceEntityId.Value,
            targetEntityId.Value,
            action.RelationshipType,
            action.Role,
            action.SemanticType
        );

        if (exists)
        {
            _logger.LogDebug(
                "关系已存在，跳过创建: SourceId={SourceId}, TargetId={TargetId}, Type={Type}",
                sourceEntityId, targetEntityId, action.RelationshipType
            );
            return null;
        }

        // 创建关系
        var input = new CreateRelationshipInput
        {
            SourceEntityId = sourceEntityId.Value,
            SourceEntityType = sourceEntityType,
            TargetEntityId = targetEntityId.Value,
            TargetEntityType = targetEntityType,
            RelationshipType = action.RelationshipType,
            Role = action.Role,
            SemanticType = action.SemanticType,
            Description = $"自动创建：{rule.Name}",
            Weight = action.Weight ?? 1.0,
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["ruleId"] = rule.Id.ToString(),
                ["ruleName"] = rule.Name,
                ["createdAt"] = DateTime.UtcNow
            }
        };

        // 合并规则属性
        if (action.Properties != null)
        {
            foreach (var prop in action.Properties)
            {
                input.Properties[prop.Key] = prop.Value;
            }
        }

        return await _knowledgeGraphService.CreateRelationshipAsync(input);
    }

    private Guid? ResolveEntityId(
        Dictionary<string, object> mapping,
        Dictionary<string, object> context)
    {
        if (mapping == null || !mapping.ContainsKey("entityId"))
            return null;

        var entityIdPath = mapping["entityId"]?.ToString();
        if (string.IsNullOrEmpty(entityIdPath))
            return null;

        var value = GetFieldValue(context, entityIdPath);
        if (value is Guid guid)
            return guid;
        if (value is string str && Guid.TryParse(str, out var parsedGuid))
            return parsedGuid;

        return null;
    }

    private string ResolveEntityType(
        Dictionary<string, object> mapping,
        Dictionary<string, object> context)
    {
        if (mapping == null || !mapping.ContainsKey("entityType"))
            return "Unknown";

        var entityTypePath = mapping["entityType"]?.ToString();
        if (string.IsNullOrEmpty(entityTypePath))
            return mapping.GetValueOrDefault("entityType")?.ToString() ?? "Unknown";

        var value = GetFieldValue(context, entityTypePath);
        return value?.ToString() ?? "Unknown";
    }

    public async Task LoadRulesAsync()
    {
        await _reloadLock.WaitAsync();
        try
        {
            var rules = await _ruleRepository.GetListAsync(
                predicate: r => r.Enabled,
                orderBy: r => r.OrderBy(rule => rule.Priority)
            );
            _cachedRules = rules;
            _logger.LogInformation("规则加载完成: Count={Count}", rules.Count);
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    public async Task ReloadRulesAsync()
    {
        await LoadRulesAsync();
    }
}
```

### 3.2 预定义业务规则

#### 3.2.1 分类创建规则

```csharp
// 规则1：创建父子关系
var parentChildRule = new RelationshipCreationRule
{
    Name = "分类创建-父子关系",
    Description = "当分类有父分类时，自动创建父子关系",
    Trigger = new RuleTrigger
    {
        Type = TriggerType.EntityCreated,
        EntityType = "Catalogue",
        Properties = new Dictionary<string, object>
        {
            ["hasParent"] = true
        }
    },
    Condition = new RuleCondition
    {
        Expressions = new List<ConditionExpression>
        {
            new ConditionExpression
            {
                Field = "Entity.ParentId",
                Operator = ComparisonOperator.IsNotNull
            }
        },
        Operator = LogicalOperator.And
    },
    Action = new RelationshipCreationAction
    {
        RelationshipType = RelationshipType.CatalogueHasChild,
        SourceEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.ParentId",
            ["entityType"] = "Catalogue"
        },
        TargetEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.Id",
            ["entityType"] = "Catalogue"
        },
        Properties = new Dictionary<string, object>
        {
            ["autoCreated"] = true,
            ["createdReason"] = "分类创建"
        }
    },
    Priority = 1
};

// 规则2：创建创建者关系
var creatorRule = new RelationshipCreationRule
{
    Name = "分类创建-创建者关系",
    Description = "当分类创建时，自动创建创建者关系",
    Trigger = new RuleTrigger
    {
        Type = TriggerType.EntityCreated,
        EntityType = "Catalogue"
    },
    Condition = new RuleCondition
    {
        Expressions = new List<ConditionExpression>
        {
            new ConditionExpression
            {
                Field = "Entity.CreatorId",
                Operator = ComparisonOperator.IsNotNull
            }
        },
        Operator = LogicalOperator.And
    },
    Action = new RelationshipCreationAction
    {
        RelationshipType = RelationshipType.PersonRelatesToCatalogue,
        Role = PersonRole.Creator.ToString(),
        SourceEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.CreatorId",
            ["entityType"] = "Person"
        },
        TargetEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.Id",
            ["entityType"] = "Catalogue"
        },
        Properties = new Dictionary<string, object>
        {
            ["autoCreated"] = true,
            ["createdReason"] = "分类创建"
        }
    },
    Priority = 2
};
```

#### 3.2.2 文件上传规则

```csharp
// 规则3：创建文件包含关系
var fileContainsRule = new RelationshipCreationRule
{
    Name = "文件上传-包含关系",
    Description = "当文件上传到分类时，自动创建包含关系",
    Trigger = new RuleTrigger
    {
        Type = TriggerType.EntityCreated,
        EntityType = "File"
    },
    Condition = new RuleCondition
    {
        Expressions = new List<ConditionExpression>
        {
            new ConditionExpression
            {
                Field = "Entity.AttachCatalogueId",
                Operator = ComparisonOperator.IsNotNull
            }
        },
        Operator = LogicalOperator.And
    },
    Action = new RelationshipCreationAction
    {
        RelationshipType = RelationshipType.CatalogueContainsFile,
        SourceEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.AttachCatalogueId",
            ["entityType"] = "Catalogue"
        },
        TargetEntityMapping = new Dictionary<string, object>
        {
            ["entityId"] = "Entity.Id",
            ["entityType"] = "File"
        },
        Properties = new Dictionary<string, object>
        {
            ["autoCreated"] = true,
            ["createdReason"] = "文件上传"
        }
    },
    Priority = 1
};
```

### 3.3 规则管理API

```csharp
// RelationshipRuleController.cs
[Route("api/knowledge-graph/automation/rules")]
public class RelationshipRuleController : AbpControllerBase
{
    private readonly IRelationshipRuleEngine _ruleEngine;
    private readonly IRepository<RelationshipCreationRule, Guid> _ruleRepository;

    /// <summary>
    /// 获取所有规则
    /// </summary>
    [HttpGet]
    public async Task<PagedResultDto<RelationshipCreationRuleDto>> GetRulesAsync(
        [FromQuery] RuleQueryInput input)
    {
        var queryable = await _ruleRepository.GetQueryableAsync();
        
        if (!string.IsNullOrEmpty(input.Name))
        {
            queryable = queryable.Where(r => r.Name.Contains(input.Name));
        }
        
        if (input.Enabled.HasValue)
        {
            queryable = queryable.Where(r => r.Enabled == input.Enabled.Value);
        }

        var totalCount = await AsyncExecuter.LongCountAsync(queryable);
        var rules = await AsyncExecuter.ToListAsync(
            queryable
                .OrderBy(r => r.Priority)
                .Skip((input.PageIndex - 1) * input.PageSize)
                .Take(input.PageSize)
        );

        return new PagedResultDto<RelationshipCreationRuleDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<RelationshipCreationRule>, List<RelationshipCreationRuleDto>>(rules)
        };
    }

    /// <summary>
    /// 创建规则
    /// </summary>
    [HttpPost]
    public async Task<RelationshipCreationRuleDto> CreateRuleAsync(
        [FromBody] CreateRelationshipRuleInput input)
    {
        var rule = ObjectMapper.Map<CreateRelationshipRuleInput, RelationshipCreationRule>(input);
        rule.Id = GuidGenerator.Create();
        rule.CreatedTime = DateTime.UtcNow;
        
        await _ruleRepository.InsertAsync(rule);
        
        // 重新加载规则
        await _ruleEngine.ReloadRulesAsync();
        
        return ObjectMapper.Map<RelationshipCreationRule, RelationshipCreationRuleDto>(rule);
    }

    /// <summary>
    /// 更新规则
    /// </summary>
    [HttpPut("{id}")]
    public async Task<RelationshipCreationRuleDto> UpdateRuleAsync(
        Guid id,
        [FromBody] UpdateRelationshipRuleInput input)
    {
        var rule = await _ruleRepository.GetAsync(id);
        ObjectMapper.Map(input, rule);
        rule.UpdatedTime = DateTime.UtcNow;
        
        await _ruleRepository.UpdateAsync(rule);
        
        // 重新加载规则
        await _ruleEngine.ReloadRulesAsync();
        
        return ObjectMapper.Map<RelationshipCreationRule, RelationshipCreationRuleDto>(rule);
    }

    /// <summary>
    /// 启用/禁用规则
    /// </summary>
    [HttpPut("{id}/toggle")]
    public async Task<RelationshipCreationRuleDto> ToggleRuleAsync(Guid id)
    {
        var rule = await _ruleRepository.GetAsync(id);
        rule.Enabled = !rule.Enabled;
        rule.UpdatedTime = DateTime.UtcNow;
        
        await _ruleRepository.UpdateAsync(rule);
        
        // 重新加载规则
        await _ruleEngine.ReloadRulesAsync();
        
        return ObjectMapper.Map<RelationshipCreationRule, RelationshipCreationRuleDto>(rule);
    }

    /// <summary>
    /// 测试规则
    /// </summary>
    [HttpPost("{id}/test")]
    public async Task<TestRuleResultDto> TestRuleAsync(
        Guid id,
        [FromBody] Dictionary<string, object> testContext)
    {
        var rule = await _ruleRepository.GetAsync(id);
        var trigger = rule.Trigger;
        
        // 评估条件
        var conditionMatched = await _ruleEngine.EvaluateConditionAsync(rule.Condition, testContext);
        
        // 执行规则（模拟）
        var relationships = await _ruleEngine.ExecuteRulesAsync(
            new List<RelationshipCreationRule> { rule },
            testContext
        );

        return new TestRuleResultDto
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            ConditionMatched = conditionMatched,
            CreatedRelationships = relationships,
            TestContext = testContext
        };
    }
}
```

---

## 4. 分类体系自动化构建

### 4.1 分类层级关系自动构建

分类体系自动化构建基于分类的`ParentId`和`Path`字段，自动建立和维护分类间的层级关系。

#### 4.1.1 层级关系构建服务

```csharp
// IClassificationSystemAutoBuilder.cs
public interface IClassificationSystemAutoBuilder
{
    /// <summary>
    /// 构建分类层级关系
    /// </summary>
    Task BuildHierarchyRelationshipsAsync(Guid catalogueId);

    /// <summary>
    /// 重建整个分类体系的层级关系
    /// </summary>
    Task RebuildAllHierarchyRelationshipsAsync();

    /// <summary>
    /// 构建分类间的业务关系
    /// </summary>
    Task BuildBusinessRelationshipsAsync(Guid catalogueId);

    /// <summary>
    /// 构建分类间的时间关系
    /// </summary>
    Task BuildTemporalRelationshipsAsync(Guid catalogueId);
}

// ClassificationSystemAutoBuilder.cs
public class ClassificationSystemAutoBuilder : IClassificationSystemAutoBuilder, ITransientDependency
{
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly ILogger<ClassificationSystemAutoBuilder> _logger;

    public ClassificationSystemAutoBuilder(
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        IKnowledgeGraphService knowledgeGraphService,
        ILogger<ClassificationSystemAutoBuilder> logger)
    {
        _catalogueRepository = catalogueRepository;
        _knowledgeGraphService = knowledgeGraphService;
        _logger = logger;
    }

    public async Task BuildHierarchyRelationshipsAsync(Guid catalogueId)
    {
        var catalogue = await _catalogueRepository.GetAsync(catalogueId);

        // 1. 创建直接父子关系
        if (catalogue.ParentId.HasValue)
        {
            await CreateParentChildRelationshipAsync(catalogue.ParentId.Value, catalogueId);
        }

        // 2. 创建所有祖先关系（向上遍历）
        await CreateAncestorRelationshipsAsync(catalogue);

        // 3. 创建所有后代关系（向下遍历）
        await CreateDescendantRelationshipsAsync(catalogue);
    }

    private async Task CreateParentChildRelationshipAsync(Guid parentId, Guid childId)
    {
        // 检查关系是否已存在
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            parentId,
            childId,
            RelationshipType.CatalogueHasChild,
            null,
            null
        );

        if (exists)
            return;

        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
        {
            SourceEntityId = parentId,
            SourceEntityType = "Catalogue",
            TargetEntityId = childId,
            TargetEntityType = "Catalogue",
            RelationshipType = RelationshipType.CatalogueHasChild,
            Description = "分类层级关系",
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["createdReason"] = "分类体系构建",
                ["relationshipLevel"] = "direct"
            }
        });
    }

    private async Task CreateAncestorRelationshipsAsync(AttachCatalogue catalogue)
    {
        if (!catalogue.ParentId.HasValue)
            return;

        var currentParentId = catalogue.ParentId.Value;
        var level = 1;

        while (currentParentId != Guid.Empty)
        {
            var parent = await _catalogueRepository.FindAsync(currentParentId);
            if (parent == null)
                break;

            // 创建间接祖先关系（用于快速查询）
            await CreateIndirectRelationshipAsync(
                parent.Id,
                catalogue.Id,
                RelationshipType.CatalogueRelatesToCatalogue,
                CatalogueSemanticType.DependsOn.ToString(),
                level
            );

            currentParentId = parent.ParentId ?? Guid.Empty;
            level++;
        }
    }

    private async Task CreateDescendantRelationshipsAsync(AttachCatalogue catalogue)
    {
        // 获取所有子分类
        var children = await _catalogueRepository.GetListAsync(
            predicate: c => c.ParentId == catalogue.Id
        );

        foreach (var child in children)
        {
            // 创建父子关系
            await CreateParentChildRelationshipAsync(catalogue.Id, child.Id);

            // 递归处理子分类
            await CreateDescendantRelationshipsAsync(child);
        }
    }

    private async Task CreateIndirectRelationshipAsync(
        Guid sourceId,
        Guid targetId,
        RelationshipType relationshipType,
        string semanticType,
        int level)
    {
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            sourceId,
            targetId,
            relationshipType,
            null,
            semanticType
        );

        if (exists)
            return;

        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
        {
            SourceEntityId = sourceId,
            SourceEntityType = "Catalogue",
            TargetEntityId = targetId,
            TargetEntityType = "Catalogue",
            RelationshipType = relationshipType,
            SemanticType = semanticType,
            Description = $"间接关系（{level}级）",
            Weight = 1.0 / level, // 层级越远，权重越低
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["createdReason"] = "分类体系构建",
                ["relationshipLevel"] = level,
                ["isIndirect"] = true
            }
        });
    }

    public async Task RebuildAllHierarchyRelationshipsAsync()
    {
        _logger.LogInformation("开始重建所有分类层级关系");

        // 获取所有分类
        var allCatalogues = await _catalogueRepository.GetListAsync();

        // 按层级排序（从根节点开始）
        var rootCatalogues = allCatalogues.Where(c => !c.ParentId.HasValue).ToList();

        foreach (var root in rootCatalogues)
        {
            await BuildHierarchyRelationshipsAsync(root.Id);
        }

        _logger.LogInformation("分类层级关系重建完成: TotalCount={Count}", allCatalogues.Count);
    }

    public async Task BuildBusinessRelationshipsAsync(Guid catalogueId)
    {
        var catalogue = await _catalogueRepository.GetAsync(catalogueId);

        if (string.IsNullOrEmpty(catalogue.Reference))
            return;

        // 查找同一业务实体的其他分类
        var relatedCatalogues = await _catalogueRepository.GetListAsync(
            predicate: c => c.Id != catalogueId &&
                           c.Reference == catalogue.Reference &&
                           c.ReferenceType == catalogue.ReferenceType
        );

        foreach (var relatedCatalogue in relatedCatalogues)
        {
            // 检查关系是否已存在
            var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
                catalogueId,
                relatedCatalogue.Id,
                RelationshipType.CatalogueRelatesToCatalogue,
                null,
                CatalogueSemanticType.Business.ToString()
            );

            if (exists)
                continue;

            // 创建业务关系
            await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
            {
                SourceEntityId = catalogueId,
                SourceEntityType = "Catalogue",
                TargetEntityId = relatedCatalogue.Id,
                TargetEntityType = "Catalogue",
                RelationshipType = RelationshipType.CatalogueRelatesToCatalogue,
                SemanticType = CatalogueSemanticType.Business.ToString(),
                Description = $"业务关系：{catalogue.Reference}",
                Properties = new Dictionary<string, object>
                {
                    ["autoCreated"] = true,
                    ["createdReason"] = "业务关系构建",
                    ["reference"] = catalogue.Reference,
                    ["referenceType"] = catalogue.ReferenceType
                }
            });
        }
    }

    public async Task BuildTemporalRelationshipsAsync(Guid catalogueId)
    {
        var catalogue = await _catalogueRepository.GetAsync(catalogueId);

        if (string.IsNullOrEmpty(catalogue.Reference))
            return;

        // 查找同一业务实体的其他分类，按创建时间排序
        var relatedCatalogues = await _catalogueRepository.GetListAsync(
            predicate: c => c.Id != catalogueId &&
                           c.Reference == catalogue.Reference &&
                           c.ReferenceType == catalogue.ReferenceType,
            orderBy: c => c.OrderBy(cat => cat.CreationTime)
        );

        // 创建时间序列关系
        for (int i = 0; i < relatedCatalogues.Count - 1; i++)
        {
            var current = relatedCatalogues[i];
            var next = relatedCatalogues[i + 1];

            var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
                current.Id,
                next.Id,
                RelationshipType.CatalogueRelatesToCatalogue,
                null,
                CatalogueSemanticType.Temporal.ToString()
            );

            if (exists)
                continue;

            await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
            {
                SourceEntityId = current.Id,
                SourceEntityType = "Catalogue",
                TargetEntityId = next.Id,
                TargetEntityType = "Catalogue",
                RelationshipType = RelationshipType.CatalogueRelatesToCatalogue,
                SemanticType = CatalogueSemanticType.Temporal.ToString(),
                Description = $"时间关系：{current.CreationTime:yyyy-MM-dd} -> {next.CreationTime:yyyy-MM-dd}",
                Properties = new Dictionary<string, object>
                {
                    ["autoCreated"] = true,
                    ["createdReason"] = "时间关系构建",
                    ["sourceTime"] = current.CreationTime,
                    ["targetTime"] = next.CreationTime,
                    ["timeGap"] = (next.CreationTime - current.CreationTime).TotalDays
                }
            });
        }
    }
}
```

---

## 5. 智能关系发现引擎

### 5.1 语义关系自动挖掘

语义关系自动挖掘通过NLP技术和实体识别，从文件内容、元数据等非结构化数据中提取实体和关系。

#### 5.1.1 实体识别服务

```csharp
// IEntityRecognitionService.cs
public interface IEntityRecognitionService
{
    /// <summary>
    /// 从文本中识别实体
    /// </summary>
    Task<EntityRecognitionResult> RecognizeEntitiesAsync(string text, List<string> entityTypes);

    /// <summary>
    /// 从文本中提取关系
    /// </summary>
    Task<List<ExtractedRelationship>> ExtractRelationshipsAsync(string text);

    /// <summary>
    /// 识别人员角色
    /// </summary>
    Task<List<PersonRoleInfo>> RecognizePersonRolesAsync(string text);
}

// EntityRecognitionResult.cs
public class EntityRecognitionResult
{
    public List<RecognizedEntity> Entities { get; set; } = new List<RecognizedEntity>();
    public double Confidence { get; set; }
}

public class RecognizedEntity
{
    public string Text { get; set; }
    public string Type { get; set; } // Person, Department, Catalogue, BusinessEntity
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
}

public class ExtractedRelationship
{
    public string SourceEntityText { get; set; }
    public string TargetEntityText { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string? Role { get; set; }
    public string? SemanticType { get; set; }
    public double Confidence { get; set; }
    public string Context { get; set; } // 提取关系的上下文文本
}

public class PersonRoleInfo
{
    public string PersonName { get; set; }
    public PersonRole Role { get; set; }
    public string Context { get; set; }
    public double Confidence { get; set; }
}
```

#### 5.1.2 实体识别实现

```csharp
// EntityRecognitionService.cs
public class EntityRecognitionService : IEntityRecognitionService, ITransientDependency
{
    private readonly ILogger<EntityRecognitionService> _logger;
    private readonly IRepository<Person, Guid> _personRepository; // 假设存在Person实体

    // 人员角色关键词模式
    private static readonly Dictionary<string, PersonRole> RolePatterns = new Dictionary<string, PersonRole>
    {
        [@"项目经理[：:]\s*(\S+)"] = PersonRole.ProjectManager,
        [@"项目负责人[：:]\s*(\S+)"] = PersonRole.ProjectManager,
        [@"审核人[：:]\s*(\S+)"] = PersonRole.Reviewer,
        [@"审批人[：:]\s*(\S+)"] = PersonRole.Reviewer,
        [@"专家[：:]\s*(\S+)"] = PersonRole.Expert,
        [@"顾问[：:]\s*(\S+)"] = PersonRole.Expert,
        [@"责任人[：:]\s*(\S+)"] = PersonRole.Responsible,
        [@"负责人[：:]\s*(\S+)"] = PersonRole.Responsible,
        [@"联系人[：:]\s*(\S+)"] = PersonRole.Contact,
        [@"参与人[：:]\s*(\S+)"] = PersonRole.Participant,
        [@"经办人[：:]\s*(\S+)"] = PersonRole.Participant
    };

    // 中文姓名正则表达式（2-4个汉字）
    private static readonly Regex ChineseNameRegex = new Regex(@"[\u4e00-\u9fa5]{2,4}", RegexOptions.Compiled);

    public async Task<EntityRecognitionResult> RecognizeEntitiesAsync(
        string text,
        List<string> entityTypes)
    {
        var result = new EntityRecognitionResult();
        var entities = new List<RecognizedEntity>();

        if (string.IsNullOrWhiteSpace(text))
            return result;

        // 识别人员实体
        if (entityTypes.Contains("Person"))
        {
            var personEntities = RecognizePersonEntities(text);
            entities.AddRange(personEntities);
        }

        // 识别部门实体
        if (entityTypes.Contains("Department"))
        {
            var departmentEntities = RecognizeDepartmentEntities(text);
            entities.AddRange(departmentEntities);
        }

        // 识别分类实体（通过引用编号）
        if (entityTypes.Contains("Catalogue"))
        {
            var catalogueEntities = RecognizeCatalogueEntities(text);
            entities.AddRange(catalogueEntities);
        }

        result.Entities = entities;
        result.Confidence = CalculateOverallConfidence(entities);

        return result;
    }

    private List<RecognizedEntity> RecognizePersonEntities(string text)
    {
        var entities = new List<RecognizedEntity>();

        // 使用正则表达式识别中文姓名
        var nameMatches = ChineseNameRegex.Matches(text);
        foreach (Match match in nameMatches)
        {
            var name = match.Value;
            
            // 过滤常见非人名词汇
            if (IsCommonWord(name))
                continue;

            entities.Add(new RecognizedEntity
            {
                Text = name,
                Type = "Person",
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.7, // 基础置信度
                Properties = new Dictionary<string, object>
                {
                    ["name"] = name
                }
            });
        }

        return entities;
    }

    private List<RecognizedEntity> RecognizeDepartmentEntities(string text)
    {
        var entities = new List<RecognizedEntity>();

        // 部门名称模式（如"技术部"、"财务部"等）
        var departmentPattern = new Regex(@"([\u4e00-\u9fa5]+(?:部|处|科|室|中心|组))", RegexOptions.Compiled);
        var matches = departmentPattern.Matches(text);

        foreach (Match match in matches)
        {
            entities.Add(new RecognizedEntity
            {
                Text = match.Value,
                Type = "Department",
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.8,
                Properties = new Dictionary<string, object>
                {
                    ["name"] = match.Value
                }
            });
        }

        return entities;
    }

    private List<RecognizedEntity> RecognizeCatalogueEntities(string text)
    {
        var entities = new List<RecognizedEntity>();

        // 分类引用编号模式（如"PRJ-001"、"CTR-2024-001"等）
        var cataloguePattern = new Regex(@"[A-Z]{2,4}-\d{4,}-\d{3,}", RegexOptions.Compiled);
        var matches = cataloguePattern.Matches(text);

        foreach (Match match in matches)
        {
            entities.Add(new RecognizedEntity
            {
                Text = match.Value,
                Type = "Catalogue",
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.9,
                Properties = new Dictionary<string, object>
                {
                    ["reference"] = match.Value
                }
            });
        }

        return entities;
    }

    public async Task<List<ExtractedRelationship>> ExtractRelationshipsAsync(string text)
    {
        var relationships = new List<ExtractedRelationship>();

        if (string.IsNullOrWhiteSpace(text))
            return relationships;

        // 提取人员角色关系
        var personRoleRelationships = ExtractPersonRoleRelationships(text);
        relationships.AddRange(personRoleRelationships);

        // 提取分类引用关系
        var catalogueReferenceRelationships = ExtractCatalogueReferenceRelationships(text);
        relationships.AddRange(catalogueReferenceRelationships);

        return relationships;
    }

    private List<ExtractedRelationship> ExtractPersonRoleRelationships(string text)
    {
        var relationships = new List<ExtractedRelationship>();

        foreach (var pattern in RolePatterns)
        {
            var regex = new Regex(pattern.Key, RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var personName = match.Groups[1].Value.Trim();
                    
                    relationships.Add(new ExtractedRelationship
                    {
                        SourceEntityText = personName,
                        TargetEntityText = "", // 需要从上下文获取分类ID
                        RelationshipType = RelationshipType.PersonRelatesToCatalogue,
                        Role = pattern.Value.ToString(),
                        Confidence = 0.8,
                        Context = match.Value
                    });
                }
            }
        }

        return relationships;
    }

    private List<ExtractedRelationship> ExtractCatalogueReferenceRelationships(string text)
    {
        var relationships = new List<ExtractedRelationship>();

        // 识别"参考"、"引用"、"基于"等关键词
        var referencePattern = new Regex(
            @"(?:参考|引用|基于|依据)[：:]\s*([A-Z]{2,4}-\d{4,}-\d{3,})",
            RegexOptions.IgnoreCase
        );

        var matches = referencePattern.Matches(text);
        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var referenceCode = match.Groups[1].Value;

                relationships.Add(new ExtractedRelationship
                {
                    SourceEntityText = "", // 需要从上下文获取当前分类ID
                    TargetEntityText = referenceCode,
                    RelationshipType = RelationshipType.CatalogueRelatesToCatalogue,
                    SemanticType = CatalogueSemanticType.References.ToString(),
                    Confidence = 0.85,
                    Context = match.Value
                });
            }
        }

        return relationships;
    }

    public async Task<List<PersonRoleInfo>> RecognizePersonRolesAsync(string text)
    {
        var roles = new List<PersonRoleInfo>();

        foreach (var pattern in RolePatterns)
        {
            var regex = new Regex(pattern.Key, RegexOptions.IgnoreCase);
            var matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var personName = match.Groups[1].Value.Trim();

                    roles.Add(new PersonRoleInfo
                    {
                        PersonName = personName,
                        Role = pattern.Value,
                        Context = match.Value,
                        Confidence = 0.8
                    });
                }
            }
        }

        return roles;
    }

    private bool IsCommonWord(string word)
    {
        // 常见非人名词汇列表
        var commonWords = new HashSet<string>
        {
            "项目", "流程", "档案", "文件", "部门", "人员", "系统", "管理",
            "审核", "审批", "创建", "更新", "删除", "查询", "统计", "分析"
        };

        return commonWords.Contains(word);
    }

    private double CalculateOverallConfidence(List<RecognizedEntity> entities)
    {
        if (entities.Count == 0)
            return 0.0;

        return entities.Average(e => e.Confidence);
    }
}
```

#### 5.1.3 文件内容分析处理器

```csharp
// FileContentAnalyzedHandler.cs
public class FileContentAnalyzedHandler : 
    ILocalEventHandler<FileContentAnalyzedEvent>,
    ITransientDependency
{
    private readonly IEntityRecognitionService _entityRecognitionService;
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
    private readonly IRepository<AttachFile, Guid> _fileRepository;
    private readonly ILogger<FileContentAnalyzedHandler> _logger;

    public FileContentAnalyzedHandler(
        IEntityRecognitionService entityRecognitionService,
        IKnowledgeGraphService knowledgeGraphService,
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        IRepository<AttachFile, Guid> fileRepository,
        ILogger<FileContentAnalyzedHandler> logger)
    {
        _entityRecognitionService = entityRecognitionService;
        _knowledgeGraphService = knowledgeGraphService;
        _catalogueRepository = catalogueRepository;
        _fileRepository = fileRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(FileContentAnalyzedEvent eventData)
    {
        var file = await _fileRepository.GetAsync(eventData.FileId);
        if (file == null || !file.AttachCatalogueId.HasValue)
            return;

        var catalogue = await _catalogueRepository.GetAsync(file.AttachCatalogueId.Value);
        if (catalogue == null)
            return;

        try
        {
            var extractedText = eventData.ExtractedText;

            // 1. 识别人员角色关系
            var personRoles = await _entityRecognitionService.RecognizePersonRolesAsync(extractedText);
            await CreatePersonRoleRelationshipsAsync(personRoles, catalogue.Id);

            // 2. 识别实体
            var recognitionResult = await _entityRecognitionService.RecognizeEntitiesAsync(
                extractedText,
                new List<string> { "Person", "Department", "Catalogue" }
            );

            // 3. 提取关系
            var relationships = await _entityRecognitionService.ExtractRelationshipsAsync(extractedText);
            await CreateExtractedRelationshipsAsync(relationships, catalogue.Id);

            _logger.LogInformation(
                "文件内容分析完成: FileId={FileId}, CatalogueId={CatalogueId}, " +
                "RecognizedEntities={EntityCount}, ExtractedRelationships={RelCount}",
                eventData.FileId,
                catalogue.Id,
                recognitionResult.Entities.Count,
                relationships.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "文件内容分析处理失败: FileId={FileId}",
                eventData.FileId
            );
        }
    }

    private async Task CreatePersonRoleRelationshipsAsync(
        List<PersonRoleInfo> personRoles,
        Guid catalogueId)
    {
        foreach (var personRole in personRoles)
        {
            // 通过姓名查找人员ID
            var personId = await FindPersonIdByNameAsync(personRole.PersonName);
            if (!personId.HasValue)
            {
                _logger.LogWarning(
                    "未找到人员: PersonName={PersonName}",
                    personRole.PersonName
                );
                continue;
            }

            // 检查关系是否已存在
            var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
                personId.Value,
                catalogueId,
                RelationshipType.PersonRelatesToCatalogue,
                personRole.Role.ToString(),
                null
            );

            if (exists)
                continue;

            // 创建关系
            await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
            {
                SourceEntityId = personId.Value,
                SourceEntityType = "Person",
                TargetEntityId = catalogueId,
                TargetEntityType = "Catalogue",
                RelationshipType = RelationshipType.PersonRelatesToCatalogue,
                Role = personRole.Role.ToString(),
                Description = $"从文件内容提取：{personRole.Context}",
                Properties = new Dictionary<string, object>
                {
                    ["autoCreated"] = true,
                    ["createdReason"] = "文件内容分析",
                    ["extractionMethod"] = "NLP",
                    ["confidence"] = personRole.Confidence,
                    ["context"] = personRole.Context,
                    ["personName"] = personRole.PersonName
                }
            });
        }
    }

    private async Task CreateExtractedRelationshipsAsync(
        List<ExtractedRelationship> relationships,
        Guid sourceCatalogueId)
    {
        foreach (var rel in relationships)
        {
            try
            {
                Guid? targetEntityId = null;
                string? targetEntityType = null;

                // 解析目标实体
                if (!string.IsNullOrEmpty(rel.TargetEntityText))
                {
                    if (rel.RelationshipType == RelationshipType.CatalogueRelatesToCatalogue)
                    {
                        // 通过引用编号查找分类
                        var targetCatalogue = await _catalogueRepository.FirstOrDefaultAsync(
                            c => c.Reference == rel.TargetEntityText
                        );
                        if (targetCatalogue != null)
                        {
                            targetEntityId = targetCatalogue.Id;
                            targetEntityType = "Catalogue";
                        }
                    }
                }

                if (!targetEntityId.HasValue)
                    continue;

                // 检查关系是否已存在
                var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
                    sourceCatalogueId,
                    targetEntityId.Value,
                    rel.RelationshipType,
                    rel.Role,
                    rel.SemanticType
                );

                if (exists)
                    continue;

                // 创建关系
                await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
                {
                    SourceEntityId = sourceCatalogueId,
                    SourceEntityType = "Catalogue",
                    TargetEntityId = targetEntityId.Value,
                    TargetEntityType = targetEntityType!,
                    RelationshipType = rel.RelationshipType,
                    Role = rel.Role,
                    SemanticType = rel.SemanticType,
                    Description = $"从文件内容提取：{rel.Context}",
                    Weight = rel.Confidence, // 使用置信度作为权重
                    Properties = new Dictionary<string, object>
                    {
                        ["autoCreated"] = true,
                        ["createdReason"] = "文件内容分析",
                        ["extractionMethod"] = "NLP",
                        ["confidence"] = rel.Confidence,
                        ["context"] = rel.Context
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "创建提取关系失败: SourceText={SourceText}, TargetText={TargetText}",
                    rel.SourceEntityText,
                    rel.TargetEntityText
                );
            }
        }
    }

    private async Task<Guid?> FindPersonIdByNameAsync(string personName)
    {
        // 通过用户系统查询人员ID
        // var person = await _userRepository.FindByNameAsync(personName);
        // return person?.Id;
        
        // 简化示例，实际实现需要查询用户系统
        return null;
    }
}
```

### 5.2 行为模式智能分析

行为模式智能分析通过分析用户操作历史，发现隐藏的业务关系和模式。

#### 5.2.1 行为模式分析服务

```csharp
// IBehaviorPatternAnalysisService.cs
public interface IBehaviorPatternAnalysisService
{
    /// <summary>
    /// 分析用户操作模式
    /// </summary>
    Task<List<BehaviorPattern>> AnalyzeUserBehaviorPatternsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null
    );

    /// <summary>
    /// 发现协作关系
    /// </summary>
    Task<List<CollaborationRelationship>> DiscoverCollaborationRelationshipsAsync(
        Guid catalogueId
    );

    /// <summary>
    /// 发现访问模式
    /// </summary>
    Task<List<AccessPattern>> DiscoverAccessPatternsAsync(
        Guid entityId,
        string entityType
    );
}

// BehaviorPattern.cs
public class BehaviorPattern
{
    public string PatternType { get; set; } // FrequentCollaboration, SequentialAccess, CoCreation等
    public Dictionary<string, object> PatternData { get; set; } = new Dictionary<string, object>();
    public double Confidence { get; set; }
    public int OccurrenceCount { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
}

public class CollaborationRelationship
{
    public Guid Person1Id { get; set; }
    public Guid Person2Id { get; set; }
    public Guid CatalogueId { get; set; }
    public int CollaborationCount { get; set; }
    public List<string> CollaborationTypes { get; set; } = new List<string>(); // Create, Update, Review等
    public double CollaborationStrength { get; set; }
}

public class AccessPattern
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; }
    public List<Guid> FrequentAccessors { get; set; } = new List<Guid>();
    public List<Guid> RelatedEntities { get; set; } = new List<Guid>();
    public Dictionary<string, int> AccessFrequency { get; set; } = new Dictionary<string, int>();
}
```

#### 5.2.2 行为模式分析实现

```csharp
// BehaviorPatternAnalysisService.cs
public class BehaviorPatternAnalysisService : 
    IBehaviorPatternAnalysisService,
    ITransientDependency
{
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly ILogger<BehaviorPatternAnalysisService> _logger;

    public async Task<List<BehaviorPattern>> AnalyzeUserBehaviorPatternsAsync(
        Guid userId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var patterns = new List<BehaviorPattern>();

        // 1. 分析频繁协作模式
        var collaborationPatterns = await AnalyzeCollaborationPatternsAsync(userId, startDate, endDate);
        patterns.AddRange(collaborationPatterns);

        // 2. 分析顺序访问模式
        var sequentialPatterns = await AnalyzeSequentialAccessPatternsAsync(userId, startDate, endDate);
        patterns.AddRange(sequentialPatterns);

        // 3. 分析共同创建模式
        var coCreationPatterns = await AnalyzeCoCreationPatternsAsync(userId, startDate, endDate);
        patterns.AddRange(coCreationPatterns);

        return patterns;
    }

    private async Task<List<BehaviorPattern>> AnalyzeCollaborationPatternsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var patterns = new List<BehaviorPattern>();

        // 查询用户创建的分类
        var userCatalogues = await _catalogueRepository.GetListAsync(
            predicate: c => c.CreatorId == userId &&
                           (!startDate.HasValue || c.CreationTime >= startDate.Value) &&
                           (!endDate.HasValue || c.CreationTime <= endDate.Value)
        );

        // 统计与其他用户的协作频率
        var collaborationMap = new Dictionary<Guid, int>();

        foreach (var catalogue in userCatalogues)
        {
            // 查找对该分类有操作的其他用户（通过审计日志）
            // var otherUsers = await GetOtherUsersWhoAccessedAsync(catalogue.Id);
            // foreach (var otherUserId in otherUsers)
            // {
            //     collaborationMap[otherUserId] = collaborationMap.GetValueOrDefault(otherUserId, 0) + 1;
            // }
        }

        // 识别频繁协作关系（协作次数 >= 3）
        foreach (var kvp in collaborationMap.Where(x => x.Value >= 3))
        {
            patterns.Add(new BehaviorPattern
            {
                PatternType = "FrequentCollaboration",
                PatternData = new Dictionary<string, object>
                {
                    ["collaboratorId"] = kvp.Key,
                    ["collaborationCount"] = kvp.Value
                },
                Confidence = Math.Min(kvp.Value / 10.0, 1.0),
                OccurrenceCount = kvp.Value
            });
        }

        return patterns;
    }

    private async Task<List<BehaviorPattern>> AnalyzeSequentialAccessPatternsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var patterns = new List<BehaviorPattern>();

        // 分析用户访问分类的顺序模式
        // 例如：用户经常先访问分类A，然后访问分类B
        // 这暗示分类A和分类B之间存在业务关联

        // 实现逻辑：
        // 1. 从审计日志获取用户访问序列
        // 2. 使用序列模式挖掘算法（如PrefixSpan）发现频繁序列
        // 3. 将频繁序列转换为分类间的关系

        return patterns;
    }

    private async Task<List<BehaviorPattern>> AnalyzeCoCreationPatternsAsync(
        Guid userId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var patterns = new List<BehaviorPattern>();

        // 分析用户与其他用户共同创建分类的模式
        // 例如：用户A和用户B经常共同创建同一业务实体的分类
        // 这暗示用户A和用户B之间存在协作关系

        return patterns;
    }

    public async Task<List<CollaborationRelationship>> DiscoverCollaborationRelationshipsAsync(
        Guid catalogueId)
    {
        var relationships = new List<CollaborationRelationship>();

        var catalogue = await _catalogueRepository.GetAsync(catalogueId);

        // 查找对该分类有操作的所有用户
        // var users = await GetUsersWhoAccessedAsync(catalogueId);

        // 计算用户间的协作强度
        // var collaborationMatrix = CalculateCollaborationMatrix(users, catalogueId);

        // 识别显著的协作关系（协作强度 >= 阈值）
        // foreach (var collaboration in collaborationMatrix.Where(c => c.Strength >= 0.5))
        // {
        //     relationships.Add(collaboration);
        // }

        return relationships;
    }

    public async Task<List<AccessPattern>> DiscoverAccessPatternsAsync(
        Guid entityId,
        string entityType)
    {
        var patterns = new List<AccessPattern>();

        // 分析实体的访问模式
        // 1. 识别频繁访问者
        // 2. 识别关联实体（经常一起访问的实体）
        // 3. 识别访问频率模式

        var pattern = new AccessPattern
        {
            EntityId = entityId,
            EntityType = entityType,
            FrequentAccessors = new List<Guid>(), // 从审计日志获取
            RelatedEntities = new List<Guid>(), // 从访问序列分析获取
            AccessFrequency = new Dictionary<string, int>()
        };

        patterns.Add(pattern);

        return patterns;
    }
}
```

---

## 6. 工作流集成自动化

### 6.1 审批流程关系管理

工作流集成自动化通过监听工作流事件，自动创建和管理与工作流相关的关系。

#### 6.1.1 工作流事件处理器

```csharp
// WorkflowApprovalCompletedHandler.cs
public class WorkflowApprovalCompletedHandler :
    ILocalEventHandler<WorkflowApprovalCompletedEvent>,
    ITransientDependency
{
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly ILogger<WorkflowApprovalCompletedHandler> _logger;

    public async Task HandleEventAsync(WorkflowApprovalCompletedEvent eventData)
    {
        try
        {
            // 1. 创建审核人关系
            await CreateReviewerRelationshipAsync(
                eventData.ApproverId,
                eventData.CatalogueId
            );

            // 2. 创建工作流实例关系
            await CreateWorkflowInstanceRelationshipAsync(
                eventData.WorkflowInstanceId,
                eventData.CatalogueId
            );

            // 3. 创建工作流管理关系
            await CreateWorkflowManagementRelationshipAsync(
                eventData.WorkflowInstanceId,
                eventData.CatalogueId
            );

            _logger.LogInformation(
                "工作流审批完成事件处理完成: WorkflowInstanceId={WorkflowInstanceId}, " +
                "CatalogueId={CatalogueId}, ApproverId={ApproverId}",
                eventData.WorkflowInstanceId,
                eventData.CatalogueId,
                eventData.ApproverId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "工作流审批完成事件处理失败: WorkflowInstanceId={WorkflowInstanceId}",
                eventData.WorkflowInstanceId
            );
        }
    }

    private async Task CreateReviewerRelationshipAsync(Guid approverId, Guid catalogueId)
    {
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            approverId,
            catalogueId,
            RelationshipType.PersonRelatesToCatalogue,
            PersonRole.Reviewer.ToString(),
            null
        );

        if (exists)
            return;

        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
        {
            SourceEntityId = approverId,
            SourceEntityType = "Person",
            TargetEntityId = catalogueId,
            TargetEntityType = "Catalogue",
            RelationshipType = RelationshipType.PersonRelatesToCatalogue,
            Role = PersonRole.Reviewer.ToString(),
            Description = "工作流审批完成",
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["createdReason"] = "工作流审批",
                ["workflowApproval"] = true
            }
        });
    }

    private async Task CreateWorkflowInstanceRelationshipAsync(
        Guid workflowInstanceId,
        Guid catalogueId)
    {
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            workflowInstanceId,
            catalogueId,
            RelationshipType.WorkflowInstanceBelongsToCatalogue,
            null,
            null
        );

        if (exists)
            return;

        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
        {
            SourceEntityId = workflowInstanceId,
            SourceEntityType = "Workflow",
            TargetEntityId = catalogueId,
            TargetEntityType = "Catalogue",
            RelationshipType = RelationshipType.WorkflowInstanceBelongsToCatalogue,
            Description = "工作流实例关联分类",
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["createdReason"] = "工作流审批完成"
            }
        });
    }

    private async Task CreateWorkflowManagementRelationshipAsync(
        Guid workflowInstanceId,
        Guid catalogueId)
    {
        // 获取工作流模板ID（从工作流实例获取）
        // var workflowTemplateId = await GetWorkflowTemplateIdAsync(workflowInstanceId);

        // 创建工作流管理分类的关系
        // await _knowledgeGraphService.CreateRelationshipAsync(...);
    }
}
```

### 6.2 业务流程集成

业务流程集成通过监听业务流程事件，自动创建业务实体与分类的关系。

#### 6.2.1 业务流程事件处理器

```csharp
// BusinessProcessEventHandler.cs
public class BusinessProcessEventHandler :
    ILocalEventHandler<BusinessProcessStartedEvent>,
    ILocalEventHandler<BusinessProcessCompletedEvent>,
    ITransientDependency
{
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;

    public async Task HandleEventAsync(BusinessProcessStartedEvent eventData)
    {
        // 当业务流程启动时，创建业务实体与分类的关系
        var catalogues = await _catalogueRepository.GetListAsync(
            predicate: c => c.Reference == eventData.BusinessEntityId.ToString() &&
                           c.ReferenceType == eventData.BusinessEntityType
        );

        foreach (var catalogue in catalogues)
        {
            await CreateBusinessEntityRelationshipAsync(
                eventData.BusinessEntityId,
                eventData.BusinessEntityType,
                catalogue.Id
            );
        }
    }

    public async Task HandleEventAsync(BusinessProcessCompletedEvent eventData)
    {
        // 当业务流程完成时，更新关系状态
        // 实现逻辑...
    }

    private async Task CreateBusinessEntityRelationshipAsync(
        Guid businessEntityId,
        int businessEntityType,
        Guid catalogueId)
    {
        var exists = await _knowledgeGraphService.CheckRelationshipExistsAsync(
            businessEntityId,
            catalogueId,
            RelationshipType.BusinessEntityHasCatalogue,
            null,
            null
        );

        if (exists)
            return;

        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
        {
            SourceEntityId = businessEntityId,
            SourceEntityType = "BusinessEntity",
            TargetEntityId = catalogueId,
            TargetEntityType = "Catalogue",
            RelationshipType = RelationshipType.BusinessEntityHasCatalogue,
            Description = "业务流程关联",
            Properties = new Dictionary<string, object>
            {
                ["autoCreated"] = true,
                ["createdReason"] = "业务流程启动",
                ["businessEntityType"] = businessEntityType
            }
        });
    }
}
```

---

## 7. 合规与审计保障

### 7.1 自动化审计追踪

自动化审计追踪记录所有关系自动创建的详细信息，包括创建来源、依据、时间等。

#### 7.1.1 审计记录模型

```csharp
// RelationshipAutoCreationAudit.cs
public class RelationshipAutoCreationAudit : CreationAuditedEntity<Guid>
{
    public Guid RelationshipId { get; set; }
    public Guid SourceEntityId { get; set; }
    public string SourceEntityType { get; set; }
    public Guid TargetEntityId { get; set; }
    public string TargetEntityType { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string? Role { get; set; }
    public string? SemanticType { get; set; }

    // 创建来源信息
    public AutoCreationSource CreationSource { get; set; } // RuleEngine, EntityRecognition, BehaviorAnalysis, Workflow等
    public string? RuleId { get; set; } // 如果是规则引擎创建
    public string? RuleName { get; set; }
    public Dictionary<string, object>? SourceContext { get; set; } // 创建时的上下文信息
    public double? Confidence { get; set; } // 置信度（AI提取的关系）

    // 审计信息
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public string? CreatedReason { get; set; }
    public Dictionary<string, object>? AuditMetadata { get; set; } // 扩展审计元数据
}

public enum AutoCreationSource
{
    RuleEngine,           // 规则引擎
    EntityRecognition,    // 实体识别
    BehaviorAnalysis,     // 行为分析
    WorkflowIntegration, // 工作流集成
    MetadataExtraction,   // 元数据提取
    ManualVerification   // 人工验证后创建
}
```

#### 7.1.2 审计服务实现

```csharp
// IRelationshipAutoCreationAuditService.cs
public interface IRelationshipAutoCreationAuditService
{
    /// <summary>
    /// 记录关系自动创建审计日志
    /// </summary>
    Task LogAutoCreationAsync(
        Guid relationshipId,
        AutoCreationSource source,
        Dictionary<string, object> context
    );

    /// <summary>
    /// 查询关系创建审计日志
    /// </summary>
    Task<PagedResultDto<RelationshipAutoCreationAuditDto>> QueryAuditLogsAsync(
        RelationshipAuditQueryInput input
    );

    /// <summary>
    /// 获取关系的创建来源追溯
    /// </summary>
    Task<RelationshipCreationTraceDto> GetCreationTraceAsync(Guid relationshipId);
}

// RelationshipAutoCreationAuditService.cs
public class RelationshipAutoCreationAuditService :
    IRelationshipAutoCreationAuditService,
    ITransientDependency
{
    private readonly IRepository<RelationshipAutoCreationAudit, Guid> _auditRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RelationshipAutoCreationAuditService> _logger;

    public async Task LogAutoCreationAsync(
        Guid relationshipId,
        AutoCreationSource source,
        Dictionary<string, object> context)
    {
        var audit = new RelationshipAutoCreationAudit
        {
            Id = GuidGenerator.Create(),
            RelationshipId = relationshipId,
            CreationSource = source,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUser.Id,
            SourceContext = context
        };

        // 从上下文中提取信息
        if (context.ContainsKey("sourceEntityId"))
            audit.SourceEntityId = Guid.Parse(context["sourceEntityId"].ToString()!);
        if (context.ContainsKey("sourceEntityType"))
            audit.SourceEntityType = context["sourceEntityType"].ToString()!;
        if (context.ContainsKey("targetEntityId"))
            audit.TargetEntityId = Guid.Parse(context["targetEntityId"].ToString()!);
        if (context.ContainsKey("targetEntityType"))
            audit.TargetEntityType = context["targetEntityType"].ToString()!;
        if (context.ContainsKey("relationshipType"))
            audit.RelationshipType = Enum.Parse<RelationshipType>(context["relationshipType"].ToString()!);
        if (context.ContainsKey("role"))
            audit.Role = context["role"].ToString();
        if (context.ContainsKey("semanticType"))
            audit.SemanticType = context["semanticType"].ToString();
        if (context.ContainsKey("ruleId"))
            audit.RuleId = context["ruleId"].ToString();
        if (context.ContainsKey("ruleName"))
            audit.RuleName = context["ruleName"].ToString();
        if (context.ContainsKey("confidence"))
            audit.Confidence = Convert.ToDouble(context["confidence"]);
        if (context.ContainsKey("createdReason"))
            audit.CreatedReason = context["createdReason"].ToString();

        await _auditRepository.InsertAsync(audit);

        _logger.LogInformation(
            "关系自动创建审计日志已记录: RelationshipId={RelationshipId}, Source={Source}",
            relationshipId,
            source
        );
    }

    public async Task<PagedResultDto<RelationshipAutoCreationAuditDto>> QueryAuditLogsAsync(
        RelationshipAuditQueryInput input)
    {
        var queryable = await _auditRepository.GetQueryableAsync();

        // 应用过滤条件
        if (input.RelationshipId.HasValue)
        {
            queryable = queryable.Where(a => a.RelationshipId == input.RelationshipId.Value);
        }

        if (input.Source.HasValue)
        {
            queryable = queryable.Where(a => a.CreationSource == input.Source.Value);
        }

        if (input.StartDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt >= input.StartDate.Value);
        }

        if (input.EndDate.HasValue)
        {
            queryable = queryable.Where(a => a.CreatedAt <= input.EndDate.Value);
        }

        var totalCount = await AsyncExecuter.LongCountAsync(queryable);

        var audits = await AsyncExecuter.ToListAsync(
            queryable
                .OrderByDescending(a => a.CreatedAt)
                .Skip((input.PageIndex - 1) * input.PageSize)
                .Take(input.PageSize)
        );

        return new PagedResultDto<RelationshipAutoCreationAuditDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<RelationshipAutoCreationAudit>, List<RelationshipAutoCreationAuditDto>>(audits)
        };
    }

    public async Task<RelationshipCreationTraceDto> GetCreationTraceAsync(Guid relationshipId)
    {
        var audits = await _auditRepository.GetListAsync(
            predicate: a => a.RelationshipId == relationshipId,
            orderBy: a => a.OrderByDescending(audit => audit.CreatedAt)
        );

        if (audits.Count == 0)
            return new RelationshipCreationTraceDto
            {
                RelationshipId = relationshipId,
                HasTrace = false
            };

        var firstAudit = audits.First();

        return new RelationshipCreationTraceDto
        {
            RelationshipId = relationshipId,
            HasTrace = true,
            CreationSource = firstAudit.CreationSource,
            CreatedAt = firstAudit.CreatedAt,
            CreatedBy = firstAudit.CreatedBy,
            RuleId = firstAudit.RuleId,
            RuleName = firstAudit.RuleName,
            Confidence = firstAudit.Confidence,
            SourceContext = firstAudit.SourceContext,
            CreationReason = firstAudit.CreatedReason,
            AuditHistory = ObjectMapper.Map<List<RelationshipAutoCreationAudit>, List<RelationshipAutoCreationAuditDto>>(audits)
        };
    }
}
```

### 7.2 标准化合规支持

标准化合规支持确保关系自动创建符合业务规范和合规要求。

#### 7.2.1 合规性验证服务

```csharp
// IComplianceValidationService.cs
public interface IComplianceValidationService
{
    /// <summary>
    /// 验证关系创建是否符合合规要求
    /// </summary>
    Task<ComplianceValidationResult> ValidateRelationshipCreationAsync(
        CreateRelationshipInput input,
        AutoCreationSource source
    );

    /// <summary>
    /// 检查关系是否符合业务规范
    /// </summary>
    Task<bool> CheckBusinessComplianceAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        RelationshipType relationshipType
    );
}

// ComplianceValidationService.cs
public class ComplianceValidationService :
    IComplianceValidationService,
    ITransientDependency
{
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
    private readonly ILogger<ComplianceValidationService> _logger;

    public async Task<ComplianceValidationResult> ValidateRelationshipCreationAsync(
        CreateRelationshipInput input,
        AutoCreationSource source)
    {
        var result = new ComplianceValidationResult
        {
            IsCompliant = true,
            Violations = new List<ComplianceViolation>()
        };

        // 1. 检查实体权限合规性
        var permissionViolation = await CheckPermissionComplianceAsync(input);
        if (permissionViolation != null)
        {
            result.IsCompliant = false;
            result.Violations.Add(permissionViolation);
        }

        // 2. 检查数据完整性合规性
        var integrityViolation = await CheckDataIntegrityComplianceAsync(input);
        if (integrityViolation != null)
        {
            result.IsCompliant = false;
            result.Violations.Add(integrityViolation);
        }

        // 3. 检查业务规则合规性
        var businessRuleViolation = await CheckBusinessRuleComplianceAsync(input);
        if (businessRuleViolation != null)
        {
            result.IsCompliant = false;
            result.Violations.Add(businessRuleViolation);
        }

        // 4. 检查AI提取关系的置信度阈值
        if (source == AutoCreationSource.EntityRecognition ||
            source == AutoCreationSource.BehaviorAnalysis)
        {
            var confidence = input.Properties?.GetValueOrDefault("confidence") as double?;
            if (confidence.HasValue && confidence.Value < 0.7) // 置信度阈值
            {
                result.IsCompliant = false;
                result.Violations.Add(new ComplianceViolation
                {
                    Type = ComplianceViolationType.LowConfidence,
                    Message = $"AI提取的关系置信度过低: {confidence.Value:F2}",
                    Severity = ComplianceViolationSeverity.Warning
                });
            }
        }

        return result;
    }

    private async Task<ComplianceViolation?> CheckPermissionComplianceAsync(
        CreateRelationshipInput input)
    {
        // 检查源实体和目标实体的权限
        // 确保关系创建不会违反权限控制规则

        if (input.SourceEntityType == "Catalogue")
        {
            var catalogue = await _catalogueRepository.GetAsync(input.SourceEntityId);
            // 检查分类的权限设置
            // 如果分类有严格的权限控制，可能需要人工审核
        }

        return null;
    }

    private async Task<ComplianceViolation?> CheckDataIntegrityComplianceAsync(
        CreateRelationshipInput input)
    {
        // 检查关系创建是否会影响数据完整性
        // 例如：创建循环关系、重复关系等

        return null;
    }

    private async Task<ComplianceViolation?> CheckBusinessRuleComplianceAsync(
        CreateRelationshipInput input)
    {
        // 检查关系创建是否符合业务规则
        // 例如：某些关系类型只能在特定条件下创建

        return null;
    }

    public async Task<bool> CheckBusinessComplianceAsync(
        Guid sourceEntityId,
        Guid targetEntityId,
        RelationshipType relationshipType)
    {
        // 实现业务合规性检查逻辑
        return true;
    }
}

// ComplianceValidationResult.cs
public class ComplianceValidationResult
{
    public bool IsCompliant { get; set; }
    public List<ComplianceViolation> Violations { get; set; } = new List<ComplianceViolation>();
}

public class ComplianceViolation
{
    public ComplianceViolationType Type { get; set; }
    public ComplianceViolationSeverity Severity { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object>? Details { get; set; }
}

public enum ComplianceViolationType
{
    PermissionViolation,    // 权限违规
    DataIntegrityViolation, // 数据完整性违规
    BusinessRuleViolation,  // 业务规则违规
    LowConfidence          // 低置信度
}

public enum ComplianceViolationSeverity
{
    Error,   // 错误：必须拒绝
    Warning  // 警告：可以创建但需要审核
}
```

#### 7.2.2 关系质量评估

```csharp
// IRelationshipQualityAssessmentService.cs
public interface IRelationshipQualityAssessmentService
{
    /// <summary>
    /// 评估关系质量
    /// </summary>
    Task<RelationshipQualityScore> AssessRelationshipQualityAsync(Guid relationshipId);

    /// <summary>
    /// 批量评估关系质量
    /// </summary>
    Task<List<RelationshipQualityScore>> BatchAssessRelationshipQualityAsync(
        List<Guid> relationshipIds
    );
}

// RelationshipQualityScore.cs
public class RelationshipQualityScore
{
    public Guid RelationshipId { get; set; }
    public double OverallScore { get; set; } // 0-100
    public Dictionary<string, double> DimensionScores { get; set; } = new Dictionary<string, double>
    {
        ["accuracy"] = 0.0,      // 准确性
        ["completeness"] = 0.0, // 完整性
        ["consistency"] = 0.0,   // 一致性
        ["timeliness"] = 0.0    // 及时性
    };
    public List<string> Issues { get; set; } = new List<string>();
    public QualityLevel QualityLevel { get; set; }
}

public enum QualityLevel
{
    Excellent, // 90-100
    Good,      // 70-89
    Fair,      // 50-69
    Poor       // 0-49
}
```

---

## 8. 实施计划与监控

### 8.1 分阶段实施计划

#### 第一阶段：基础自动化（2周）
- 实现事件驱动引擎
- 实现基础业务规则（父子关系、创建者关系等）
- 实现分类体系自动化构建

#### 第二阶段：智能发现（3周）
- 实现实体识别服务
- 实现文件内容分析处理器
- 实现语义关系自动挖掘

#### 第三阶段：行为分析（2周）
- 实现行为模式分析服务
- 实现协作关系发现
- 实现访问模式分析

#### 第四阶段：工作流集成（2周）
- 实现工作流事件处理器
- 实现审批流程关系管理
- 实现业务流程集成

#### 第五阶段：合规保障（1周）
- 实现审计追踪服务
- 实现合规性验证服务
- 实现关系质量评估

### 8.2 监控与指标

#### 8.2.1 关键指标

```csharp
// AutomationMetrics.cs
public class AutomationMetrics
{
    // 自动化率
    public double AutomationRate { get; set; } // 自动创建的关系数 / 总关系数

    // 准确率
    public double AccuracyRate { get; set; } // 正确的关系数 / 总关系数

    // 平均处理时间
    public TimeSpan AverageProcessingTime { get; set; }

    // 规则执行统计
    public Dictionary<string, int> RuleExecutionCounts { get; set; } = new Dictionary<string, int>();

    // 来源统计
    public Dictionary<AutoCreationSource, int> SourceStatistics { get; set; } = new Dictionary<AutoCreationSource, int>();

    // 错误统计
    public int TotalErrors { get; set; }
    public Dictionary<string, int> ErrorTypes { get; set; } = new Dictionary<string, int>();
}
```

#### 8.2.2 监控仪表板

```csharp
// AutomationMonitoringController.cs
[Route("api/knowledge-graph/automation/monitoring")]
public class AutomationMonitoringController : AbpControllerBase
{
    private readonly IRelationshipAutoCreationService _autoCreationService;

    /// <summary>
    /// 获取自动化指标
    /// </summary>
    [HttpGet("metrics")]
    public async Task<AutomationMetricsDto> GetMetricsAsync(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        // 计算各项指标
        var metrics = await _autoCreationService.GetMetricsAsync(startDate, endDate);
        return ObjectMapper.Map<AutomationMetrics, AutomationMetricsDto>(metrics);
    }

    /// <summary>
    /// 获取规则执行统计
    /// </summary>
    [HttpGet("rules/statistics")]
    public async Task<Dictionary<string, RuleExecutionStatisticsDto>> GetRuleStatisticsAsync()
    {
        // 返回规则执行统计信息
        return new Dictionary<string, RuleExecutionStatisticsDto>();
    }
}
```

---

## 9. 总结

知识图谱关系自动化创建业务方案通过事件驱动、规则引擎、智能发现和工作流集成等技术手段，实现了知识图谱关系的自动构建和维护。方案具有以下特点：

1. **高度自动化**：80%以上的关系自动创建，大幅减少人工维护成本
2. **智能发现**：通过NLP和行为分析，发现隐藏的业务关系
3. **可追溯性**：完整的审计追踪，支持合规性要求
4. **可扩展性**：基于规则引擎，支持灵活的业务规则配置
5. **高质量**：通过合规性验证和质量评估，确保关系质量

该方案与现有的技术方案完美集成，基于ABP框架和领域事件机制，符合行业最佳实践，能够有效提升知识图谱系统的自动化水平和业务价值。

