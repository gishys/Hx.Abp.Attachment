# 多维知识图谱系统技术方案报告

## 1. 项目概述

### 1.1 项目背景

多维知识图谱系统旨在构建一个以附件管理系统为核心的五维知识网络可视化平台，通过关系图谱、时间轴追溯、影响分析等功能，实现业务关系的直观呈现和智能分析。系统以**分类（Catalogue）**为核心实体，通过**人员（Person）**、**部门（Department）**、**业务实体（BusinessEntity）**和**工作流（Workflow）**等维度构建完整的知识网络。分类与分类之间的关系（如项目阶段不同时期的档案、按时间或业务划分的分类）是图谱的核心关系。工作流维度用于管理档案的创建、审核、归档等业务流程。

### 1.2 核心目标

-   **可视化展示**：直观呈现五维实体（分类、人员、部门、业务实体、工作流）间的复杂业务关系网络，重点展示分类与分类之间的关系（时间维度、业务维度等）以及工作流对分类生命周期的管理
-   **智能搜索**：支持多维度节点搜索和精确定位，包括分类名称、标签、工作流名称等
-   **时间追溯**：按业务阶段展示实体变化和关系演进，追踪分类的创建、更新历史、分类间的时间关系以及工作流执行历史
-   **影响分析**：自动计算节点变更的影响范围和风险等级，分析分类变更对其他分类、人员、部门、业务实体、工作流的影响
-   **风险预警**：实时监控和分级预警，缩短响应时间，重点关注分类完整性、关系一致性、工作流执行异常等

### 1.3 技术特点

-   基于图数据库的高性能关系查询
-   实时数据同步和增量更新
-   多维度数据聚合和统计分析
-   智能影响半径计算算法
-   分级风险预警机制

---

## 2. 系统架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────────┐
│                     前端展示层                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │ 图谱视图  │  │ 搜索面板  │  │ 时间轴   │  │ 详情面板  │ │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↕ HTTP/WebSocket
┌─────────────────────────────────────────────────────────────┐
│                    API网关层                                 │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐ │
│  │ 认证授权  │  │ 限流熔断  │  │ 日志监控  │  │ 路由转发  │ │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                   业务服务层                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 图谱服务      │  │ 搜索服务      │  │ 分析服务      │      │
│  │ - 关系查询    │  │ - 全文检索    │  │ - 影响分析    │      │
│  │ - 路径计算    │  │ - 排序算法    │  │ - 风险评估    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 时间轴服务    │  │ 预警服务      │  │ 通知服务      │      │
│  │ - 阶段划分    │  │ - 规则引擎    │  │ - 消息推送    │      │
│  │ - 变化追踪    │  │ - 分级预警    │  │ - 责任人通知  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────────┐
│                   数据存储层                                 │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │ 图数据库      │  │ 关系数据库    │                        │
│  │ Neo4j         │  │ PostgreSQL   │                        │
│  │ - 实体节点    │  │ - 业务数据    │                        │
│  │ - 关系边      │  │ - 元数据      │                        │
│  │               │  │ - 全文搜索    │                        │
│  └──────────────┘  └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 技术栈选择

#### 2.2.1 前端技术栈

-   **框架**：React 18+
-   **图谱可视化**：Cytoscape.js（强大的图布局算法，支持复杂关系网络）
-   **状态管理**：Zustand
-   **UI 组件库**：Ant Design
-   **时间轴组件**：vis-timeline
-   **WebSocket 客户端**：Socket.io-client（实时数据推送）

#### 2.2.2 后端技术栈

-   **框架**：ASP.NET Core 8.0（基于现有 ABP 框架）
-   **图数据库**：Neo4j（成熟稳定，Cypher 查询语言强大）
-   **全文搜索**：PostgreSQL 全文搜索（利用 PostgreSQL 内置全文搜索功能，无需额外搜索引擎）
-   **消息队列**：RabbitMQ / Redis Streams（异步处理和通知）
-   **缓存**：Redis（热点数据缓存）

#### 2.2.3 数据同步

-   **领域事件**：使用 ABP 框架的领域事件（Domain Events）机制监听实体变更
-   **后台服务**：使用 ABP 框架的后台作业（Background Jobs）进行异步数据同步
-   **事件总线**：使用 ABP 框架的本地事件总线（Local Event Bus）处理领域事件

---

## 3. 数据模型设计

### 3.1 实体模型（Entity Model）

#### 3.1.1 五维实体定义（优化后）

> **重要说明**：知识图谱的实体定义采用**引用模式**，不重复定义已有实体的所有字段。
>
> -   **已有实体**：`AttachCatalogue`（分类）
> -   **知识图谱实体**：只定义图谱查询和分析需要的核心字段，通过 `Id` 关联到现有实体
> -   **数据获取**：实体的完整信息通过 JOIN 现有实体表获取，避免数据冗余
> -   **维度说明**：
>     -   **分类（Catalogue）**：核心维度，文件通过分类直接访问，模板和分面信息已体现在分类中
>     -   **人员（Person）**：通过审计字段关联，表示创建、管理分类的人员
>     -   **部门（Department）**：组织维度，表示分类所属的部门
>     -   **业务实体（BusinessEntity）**：业务维度，表示项目、流程、合同等业务实体
>     -   **工作流（Workflow）**：流程维度，管理档案的创建、审核、归档等业务流程，定义分类的生命周期和审批流程

> **✅ 已创建视图模型**：以下视图模型已在项目中创建，位于 `Hx.Abp.Attachment.Application.Contracts/KnowledgeGraph/` 命名空间下。

```csharp
// 实体基类（图谱查询视图模型）
// 注意：这是图谱查询的视图模型，不是数据库实体
// 实际数据存储在现有实体表中（APPATTACH_CATALOGUES等）
// 通过 EntityId 关联到现有实体的 Id 字段，避免与继承类的标识字段冲突
// 文件位置：KnowledgeGraphEntityViewModel.cs
public abstract class KnowledgeGraphEntityViewModel
{
    /// <summary>
    /// 关联到现有实体表的ID（如 AttachCatalogue.Id）
    /// 使用 EntityId 而不是 Id，避免与继承类的标识字段冲突
    /// </summary>
    public Guid EntityId { get; set; }

    public string EntityType { get; set; } // 实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
    public string Name { get; set; } // 实体名称（从现有实体表获取）
    public List<string> Tags { get; set; } // 标签列表（从现有实体表获取）
    public string Status { get; set; } // 统一状态管理（ACTIVE, ARCHIVED, DELETED等）
    public Dictionary<string, object> GraphProperties { get; set; } // 图谱特有属性（如重要性评分、中心度等）
    // 注意：其他业务字段（如CatalogueName等）通过JOIN现有实体表获取
    // CreatedBy/UpdatedBy等信息通过ABP框架的审计字段获取
}

// 分类实体视图模型（核心）- 引用AttachCatalogue
// 只包含图谱查询需要的核心字段
// 文件位置：CatalogueEntityViewModel.cs
public class CatalogueEntityViewModel : KnowledgeGraphEntityViewModel
{
    // 核心关联字段（用于图谱查询）
    public string Reference { get; set; } // 业务引用ID（用于关联BusinessEntity）
    public int ReferenceType { get; set; } // 业务类型标识（用于关联BusinessEntity）
    public Guid? ParentId { get; set; } // 父分类ID（用于建立树形关系和分类间关系）

    // 图谱查询优化字段（可选，存储在kg_entity_graph_metadata表）
    public double? ImportanceScore { get; set; } // 重要性评分（用于影响分析）
    public int? RelationshipCount { get; set; } // 关系数量（用于中心度计算）

    // 注意：其他字段（CatalogueName、FacetType、AttachCount等）通过JOIN APPATTACH_CATALOGUES表获取
    // 模板和分面信息已体现在分类的FacetType等字段中，无需单独维度
}

// 人员实体视图模型 - 通过审计字段关联（CreatorId等）
// 简化定义，只包含图谱查询需要的字段
// 文件位置：PersonEntityViewModel.cs
public class PersonEntityViewModel : KnowledgeGraphEntityViewModel
{
    public string EmployeeId { get; set; } // 员工ID（关联到用户系统）
    public Guid? DepartmentId { get; set; } // 关联部门ID

    // 图谱统计字段（可选）
    public int CreatedCatalogueCount { get; set; } // 创建的分类数量

    // 注意：其他字段（Position、Email、Phone等）通过关联用户系统获取
}

// 部门实体视图模型 - 组织维度
// 表示分类所属的部门，通过Reference和ReferenceType关联
// 文件位置：DepartmentEntityViewModel.cs
public class DepartmentEntityViewModel : KnowledgeGraphEntityViewModel
{
    public string DepartmentCode { get; set; } // 部门编码
    public Guid? ParentDepartmentId { get; set; } // 父部门ID（用于层级结构）

    // 图谱统计字段（可选）
    public int CatalogueCount { get; set; } // 关联的分类数量
    public int PersonCount { get; set; } // 部门人员数量

    // 注意：其他字段（DepartmentName、ManagerId等）通过关联组织系统获取
}

// 业务实体视图模型 - 通过Reference和ReferenceType关联的外部业务实体
// 支持项目、流程、合同、任务等多种业务类型（不包括部门，部门单独作为维度）
// 文件位置：BusinessEntityViewModel.cs
public class BusinessEntityViewModel : KnowledgeGraphEntityViewModel
{
    public string ReferenceId { get; set; } // 对应AttachCatalogue.Reference
    public int ReferenceType { get; set; } // 对应AttachCatalogue.ReferenceType
    public string BusinessType { get; set; } // 业务类型名称（如"Project"、"Process"、"Contract"等）

    // 图谱统计字段（可选）
    public int CatalogueCount { get; set; } // 关联的分类数量

    // 注意：业务专属属性通过关联外部业务系统获取，不在此处存储
}

// 工作流实体视图模型 - 流程维度
// 管理档案的创建、审核、归档等业务流程，定义分类的生命周期和审批流程
// 文件位置：WorkflowEntityViewModel.cs
public class WorkflowEntityViewModel : KnowledgeGraphEntityViewModel
{
    public string WorkflowCode { get; set; } // 工作流编码（唯一标识）
    public string WorkflowType { get; set; } // 工作流类型（创建审批、归档审批、销毁审批等）
    public string Status { get; set; } // 工作流状态（ACTIVE, ARCHIVED, DISABLED）
    public Guid? TemplateDefinitionId { get; set; } // 模板定义Id（关联到工作流模板定义）
    public int TemplateDefinitionVersion { get; set; } // 模板定义版本（工作流模板定义的版本号）

    // 关联信息
    public Guid? OwnerDepartmentId { get; set; } // 拥有部门ID
    public Guid? ManagerPersonId { get; set; } // 管理员人员ID

    // 图谱统计字段（可选）
    public int CatalogueCount { get; set; } // 关联的分类数量（使用该工作流的分类数）
    public int InstanceCount { get; set; } // 工作流实例数量
    public int ActiveInstanceCount { get; set; } // 活跃实例数量

    // 注意：工作流的详细定义（节点、边、条件等）通过关联工作流引擎获取，不在此处存储
}
```

#### 3.1.2 关系模型（Relationship Model）

````csharp
> **✅ 已创建关系实体**：以下关系实体和枚举已在项目中创建。
> - 关系实体：`Hx.Abp.Attachment.Domain/KnowledgeGraph/KnowledgeGraphRelationship.cs`
> - 枚举类型：`Hx.Abp.Attachment.Dmain.Shared/Domain/Shared/KnowledgeGraph/` 目录下
> - 数据库配置：`Hx.Abp.Attachment.EntityFrameworkCore/KnowledgeGraphRelationshipEntityTypeConfiguration.cs`

```csharp
// 知识图谱关系实体
// 注意：关系数据存储在APPKG_RELATIONSHIPS表中，通过entity_id关联到现有实体表
// 继承 ExtensibleFullAuditedEntity 以使用ABP的审计字段和扩展字段
// 文件位置：KnowledgeGraphRelationship.cs
public class KnowledgeGraphRelationship : ExtensibleFullAuditedEntity<Guid>
{
    public Guid SourceEntityId { get; set; } // 源实体ID（关联到现有实体表）
    public string SourceEntityType { get; set; } // 源实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
    public Guid TargetEntityId { get; set; } // 目标实体ID（关联到现有实体表）
    public string TargetEntityType { get; set; } // 目标实体类型
    public RelationshipType Type { get; set; }
    public string? Description { get; set; }

    // 关系语义属性（用于抽象关系类型的具体语义描述）
    public string? Role { get; set; } // 角色（用于 PersonRelatesToCatalogue、PersonRelatesToWorkflow 等）
    public string? SemanticType { get; set; } // 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等）

    public double Weight { get; set; } = 1.0; // 关系权重（用于影响分析）

    // 注意：
    // - Id, CreationTime, LastModificationTime 等审计字段由 ExtensibleFullAuditedEntity 提供
    // - CreatorId, LastModifierId 等创建人/修改人字段由 ExtensibleFullAuditedEntity 提供
    // - ExtraProperties 扩展字段由 ExtensibleFullAuditedEntity 提供，用于存储关系扩展属性（替代原来的 Properties 字段）

    // 辅助方法：获取关系的显示名称
    public string GetDisplayName()
    {
        return Type switch
        {
            RelationshipType.PersonRelatesToCatalogue => Role != null
                ? $"人员-分类关系({Role})"
                : "人员-分类关系",
            RelationshipType.CatalogueRelatesToCatalogue => SemanticType != null
                ? $"分类-分类关系({SemanticType})"
                : "分类-分类关系",
            RelationshipType.PersonRelatesToWorkflow => Role != null
                ? $"人员-工作流关系({Role})"
                : "人员-工作流关系",
            RelationshipType.WorkflowRelatesToWorkflow => SemanticType != null
                ? $"工作流-工作流关系({SemanticType})"
                : "工作流-工作流关系",
            _ => Type.ToString()
        };
    }
}

// 关系类型枚举（优化后 - 采用抽象、可扩展的设计）
public enum RelationshipType
{
    // ========== 分类与分类的关系（抽象化设计） ==========
    // 核心关系：通过 semanticType 属性描述关系的具体语义（时间、业务、版本等）
    CatalogueRelatesToCatalogue,     // 分类关联分类（通用关系，通过 semanticType 区分：时间、业务、版本、依赖等）
    CatalogueHasChild,                // 分类有子分类（树形结构，语义明确，保留独立类型）
    CatalogueReferencesBusiness,     // 分类引用业务实体

    // ========== 人员与分类的关系（抽象化设计） ==========
    // 核心关系：通过 role 属性描述人员的具体角色（项目经理、审核人、专家等）
    PersonRelatesToCatalogue,        // 人员关联分类（通用关系，通过 role 属性区分：创建、管理、项目经理、审核、专家、责任人、联系人、参与等）
    PersonBelongsToDepartment,       // 人员属于部门

    // ========== 部门相关 ==========
    DepartmentOwnsCatalogue,         // 部门拥有分类
    DepartmentManagesCatalogue,      // 部门管理分类
    DepartmentHasParent,             // 部门层级关系

    // ========== 业务实体相关 ==========
    BusinessEntityHasCatalogue,      // 业务实体有分类
    BusinessEntityManagesCatalogue,  // 业务实体管理分类

    // ========== 工作流相关 ==========
    CatalogueUsesWorkflow,           // 分类使用工作流（分类关联工作流模板）
    WorkflowManagesCatalogue,        // 工作流管理分类（工作流实例管理分类的生命周期）
    WorkflowInstanceBelongsToCatalogue, // 工作流实例属于分类（具体的工作流执行实例）
    PersonRelatesToWorkflow,         // 人员关联工作流（通用关系，通过 role 属性区分：管理、执行等）
    DepartmentOwnsWorkflow,          // 部门拥有工作流
    WorkflowRelatesToWorkflow        // 工作流关联工作流（通用关系，通过 semanticType 区分：版本、替换等）
}

// 人员角色枚举（用于 PersonRelatesToCatalogue 关系的 role 属性）
public enum PersonRole
{
    Creator,              // 创建者
    Manager,              // 管理者
    ProjectManager,       // 项目经理
    Reviewer,             // 审核人
    Expert,               // 专家
    Responsible,          // 责任人
    Contact,              // 联系人
    Participant           // 参与者
}

// 分类关系语义类型枚举（用于 CatalogueRelatesToCatalogue 关系的 semanticType 属性）
public enum CatalogueSemanticType
{
    Temporal,             // 时间关系（项目阶段不同时期的档案）
    Business,             // 业务关系（按业务划分的分类关系）
    Version,              // 版本关系
    Replaces,             // 替换关系（版本演进）
    DependsOn,            // 依赖关系
    References,           // 引用关系
    SimilarTo             // 相似关系
}
````

### 3.2 图数据库 Schema 设计

#### 3.2.1 Neo4j 节点标签和属性

```cypher
// 节点标签（优化后）
(:Catalogue)
(:Person)
(:Department)
(:BusinessEntity)
(:Workflow)

// 节点属性索引
CREATE INDEX catalogue_name_index FOR (c:Catalogue) ON (c.catalogueName);
CREATE INDEX catalogue_reference_index FOR (c:Catalogue) ON (c.reference, c.referenceType);
CREATE INDEX catalogue_status_index FOR (c:Catalogue) ON (c.status);
CREATE INDEX person_employee_id_index FOR (p:Person) ON (p.employeeId);
CREATE INDEX department_code_index FOR (d:Department) ON (d.departmentCode);
CREATE INDEX business_entity_reference_index FOR (b:BusinessEntity) ON (b.referenceId, b.referenceType);
CREATE INDEX workflow_code_index FOR (w:Workflow) ON (w.workflowCode);
CREATE INDEX workflow_status_index FOR (w:Workflow) ON (w.status);
CREATE INDEX entity_status_index FOR (e) ON (e.status);

// 全文索引
CREATE FULLTEXT INDEX catalogue_tags_index FOR (c:Catalogue) ON EACH [c.tags];

// 关系类型（优化后 - 采用抽象、可扩展的设计）
// 分类之间的关系（抽象化设计）
(:Catalogue)-[:RELATES_TO {semanticType: 'Temporal'}]->(:Catalogue)      // 时间关系
(:Catalogue)-[:RELATES_TO {semanticType: 'Business'}]->(:Catalogue)       // 业务关系
(:Catalogue)-[:RELATES_TO {semanticType: 'Version'}]->(:Catalogue)        // 版本关系
(:Catalogue)-[:RELATES_TO {semanticType: 'Replaces'}]->(:Catalogue)       // 替换关系
(:Catalogue)-[:RELATES_TO {semanticType: 'DependsOn'}]->(:Catalogue)      // 依赖关系
(:Catalogue)-[:RELATES_TO {semanticType: 'References'}]->(:Catalogue)     // 引用关系
(:Catalogue)-[:RELATES_TO {semanticType: 'SimilarTo'}]->(:Catalogue)      // 相似关系
(:Catalogue)-[:HAS_CHILD]->(:Catalogue)                                   // 树形结构（语义明确，保留独立类型）

// 分类与人员的关系（抽象化设计）
(:Person)-[:RELATES_TO {role: 'Creator'}]->(:Catalogue)                   // 创建者
(:Person)-[:RELATES_TO {role: 'Manager'}]->(:Catalogue)                  // 管理者
(:Person)-[:RELATES_TO {role: 'ProjectManager'}]->(:Catalogue)           // 项目经理
(:Person)-[:RELATES_TO {role: 'Reviewer'}]->(:Catalogue)                 // 审核人
(:Person)-[:RELATES_TO {role: 'Expert'}]->(:Catalogue)                   // 专家
(:Person)-[:RELATES_TO {role: 'Responsible'}]->(:Catalogue)               // 责任人
(:Person)-[:RELATES_TO {role: 'Contact'}]->(:Catalogue)                  // 联系人
(:Person)-[:RELATES_TO {role: 'Participant'}]->(:Catalogue)               // 参与者

// 分类与部门的关系
(:Department)-[:OWNS]->(:Catalogue)
(:Department)-[:MANAGES]->(:Catalogue)

// 分类与业务实体的关系
(:BusinessEntity)-[:HAS]->(:Catalogue)
(:BusinessEntity)-[:MANAGES]->(:Catalogue)
(:Catalogue)-[:REFERENCES]->(:BusinessEntity)

// 人员与部门的关系
(:Person)-[:BELONGS_TO]->(:Department)

// 部门层级关系
(:Department)-[:HAS_PARENT]->(:Department)

// 分类与工作流的关系
(:Catalogue)-[:USES]->(:Workflow)
(:Workflow)-[:MANAGES]->(:Catalogue)
(:Workflow)-[:INSTANCE_OF]->(:Catalogue)

// 人员与工作流的关系（抽象化设计）
(:Person)-[:RELATES_TO {role: 'Manager'}]->(:Workflow)                    // 工作流管理员
(:Person)-[:RELATES_TO {role: 'Executor'}]->(:Workflow)                  // 工作流执行人

// 部门与工作流的关系
(:Department)-[:OWNS]->(:Workflow)

// 工作流版本关系（抽象化设计）
(:Workflow)-[:RELATES_TO {semanticType: 'Version'}]->(:Workflow)          // 版本关系
(:Workflow)-[:RELATES_TO {semanticType: 'Replaces'}]->(:Workflow)         // 替换关系
```

#### 3.2.2 关系数据库表设计

> **重要说明**：知识图谱的关系型数据库设计采用**引用模式**，不重复存储已有实体数据。
>
> -   **已有实体表**：`APPATTACH_CATALOGUES`（分类）
> -   **知识图谱表**：只存储图谱特有的数据（关系、图查询优化、时间轴快照等）
> -   **关联方式**：通过外键关联到现有实体表，避免数据冗余
> -   **维度说明**：文件通过分类直接访问，模板和分面信息已体现在分类中，无需单独维度

```sql
-- =====================================================
-- 知识图谱关系表（核心表）
-- 存储实体间的关系，不存储实体本身的数据
-- 注意：使用ABP的审计字段（CreationTime, LastModificationTime, CreatorId, LastModifierId等）
-- 扩展属性存储在 ExtraProperties JSONB 字段中（由 ExtensibleFullAuditedEntity 提供）
-- =====================================================
CREATE TABLE "APPKG_RELATIONSHIPS" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "SOURCE_ENTITY_ID" UUID NOT NULL, -- 源实体ID（关联到APPATTACH_CATALOGUES等）
    "SOURCE_ENTITY_TYPE" VARCHAR(50) NOT NULL, -- 源实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
    "TARGET_ENTITY_ID" UUID NOT NULL, -- 目标实体ID
    "TARGET_ENTITY_TYPE" VARCHAR(50) NOT NULL, -- 目标实体类型
    "RELATIONSHIP_TYPE" VARCHAR(50) NOT NULL, -- 关系类型（RELATES_TO, HAS_CHILD等）
    "ROLE" VARCHAR(50), -- 角色（用于 PersonRelatesToCatalogue、PersonRelatesToWorkflow 等）
    "SEMANTIC_TYPE" VARCHAR(50), -- 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等）
    "DESCRIPTION" TEXT,
    "WEIGHT" DOUBLE PRECISION DEFAULT 1.0, -- 关系权重（用于影响分析）

    -- ABP审计字段（由 ExtensibleFullAuditedEntity 提供）
    "CreationTime" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatorId" UUID,
    "LastModificationTime" TIMESTAMP,
    "LastModifierId" UUID,
    "IsDeleted" BOOLEAN NOT NULL DEFAULT FALSE,
    "DeletionTime" TIMESTAMP,
    "DeleterId" UUID,

    -- ABP扩展字段（由 ExtensibleFullAuditedEntity 提供）
    "ExtraProperties" JSONB, -- 关系扩展属性（替代原来的 properties 字段）

    CONSTRAINT "UK_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE_ROLE_SEMANTIC"
        UNIQUE ("SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "RELATIONSHIP_TYPE", COALESCE("ROLE", ''), COALESCE("SEMANTIC_TYPE", ''))
);

CREATE INDEX "IDX_KG_RELATIONSHIPS_SOURCE" ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "SOURCE_ENTITY_TYPE");
CREATE INDEX "IDX_KG_RELATIONSHIPS_TARGET" ON "APPKG_RELATIONSHIPS"("TARGET_ENTITY_ID", "TARGET_ENTITY_TYPE");
CREATE INDEX "IDX_KG_RELATIONSHIPS_TYPE" ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE");
CREATE INDEX "IDX_KG_RELATIONSHIPS_SOURCE_TYPE" ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_TYPE", "RELATIONSHIP_TYPE");
CREATE INDEX "IDX_KG_RELATIONSHIPS_ROLE" ON "APPKG_RELATIONSHIPS"("ROLE") WHERE "ROLE" IS NOT NULL;
CREATE INDEX "IDX_KG_RELATIONSHIPS_SEMANTIC_TYPE" ON "APPKG_RELATIONSHIPS"("SEMANTIC_TYPE") WHERE "SEMANTIC_TYPE" IS NOT NULL;
CREATE INDEX "IDX_KG_RELATIONSHIPS_TYPE_ROLE" ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "ROLE") WHERE "ROLE" IS NOT NULL;
CREATE INDEX "IDX_KG_RELATIONSHIPS_TYPE_SEMANTIC" ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "SEMANTIC_TYPE") WHERE "SEMANTIC_TYPE" IS NOT NULL;

-- =====================================================
-- 实体图查询优化表（可选，用于提升查询性能）
-- 存储实体的图查询相关元数据，不存储实体业务数据
-- =====================================================
CREATE TABLE kg_entity_graph_metadata (
    entity_id UUID NOT NULL PRIMARY KEY, -- 实体ID（关联到现有实体表）
    entity_type VARCHAR(50) NOT NULL, -- 实体类型
    graph_properties JSONB, -- 图查询相关属性（如重要性评分、中心度等）
    last_graph_update TIMESTAMP, -- 最后图数据更新时间
    CONSTRAINT uk_entity_graph_metadata UNIQUE (entity_id, entity_type)
);

CREATE INDEX idx_entity_graph_metadata_type ON kg_entity_graph_metadata(entity_type);
CREATE INDEX idx_entity_graph_metadata_update ON kg_entity_graph_metadata(last_graph_update DESC);

-- =====================================================
-- 时间轴快照表
-- 存储业务阶段的时间轴快照数据
-- =====================================================
CREATE TABLE kg_timeline_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    business_stage VARCHAR(100) NOT NULL, -- 业务阶段（项目启动、流程设计等）
    stage_time TIMESTAMP NOT NULL, -- 阶段时间
    entity_counts JSONB NOT NULL, -- 实体数量统计 {Catalogue: 50, Person: 20, Department: 5, BusinessEntity: 10}
    relationship_counts JSONB NOT NULL, -- 关系数量统计 {CONTAINS: 120, HAS_CHILD: 45, ...}
    business_status VARCHAR(50), -- 业务状态
    snapshot_data JSONB, -- 完整快照数据（可选，用于详细分析）
    created_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_timeline_stage_time ON kg_timeline_snapshots(stage_time DESC);
CREATE INDEX idx_timeline_stage ON kg_timeline_snapshots(business_stage);

-- =====================================================
-- 风险预警表
-- 存储知识图谱相关的风险预警信息
-- =====================================================
CREATE TABLE kg_risk_alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL, -- 实体ID（关联到现有实体表）
    entity_type VARCHAR(50) NOT NULL, -- 实体类型
    risk_level VARCHAR(20) NOT NULL, -- HIGH, MEDIUM, LOW
    risk_type VARCHAR(50) NOT NULL, -- 风险类型
    risk_category VARCHAR(50) NOT NULL, -- 风险分类（数据完整性、安全性、合规性等）
    risk_description TEXT NOT NULL, -- 风险描述
    affected_entity_count INT, -- 受影响实体数量
    business_impact JSONB, -- 业务影响（包含数据完整性风险、合规性风险、操作影响等）
    mitigation_suggestion TEXT, -- 应对建议
    responsible_person_id UUID, -- 责任人ID（关联到用户表）
    status VARCHAR(20) DEFAULT 'ACTIVE', -- ACTIVE, ACKNOWLEDGED, RESOLVED
    due_date TIMESTAMP, -- 处理截止日期
    created_time TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    acknowledged_time TIMESTAMP,
    resolved_time TIMESTAMP
);

-- =====================================================
-- 知识图谱审计轨迹表（可选，用于记录图谱相关变更）
-- 注意：实体的业务变更审计由ABP框架的审计日志处理
-- 此表仅记录知识图谱特有的操作（如关系创建、图数据更新等）
-- =====================================================
CREATE TABLE kg_graph_audit_trail (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_id UUID NOT NULL, -- 实体ID（关联到现有实体表）
    entity_type VARCHAR(50) NOT NULL, -- 实体类型
    action_type VARCHAR(50) NOT NULL, -- 操作类型（RELATIONSHIP_CREATE, GRAPH_UPDATE等）
    action_description TEXT NOT NULL, -- 操作描述
    old_values JSONB, -- 变更前的值（图谱相关）
    new_values JSONB, -- 变更后的值（图谱相关）
    performed_by VARCHAR(100) NOT NULL, -- 操作人ID
    performed_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT
);

CREATE INDEX idx_graph_audit_entity ON kg_graph_audit_trail(entity_id, entity_type, performed_at DESC);
CREATE INDEX idx_graph_audit_action ON kg_graph_audit_trail(action_type, performed_at DESC);

CREATE INDEX idx_risk_alerts_level ON kg_risk_alerts(risk_level, status);
CREATE INDEX idx_risk_alerts_entity ON kg_risk_alerts(entity_id, entity_type);
CREATE INDEX idx_risk_alerts_status ON kg_risk_alerts(status) WHERE status = 'ACTIVE';
CREATE INDEX idx_risk_alerts_category ON kg_risk_alerts(risk_category);
CREATE INDEX idx_risk_alerts_due_date ON kg_risk_alerts(due_date) WHERE status = 'ACTIVE';

-- =====================================================
-- 视图：实体关系视图（用于方便查询）
-- 注意：由于PostgreSQL不支持动态表名，建议在应用层通过JOIN查询
-- 或创建多个类型特定的视图
-- =====================================================
-- 分类关系视图
CREATE OR REPLACE VIEW v_kg_catalogue_relationships AS
SELECT
    r.id AS relationship_id,
    r.source_entity_id,
    r.source_entity_type,
    r.target_entity_id,
    r.target_entity_type,
    r.relationship_type,
    r.weight,
    r.properties AS relationship_properties,
    r.created_time AS relationship_created_time,
    c1."CATALOGUE_NAME" AS source_catalogue_name,
    c2."CATALOGUE_NAME" AS target_catalogue_name
FROM kg_relationships r
LEFT JOIN "APPATTACH_CATALOGUES" c1 ON r.source_entity_type = 'Catalogue' AND c1."ID" = r.source_entity_id
LEFT JOIN "APPATTACH_CATALOGUES" c2 ON r.target_entity_type = 'Catalogue' AND c2."ID" = r.target_entity_id
WHERE r.source_entity_type = 'Catalogue' OR r.target_entity_type = 'Catalogue';

-- 注意：File 不再是知识图谱的独立维度，文件通过分类直接访问
-- 因此不需要单独的 v_kg_file_relationships 视图
```

---

## 4. API 接口设计

### 4.1 图谱查询接口

#### 4.1.1 获取图谱数据

```http
GET /api/knowledge-graph/graph
```

**查询参数**：

-   `entityTypes`：实体类型过滤（可选，多个用逗号分隔）：`Catalogue`, `Person`, `Department`, `BusinessEntity`, `Workflow`
-   `relationshipTypes`：关系类型过滤（可选）
-   `depth`：查询深度（默认 2）
-   `centerEntityId`：中心实体 ID（可选，以该实体为中心展开，支持分类、人员、部门、业务实体）
-   `maxNodes`：最大节点数（默认 500）
-   `referenceType`：过滤特定业务类型的分类（可选）

**响应示例**：

```json
{
    "nodes": [
        {
            "id": "guid-1",
            "type": "Project",
            "name": "项目A",
            "properties": {
                "status": "进行中",
                "budget": 1000000
            },
            "tags": ["重要", "2024"],
            "securityLevel": "内部",
            "updatedTime": "2024-11-21T10:00:00Z"
        }
    ],
    "edges": [
        {
            "id": "rel-1",
            "source": "guid-1",
            "target": "guid-2",
            "type": "MANAGES",
            "weight": 0.8,
            "properties": {}
        }
    ],
    "statistics": {
        "totalNodes": 150,
        "totalEdges": 320,
        "nodeTypes": {
            "Catalogue": 50,
            "Person": 20,
            "Department": 5,
            "BusinessEntity": 10
        }
    }
}
```

#### 4.1.2 获取实体详情

```http
GET /api/knowledge-graph/entities/{entityId}
```

**响应示例**：

```json
{
    "id": "guid-1",
    "type": "Project",
    "name": "项目A",
    "description": "项目描述",
    "securityLevel": "内部",
    "businessPriority": "高",
    "createdTime": "2024-01-01T00:00:00Z",
    "updatedTime": "2024-11-21T10:00:00Z",
    "properties": {
        "projectCode": "PRJ-001",
        "status": "进行中",
        "budget": 1000000,
        "startDate": "2024-01-01",
        "endDate": "2024-12-31"
    },
    "tags": ["重要", "2024"],
    "relationships": {
        "outgoing": [
            {
                "type": "MANAGES",
                "targetEntity": {
                    "id": "guid-2",
                    "name": "流程B",
                    "type": "Process"
                }
            }
        ],
        "incoming": [
            {
                "type": "MANAGES",
                "sourceEntity": {
                    "id": "guid-3",
                    "name": "技术部",
                    "type": "Department"
                }
            }
        ]
    },
    "impactAnalysis": {
        "impactRadius": 3,
        "affectedNodeCount": 15,
        "businessImpact": {
            "severity": "MEDIUM",
            "affectedBusinessProcesses": 5,
            "dataIntegrityRisk": "MEDIUM",
            "complianceRisk": "LOW"
        },
        "riskLevel": "MEDIUM",
        "mitigationSuggestions": [
            "建议通知相关流程负责人",
            "检查依赖档案的完整性"
        ]
    }
}
```

### 4.2 搜索接口

#### 4.2.1 节点搜索

```http
GET /api/knowledge-graph/search
```

**查询参数**：

-   `keyword`：搜索关键词（必需）
-   `entityTypes`：实体类型过滤（可选）
-   `tags`：标签过滤（可选，多个用逗号分隔）
-   `pageIndex`：页码（默认 1）
-   `pageSize`：每页数量（默认 20）
-   `sortBy`：排序字段（`relevance`|`priority`|`updatedTime`，默认`relevance`）

**响应示例**：

```json
{
    "totalCount": 45,
    "items": [
        {
            "id": "guid-1",
            "type": "Project",
            "name": "项目A",
            "matchedFields": ["name"],
            "matchScore": 0.95,
            "businessPriority": "高",
            "securityLevel": "内部",
            "updatedTime": "2024-11-21T10:00:00Z",
            "highlight": {
                "name": "**项目**A"
            },
            "summary": "核心业务项目，涉及多个流程和档案"
        }
    ],
    "facets": {
        "entityTypes": {
            "Project": 10,
            "Process": 20,
            "Archive": 15
        },
        "tags": {
            "重要": 25,
            "2024": 30
        }
    }
}
```

### 4.3 时间轴接口

#### 4.3.1 获取时间轴数据

```http
GET /api/knowledge-graph/timeline
```

**查询参数**：

-   `startDate`：开始日期（可选）
-   `endDate`：结束日期（可选）
-   `businessStages`：业务阶段过滤（可选，多个用逗号分隔）
-   `granularity`：时间粒度（`day`|`week`|`month`，默认`day`）

**响应示例**：

```json
{
    "timeline": [
        {
            "stage": "项目启动",
            "stageTime": "2024-01-01T00:00:00Z",
            "entityCounts": {
                "Project": 1,
                "Process": 0,
                "Archive": 0,
                "Department": 1,
                "Person": 3
            },
            "relationshipCounts": {
                "MANAGES": 1,
                "HAS": 3
            },
            "businessStatus": "启动中",
            "changes": {
                "newEntities": 5,
                "newRelationships": 4,
                "updatedEntities": 0
            }
        },
        {
            "stage": "流程设计",
            "stageTime": "2024-01-15T00:00:00Z",
            "entityCounts": {
                "Project": 1,
                "Process": 3,
                "Archive": 0,
                "Department": 1,
                "Person": 5
            },
            "relationshipCounts": {
                "MANAGES": 1,
                "CONTAINS": 0,
                "ASSIGNED_TO": 3
            },
            "businessStatus": "设计中",
            "changes": {
                "newEntities": 3,
                "newRelationships": 3,
                "updatedEntities": 1
            }
        }
    ],
    "statistics": {
        "totalStages": 8,
        "totalEntities": 150,
        "totalRelationships": 320,
        "growthTrend": "上升"
    }
}
```

### 4.4 关系管理接口

#### 4.4.1 创建关系

```http
POST /api/knowledge-graph/relationships
```

**请求体**：

```json
{
    "sourceEntityId": "guid-1",
    "sourceEntityType": "Person",
    "targetEntityId": "guid-2",
    "targetEntityType": "Catalogue",
    "relationshipType": "PersonIsProjectManagerForCatalogue",
    "description": "人员是分类的项目经理",
    "weight": 1.0,
    "properties": {
        "createdReason": "从文件内容提取",
        "autoCreated": true,
        "extractedText": "项目经理：张三",
        "personName": "张三"
    }
}
```

**响应示例**：

```json
{
    "id": "rel-guid-1",
    "sourceEntityId": "guid-1",
    "sourceEntityType": "Person",
    "targetEntityId": "guid-2",
    "targetEntityType": "Catalogue",
    "relationshipType": "PersonIsProjectManagerForCatalogue",
    "description": "人员是分类的项目经理",
    "weight": 1.0,
    "properties": {
        "createdReason": "从文件内容提取",
        "autoCreated": true,
        "extractedText": "项目经理：张三",
        "personName": "张三"
    },
    "createdTime": "2024-11-21T10:00:00Z",
    "createdBy": "user-guid-1"
}
```

#### 4.4.2 批量创建关系

```http
POST /api/knowledge-graph/relationships/batch
```

**请求体**：

```json
{
    "relationships": [
        {
            "sourceEntityId": "guid-1",
            "sourceEntityType": "Catalogue",
            "targetEntityId": "guid-2",
            "targetEntityType": "File",
            "relationshipType": "CatalogueContainsFile",
            "weight": 1.0
        }
    ],
    "skipValidation": false,
    "skipDuplicates": true
}
```

#### 4.4.3 更新关系

```http
PUT /api/knowledge-graph/relationships/{relationshipId}
```

#### 4.4.4 删除关系

```http
DELETE /api/knowledge-graph/relationships/{relationshipId}
```

#### 4.4.5 获取实体的关系网络

```http
GET /api/knowledge-graph/entities/{entityId}/relationships?direction=both&maxDepth=1
```

### 4.5 影响分析接口

#### 4.5.1 计算影响分析

```http
POST /api/knowledge-graph/impact-analysis
```

**请求体**：

```json
{
    "entityId": "guid-1",
    "changeType": "UPDATE", // CREATE, UPDATE, DELETE
    "impactRadius": 3, // 影响半径（可选，默认自动计算）
    "includeBusinessImpact": true
}
```

**响应示例**：

```json
{
    "entityId": "guid-1",
    "entityName": "项目A",
    "impactRadius": 3,
    "affectedNodes": [
        {
            "id": "guid-2",
            "name": "流程B",
            "type": "Process",
            "impactLevel": "HIGH",
            "impactReason": "直接依赖关系"
        },
        {
            "id": "guid-3",
            "name": "档案C",
            "type": "Archive",
            "impactLevel": "MEDIUM",
            "impactReason": "间接依赖关系（2跳）"
        }
    ],
    "statistics": {
        "totalAffectedNodes": 15,
        "highImpactNodes": 3,
        "mediumImpactNodes": 8,
        "lowImpactNodes": 4
    },
    "businessImpact": {
        "severity": "MEDIUM",
        "affectedBusinessProcesses": 5,
        "dataIntegrityRisk": "MEDIUM",
        "complianceRisk": "LOW",
        "operationalImpact": {
            "affectedCatalogues": 12,
            "affectedFiles": 45,
            "estimatedRecoveryTime": "2-3天",
            "complexity": "MEDIUM"
        },
        "breakdown": {
            "dataIntegrityImpact": {
                "riskLevel": "MEDIUM",
                "affectedDataSources": 3,
                "potentialDataLoss": false
            },
            "complianceImpact": {
                "riskLevel": "LOW",
                "affectedComplianceRules": 1,
                "auditRisk": "LOW"
            },
            "operationalImpact": {
                "affectedUsers": 15,
                "workflowDisruption": "MEDIUM",
                "recoveryComplexity": "MEDIUM"
            }
        }
    },
    "riskAssessment": {
        "riskLevel": "MEDIUM",
        "riskScore": 65,
        "riskFactors": [
            {
                "factor": "影响范围大",
                "weight": 0.4,
                "score": 80
            },
            {
                "factor": "业务影响中等",
                "weight": 0.3,
                "score": 60
            }
        ]
    },
    "mitigationSuggestions": [
        {
            "priority": "HIGH",
            "suggestion": "建议分阶段实施变更，先更新核心流程",
            "estimatedEffort": "2周"
        },
        {
            "priority": "MEDIUM",
            "suggestion": "通知所有受影响的责任人，准备应急预案",
            "estimatedEffort": "3天"
        }
    ]
}
```

### 4.5 风险预警接口

#### 4.5.1 获取风险预警列表

```http
GET /api/knowledge-graph/risk-alerts
```

**查询参数**：

-   `riskLevel`：风险等级过滤（`HIGH`|`MEDIUM`|`LOW`，可选）
-   `status`：状态过滤（`ACTIVE`|`ACKNOWLEDGED`|`RESOLVED`，默认`ACTIVE`）
-   `entityId`：实体 ID 过滤（可选）
-   `pageIndex`：页码（默认 1）
-   `pageSize`：每页数量（默认 20）

**响应示例**：

```json
{
    "totalCount": 12,
    "items": [
        {
            "id": "alert-1",
            "entityId": "guid-1",
            "entityName": "项目A",
            "entityType": "Project",
            "riskLevel": "HIGH",
            "riskType": "数据完整性风险",
            "riskDescription": "项目关联的3个流程存在数据不一致问题",
            "affectedEntityCount": 8,
            "businessImpact": {
                "severity": "HIGH",
                "dataIntegrityRisk": "HIGH",
                "complianceRisk": "MEDIUM",
                "operationalImpact": {
                    "affectedCatalogues": 5,
                    "affectedFiles": 20,
                    "estimatedRecoveryTime": "1-2天",
                    "complexity": "HIGH"
                }
            },
            "mitigationSuggestion": "建议立即检查并修复数据不一致问题",
            "responsiblePersonId": "person-1",
            "responsiblePersonName": "张三",
            "status": "ACTIVE",
            "createdTime": "2024-11-21T09:00:00Z",
            "acknowledgedTime": null,
            "resolvedTime": null
        }
    ],
    "statistics": {
        "highRiskCount": 3,
        "mediumRiskCount": 5,
        "lowRiskCount": 4,
        "activeCount": 12,
        "acknowledgedCount": 0,
        "resolvedCount": 0
    }
}
```

#### 4.5.2 创建风险预警

```http
POST /api/knowledge-graph/risk-alerts
```

**请求体**：

```json
{
    "entityId": "guid-1",
    "riskLevel": "HIGH",
    "riskType": "数据完整性风险",
    "riskDescription": "详细描述",
    "mitigationSuggestion": "应对建议",
    "responsiblePersonId": "person-1"
}
```

#### 4.5.3 确认风险预警

```http
PUT /api/knowledge-graph/risk-alerts/{alertId}/acknowledge
```

#### 4.5.4 解决风险预警

```http
PUT /api/knowledge-graph/risk-alerts/{alertId}/resolve
```

---

## 5. 核心算法设计

### 5.1 影响半径计算算法

#### 5.1.1 算法描述

影响半径计算采用基于图遍历的 BFS（广度优先搜索）算法，考虑关系权重和实体重要性。

```csharp
public class ImpactAnalysisService
{
    /// <summary>
    /// 计算节点变更的影响范围
    /// </summary>
    public ImpactAnalysisResult CalculateImpact(
        Guid entityId,
        ChangeType changeType,
        int? maxDepth = null)
    {
        var result = new ImpactAnalysisResult
        {
            EntityId = entityId,
            AffectedNodes = new List<AffectedNode>(),
            ImpactRadius = 0
        };

        // 使用BFS遍历图
        var queue = new Queue<(Guid nodeId, int depth, double cumulativeWeight)>();
        var visited = new HashSet<Guid>();
        var nodeImportance = new Dictionary<Guid, double>();

        queue.Enqueue((entityId, 0, 1.0));
        visited.Add(entityId);

        while (queue.Count > 0)
        {
            var (currentId, depth, weight) = queue.Dequeue();

            // 如果达到最大深度，停止遍历
            if (maxDepth.HasValue && depth >= maxDepth.Value)
                continue;

            // 获取当前节点的所有出边关系
            var relationships = GetOutgoingRelationships(currentId);

            foreach (var rel in relationships)
            {
                if (visited.Contains(rel.TargetEntityId))
                    continue;

                // 计算影响权重（关系权重 × 累积权重 × 实体重要性）
                var entityImp = GetEntityImportance(rel.TargetEntityId);
                var impactWeight = rel.Weight * weight * entityImp;

                // 如果影响权重低于阈值，跳过
                if (impactWeight < 0.1)
                    continue;

                visited.Add(rel.TargetEntityId);
                queue.Enqueue((rel.TargetEntityId, depth + 1, impactWeight));

                // 记录受影响节点
                result.AffectedNodes.Add(new AffectedNode
                {
                    Id = rel.TargetEntityId,
                    ImpactLevel = DetermineImpactLevel(impactWeight),
                    ImpactReason = $"通过{rel.Type}关系影响（{depth + 1}跳）",
                    ImpactWeight = impactWeight
                });

                result.ImpactRadius = Math.Max(result.ImpactRadius, depth + 1);
            }
        }

        // 计算统计信息
        result.Statistics = CalculateStatistics(result.AffectedNodes);

        // 计算业务影响
        result.BusinessImpact = CalculateBusinessImpact(
            result.AffectedNodes,
            changeType);

        // 风险评估
        result.RiskAssessment = AssessRisk(result);

        // 生成应对建议
        result.MitigationSuggestions = GenerateMitigationSuggestions(result);

        return result;
    }

    private ImpactLevel DetermineImpactLevel(double impactWeight)
    {
        if (impactWeight >= 0.7) return ImpactLevel.High;
        if (impactWeight >= 0.4) return ImpactLevel.Medium;
        return ImpactLevel.Low;
    }

    private double GetEntityImportance(Guid entityId)
    {
        // 根据实体类型、业务优先级、关联数量等因素计算重要性
        var entity = GetEntity(entityId);
        var importance = 1.0;

        // 业务优先级权重
        switch (entity.BusinessPriority)
        {
            case "高": importance *= 1.5; break;
            case "中": importance *= 1.0; break;
            case "低": importance *= 0.7; break;
        }

        // 关联数量权重（关联越多，重要性越高）
        var relationshipCount = GetRelationshipCount(entityId);
        importance *= Math.Min(1.0 + relationshipCount * 0.1, 2.0);

        return importance;
    }
}
```

#### 5.1.2 Neo4j Cypher 查询实现

```cypher
// 影响分析查询（使用BFS遍历）
MATCH path = (start:Entity {id: $entityId})-[*1..3]-(affected:Entity)
WHERE start.id = $entityId
WITH affected,
     length(path) as depth,
     reduce(weight = 1.0, rel in relationships(path) | weight * rel.weight) as impactWeight
WHERE impactWeight >= 0.1
RETURN affected.id as entityId,
       affected.name as entityName,
       affected.type as entityType,
       depth,
       impactWeight,
       CASE
         WHEN impactWeight >= 0.7 THEN 'HIGH'
         WHEN impactWeight >= 0.4 THEN 'MEDIUM'
         ELSE 'LOW'
       END as impactLevel
ORDER BY impactWeight DESC, depth ASC
LIMIT 100
```

### 5.2 搜索排序算法

#### 5.2.1 匹配度计算

```csharp
public class SearchService
{
    /// <summary>
    /// 计算搜索匹配度
    /// 注意：entity参数包含从现有实体表JOIN获取的完整信息
    /// </summary>
    public double CalculateRelevanceScore(
        KnowledgeGraphEntityViewModel entity, // 包含从现有实体表JOIN获取的Name、Tags等信息
        string keyword,
        List<string> matchedFields)
    {
        double score = 0.0;
        var keywordLower = keyword.ToLowerInvariant();

        // 名称匹配（权重最高）
        if (matchedFields.Contains("name"))
        {
            var nameMatch = CalculateStringSimilarity(
                entity.Name.ToLowerInvariant(),
                keywordLower);
            score += nameMatch * 0.5; // 名称匹配权重50%
        }

        // 标签匹配
        if (entity.Tags != null)
        {
            var tagMatches = entity.Tags.Count(tag =>
                tag.ToLowerInvariant().Contains(keywordLower));
            if (tagMatches > 0)
            {
                score += (tagMatches / (double)entity.Tags.Count) * 0.3; // 标签匹配权重30%
            }
        }

        // 描述匹配（如果实体有Description字段）
        // 注意：Description字段从现有实体表的Summary或Description字段获取
        var description = entity.GraphProperties?.GetValueOrDefault("description")?.ToString();
        if (!string.IsNullOrEmpty(description) &&
            description.ToLowerInvariant().Contains(keywordLower))
        {
            score += 0.2; // 描述匹配权重20%
        }

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// 综合排序（匹配度 + 业务优先级）
    /// </summary>
    public List<SearchResult> SortSearchResults(
        List<SearchResult> results,
        SortBy sortBy)
    {
        return sortBy switch
        {
            SortBy.Relevance => results
                .OrderByDescending(r => r.MatchScore)
                .ThenByDescending(r => GetBusinessPriorityWeight(r.BusinessPriority))
                .ToList(),

            SortBy.Priority => results
                .OrderByDescending(r => GetBusinessPriorityWeight(r.BusinessPriority))
                .ThenByDescending(r => r.MatchScore)
                .ToList(),

            SortBy.UpdatedTime => results
                .OrderByDescending(r => r.UpdatedTime)
                .ThenByDescending(r => r.MatchScore)
                .ToList(),

            _ => results
        };
    }

    private double GetBusinessPriorityWeight(string? priority)
    {
        return priority switch
        {
            "高" => 3.0,
            "中" => 2.0,
            "低" => 1.0,
            _ => 1.0
        };
    }

    private double CalculateStringSimilarity(string str1, string str2)
    {
        // 使用Levenshtein距离或Jaro-Winkler相似度
        // 简化实现：使用包含关系
        if (str1 == str2) return 1.0;
        if (str1.Contains(str2) || str2.Contains(str1)) return 0.8;

        // 可以使用更复杂的字符串相似度算法
        return 0.0;
    }
}
```

#### 5.2.2 PostgreSQL 全文搜索实现

```sql
-- 使用PostgreSQL全文搜索进行节点搜索
-- 假设已创建全文搜索索引（GIN索引）
SELECT
    e.id,
    e.entity_type,
    e.name,
    e.tags,
    e.properties,
    -- 计算匹配度分数（名称权重3.0，标签权重2.0，描述权重1.0）
    (
        ts_rank_cd(
            to_tsvector('simple', COALESCE(e.name, '')),
            plainto_tsquery('simple', $keyword)
        ) * 3.0 +
        ts_rank_cd(
            to_tsvector('simple', COALESCE(array_to_string(e.tags, ' '), '')),
            plainto_tsquery('simple', $keyword)
        ) * 2.0 +
        ts_rank_cd(
            to_tsvector('simple', COALESCE(e.properties->>'description', '')),
            plainto_tsquery('simple', $keyword)
        ) * 1.0
    ) as match_score,
    e.business_priority,
    e.security_level,
    e.updated_time
FROM kg_entity_graph_metadata e
WHERE
    -- 全文搜索条件
    (
        to_tsvector('simple', COALESCE(e.name, '')) @@ plainto_tsquery('simple', $keyword) OR
        to_tsvector('simple', COALESCE(array_to_string(e.tags, ' '), '')) @@ plainto_tsquery('simple', $keyword) OR
        to_tsvector('simple', COALESCE(e.properties->>'description', '')) @@ plainto_tsquery('simple', $keyword)
    )
    -- 实体类型过滤
    AND ($entityTypes IS NULL OR e.entity_type = ANY($entityTypes))
    -- 标签过滤
    AND ($tags IS NULL OR e.tags && $tags)
ORDER BY
    match_score DESC,
    CASE e.business_priority
        WHEN '高' THEN 3
        WHEN '中' THEN 2
        WHEN '低' THEN 1
        ELSE 0
    END DESC,
    e.updated_time DESC
LIMIT $pageSize OFFSET $offset;
```

**PostgreSQL 全文搜索索引创建**：

```sql
-- 为实体名称创建全文搜索索引
CREATE INDEX idx_entity_name_fts ON kg_entity_graph_metadata
    USING GIN(to_tsvector('simple', COALESCE((graph_properties->>'name')::text, '')));

-- 为标签数组创建全文搜索索引
CREATE INDEX idx_entity_tags_fts ON kg_entity_graph_metadata
    USING GIN(to_tsvector('simple', COALESCE(array_to_string(tags, ' '), '')));

-- 为描述创建全文搜索索引
CREATE INDEX idx_entity_description_fts ON kg_entity_graph_metadata
    USING GIN(to_tsvector('simple', COALESCE((graph_properties->>'description')::text, '')));

-- 复合全文搜索索引（提升查询性能）
CREATE INDEX idx_entity_fulltext_fts ON kg_entity_graph_metadata
    USING GIN(
        to_tsvector('simple',
            COALESCE((graph_properties->>'name')::text, '') || ' ' ||
            COALESCE(array_to_string(tags, ' '), '') || ' ' ||
            COALESCE((graph_properties->>'description')::text, '')
        )
    );
```

### 5.3 风险评估算法

```csharp
public class RiskAssessmentService
{
    /// <summary>
    /// 评估节点变更的风险等级
    /// </summary>
    public RiskAssessment AssessRisk(ImpactAnalysisResult impactAnalysis)
    {
        var riskFactors = new List<RiskFactor>();

        // 因子1：影响范围
        var scopeFactor = new RiskFactor
        {
            Factor = "影响范围",
            Weight = 0.4,
            Score = CalculateScopeScore(impactAnalysis.AffectedNodes.Count)
        };
        riskFactors.Add(scopeFactor);

        // 因子2：业务影响（数据完整性、合规性、操作影响）
        var businessImpactFactor = new RiskFactor
        {
            Factor = "业务影响",
            Weight = 0.3,
            Score = CalculateBusinessImpactScore(impactAnalysis.BusinessImpact)
        };
        riskFactors.Add(businessImpactFactor);

        // 因子3：高影响节点比例
        var highImpactRatio = impactAnalysis.AffectedNodes.Count > 0
            ? impactAnalysis.AffectedNodes.Count(n => n.ImpactLevel == ImpactLevel.High)
              / (double)impactAnalysis.AffectedNodes.Count
            : 0.0;
        var highImpactFactor = new RiskFactor
        {
            Factor = "高影响节点比例",
            Weight = 0.2,
            Score = highImpactRatio * 100
        };
        riskFactors.Add(highImpactFactor);

        // 因子4：影响半径
        var radiusFactor = new RiskFactor
        {
            Factor = "影响半径",
            Weight = 0.1,
            Score = Math.Min(impactAnalysis.ImpactRadius * 20, 100)
        };
        riskFactors.Add(radiusFactor);

        // 计算综合风险分数
        var totalScore = riskFactors.Sum(f => f.Score * f.Weight);

        // 确定风险等级
        var riskLevel = totalScore >= 70 ? RiskLevel.High
                     : totalScore >= 40 ? RiskLevel.Medium
                     : RiskLevel.Low;

        return new RiskAssessment
        {
            RiskLevel = riskLevel,
            RiskScore = totalScore,
            RiskFactors = riskFactors
        };
    }

    private double CalculateScopeScore(int affectedCount)
    {
        // 影响节点数越多，分数越高（最高100分）
        return Math.Min(affectedCount * 5, 100);
    }

    /// <summary>
    /// 计算业务影响分数（综合考虑数据完整性、合规性、操作影响）
    /// </summary>
    private double CalculateBusinessImpactScore(BusinessImpact? businessImpact)
    {
        if (businessImpact == null) return 0.0;

        double score = 0.0;

        // 数据完整性风险评分（权重40%）
        var dataIntegrityScore = businessImpact.DataIntegrityRisk switch
        {
            "HIGH" => 100.0,
            "MEDIUM" => 60.0,
            "LOW" => 20.0,
            _ => 0.0
        };
        score += dataIntegrityScore * 0.4;

        // 合规性风险评分（权重30%）
        var complianceScore = businessImpact.ComplianceRisk switch
        {
            "HIGH" => 100.0,
            "MEDIUM" => 60.0,
            "LOW" => 20.0,
            _ => 0.0
        };
        score += complianceScore * 0.3;

        // 操作影响评分（权重30%）
        var operationalScore = businessImpact.OperationalImpact?.Complexity switch
        {
            "HIGH" => 100.0,
            "MEDIUM" => 60.0,
            "LOW" => 20.0,
            _ => 0.0
        } ?? 0.0;
        score += operationalScore * 0.3;

        return Math.Min(score, 100.0);
    }

    /// <summary>
    /// 计算业务影响（基于受影响节点和变更类型）
    /// </summary>
    private BusinessImpact CalculateBusinessImpact(
        List<AffectedNode> affectedNodes,
        ChangeType changeType)
    {
        var result = new BusinessImpact
        {
            Severity = DetermineBusinessSeverity(affectedNodes, changeType),
            AffectedBusinessProcesses = CalculateAffectedBusinessProcesses(affectedNodes),
            DataIntegrityRisk = AssessDataIntegrityRisk(affectedNodes, changeType),
            ComplianceRisk = AssessComplianceRisk(affectedNodes, changeType),
            OperationalImpact = CalculateOperationalImpact(affectedNodes, changeType)
        };

        return result;
    }

    /// <summary>
    /// 确定业务影响严重程度
    /// </summary>
    private string DetermineBusinessSeverity(List<AffectedNode> affectedNodes, ChangeType changeType)
    {
        var highImpactCount = affectedNodes.Count(n => n.ImpactLevel == ImpactLevel.High);
        var totalCount = affectedNodes.Count;

        // DELETE操作的影响最严重
        if (changeType == ChangeType.Delete)
        {
            if (highImpactCount > totalCount * 0.5 || totalCount > 20)
                return "HIGH";
            if (highImpactCount > 0 || totalCount > 10)
                return "MEDIUM";
            return "LOW";
        }

        // UPDATE操作的影响中等
        if (changeType == ChangeType.Update)
        {
            if (highImpactCount > totalCount * 0.3 || totalCount > 15)
                return "MEDIUM";
            return "LOW";
        }

        // CREATE操作的影响较低
        return totalCount > 10 ? "MEDIUM" : "LOW";
    }

    /// <summary>
    /// 计算受影响的业务流程数量
    /// </summary>
    private int CalculateAffectedBusinessProcesses(List<AffectedNode> affectedNodes)
    {
        // 根据受影响节点的ReferenceType统计不同的业务流程
        var businessProcesses = affectedNodes
            .Select(n => GetEntity(n.Id)?.ReferenceType)
            .Where(rt => rt.HasValue)
            .Distinct()
            .Count();

        return businessProcesses;
    }

    /// <summary>
    /// 评估数据完整性风险
    /// </summary>
    private string AssessDataIntegrityRisk(List<AffectedNode> affectedNodes, ChangeType changeType)
    {
        var catalogueCount = affectedNodes.Count(n => GetEntity(n.Id)?.Type == "Catalogue");

        // DELETE操作对数据完整性影响最大
        if (changeType == ChangeType.Delete)
        {
            if (catalogueCount > 5)
                return "HIGH";
            if (catalogueCount > 2)
                return "MEDIUM";
            return "LOW";
        }

        // UPDATE操作可能影响数据一致性
        if (changeType == ChangeType.Update)
        {
            if (catalogueCount > 3)
                return "MEDIUM";
            return "LOW";
        }

        return "LOW";
    }

    /// <summary>
    /// 评估合规性风险
    /// </summary>
    private string AssessComplianceRisk(List<AffectedNode> affectedNodes, ChangeType changeType)
    {
        // 检查受影响节点中是否有归档的分类（归档数据通常有合规性要求）
        var archivedCount = affectedNodes.Count(n =>
        {
            var entity = GetEntity(n.Id);
            return entity?.Type == "Catalogue" && entity.IsArchived == true;
        });

        // DELETE操作对合规性影响最大
        if (changeType == ChangeType.Delete)
        {
            if (archivedCount > 2)
                return "HIGH";
            if (archivedCount > 0)
                return "MEDIUM";
            return "LOW";
        }

        // UPDATE操作可能影响审计轨迹
        if (changeType == ChangeType.Update && archivedCount > 0)
            return "MEDIUM";

        return "LOW";
    }

    /// <summary>
    /// 计算操作影响
    /// </summary>
    private OperationalImpact CalculateOperationalImpact(
        List<AffectedNode> affectedNodes,
        ChangeType changeType)
    {
        var catalogueCount = affectedNodes.Count(n => GetEntity(n.Id)?.Type == "Catalogue");
        var totalCount = affectedNodes.Count;

        // 确定复杂度
        string complexity;
        string estimatedRecoveryTime;

        if (changeType == ChangeType.Delete)
        {
            if (totalCount > 20 || catalogueCount > 5)
            {
                complexity = "HIGH";
                estimatedRecoveryTime = "3-5天";
            }
            else if (totalCount > 10 || catalogueCount > 2)
            {
                complexity = "MEDIUM";
                estimatedRecoveryTime = "1-2天";
            }
            else
            {
                complexity = "LOW";
                estimatedRecoveryTime = "半天内";
            }
        }
        else if (changeType == ChangeType.Update)
        {
            if (totalCount > 15 || catalogueCount > 3)
            {
                complexity = "MEDIUM";
                estimatedRecoveryTime = "1-2天";
            }
            else
            {
                complexity = "LOW";
                estimatedRecoveryTime = "半天内";
            }
        }
        else
        {
            complexity = "LOW";
            estimatedRecoveryTime = "无需恢复";
        }

        return new OperationalImpact
        {
            AffectedCatalogues = catalogueCount,
            AffectedFiles = 0, // 文件通过分类直接访问，不单独统计
            EstimatedRecoveryTime = estimatedRecoveryTime,
            Complexity = complexity
        };
    }
}
```

---

## 6. 前端实现方案

### 6.1 图谱可视化组件

#### 6.1.1 React 组件结构

```tsx
// KnowledgeGraphView.tsx
import React, { useEffect, useRef, useState } from 'react';
import cytoscape from 'cytoscape';
import dagre from 'cytoscape-dagre';
import { Card, Spin, message } from 'antd';

cytoscape.use(dagre);

interface KnowledgeGraphViewProps {
    entityTypes?: string[];
    relationshipTypes?: string[];
    centerEntityId?: string;
    onNodeClick?: (nodeId: string) => void;
}

const KnowledgeGraphView: React.FC<KnowledgeGraphViewProps> = ({
    entityTypes,
    relationshipTypes,
    centerEntityId,
    onNodeClick,
}) => {
    const containerRef = useRef<HTMLDivElement>(null);
    const cyRef = useRef<cytoscape.Core | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (!containerRef.current) return;

        // 初始化Cytoscape
        const cy = cytoscape({
            container: containerRef.current,
            elements: [],
            style: [
                {
                    selector: 'node',
                    style: {
                        label: 'data(name)',
                        width: 'mapData(importance, 0, 100, 20, 60)',
                        height: 'mapData(importance, 0, 100, 20, 60)',
                        'background-color': 'data(color)',
                        'border-width': 2,
                        'border-color': '#fff',
                        'font-size': '12px',
                        'text-valign': 'center',
                        'text-halign': 'center',
                    },
                },
                {
                    selector: 'edge',
                    style: {
                        width: 'mapData(weight, 0, 1, 1, 5)',
                        'line-color': '#999',
                        'target-arrow-color': '#999',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier',
                        label: 'data(type)',
                        'font-size': '10px',
                    },
                },
                {
                    selector: 'node[type="Project"]',
                    style: { 'background-color': '#1890ff' },
                },
                {
                    selector: 'node[type="Process"]',
                    style: { 'background-color': '#52c41a' },
                },
                {
                    selector: 'node[type="Archive"]',
                    style: { 'background-color': '#faad14' },
                },
                {
                    selector: 'node[type="Department"]',
                    style: { 'background-color': '#722ed1' },
                },
                {
                    selector: 'node[type="Person"]',
                    style: { 'background-color': '#eb2f96' },
                },
            ],
            layout: {
                name: 'dagre',
                rankDir: 'TB',
                spacingFactor: 1.5,
            },
        });

        cyRef.current = cy;

        // 节点点击事件
        cy.on('tap', 'node', (evt) => {
            const node = evt.target;
            onNodeClick?.(node.id());
        });

        // 加载图谱数据
        loadGraphData();

        return () => {
            cy.destroy();
        };
    }, [entityTypes, relationshipTypes, centerEntityId]);

    const loadGraphData = async () => {
        setLoading(true);
        try {
            const response = await fetch(
                '/api/knowledge-graph/graph?' +
                    new URLSearchParams({
                        entityTypes: entityTypes?.join(',') || '',
                        relationshipTypes: relationshipTypes?.join(',') || '',
                        centerEntityId: centerEntityId || '',
                    })
            );
            const data = await response.json();

            // 转换数据格式
            const elements = [
                ...data.nodes.map((node: any) => ({
                    data: {
                        id: node.id,
                        name: node.name,
                        type: node.type,
                        importance: node.properties?.importance || 50,
                        color: getNodeColor(node.type),
                    },
                })),
                ...data.edges.map((edge: any) => ({
                    data: {
                        id: edge.id,
                        source: edge.source,
                        target: edge.target,
                        type: edge.type,
                        weight: edge.weight || 0.5,
                    },
                })),
            ];

            cyRef.current?.json({ elements });
            cyRef.current?.layout({ name: 'dagre' }).run();
        } catch (error) {
            message.error('加载图谱数据失败');
        } finally {
            setLoading(false);
        }
    };

    const getNodeColor = (type: string): string => {
        const colorMap: Record<string, string> = {
            Project: '#1890ff',
            Process: '#52c41a',
            Archive: '#faad14',
            Department: '#722ed1',
            Person: '#eb2f96',
        };
        return colorMap[type] || '#999';
    };

    return (
        <Card>
            <Spin spinning={loading}>
                <div
                    ref={containerRef}
                    style={{
                        width: '100%',
                        height: '800px',
                        border: '1px solid #d9d9d9',
                        borderRadius: '4px',
                    }}
                />
            </Spin>
        </Card>
    );
};

export default KnowledgeGraphView;
```

#### 6.1.2 节点搜索组件

```tsx
// NodeSearchPanel.tsx
import React, { useState, useEffect } from 'react';
import { Input, List, Tag, Empty, Spin } from 'antd';
import { SearchOutlined } from '@ant-design/icons';

interface SearchResult {
    id: string;
    type: string;
    name: string;
    matchedFields: string[];
    matchScore: number;
    businessPriority?: string;
    securityLevel?: string;
    updatedTime: string;
    highlight?: {
        name?: string;
    };
}

const NodeSearchPanel: React.FC<{
    onSelect: (entityId: string) => void;
}> = ({ onSelect }) => {
    const [keyword, setKeyword] = useState('');
    const [results, setResults] = useState<SearchResult[]>([]);
    const [loading, setLoading] = useState(false);
    const [debounceTimer, setDebounceTimer] = useState<NodeJS.Timeout>();

    useEffect(() => {
        if (debounceTimer) {
            clearTimeout(debounceTimer);
        }

        if (!keyword.trim()) {
            setResults([]);
            return;
        }

        const timer = setTimeout(() => {
            performSearch(keyword);
        }, 300); // 防抖300ms

        setDebounceTimer(timer);

        return () => {
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }
        };
    }, [keyword]);

    const performSearch = async (searchKeyword: string) => {
        setLoading(true);
        try {
            const response = await fetch(
                `/api/knowledge-graph/search?keyword=${encodeURIComponent(
                    searchKeyword
                )}`
            );
            const data = await response.json();
            setResults(data.items || []);
        } catch (error) {
            console.error('搜索失败:', error);
        } finally {
            setLoading(false);
        }
    };

    const getTypeColor = (type: string): string => {
        const colorMap: Record<string, string> = {
            Project: 'blue',
            Process: 'green',
            Archive: 'orange',
            Department: 'purple',
            Person: 'pink',
        };
        return colorMap[type] || 'default';
    };

    const getPriorityColor = (priority?: string): string => {
        const colorMap: Record<string, string> = {
            高: 'red',
            中: 'orange',
            低: 'blue',
        };
        return colorMap[priority || ''] || 'default';
    };

    return (
        <div>
            <Input
                placeholder="搜索节点名称或标签（不区分大小写）"
                prefix={<SearchOutlined />}
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                allowClear
            />
            <Spin spinning={loading}>
                <List
                    style={{
                        marginTop: 16,
                        maxHeight: '600px',
                        overflowY: 'auto',
                    }}
                    dataSource={results}
                    locale={{ emptyText: <Empty description="暂无搜索结果" /> }}
                    renderItem={(item) => (
                        <List.Item
                            style={{ cursor: 'pointer' }}
                            onClick={() => onSelect(item.id)}
                        >
                            <List.Item.Meta
                                title={
                                    <div>
                                        <span
                                            dangerouslySetInnerHTML={{
                                                __html:
                                                    item.highlight?.name ||
                                                    item.name,
                                            }}
                                        />
                                        <Tag
                                            color={getTypeColor(item.type)}
                                            style={{ marginLeft: 8 }}
                                        >
                                            {item.type}
                                        </Tag>
                                        {item.businessPriority && (
                                            <Tag
                                                color={getPriorityColor(
                                                    item.businessPriority
                                                )}
                                            >
                                                {item.businessPriority}优先级
                                            </Tag>
                                        )}
                                    </div>
                                }
                                description={
                                    <div>
                                        <div>
                                            匹配度:{' '}
                                            {(item.matchScore * 100).toFixed(0)}
                                            %
                                        </div>
                                        <div>
                                            密级:{' '}
                                            {item.securityLevel || '未设置'}
                                        </div>
                                        <div>
                                            更新时间:{' '}
                                            {new Date(
                                                item.updatedTime
                                            ).toLocaleString()}
                                        </div>
                                    </div>
                                }
                            />
                        </List.Item>
                    )}
                />
            </Spin>
        </div>
    );
};

export default NodeSearchPanel;
```

#### 6.1.3 时间轴组件

```tsx
// TimelineView.tsx
import React, { useEffect, useState } from 'react';
import { Timeline, Card, Statistic, Row, Col, Select } from 'antd';
import { ClockCircleOutlined } from '@ant-design/icons';

interface TimelineSnapshot {
    stage: string;
    stageTime: string;
    entityCounts: Record<string, number>;
    relationshipCounts: Record<string, number>;
    businessStatus: string;
    changes: {
        newEntities: number;
        newRelationships: number;
        updatedEntities: number;
    };
}

const TimelineView: React.FC = () => {
    const [snapshots, setSnapshots] = useState<TimelineSnapshot[]>([]);
    const [loading, setLoading] = useState(false);
    const [granularity, setGranularity] = useState<'day' | 'week' | 'month'>(
        'day'
    );

    useEffect(() => {
        loadTimelineData();
    }, [granularity]);

    const loadTimelineData = async () => {
        setLoading(true);
        try {
            const response = await fetch(
                `/api/knowledge-graph/timeline?granularity=${granularity}`
            );
            const data = await response.json();
            setSnapshots(data.timeline || []);
        } catch (error) {
            console.error('加载时间轴数据失败:', error);
        } finally {
            setLoading(false);
        }
    };

    const getTotalEntities = (counts: Record<string, number>): number => {
        return Object.values(counts).reduce((sum, count) => sum + count, 0);
    };

    const getTotalRelationships = (counts: Record<string, number>): number => {
        return Object.values(counts).reduce((sum, count) => sum + count, 0);
    };

    return (
        <Card
            title="业务时间轴"
            extra={
                <Select
                    value={granularity}
                    onChange={setGranularity}
                    style={{ width: 120 }}
                >
                    <Select.Option value="day">按天</Select.Option>
                    <Select.Option value="week">按周</Select.Option>
                    <Select.Option value="month">按月</Select.Option>
                </Select>
            }
        >
            <Timeline mode="left">
                {snapshots.map((snapshot, index) => (
                    <Timeline.Item
                        key={index}
                        dot={
                            <ClockCircleOutlined style={{ fontSize: '16px' }} />
                        }
                    >
                        <Card size="small">
                            <div style={{ marginBottom: 8 }}>
                                <strong>{snapshot.stage}</strong>
                                <span style={{ marginLeft: 16, color: '#999' }}>
                                    {new Date(
                                        snapshot.stageTime
                                    ).toLocaleString()}
                                </span>
                            </div>
                            <Row gutter={16}>
                                <Col span={6}>
                                    <Statistic
                                        title="实体总数"
                                        value={getTotalEntities(
                                            snapshot.entityCounts
                                        )}
                                    />
                                </Col>
                                <Col span={6}>
                                    <Statistic
                                        title="关系总数"
                                        value={getTotalRelationships(
                                            snapshot.relationshipCounts
                                        )}
                                    />
                                </Col>
                                <Col span={6}>
                                    <Statistic
                                        title="新增实体"
                                        value={snapshot.changes.newEntities}
                                        valueStyle={{ color: '#3f8600' }}
                                    />
                                </Col>
                                <Col span={6}>
                                    <Statistic
                                        title="新增关系"
                                        value={
                                            snapshot.changes.newRelationships
                                        }
                                        valueStyle={{ color: '#3f8600' }}
                                    />
                                </Col>
                            </Row>
                            <div style={{ marginTop: 8 }}>
                                <strong>业务状态:</strong>{' '}
                                {snapshot.businessStatus}
                            </div>
                            <div style={{ marginTop: 8 }}>
                                <strong>实体分布:</strong>
                                {Object.entries(snapshot.entityCounts).map(
                                    ([type, count]) => (
                                        <span
                                            key={type}
                                            style={{ marginLeft: 8 }}
                                        >
                                            {type}: {count}
                                        </span>
                                    )
                                )}
                            </div>
                        </Card>
                    </Timeline.Item>
                ))}
            </Timeline>
        </Card>
    );
};

export default TimelineView;
```

#### 6.1.4 节点详情面板

```tsx
// NodeDetailPanel.tsx
import React, { useEffect, useState } from 'react';
import { Drawer, Descriptions, Tag, Alert, List, Card, Tabs } from 'antd';
import { WarningOutlined } from '@ant-design/icons';

interface EntityDetail {
    id: string;
    type: string;
    name: string;
    description?: string;
    securityLevel?: string;
    businessPriority?: string;
    createdTime: string;
    updatedTime?: string;
    properties: Record<string, any>;
    tags: string[];
    relationships: {
        outgoing: Array<{
            type: string;
            targetEntity: { id: string; name: string; type: string };
        }>;
        incoming: Array<{
            type: string;
            sourceEntity: { id: string; name: string; type: string };
        }>;
    };
    impactAnalysis?: {
        impactRadius: number;
        affectedNodeCount: number;
        businessImpact?: {
            severity: string;
            affectedBusinessProcesses?: number;
            dataIntegrityRisk: string;
            complianceRisk: string;
            operationalImpact?: {
                affectedCatalogues: number;
                affectedFiles: number;
                estimatedRecoveryTime: string;
                complexity: string;
            };
        };
        riskLevel: string;
        mitigationSuggestions: string[];
    };
}

const NodeDetailPanel: React.FC<{
    entityId: string | null;
    visible: boolean;
    onClose: () => void;
    onEntityClick: (entityId: string) => void;
}> = ({ entityId, visible, onClose, onEntityClick }) => {
    const [detail, setDetail] = useState<EntityDetail | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (entityId && visible) {
            loadEntityDetail(entityId);
        }
    }, [entityId, visible]);

    const loadEntityDetail = async (id: string) => {
        setLoading(true);
        try {
            const response = await fetch(`/api/knowledge-graph/entities/${id}`);
            const data = await response.json();
            setDetail(data);
        } catch (error) {
            console.error('加载实体详情失败:', error);
        } finally {
            setLoading(false);
        }
    };

    const getRiskLevelColor = (level: string): string => {
        const colorMap: Record<string, string> = {
            HIGH: 'red',
            MEDIUM: 'orange',
            LOW: 'blue',
        };
        return colorMap[level] || 'default';
    };

    if (!detail) return null;

    return (
        <Drawer
            title={detail.name}
            placement="right"
            width={600}
            open={visible}
            onClose={onClose}
            loading={loading}
        >
            <Tabs defaultActiveKey="basic">
                <Tabs.TabPane tab="基本信息" key="basic">
                    <Descriptions column={1} bordered>
                        <Descriptions.Item label="实体类型">
                            <Tag>{detail.type}</Tag>
                        </Descriptions.Item>
                        <Descriptions.Item label="名称">
                            {detail.name}
                        </Descriptions.Item>
                        <Descriptions.Item label="描述">
                            {detail.description || '无'}
                        </Descriptions.Item>
                        <Descriptions.Item label="密级">
                            {detail.securityLevel || '未设置'}
                        </Descriptions.Item>
                        <Descriptions.Item label="业务优先级">
                            {detail.businessPriority ? (
                                <Tag
                                    color={
                                        detail.businessPriority === '高'
                                            ? 'red'
                                            : 'blue'
                                    }
                                >
                                    {detail.businessPriority}
                                </Tag>
                            ) : (
                                '未设置'
                            )}
                        </Descriptions.Item>
                        <Descriptions.Item label="标签">
                            {detail.tags.map((tag) => (
                                <Tag key={tag}>{tag}</Tag>
                            ))}
                        </Descriptions.Item>
                        <Descriptions.Item label="创建时间">
                            {new Date(detail.createdTime).toLocaleString()}
                        </Descriptions.Item>
                        <Descriptions.Item label="更新时间">
                            {detail.updatedTime
                                ? new Date(detail.updatedTime).toLocaleString()
                                : '无'}
                        </Descriptions.Item>
                    </Descriptions>

                    {Object.keys(detail.properties).length > 0 && (
                        <Card title="业务专属信息" style={{ marginTop: 16 }}>
                            <Descriptions column={1}>
                                {Object.entries(detail.properties).map(
                                    ([key, value]) => (
                                        <Descriptions.Item
                                            key={key}
                                            label={key}
                                        >
                                            {String(value)}
                                        </Descriptions.Item>
                                    )
                                )}
                            </Descriptions>
                        </Card>
                    )}
                </Tabs.TabPane>

                <Tabs.TabPane tab="关系网络" key="relationships">
                    <Card title="出边关系" style={{ marginBottom: 16 }}>
                        <List
                            dataSource={detail.relationships.outgoing}
                            renderItem={(rel) => (
                                <List.Item
                                    style={{ cursor: 'pointer' }}
                                    onClick={() =>
                                        onEntityClick(rel.targetEntity.id)
                                    }
                                >
                                    <List.Item.Meta
                                        title={
                                            <div>
                                                <Tag>{rel.type}</Tag>
                                                <span style={{ marginLeft: 8 }}>
                                                    {rel.targetEntity.name}
                                                </span>
                                                <Tag
                                                    color="blue"
                                                    style={{ marginLeft: 8 }}
                                                >
                                                    {rel.targetEntity.type}
                                                </Tag>
                                            </div>
                                        }
                                    />
                                </List.Item>
                            )}
                        />
                    </Card>
                    <Card title="入边关系">
                        <List
                            dataSource={detail.relationships.incoming}
                            renderItem={(rel) => (
                                <List.Item
                                    style={{ cursor: 'pointer' }}
                                    onClick={() =>
                                        onEntityClick(rel.sourceEntity.id)
                                    }
                                >
                                    <List.Item.Meta
                                        title={
                                            <div>
                                                <Tag>{rel.type}</Tag>
                                                <span style={{ marginLeft: 8 }}>
                                                    {rel.sourceEntity.name}
                                                </span>
                                                <Tag
                                                    color="blue"
                                                    style={{ marginLeft: 8 }}
                                                >
                                                    {rel.sourceEntity.type}
                                                </Tag>
                                            </div>
                                        }
                                    />
                                </List.Item>
                            )}
                        />
                    </Card>
                </Tabs.TabPane>

                {detail.impactAnalysis && (
                    <Tabs.TabPane tab="影响分析" key="impact">
                        <Alert
                            message={`风险等级: ${detail.impactAnalysis.riskLevel}`}
                            type={
                                detail.impactAnalysis.riskLevel === 'HIGH'
                                    ? 'error'
                                    : detail.impactAnalysis.riskLevel ===
                                      'MEDIUM'
                                    ? 'warning'
                                    : 'info'
                            }
                            icon={<WarningOutlined />}
                            style={{ marginBottom: 16 }}
                        />
                        <Descriptions column={1} bordered>
                            <Descriptions.Item label="影响半径">
                                {detail.impactAnalysis.impactRadius} 跳
                            </Descriptions.Item>
                            <Descriptions.Item label="受影响节点数">
                                {detail.impactAnalysis.affectedNodeCount}
                            </Descriptions.Item>
                            {detail.impactAnalysis.businessImpact && (
                                <>
                                    <Descriptions.Item label="业务影响等级">
                                        <Tag
                                            color={
                                                detail.impactAnalysis
                                                    .businessImpact.severity ===
                                                'HIGH'
                                                    ? 'red'
                                                    : detail.impactAnalysis
                                                          .businessImpact
                                                          .severity === 'MEDIUM'
                                                    ? 'orange'
                                                    : 'blue'
                                            }
                                        >
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact.severity
                                            }
                                        </Tag>
                                    </Descriptions.Item>
                                    <Descriptions.Item label="数据完整性风险">
                                        <Tag
                                            color={
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .dataIntegrityRisk ===
                                                'HIGH'
                                                    ? 'red'
                                                    : detail.impactAnalysis
                                                          .businessImpact
                                                          .dataIntegrityRisk ===
                                                      'MEDIUM'
                                                    ? 'orange'
                                                    : 'blue'
                                            }
                                        >
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .dataIntegrityRisk
                                            }
                                        </Tag>
                                    </Descriptions.Item>
                                    <Descriptions.Item label="合规性风险">
                                        <Tag
                                            color={
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .complianceRisk === 'HIGH'
                                                    ? 'red'
                                                    : detail.impactAnalysis
                                                          .businessImpact
                                                          .complianceRisk ===
                                                      'MEDIUM'
                                                    ? 'orange'
                                                    : 'blue'
                                            }
                                        >
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .complianceRisk
                                            }
                                        </Tag>
                                    </Descriptions.Item>
                                    {detail.impactAnalysis.businessImpact
                                        .operationalImpact && (
                                        <Descriptions.Item label="操作影响">
                                            受影响分类:{' '}
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .operationalImpact
                                                    .affectedCatalogues
                                            }{' '}
                                            | 受影响文件:{' '}
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .operationalImpact
                                                    .affectedFiles
                                            }{' '}
                                            | 预计恢复时间:{' '}
                                            {
                                                detail.impactAnalysis
                                                    .businessImpact
                                                    .operationalImpact
                                                    .estimatedRecoveryTime
                                            }
                                        </Descriptions.Item>
                                    )}
                                </>
                            )}
                        </Descriptions>
                        {detail.impactAnalysis.mitigationSuggestions.length >
                            0 && (
                            <Card title="应对建议" style={{ marginTop: 16 }}>
                                <List
                                    dataSource={
                                        detail.impactAnalysis
                                            .mitigationSuggestions
                                    }
                                    renderItem={(suggestion, index) => (
                                        <List.Item>
                                            {index + 1}. {suggestion}
                                        </List.Item>
                                    )}
                                />
                            </Card>
                        )}
                    </Tabs.TabPane>
                )}
            </Tabs>
        </Drawer>
    );
};

export default NodeDetailPanel;
```

#### 6.1.5 风险预警组件

```tsx
// RiskAlertPanel.tsx
import React, { useEffect, useState } from 'react';
import { Card, List, Tag, Badge, Button, Space, Modal } from 'antd';
import { WarningOutlined, BellOutlined } from '@ant-design/icons';

interface RiskAlert {
    id: string;
    entityId: string;
    entityName: string;
    entityType: string;
    riskLevel: 'HIGH' | 'MEDIUM' | 'LOW';
    riskType: string;
    riskDescription: string;
    affectedEntityCount: number;
    businessImpact?: {
        severity: string;
        dataIntegrityRisk: string;
        complianceRisk: string;
        operationalImpact?: {
            affectedCatalogues: number;
            affectedFiles: number;
            estimatedRecoveryTime: string;
            complexity: string;
        };
    };
    mitigationSuggestion: string;
    responsiblePersonName?: string;
    status: 'ACTIVE' | 'ACKNOWLEDGED' | 'RESOLVED';
    createdTime: string;
}

const RiskAlertPanel: React.FC<{
    fixed?: boolean;
    onAlertClick?: (alert: RiskAlert) => void;
}> = ({ fixed = false, onAlertClick }) => {
    const [alerts, setAlerts] = useState<RiskAlert[]>([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        loadRiskAlerts();
        // 定时刷新
        const interval = setInterval(loadRiskAlerts, 30000); // 30秒刷新一次
        return () => clearInterval(interval);
    }, []);

    const loadRiskAlerts = async () => {
        setLoading(true);
        try {
            const response = await fetch(
                '/api/knowledge-graph/risk-alerts?status=ACTIVE'
            );
            const data = await response.json();
            setAlerts(data.items || []);
        } catch (error) {
            console.error('加载风险预警失败:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleNotify = async (alert: RiskAlert) => {
        try {
            // 调用通知API
            await fetch(`/api/knowledge-graph/risk-alerts/${alert.id}/notify`, {
                method: 'POST',
            });
            Modal.success({
                title: '通知成功',
                content: `已通知责任人: ${
                    alert.responsiblePersonName || '未指定'
                }`,
            });
        } catch (error) {
            Modal.error({ title: '通知失败', content: '请稍后重试' });
        }
    };

    const getRiskLevelColor = (level: string): string => {
        const colorMap: Record<string, string> = {
            HIGH: 'red',
            MEDIUM: 'orange',
            LOW: 'blue',
        };
        return colorMap[level] || 'default';
    };

    const getRiskLevelText = (level: string): string => {
        const textMap: Record<string, string> = {
            HIGH: '高',
            MEDIUM: '中',
            LOW: '低',
        };
        return textMap[level] || level;
    };

    const highRiskCount = alerts.filter((a) => a.riskLevel === 'HIGH').length;
    const mediumRiskCount = alerts.filter(
        (a) => a.riskLevel === 'MEDIUM'
    ).length;
    const lowRiskCount = alerts.filter((a) => a.riskLevel === 'LOW').length;

    return (
        <Card
            title={
                <Space>
                    <WarningOutlined />
                    <span>风险预警</span>
                    <Badge
                        count={highRiskCount}
                        style={{ backgroundColor: '#ff4d4f' }}
                    />
                    <Badge
                        count={mediumRiskCount}
                        style={{ backgroundColor: '#ff9800' }}
                    />
                    <Badge
                        count={lowRiskCount}
                        style={{ backgroundColor: '#1890ff' }}
                    />
                </Space>
            }
            style={
                fixed
                    ? {
                          position: 'fixed',
                          top: 80,
                          right: 20,
                          width: 400,
                          zIndex: 1000,
                      }
                    : {}
            }
            loading={loading}
        >
            <List
                dataSource={alerts}
                renderItem={(alert) => (
                    <List.Item
                        style={{
                            cursor: 'pointer',
                            borderLeft: `4px solid ${
                                alert.riskLevel === 'HIGH'
                                    ? '#ff4d4f'
                                    : alert.riskLevel === 'MEDIUM'
                                    ? '#ff9800'
                                    : '#1890ff'
                            }`,
                            paddingLeft: 12,
                        }}
                        onClick={() => onAlertClick?.(alert)}
                    >
                        <List.Item.Meta
                            title={
                                <div>
                                    <Tag
                                        color={getRiskLevelColor(
                                            alert.riskLevel
                                        )}
                                    >
                                        {getRiskLevelText(alert.riskLevel)}风险
                                    </Tag>
                                    <span style={{ marginLeft: 8 }}>
                                        {alert.entityName}
                                    </span>
                                    <Tag style={{ marginLeft: 8 }}>
                                        {alert.entityType}
                                    </Tag>
                                </div>
                            }
                            description={
                                <div>
                                    <div style={{ marginTop: 4 }}>
                                        {alert.riskDescription}
                                    </div>
                                    <div
                                        style={{
                                            marginTop: 4,
                                            fontSize: '12px',
                                            color: '#999',
                                        }}
                                    >
                                        影响节点: {alert.affectedEntityCount} |
                                        {alert.businessImpact &&
                                            ` 业务影响: ${alert.businessImpact.severity} | 数据完整性风险: ${alert.businessImpact.dataIntegrityRisk} |`}{' '}
                                        责任人:{' '}
                                        {alert.responsiblePersonName ||
                                            '未指定'}
                                    </div>
                                </div>
                            }
                        />
                        <Button
                            type="link"
                            icon={<BellOutlined />}
                            onClick={(e) => {
                                e.stopPropagation();
                                handleNotify(alert);
                            }}
                        >
                            通知
                        </Button>
                    </List.Item>
                )}
            />
        </Card>
    );
};

export default RiskAlertPanel;
```

---

## 7. 后端实现方案

### 7.1 DTO 定义

#### 7.1.1 业务影响相关 DTO

```csharp
// BusinessImpact.cs
public class BusinessImpact
{
    /// <summary>
    /// 业务影响严重程度（HIGH, MEDIUM, LOW）
    /// </summary>
    public string Severity { get; set; }

    /// <summary>
    /// 受影响的业务流程数量
    /// </summary>
    public int? AffectedBusinessProcesses { get; set; }

    /// <summary>
    /// 数据完整性风险等级（HIGH, MEDIUM, LOW）
    /// </summary>
    public string DataIntegrityRisk { get; set; }

    /// <summary>
    /// 合规性风险等级（HIGH, MEDIUM, LOW）
    /// </summary>
    public string ComplianceRisk { get; set; }

    /// <summary>
    /// 操作影响详情
    /// </summary>
    public OperationalImpact? OperationalImpact { get; set; }
}

// OperationalImpact.cs
public class OperationalImpact
{
    /// <summary>
    /// 受影响的分类数量
    /// </summary>
    public int AffectedCatalogues { get; set; }

    /// <summary>
    /// 受影响的文件数量
    /// </summary>
    public int AffectedFiles { get; set; }

    /// <summary>
    /// 预计恢复时间
    /// </summary>
    public string EstimatedRecoveryTime { get; set; }

    /// <summary>
    /// 操作复杂度（HIGH, MEDIUM, LOW）
    /// </summary>
    public string Complexity { get; set; }
}
```

### 7.2 服务层设计

#### 7.2.1 图谱服务接口

```csharp
// IKnowledgeGraphService.cs
public interface IKnowledgeGraphService
{
    /// <summary>
    /// 获取图谱数据
    /// </summary>
    Task<GraphDataDto> GetGraphDataAsync(GraphQueryInput input);

    /// <summary>
    /// 获取实体详情
    /// </summary>
    Task<EntityDetailDto> GetEntityDetailAsync(Guid entityId);

    /// <summary>
    /// 搜索节点
    /// </summary>
    Task<PagedResultDto<SearchResultDto>> SearchNodesAsync(NodeSearchInput input);

    /// <summary>
    /// 获取时间轴数据
    /// </summary>
    Task<TimelineDataDto> GetTimelineDataAsync(TimelineQueryInput input);

    /// <summary>
    /// 计算影响分析
    /// </summary>
    Task<ImpactAnalysisResultDto> CalculateImpactAnalysisAsync(ImpactAnalysisInput input);

    /// <summary>
    /// 获取风险预警列表
    /// </summary>
    Task<PagedResultDto<RiskAlertDto>> GetRiskAlertsAsync(RiskAlertQueryInput input);

    /// <summary>
    /// 创建风险预警
    /// </summary>
    Task<RiskAlertDto> CreateRiskAlertAsync(CreateRiskAlertInput input);

    /// <summary>
    /// 确认风险预警
    /// </summary>
    Task AcknowledgeRiskAlertAsync(Guid alertId);

    /// <summary>
    /// 解决风险预警
    /// </summary>
    Task ResolveRiskAlertAsync(Guid alertId, string resolution);

    /// <summary>
    /// 通知风险预警责任人
    /// </summary>
    Task NotifyRiskAlertAsync(Guid alertId);
}
```

#### 7.1.2 图谱服务实现

```csharp
// KnowledgeGraphService.cs
public class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly INeo4jDriver _neo4jDriver;
    private readonly IRepository<KnowledgeGraphEntityGraphMetadata, Guid> _graphMetadataRepository; // 图元数据仓库（用于全文搜索）
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository; // 引用现有实体仓库
    private readonly IRepository<KnowledgeGraphRelationship, Guid> _relationshipRepository;
    private readonly IImpactAnalysisService _impactAnalysisService;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly INotificationService _notificationService;

    public async Task<GraphDataDto> GetGraphDataAsync(GraphQueryInput input)
    {
        var query = new StringBuilder();
        query.AppendLine("MATCH (n:Entity)");

        var conditions = new List<string>();
        if (input.EntityTypes?.Any() == true)
        {
            conditions.Add($"n.type IN {JsonSerializer.Serialize(input.EntityTypes)}");
        }

        if (input.CenterEntityId.HasValue)
        {
            query.Clear();
            query.AppendLine($"MATCH path = (center:Entity {{id: $centerId}})-[*1..{input.Depth ?? 2}]-(related:Entity)");
            query.AppendLine("WHERE center.id = $centerId");
            query.AppendLine("WITH DISTINCT related as n");
        }
        else
        {
            if (conditions.Any())
            {
                query.AppendLine($"WHERE {string.Join(" AND ", conditions)}");
            }
        }

        query.AppendLine("OPTIONAL MATCH (n)-[r]->(m:Entity)");
        query.AppendLine($"RETURN n, collect({{rel: r, target: m}}) as relationships");
        query.AppendLine($"LIMIT {input.MaxNodes ?? 500}");

        var session = _neo4jDriver.AsyncSession();
        try
        {
            var result = await session.RunAsync(query.ToString(), new
            {
                centerId = input.CenterEntityId?.ToString(),
                entityTypes = input.EntityTypes
            });

            var nodes = new List<NodeDto>();
            var edges = new List<EdgeDto>();
            var userId = CurrentUser.Id;
            var userRoles = await GetUserRolesAsync(userId);

            await foreach (var record in result)
            {
                var node = record["n"].As<INode>();
                var nodeDto = MapToNodeDto(node);

                // 访问控制：利用AttachCatalogue.Permissions过滤无权限的实体
                // 注意：nodeDto.EntityId 对应现有实体的 Id（如 AttachCatalogue.Id）
                if (await CheckEntityAccessAsync(nodeDto.EntityId, nodeDto.Type, userId, PermissionAction.Read, userRoles))
                {
                    nodes.Add(nodeDto);

                    var relationships = record["relationships"].As<List<object>>();
                    foreach (var relObj in relationships)
                    {
                        // 解析关系数据
                        // ...
                    }
                }
            }

            return new GraphDataDto
            {
                Nodes = nodes,
                Edges = edges,
                Statistics = CalculateStatistics(nodes, edges)
            };
        }
        finally
        {
            await session.CloseAsync();
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
        PermissionAction action,
        List<string> userRoles)
    {
        if (entityType == "Catalogue")
        {
            var catalogue = await _catalogueRepository.GetAsync(entityId);
            if (catalogue == null) return false;

            // 使用AttachCatalogue的权限检查方法
            return catalogue.HasPermission(userId, action, userRoles) ||
                   catalogue.HasInheritedPermission(userId, action, userRoles,
                       catalogue.ParentId.HasValue ? await _catalogueRepository.GetAsync(catalogue.ParentId.Value) : null,
                       null); // Template信息已体现在分类中，无需单独查询
        }

        // 其他实体类型（Person、Department、BusinessEntity、Workflow）默认允许访问
        // 可根据需要扩展权限检查逻辑
        return true;
    }

    public async Task<PagedResultDto<SearchResultDto>> SearchNodesAsync(NodeSearchInput input)
    {
        var stopwatch = Stopwatch.StartNew();

        // 使用PostgreSQL全文搜索进行节点搜索
        var queryable = await _graphMetadataRepository.GetQueryableAsync();

        // 构建全文搜索查询
        var keyword = input.Keyword.Trim();

        // 构建WHERE条件
        var whereConditions = new List<Expression<Func<KnowledgeGraphEntityGraphMetadata, bool>>>();

        // 全文搜索条件（名称、标签、描述）
        whereConditions.Add(e =>
            EF.Functions.ToTsVector("simple", e.GraphProperties["name"].ToString() ?? "")
                .Matches(EF.Functions.PlainToTsQuery("simple", keyword)) ||
            EF.Functions.ToTsVector("simple", string.Join(" ", e.Tags ?? new List<string>()))
                .Matches(EF.Functions.PlainToTsQuery("simple", keyword)) ||
            EF.Functions.ToTsVector("simple", e.GraphProperties["description"].ToString() ?? "")
                .Matches(EF.Functions.PlainToTsQuery("simple", keyword))
        );

        // 实体类型过滤
        if (input.EntityTypes?.Any() == true)
        {
            whereConditions.Add(e => input.EntityTypes.Contains(e.EntityType));
        }

        // 标签过滤
        if (input.Tags?.Any() == true)
        {
            whereConditions.Add(e => e.Tags != null && e.Tags.Any(t => input.Tags.Contains(t)));
        }

        // 应用所有条件
        foreach (var condition in whereConditions)
        {
            queryable = queryable.Where(condition);
        }

        // 计算总数
        var totalCount = await AsyncExecuter.LongCountAsync(queryable);

        // 计算匹配度并排序
        var results = await AsyncExecuter.ToListAsync(
            queryable
                .Select(e => new
                {
                    Entity = e,
                    MatchScore = CalculateMatchScore(e, keyword)
                })
                .OrderByDescending(x => x.MatchScore)
                .ThenByDescending(x => GetBusinessPriorityWeight(x.Entity.GraphProperties.GetValueOrDefault("businessPriority")?.ToString()))
                .ThenByDescending(x => x.Entity.LastGraphUpdate ?? DateTime.MinValue)
                .Skip((input.PageIndex - 1) * input.PageSize)
                .Take(input.PageSize)
        );

        // 转换为DTO
        var items = results.Select(x => new SearchResultDto
        {
            EntityId = x.Entity.EntityId, // 使用 EntityId 而不是 Id，对应视图模型的 EntityId 属性
            Type = x.Entity.EntityType,
            Name = x.Entity.GraphProperties.GetValueOrDefault("name")?.ToString() ?? "",
            MatchScore = x.MatchScore,
            BusinessPriority = x.Entity.GraphProperties.GetValueOrDefault("businessPriority")?.ToString(),
            SecurityLevel = x.Entity.GraphProperties.GetValueOrDefault("securityLevel")?.ToString(),
            UpdatedTime = x.Entity.LastGraphUpdate ?? DateTime.MinValue
        }).ToList();

        stopwatch.Stop();

        var result = new PagedResultDto<SearchResultDto>
        {
            TotalCount = totalCount,
            Items = items
        };

        // 记录审计日志
        await _auditService.LogAsync(new AuditLogEntry
        {
            ActionType = AuditActionType.SEARCH,
            ActionCategory = AuditActionCategory.SEARCH,
            ActionDescription = $"搜索节点：关键词={input.Keyword}, 结果数={items.Count}",
            NewValues = new Dictionary<string, object>
            {
                ["keyword"] = input.Keyword,
                ["entityTypes"] = input.EntityTypes ?? new List<string>(),
                ["pageIndex"] = input.PageIndex,
                ["pageSize"] = input.PageSize,
                ["resultCount"] = items.Count,
                ["totalCount"] = totalCount
            },
            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
            Status = AuditStatus.Success
        });

        return result;
    }

    /// <summary>
    /// 计算匹配度分数（使用PostgreSQL全文搜索）
    /// </summary>
    private double CalculateMatchScore(KnowledgeGraphEntityGraphMetadata entity, string keyword)
    {
        var name = entity.GraphProperties.GetValueOrDefault("name")?.ToString() ?? "";
        var tags = string.Join(" ", entity.Tags ?? new List<string>());
        var description = entity.GraphProperties.GetValueOrDefault("description")?.ToString() ?? "";

        // 使用PostgreSQL的ts_rank_cd函数计算匹配度
        // 名称权重3.0，标签权重2.0，描述权重1.0
        var nameScore = CalculateTextMatchScore(name, keyword) * 3.0;
        var tagScore = CalculateTextMatchScore(tags, keyword) * 2.0;
        var descScore = CalculateTextMatchScore(description, keyword) * 1.0;

        return nameScore + tagScore + descScore;
    }

    private double CalculateTextMatchScore(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            return 0.0;

        var textLower = text.ToLowerInvariant();
        var keywordLower = keyword.ToLowerInvariant();

        // 完全匹配
        if (textLower == keywordLower) return 1.0;

        // 包含匹配
        if (textLower.Contains(keywordLower)) return 0.8;

        // 单词匹配
        var words = keywordLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchCount = words.Count(w => textLower.Contains(w));
        return matchCount > 0 ? (double)matchCount / words.Length * 0.6 : 0.0;
    }

    private double GetBusinessPriorityWeight(string? priority)
    {
        return priority switch
        {
            "高" => 3.0,
            "中" => 2.0,
            "低" => 1.0,
            _ => 1.0
        };
    }

    public async Task<ImpactAnalysisResultDto> CalculateImpactAnalysisAsync(ImpactAnalysisInput input)
    {
        return await _impactAnalysisService.CalculateImpact(
            input.EntityId,
            input.ChangeType,
            input.ImpactRadius
        );
    }

    // ========== 关系管理方法实现 ==========

    /// <summary>
    /// 创建关系
    /// </summary>
    public async Task<RelationshipDto> CreateRelationshipAsync(CreateRelationshipInput input)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. 验证源实体和目标实体是否存在
        await ValidateEntitiesExistAsync(input.SourceEntityId, input.SourceEntityType);
        await ValidateEntitiesExistAsync(input.TargetEntityId, input.TargetEntityType);

        // 2. 验证关系类型是否有效
        ValidateRelationshipType(input.RelationshipType, input.SourceEntityType, input.TargetEntityType);

        // 3. 检查关系是否已存在（根据业务规则，某些关系类型不允许重复）
        // 对于抽象关系类型，需要考虑 role 或 semanticType 的组合唯一性
        await CheckRelationshipExistsAsync(input.SourceEntityId, input.TargetEntityId, input.RelationshipType, input.Role, input.SemanticType);

        // 4. 验证权限：检查用户是否有权限创建该关系
        await ValidateRelationshipCreationPermissionAsync(input.SourceEntityId, input.SourceEntityType, input.TargetEntityId, input.TargetEntityType);

        // 5. 验证业务规则（循环关系检查等）
        await ValidateBusinessRulesAsync(input);

        // 6. 创建关系实体（支持抽象关系类型）
        var relationship = new KnowledgeGraphRelationship
        {
            SourceEntityId = input.SourceEntityId,
            SourceEntityType = input.SourceEntityType,
            TargetEntityId = input.TargetEntityId,
            TargetEntityType = input.TargetEntityType,
            Type = input.RelationshipType,
            Role = input.Role, // 角色（用于 PersonRelatesToCatalogue、PersonRelatesToWorkflow 等）
            SemanticType = input.SemanticType, // 语义类型（用于 CatalogueRelatesToCatalogue、WorkflowRelatesToWorkflow 等）
            Description = input.Description,
            Weight = input.Weight ?? 1.0,
            // 使用 ABP 的 ExtraProperties 存储扩展属性
            // 注意：CreationTime 由 ExtensibleFullAuditedEntity 自动设置
        };

        // 设置扩展属性
        if (input.Properties != null)
        {
            foreach (var prop in input.Properties)
            {
                relationship.SetProperty(prop.Key, prop.Value);
            }
        }
        };

        await _relationshipRepository.InsertAsync(relationship);

        // 7. 同步到Neo4j（异步，使用后台作业）
        await _syncService.SyncRelationshipToNeo4jAsync(relationship);

        // 8. 记录审计日志
        stopwatch.Stop();
        await _auditService.LogAsync(new AuditLogEntry
        {
            EntityId = relationship.Id,
            EntityType = "Relationship",
            ActionType = AuditActionType.RELATIONSHIP_CREATE,
            ActionCategory = AuditActionCategory.GRAPH_OPERATION,
            ActionDescription = $"创建关系：{input.SourceEntityType}({input.SourceEntityId}) -[{input.RelationshipType}{(string.IsNullOrEmpty(input.Role) ? "" : $":{input.Role}")}{(string.IsNullOrEmpty(input.SemanticType) ? "" : $":{input.SemanticType}")}]-> {input.TargetEntityType}({input.TargetEntityId})",
            NewValues = new Dictionary<string, object>
            {
                ["relationshipId"] = relationship.Id,
                ["sourceEntityId"] = input.SourceEntityId,
                ["targetEntityId"] = input.TargetEntityId,
                ["relationshipType"] = input.RelationshipType.ToString(),
                ["role"] = input.Role ?? "",
                ["semanticType"] = input.SemanticType ?? ""
            },
            ExecutionTimeMs = (int)stopwatch.ElapsedMilliseconds,
            Status = AuditStatus.Success
        });

        // 9. 返回DTO
        return await MapToRelationshipDtoAsync(relationship);
    }

    /// <summary>
    /// 批量创建关系
    /// </summary>
    public async Task<BatchCreateRelationshipResultDto> BatchCreateRelationshipsAsync(BatchCreateRelationshipInput input)
    {
        var results = new List<BatchCreateRelationshipItemResult>();
        var successCount = 0;
        var failedCount = 0;

        foreach (var relInput in input.Relationships)
        {
            var itemResult = new BatchCreateRelationshipItemResult
            {
                Index = results.Count
            };

            try
            {
                // 检查是否跳过重复关系
                if (input.SkipDuplicates)
                {
                    var exists = await CheckRelationshipExistsAsync(
                        relInput.SourceEntityId,
                        relInput.TargetEntityId,
                        relInput.RelationshipType);
                    if (exists)
                    {
                        itemResult.Success = false;
                        itemResult.Error = "关系已存在";
                        itemResult.Skipped = true;
                        results.Add(itemResult);
                        continue;
                    }
                }

                // 验证（如果未跳过验证）
                if (!input.SkipValidation)
                {
                    await ValidateEntitiesExistAsync(relInput.SourceEntityId, relInput.SourceEntityType);
                    await ValidateEntitiesExistAsync(relInput.TargetEntityId, relInput.TargetEntityType);
                    ValidateRelationshipType(relInput.RelationshipType, relInput.SourceEntityType, relInput.TargetEntityType);
                }

                // 创建关系
                var relationship = new KnowledgeGraphRelationship
                {
                    SourceEntityId = relInput.SourceEntityId,
                    SourceEntityType = relInput.SourceEntityType,
                    TargetEntityId = relInput.TargetEntityId,
                    TargetEntityType = relInput.TargetEntityType,
                    Type = relInput.RelationshipType,
                    Description = relInput.Description,
                    Weight = relInput.Weight ?? 1.0,
                    Properties = relInput.Properties ?? new Dictionary<string, object>(),
                    CreatedTime = DateTime.UtcNow
                };

                await _relationshipRepository.InsertAsync(relationship);
                await _syncService.SyncRelationshipToNeo4jAsync(relationship);

                itemResult.Success = true;
                itemResult.RelationshipId = relationship.Id;
                successCount++;
            }
            catch (Exception ex)
            {
                itemResult.Success = false;
                itemResult.Error = ex.Message;
                failedCount++;
            }

            results.Add(itemResult);
        }

        return new BatchCreateRelationshipResultDto
        {
            TotalCount = input.Relationships.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            Results = results
        };
    }

    /// <summary>
    /// 更新关系
    /// </summary>
    public async Task<RelationshipDto> UpdateRelationshipAsync(Guid relationshipId, UpdateRelationshipInput input)
    {
        var relationship = await _relationshipRepository.GetAsync(relationshipId);

        // 验证权限
        await ValidateRelationshipUpdatePermissionAsync(relationship);

        // 更新字段（支持抽象关系类型）
        if (input.Description != null)
            relationship.Description = input.Description;
        if (input.Weight.HasValue)
            relationship.Weight = input.Weight.Value;
        if (input.Role != null)
            relationship.Role = input.Role;
        if (input.SemanticType != null)
            relationship.SemanticType = input.SemanticType;
        // 更新扩展属性
        if (input.Properties != null)
        {
            foreach (var prop in input.Properties)
            {
                relationship.SetProperty(prop.Key, prop.Value);
            }
        }
        // 注意：LastModificationTime 由 ExtensibleFullAuditedEntity 自动更新

        await _relationshipRepository.UpdateAsync(relationship);

        // 同步到Neo4j
        await _syncService.SyncRelationshipToNeo4jAsync(relationship);

        // 记录审计日志
        await _auditService.LogAsync(new AuditLogEntry
        {
            EntityId = relationshipId,
            EntityType = "Relationship",
            ActionType = AuditActionType.RELATIONSHIP_UPDATE,
            ActionCategory = AuditActionCategory.GRAPH_OPERATION,
            ActionDescription = $"更新关系：{relationship.Type}",
            OldValues = new Dictionary<string, object>
            {
                ["description"] = input.Description ?? relationship.Description,
                ["weight"] = input.Weight ?? relationship.Weight
            },
            NewValues = new Dictionary<string, object>
            {
                ["description"] = relationship.Description,
                ["weight"] = relationship.Weight
            },
            Status = AuditStatus.Success
        });

        return await MapToRelationshipDtoAsync(relationship);
    }

    /// <summary>
    /// 删除关系
    /// </summary>
    public async Task DeleteRelationshipAsync(Guid relationshipId)
    {
        var relationship = await _relationshipRepository.GetAsync(relationshipId);

        // 验证权限
        await ValidateRelationshipDeletePermissionAsync(relationship);

        // 删除关系
        await _relationshipRepository.DeleteAsync(relationship);

        // 从Neo4j删除
        await _syncService.DeleteRelationshipFromNeo4jAsync(relationshipId);

        // 记录审计日志
        await _auditService.LogAsync(new AuditLogEntry
        {
            EntityId = relationshipId,
            EntityType = "Relationship",
            ActionType = AuditActionType.RELATIONSHIP_DELETE,
            ActionCategory = AuditActionCategory.GRAPH_OPERATION,
            ActionDescription = $"删除关系：{relationship.Type}",
            OldValues = new Dictionary<string, object>
            {
                ["sourceEntityId"] = relationship.SourceEntityId,
                ["targetEntityId"] = relationship.TargetEntityId,
                ["relationshipType"] = relationship.Type.ToString()
            },
            Status = AuditStatus.Success
        });
    }

    /// <summary>
    /// 获取关系列表
    /// </summary>
    public async Task<PagedResultDto<RelationshipDto>> GetRelationshipsAsync(RelationshipQueryInput input)
    {
        var queryable = await _relationshipRepository.GetQueryableAsync();

        // 应用过滤条件
        if (input.SourceEntityId.HasValue)
            queryable = queryable.Where(r => r.SourceEntityId == input.SourceEntityId.Value);
        if (input.TargetEntityId.HasValue)
            queryable = queryable.Where(r => r.TargetEntityId == input.TargetEntityId.Value);
        if (input.RelationshipType.HasValue)
            queryable = queryable.Where(r => r.Type == input.RelationshipType.Value);
        if (input.EntityType != null)
            queryable = queryable.Where(r => r.SourceEntityType == input.EntityType || r.TargetEntityType == input.EntityType);

        var totalCount = await AsyncExecuter.LongCountAsync(queryable);

        var relationships = await AsyncExecuter.ToListAsync(
            queryable
                .OrderByDescending(r => r.CreatedTime)
                .Skip((input.PageIndex - 1) * input.PageSize)
                .Take(input.PageSize)
        );

        var items = new List<RelationshipDto>();
        foreach (var rel in relationships)
        {
            items.Add(await MapToRelationshipDtoAsync(rel));
        }

        return new PagedResultDto<RelationshipDto>
        {
            TotalCount = totalCount,
            Items = items
        };
    }

    /// <summary>
    /// 获取实体的关系网络
    /// </summary>
    public async Task<EntityRelationshipNetworkDto> GetEntityRelationshipsAsync(Guid entityId, RelationshipNetworkQueryInput input)
    {
        var queryable = await _relationshipRepository.GetQueryableAsync();

        var outgoingQuery = queryable.Where(r => r.SourceEntityId == entityId);
        var incomingQuery = queryable.Where(r => r.TargetEntityId == entityId);

        // 关系类型过滤
        if (input.RelationshipTypes?.Any() == true)
        {
            outgoingQuery = outgoingQuery.Where(r => input.RelationshipTypes.Contains(r.Type));
            incomingQuery = incomingQuery.Where(r => input.RelationshipTypes.Contains(r.Type));
        }

        var outgoingRelationships = await AsyncExecuter.ToListAsync(outgoingQuery);
        var incomingRelationships = await AsyncExecuter.ToListAsync(incomingQuery);

        var outgoingDtos = new List<RelationshipDto>();
        var incomingDtos = new List<RelationshipDto>();

        foreach (var rel in outgoingRelationships)
        {
            outgoingDtos.Add(await MapToRelationshipDtoAsync(rel));
        }

        foreach (var rel in incomingRelationships)
        {
            incomingDtos.Add(await MapToRelationshipDtoAsync(rel));
        }

        // 计算统计信息
        var statistics = new RelationshipNetworkStatisticsDto
        {
            TotalOutgoing = outgoingDtos.Count,
            TotalIncoming = incomingDtos.Count,
            RelationshipTypeCounts = outgoingDtos
                .Concat(incomingDtos)
                .GroupBy(r => r.RelationshipType)
                .ToDictionary(g => g.Key.ToString(), g => g.Count())
        };

        return new EntityRelationshipNetworkDto
        {
            EntityId = entityId,
            EntityType = (await GetEntityTypeAsync(entityId)) ?? "Unknown",
            OutgoingRelationships = outgoingDtos,
            IncomingRelationships = incomingDtos,
            Statistics = statistics
        };
    }

    #region 关系管理私有辅助方法

    /// <summary>
    /// 验证实体是否存在
    /// </summary>
    private async Task ValidateEntitiesExistAsync(Guid entityId, string entityType)
    {
        var exists = entityType switch
        {
            "Catalogue" => await _catalogueRepository.AnyAsync(e => e.Id == entityId),
            "File" => await _fileRepository.AnyAsync(e => e.Id == entityId),
            "Template" => await _templateRepository.AnyAsync(e => e.Id == entityId),
            _ => false
        };

        if (!exists)
            throw new UserFriendlyException($"实体不存在：{entityType}({entityId})");
    }

    /// <summary>
    /// 验证关系类型是否有效
    /// </summary>
    private void ValidateRelationshipType(RelationshipType relationshipType, string sourceEntityType, string targetEntityType)
    {
        // 定义有效的关系类型组合
        var validCombinations = new Dictionary<RelationshipType, (string source, string target)[]>
        {
            { RelationshipType.CatalogueHasChild, new[] { ("Catalogue", "Catalogue") } },
            { RelationshipType.CatalogueReferencesBusiness, new[] { ("Catalogue", "BusinessEntity") } },
            { RelationshipType.CatalogueRelatedByTime, new[] { ("Catalogue", "Catalogue") } },
            { RelationshipType.CatalogueRelatedByBusiness, new[] { ("Catalogue", "Catalogue") } },
            { RelationshipType.CatalogueReplaces, new[] { ("Catalogue", "Catalogue") } },
            { RelationshipType.CatalogueHasVersion, new[] { ("Catalogue", "Catalogue") } },
            { RelationshipType.PersonCreatesCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonManagesCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonIsProjectManagerForCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonReviewsCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonIsExpertForCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonIsResponsibleForCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonIsContactForCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonParticipatesInCatalogue, new[] { ("Person", "Catalogue") } },
            { RelationshipType.PersonBelongsToDepartment, new[] { ("Person", "Department") } },
            { RelationshipType.DepartmentOwnsCatalogue, new[] { ("Department", "Catalogue") } },
            { RelationshipType.DepartmentManagesCatalogue, new[] { ("Department", "Catalogue") } },
            { RelationshipType.DepartmentHasParent, new[] { ("Department", "Department") } },
            { RelationshipType.BusinessEntityHasCatalogue, new[] { ("BusinessEntity", "Catalogue") } },
            { RelationshipType.BusinessEntityManagesCatalogue, new[] { ("BusinessEntity", "Catalogue") } },
            { RelationshipType.CatalogueUsesWorkflow, new[] { ("Catalogue", "Workflow") } },
            { RelationshipType.WorkflowManagesCatalogue, new[] { ("Workflow", "Catalogue") } },
            { RelationshipType.WorkflowInstanceBelongsToCatalogue, new[] { ("Workflow", "Catalogue") } },
            { RelationshipType.PersonManagesWorkflow, new[] { ("Person", "Workflow") } },
            { RelationshipType.PersonExecutesWorkflow, new[] { ("Person", "Workflow") } },
            { RelationshipType.DepartmentOwnsWorkflow, new[] { ("Department", "Workflow") } },
            { RelationshipType.WorkflowHasVersion, new[] { ("Workflow", "Workflow") } },
            { RelationshipType.WorkflowReplaces, new[] { ("Workflow", "Workflow") } }
        };

        if (!validCombinations.ContainsKey(relationshipType))
            throw new UserFriendlyException($"无效的关系类型：{relationshipType}");

        var validCombos = validCombinations[relationshipType];
        var isValid = validCombos.Any(c => c.source == sourceEntityType && c.target == targetEntityType);

        if (!isValid)
            throw new UserFriendlyException(
                $"关系类型 {relationshipType} 不支持 {sourceEntityType} -> {targetEntityType} 的组合");
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
        var queryable = await _relationshipRepository.GetQueryableAsync();
        queryable = queryable.Where(r =>
            r.SourceEntityId == sourceId &&
            r.TargetEntityId == targetId &&
            r.Type == relationshipType);

        // 对于抽象关系类型，需要检查 role 或 semanticType
        if (relationshipType == RelationshipType.PersonRelatesToCatalogue ||
            relationshipType == RelationshipType.PersonRelatesToWorkflow)
        {
            if (!string.IsNullOrEmpty(role))
            {
                queryable = queryable.Where(r => r.Role == role);
            }
        }

        if (relationshipType == RelationshipType.CatalogueRelatesToCatalogue ||
            relationshipType == RelationshipType.WorkflowRelatesToWorkflow)
        {
            if (!string.IsNullOrEmpty(semanticType))
            {
                queryable = queryable.Where(r => r.SemanticType == semanticType);
            }
        }

        return await AsyncExecuter.AnyAsync(queryable);
    }

    /// <summary>
    /// 验证关系创建权限
    /// </summary>
    private async Task ValidateRelationshipCreationPermissionAsync(
        Guid sourceEntityId, string sourceEntityType,
        Guid targetEntityId, string targetEntityType)
    {
        var userId = CurrentUser.Id;
        var userRoles = await GetUserRolesAsync(userId);

        // 检查源实体的写权限
        if (sourceEntityType == "Catalogue")
        {
            var catalogue = await _catalogueRepository.GetAsync(sourceEntityId);
            if (!catalogue.HasPermission(userId, PermissionAction.Write, userRoles))
                throw new UserFriendlyException("没有权限创建关系：源实体权限不足");
        }

        // 检查目标实体的读权限（至少需要读权限才能关联）
        if (targetEntityType == "Catalogue")
        {
            var catalogue = await _catalogueRepository.GetAsync(targetEntityId);
            if (!catalogue.HasPermission(userId, PermissionAction.Read, userRoles))
                throw new UserFriendlyException("没有权限创建关系：目标实体权限不足");
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
             input.SemanticType == CatalogueSemanticType.DependsOn.ToString()))
        {
            // 检查是否会导致循环：目标分类是否是源分类的祖先
            var hasCycle = await CheckCycleAsync(input.TargetEntityId, input.SourceEntityId, "Catalogue");
            if (hasCycle)
                throw new UserFriendlyException("不能创建循环关系：目标分类是源分类的祖先");
        }

        // 其他业务规则验证...
    }

    /// <summary>
    /// 检查是否存在循环关系
    /// </summary>
    private async Task<bool> CheckCycleAsync(Guid ancestorId, Guid descendantId, string entityType)
    {
        // 使用Neo4j查询检查是否存在从descendantId到ancestorId的路径
        var session = _neo4jDriver.AsyncSession();
        try
        {
            var query = @"
                MATCH path = (ancestor:Entity {id: $ancestorId})-[*]->(descendant:Entity {id: $descendantId})
                WHERE ancestor.id = $ancestorId AND descendant.id = $descendantId
                RETURN count(path) as pathCount";

            var result = await session.RunAsync(query, new
            {
                ancestorId = ancestorId.ToString(),
                descendantId = descendantId.ToString()
            });

            var record = await result.SingleAsync();
            var pathCount = record["pathCount"].As<long>();
            return pathCount > 0;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// 验证关系更新权限
    /// </summary>
    private async Task ValidateRelationshipUpdatePermissionAsync(KnowledgeGraphRelationship relationship)
    {
        var userId = CurrentUser.Id;
        var userRoles = await GetUserRolesAsync(userId);

        if (relationship.SourceEntityType == "Catalogue")
        {
            var catalogue = await _catalogueRepository.GetAsync(relationship.SourceEntityId);
            if (!catalogue.HasPermission(userId, PermissionAction.Write, userRoles))
                throw new UserFriendlyException("没有权限更新关系");
        }
    }

    /// <summary>
    /// 验证关系删除权限
    /// </summary>
    private async Task ValidateRelationshipDeletePermissionAsync(KnowledgeGraphRelationship relationship)
    {
        await ValidateRelationshipUpdatePermissionAsync(relationship); // 删除权限与更新权限相同
    }

    /// <summary>
    /// 映射关系实体到DTO
    /// </summary>
    private async Task<RelationshipDto> MapToRelationshipDtoAsync(KnowledgeGraphRelationship relationship)
    {
        var sourceEntity = await GetEntityBasicInfoAsync(relationship.SourceEntityId, relationship.SourceEntityType);
        var targetEntity = await GetEntityBasicInfoAsync(relationship.TargetEntityId, relationship.TargetEntityType);

        return new RelationshipDto
        {
            Id = relationship.Id,
            SourceEntity = sourceEntity,
            TargetEntity = targetEntity,
            RelationshipType = relationship.Type,
            Description = relationship.Description,
            Weight = relationship.Weight,
            // 从 ExtraProperties 获取扩展属性
            Properties = relationship.ExtraProperties.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            CreationTime = relationship.CreationTime,
            LastModificationTime = relationship.LastModificationTime
        };
    }

    /// <summary>
    /// 获取实体基本信息
    /// </summary>
    private async Task<EntityBasicInfoDto> GetEntityBasicInfoAsync(Guid entityId, string entityType)
    {
        return entityType switch
        {
            "Catalogue" => await GetCatalogueBasicInfoAsync(entityId),
            "Person" => await GetPersonBasicInfoAsync(entityId),
            "Department" => await GetDepartmentBasicInfoAsync(entityId),
            "BusinessEntity" => await GetBusinessEntityBasicInfoAsync(entityId),
            "Workflow" => await GetWorkflowBasicInfoAsync(entityId),
            _ => new EntityBasicInfoDto { Id = entityId, Type = entityType }
        };
    }

    private async Task<EntityBasicInfoDto> GetCatalogueBasicInfoAsync(Guid catalogueId)
    {
        var catalogue = await _catalogueRepository.GetAsync(catalogueId);
        return new EntityBasicInfoDto
        {
            Id = catalogue.Id,
            Name = catalogue.CatalogueName,
            Type = "Catalogue"
        };
    }

    private async Task<EntityBasicInfoDto> GetPersonBasicInfoAsync(Guid personId)
    {
        // 从用户系统获取人员信息
        // var person = await _userRepository.GetAsync(personId);
        return new EntityBasicInfoDto
        {
            Id = personId,
            Name = "", // person.Name,
            Type = "Person"
        };
    }

    private async Task<EntityBasicInfoDto> GetDepartmentBasicInfoAsync(Guid departmentId)
    {
        // 从组织系统获取部门信息
        // var department = await _departmentRepository.GetAsync(departmentId);
        return new EntityBasicInfoDto
        {
            Id = departmentId,
            Name = "", // department.DepartmentName,
            Type = "Department"
        };
    }

    private async Task<EntityBasicInfoDto> GetBusinessEntityBasicInfoAsync(Guid businessEntityId)
    {
        // 从外部业务系统获取业务实体信息
        return new EntityBasicInfoDto
        {
            Id = businessEntityId,
            Name = "",
            Type = "BusinessEntity"
        };
    }

    private async Task<EntityBasicInfoDto> GetWorkflowBasicInfoAsync(Guid workflowId)
    {
        // 从工作流引擎获取工作流信息
        // var workflow = await _workflowRepository.GetAsync(workflowId);
        return new EntityBasicInfoDto
        {
            Id = workflowId,
            Name = "", // workflow.WorkflowName,
            Type = "Workflow"
        };
    }

    private async Task<string?> GetEntityTypeAsync(Guid entityId)
    {
        if (await _catalogueRepository.AnyAsync(e => e.Id == entityId))
            return "Catalogue";
        // 其他实体类型需要通过对应的系统查询
        return null;
    }

    #endregion
}
```

### 7.2 关系创建设计要点

#### 7.2.1 关系创建流程

```
用户请求创建关系
    ↓
1. 参数验证（实体ID、关系类型等）
    ↓
2. 实体存在性验证（源实体、目标实体）
    ↓
3. 关系类型有效性验证（源/目标实体类型组合）
    ↓
4. 权限验证（源实体写权限、目标实体读权限）
    ↓
5. 业务规则验证（重复关系检查、循环关系检查）
    ↓
6. 创建关系实体（PostgreSQL）
    ↓
7. 同步到Neo4j（异步后台作业）
    ↓
8. 记录审计日志
    ↓
返回关系DTO
```

#### 7.2.2 验证机制

**1. 实体存在性验证**：

-   验证源实体和目标实体在数据库中是否存在
-   根据实体类型查询对应的实体表（`APPATTACH_CATALOGUES`等）

**2. 关系类型有效性验证**：

-   定义有效的关系类型组合规则
-   例如：`CatalogueHasChild` 只能用于 `Catalogue -> Catalogue`，`PersonRelatesToCatalogue` 只能用于 `Person -> Catalogue`
-   对于抽象关系类型（`PersonRelatesToCatalogue`、`CatalogueRelatesToCatalogue` 等），需要验证 `role` 或 `semanticType` 的有效性
-   使用字典存储有效组合，快速验证

**3. 重复关系检查**：

-   检查 `(source_entity_id, target_entity_id, relationship_type, role, semantic_type)` 的唯一性
-   对于抽象关系类型，需要考虑 `role` 或 `semanticType` 的组合唯一性
-   例如：同一人员可以同时是分类的"项目经理"和"审核人"（不同的 role），但同一 role 不允许重复
-   某些关系类型不允许重复（如"分类包含文件"）
-   某些关系类型允许重复但需要唯一性约束（如"文件下载记录"）

**4. 循环关系检查**：

-   对于树形关系（如`CatalogueHasChild`），检查是否会导致循环
-   使用 Neo4j 路径查询检查是否存在从目标实体到源实体的路径
-   防止创建导致循环的关系

**5. 权限验证**：

-   **源实体权限**：创建关系需要源实体的 `Write` 权限
-   **目标实体权限**：至少需要目标实体的 `Read` 权限
-   使用 `AttachCatalogue.HasPermission()` 方法进行权限检查

#### 7.2.3 数据一致性保证

**1. 事务处理**：

-   关系创建在数据库事务中执行
-   确保 PostgreSQL 和 Neo4j 的数据一致性

**2. 异步同步**：

-   Neo4j 同步使用 ABP 后台作业异步处理
-   如果 Neo4j 同步失败，后台作业会自动重试
-   不影响主业务流程的响应速度

**3. 补偿机制**：

-   如果 Neo4j 同步失败，记录失败日志
-   提供手动重试机制
-   定期检查并修复数据不一致问题

#### 7.2.4 自动关系创建

某些关系可以根据业务规则自动创建：

**1. 分类创建时自动创建关系**：

-   当分类有父分类时，自动创建 `CatalogueHasChild` 关系
-   当分类引用业务实体时，自动创建 `CatalogueReferencesBusiness` 关系
-   当分类由人员创建时，自动创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Creator`

**2. 分类关系自动创建（抽象化设计）**：

-   当分类创建时，如果存在同一业务实体的其他分类，可以自动创建 `CatalogueRelatesToCatalogue` 关系，`semanticType` 设置为 `Temporal`（基于创建时间）
-   当分类更新时，如果存在业务相关的其他分类，可以自动创建 `CatalogueRelatesToCatalogue` 关系，`semanticType` 设置为 `Business`
-   当分类有版本时，自动创建 `CatalogueRelatesToCatalogue` 关系，`semanticType` 设置为 `Version`
-   当分类替换其他分类时，自动创建 `CatalogueRelatesToCatalogue` 关系，`semanticType` 设置为 `Replaces`

**3. 人员与部门关系自动创建**：

-   当人员创建分类时，如果人员有部门信息，自动创建 `PersonBelongsToDepartment` 关系（如果尚未存在）
-   当分类创建时，如果分类关联部门，自动创建 `DepartmentOwnsCatalogue` 关系

**4. 人员角色关系自动创建（从文件内容和元数据提取）**：

-   **从文件内容提取**：通过 OCR 和实体识别，从分类包含的文件内容中提取人员角色信息
    -   提取项目经理：识别"项目经理"、"项目负责人"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `ProjectManager`
    -   提取审核人：识别"审核人"、"审批人"、"审核"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Reviewer`
    -   提取专家：识别"专家"、"顾问"、"咨询"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Expert`
    -   提取责任人：识别"责任人"、"负责人"、"经办人"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Responsible`
    -   提取联系人：识别"联系人"、"联系方式"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Contact`
    -   提取参与人：识别"参与人"、"参与者"、"成员"等关键词，创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Participant`
-   **从元数据字段提取**：从分类的 `MetaFields` 中提取人员角色信息
    -   如果元数据字段包含"项目经理"、"审核人"、"专家"等角色字段，自动创建 `PersonRelatesToCatalogue` 关系，`role` 设置为对应的角色值
-   **从工作流提取**：从工作流审批记录中提取审核人信息
    -   当工作流审批完成时，自动创建 `PersonRelatesToCatalogue` 关系，`role` 设置为 `Reviewer`

```csharp
// 在CatalogueCreatedHandler中自动创建关系
public class CatalogueCreatedHandler : ILocalEventHandler<EntityCreatedEventData<AttachCatalogue>>, ITransientDependency
{
    private readonly IKnowledgeGraphService _knowledgeGraphService;

    public async Task HandleEventAsync(EntityCreatedEventData<AttachCatalogue> eventData)
    {
        var catalogue = eventData.Entity;

        // 自动创建父子关系
        if (catalogue.ParentId.HasValue)
        {
            await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
            {
                SourceEntityId = catalogue.ParentId.Value,
                SourceEntityType = "Catalogue",
                TargetEntityId = catalogue.Id,
                TargetEntityType = "Catalogue",
                RelationshipType = RelationshipType.CatalogueHasChild,
                Description = "分类创建自动创建",
                Properties = new Dictionary<string, object>
                {
                    ["autoCreated"] = true,
                    ["createdReason"] = "分类创建"
                }
            });
        }

        // 自动创建人员创建关系
        if (catalogue.CreatorId.HasValue)
        {
            await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
            {
                SourceEntityId = catalogue.CreatorId.Value,
                SourceEntityType = "Person",
                TargetEntityId = catalogue.Id,
                TargetEntityType = "Catalogue",
                RelationshipType = RelationshipType.PersonRelatesToCatalogue,
                Role = PersonRole.Creator.ToString(), // 设置角色为 Creator
                Description = "分类创建自动创建",
                Properties = new Dictionary<string, object>
                {
                    ["autoCreated"] = true,
                    ["createdReason"] = "分类创建"
                }
            });
        }

        // 从元数据字段提取人员角色关系
        await ExtractPersonRoleRelationshipsFromMetaFieldsAsync(catalogue);
    }

    /// <summary>
    /// 从元数据字段提取人员角色关系
    /// </summary>
    private async Task ExtractPersonRoleRelationshipsFromMetaFieldsAsync(AttachCatalogue catalogue)
    {
        if (catalogue.MetaFields == null || catalogue.MetaFields.Count == 0)
            return;

        // 定义角色字段映射（使用抽象关系类型和角色枚举）
        var roleFieldMappings = new Dictionary<string, PersonRole>
        {
            ["项目经理"] = PersonRole.ProjectManager,
            ["项目负责人"] = PersonRole.ProjectManager,
            ["审核人"] = PersonRole.Reviewer,
            ["审批人"] = PersonRole.Reviewer,
            ["专家"] = PersonRole.Expert,
            ["顾问"] = PersonRole.Expert,
            ["责任人"] = PersonRole.Responsible,
            ["负责人"] = PersonRole.Responsible,
            ["经办人"] = PersonRole.Responsible,
            ["联系人"] = PersonRole.Contact,
            ["参与人"] = PersonRole.Participant,
            ["参与者"] = PersonRole.Participant
        };

        foreach (var metaField in catalogue.MetaFields)
        {
            if (!metaField.IsEnabled || string.IsNullOrEmpty(metaField.FieldName))
                continue;

            // 检查字段名是否匹配角色类型
            var fieldName = metaField.FieldName.Trim();
            if (roleFieldMappings.TryGetValue(fieldName, out var role))
            {
                // 从字段值中提取人员ID（假设字段值存储的是人员ID或人员姓名）
                var personId = await ExtractPersonIdFromMetaFieldValueAsync(metaField.FieldValue);
                if (personId.HasValue)
                {
                    await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
                    {
                        SourceEntityId = personId.Value,
                        SourceEntityType = "Person",
                        TargetEntityId = catalogue.Id,
                        TargetEntityType = "Catalogue",
                        RelationshipType = RelationshipType.PersonRelatesToCatalogue,
                        Role = role.ToString(), // 使用抽象关系类型，通过 role 属性区分具体角色
                        Description = $"从元数据字段'{fieldName}'自动提取",
                        Properties = new Dictionary<string, object>
                        {
                            ["autoCreated"] = true,
                            ["createdReason"] = "元数据字段提取",
                            ["sourceField"] = fieldName,
                            ["sourceFieldValue"] = metaField.FieldValue
                        }
                    });
                }
            }
        }
    }

    /// <summary>
    /// 从元数据字段值中提取人员ID
    /// </summary>
    private async Task<Guid?> ExtractPersonIdFromMetaFieldValueAsync(string? fieldValue)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
            return null;

        // 如果字段值是GUID，直接返回
        if (Guid.TryParse(fieldValue, out var personId))
            return personId;

        // 如果字段值是人员姓名，需要通过用户系统查询人员ID
        // var person = await _userRepository.FindByNameAsync(fieldValue);
        // return person?.Id;

        return null; // 简化示例，实际实现需要查询用户系统
    }
}

// 文件内容分析后自动创建人员角色关系
public class FileContentAnalyzedHandler : ILocalEventHandler<FileContentAnalyzedEventData>, ITransientDependency
{
    private readonly IKnowledgeGraphService _knowledgeGraphService;
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;

    public async Task HandleEventAsync(FileContentAnalyzedEventData eventData)
    {
        var file = eventData.File;
        if (!file.AttachCatalogueId.HasValue)
            return;

        var catalogue = await _catalogueRepository.GetAsync(file.AttachCatalogueId.Value);

        // 从OCR内容中提取人员角色信息
        if (!string.IsNullOrWhiteSpace(file.OcrContent))
        {
            await ExtractPersonRoleRelationshipsFromContentAsync(catalogue, file.OcrContent);
        }
    }

    /// <summary>
    /// 从文件内容中提取人员角色关系
    /// </summary>
    private async Task ExtractPersonRoleRelationshipsFromContentAsync(AttachCatalogue catalogue, string content)
    {
        // 使用实体识别服务提取人员信息
        // var entityRecognitionService = AIServiceFactory.GetEntityRecognitionService();
        // var recognitionResult = await entityRecognitionService.RecognizeEntitiesAsync(new EntityRecognitionInputDto
        // {
        //     Text = content,
        //     EntityTypes = ["Person", "Role"]
        // });

        // 定义角色关键词模式（使用抽象关系类型和角色枚举）
        var rolePatterns = new Dictionary<string, PersonRole>
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
            [@"参与人[：:]\s*(\S+)"] = PersonRole.Participant
        };

        foreach (var pattern in rolePatterns)
        {
            var matches = Regex.Matches(content, pattern.Key, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var personName = match.Groups[1].Value.Trim();
                    // 通过人员姓名查询人员ID
                    var personId = await FindPersonIdByNameAsync(personName);
                    if (personId.HasValue)
                    {
                        await _knowledgeGraphService.CreateRelationshipAsync(new CreateRelationshipInput
                        {
                            SourceEntityId = personId.Value,
                            SourceEntityType = "Person",
                            TargetEntityId = catalogue.Id,
                            TargetEntityType = "Catalogue",
                            RelationshipType = RelationshipType.PersonRelatesToCatalogue,
                            Role = pattern.Value.ToString(), // 使用抽象关系类型，通过 role 属性区分具体角色
                            Description = $"从文件内容自动提取：{personName}",
                            Properties = new Dictionary<string, object>
                            {
                                ["autoCreated"] = true,
                                ["createdReason"] = "文件内容提取",
                                ["extractedText"] = match.Value,
                                ["personName"] = personName
                            }
                        });
                    }
                }
            }
        }
    }

    private async Task<Guid?> FindPersonIdByNameAsync(string personName)
    {
        // 通过用户系统查询人员ID
        // var person = await _userRepository.FindByNameAsync(personName);
        // return person?.Id;
        return null; // 简化示例，实际实现需要查询用户系统
    }
}
```

### 7.3 数据同步方案

#### 7.2.1 数据同步流程

```
PostgreSQL (业务数据)
    ↓ (ABP Entity Change Events)
领域事件处理器
    ↓ (ABP Background Jobs)
数据同步服务
    ↓
Neo4j (图数据库)
PostgreSQL (全文搜索索引更新)
```

#### 7.2.2 领域事件处理器（基于 ABP 实体变更事件）

```csharp
// CatalogueGraphSyncEventHandler.cs
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;

public class CatalogueGraphSyncEventHandler :
    ILocalEventHandler<EntityCreatedEventData<AttachCatalogue>>,
    ILocalEventHandler<EntityUpdatedEventData<AttachCatalogue>>,
    ILocalEventHandler<EntityDeletedEventData<AttachCatalogue>>,
    ITransientDependency
{
    private readonly IKnowledgeGraphSyncService _syncService;

    public CatalogueGraphSyncEventHandler(IKnowledgeGraphSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task HandleEventAsync(EntityCreatedEventData<AttachCatalogue> eventData)
    {
        await _syncService.SyncEntityAsync(
            eventData.Entity.Id,
            "Catalogue",
            "CREATE",
            eventData.Entity
        );
    }

    public async Task HandleEventAsync(EntityUpdatedEventData<AttachCatalogue> eventData)
    {
        await _syncService.SyncEntityAsync(
            eventData.Entity.Id,
            "Catalogue",
            "UPDATE",
            eventData.Entity
        );
    }

    public async Task HandleEventAsync(EntityDeletedEventData<AttachCatalogue> eventData)
    {
        await _syncService.SyncEntityAsync(
            eventData.Entity.Id,
            "Catalogue",
            "DELETE",
            null
        );
    }
}

// 注意：File 和 Template 不再是知识图谱的独立维度
// File 通过 Catalogue 直接访问，Template 信息已体现在 Catalogue 中
// 因此不需要单独的 FileGraphSyncEventHandler 和 TemplateGraphSyncEventHandler
```

#### 7.2.3 知识图谱同步服务

```csharp
// IKnowledgeGraphSyncService.cs
public interface IKnowledgeGraphSyncService
{
    /// <summary>
    /// 同步实体到知识图谱（异步处理，使用后台作业）
    /// </summary>
    Task SyncEntityAsync(Guid entityId, string entityType, string operation, object? entityData = null);
}

// KnowledgeGraphSyncService.cs
public class KnowledgeGraphSyncService : IKnowledgeGraphSyncService, ITransientDependency
{
    private readonly IBackgroundJobManager _backgroundJobManager;
    private readonly ILogger<KnowledgeGraphSyncService> _logger;

    public KnowledgeGraphSyncService(
        IBackgroundJobManager backgroundJobManager,
        ILogger<KnowledgeGraphSyncService> logger)
    {
        _backgroundJobManager = backgroundJobManager;
        _logger = logger;
    }

    /// <summary>
    /// 同步实体到知识图谱（使用ABP后台作业异步处理，避免阻塞主业务流程）
    /// </summary>
    public async Task SyncEntityAsync(Guid entityId, string entityType, string operation, object? entityData = null)
    {
        try
        {
            // 使用ABP后台作业异步处理，避免阻塞主业务流程
            await _backgroundJobManager.EnqueueAsync(
                new KnowledgeGraphSyncJobArgs
                {
                    EntityId = entityId,
                    EntityType = entityType,
                    Operation = operation
                },
                delay: TimeSpan.Zero // 立即执行
            );

            _logger.LogDebug(
                "知识图谱同步任务已加入队列: EntityId={EntityId}, EntityType={EntityType}, Operation={Operation}",
                entityId, entityType, operation
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "知识图谱同步任务加入队列失败: EntityId={EntityId}, EntityType={EntityType}, Operation={Operation}",
                entityId, entityType, operation
            );
            // 不抛出异常，避免影响主业务流程
        }
    }
}

// 后台作业参数
public class KnowledgeGraphSyncJobArgs
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; }
    public string Operation { get; set; }
}

// 后台作业处理器
public class KnowledgeGraphSyncJob : AsyncBackgroundJob<KnowledgeGraphSyncJobArgs>, ITransientDependency
{
    private readonly INeo4jDriver _neo4jDriver;
    private readonly IRepository<KnowledgeGraphEntityGraphMetadata, Guid> _graphMetadataRepository;
    private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;
    private readonly ILogger<KnowledgeGraphSyncJob> _logger;

    public KnowledgeGraphSyncJob(
        INeo4jDriver neo4jDriver,
        IRepository<KnowledgeGraphEntityGraphMetadata, Guid> graphMetadataRepository,
        IRepository<AttachCatalogue, Guid> catalogueRepository,
        ILogger<KnowledgeGraphSyncJob> logger)
    {
        _neo4jDriver = neo4jDriver;
        _graphMetadataRepository = graphMetadataRepository;
        _catalogueRepository = catalogueRepository;
        _logger = logger;
    }

    public override async Task ExecuteAsync(KnowledgeGraphSyncJobArgs args)
    {
        try
        {
            var entityProperties = await GetEntityPropertiesAsync(args.EntityId, args.EntityType);

            switch (args.Operation.ToUpper())
            {
                case "CREATE":
                case "UPDATE":
                    await SyncEntityToNeo4j(args.EntityId, args.EntityType, entityProperties);
                    await UpdateFullTextSearchIndex(args.EntityId, args.EntityType, entityProperties);
                    break;
                case "DELETE":
                    await DeleteEntityFromNeo4j(args.EntityId, args.EntityType);
                    await DeleteFullTextSearchIndex(args.EntityId, args.EntityType);
                    break;
            }

            _logger.LogInformation(
                "知识图谱同步成功: EntityId={EntityId}, EntityType={EntityType}, Operation={Operation}",
                args.EntityId, args.EntityType, args.Operation
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "知识图谱同步失败: EntityId={EntityId}, EntityType={EntityType}, Operation={Operation}",
                args.EntityId, args.EntityType, args.Operation
            );
            throw; // 重新抛出异常，让ABP后台作业管理器重试
        }
    }

    /// <summary>
    /// 从现有实体表获取实体属性
    /// </summary>
    private async Task<Dictionary<string, object>> GetEntityPropertiesAsync(Guid entityId, string entityType)
    {
        return entityType switch
        {
            "Catalogue" => await GetCataloguePropertiesAsync(entityId),
            "Person" => await GetPersonPropertiesAsync(entityId),
            "Department" => await GetDepartmentPropertiesAsync(entityId),
            "BusinessEntity" => await GetBusinessEntityPropertiesAsync(entityId),
            "Workflow" => await GetWorkflowPropertiesAsync(entityId),
            _ => new Dictionary<string, object>()
        };
    }

    private async Task<Dictionary<string, object>> GetCataloguePropertiesAsync(Guid catalogueId)
    {
        var catalogue = await _catalogueRepository.GetAsync(catalogueId);
        return new Dictionary<string, object>
        {
            ["name"] = catalogue.CatalogueName,
            ["reference"] = catalogue.Reference,
            ["referenceType"] = catalogue.ReferenceType,
            ["facetType"] = catalogue.CatalogueFacetType,
            ["tags"] = catalogue.Tags ?? new List<string>(),
            ["isArchived"] = catalogue.IsArchived,
            ["attachCount"] = catalogue.AttachCount
        };
    }

    private async Task<Dictionary<string, object>> GetPersonPropertiesAsync(Guid personId)
    {
        // 从用户系统获取人员信息
        // var person = await _userRepository.GetAsync(personId);
        return new Dictionary<string, object>
        {
            ["employeeId"] = "", // person.EmployeeId,
            ["departmentId"] = null, // person.DepartmentId,
            // 其他字段从用户系统获取
        };
    }

    private async Task<Dictionary<string, object>> GetDepartmentPropertiesAsync(Guid departmentId)
    {
        // 从组织系统获取部门信息
        // var department = await _departmentRepository.GetAsync(departmentId);
        return new Dictionary<string, object>
        {
            ["departmentCode"] = "", // department.DepartmentCode,
            ["parentDepartmentId"] = null, // department.ParentDepartmentId,
            // 其他字段从组织系统获取
        };
    }

    private async Task<Dictionary<string, object>> GetBusinessEntityPropertiesAsync(Guid businessEntityId)
    {
        // 从外部业务系统获取业务实体信息
        // 根据ReferenceType查询对应的业务系统
        return new Dictionary<string, object>
        {
            ["referenceId"] = "",
            ["referenceType"] = 0,
            ["businessType"] = "",
            // 其他字段从外部业务系统获取
        };
    }

    private async Task<Dictionary<string, object>> GetWorkflowPropertiesAsync(Guid workflowId)
    {
        // 从工作流引擎获取工作流信息
        // var workflow = await _workflowRepository.GetAsync(workflowId);
        return new Dictionary<string, object>
        {
            ["workflowCode"] = "", // workflow.WorkflowCode,
            ["workflowType"] = "", // workflow.WorkflowType,
            ["status"] = "", // workflow.Status,
            ["templateDefinitionId"] = null, // workflow.TemplateDefinitionId,
            ["templateDefinitionVersion"] = 0, // workflow.TemplateDefinitionVersion,
            ["ownerDepartmentId"] = null, // workflow.OwnerDepartmentId,
            ["managerPersonId"] = null, // workflow.ManagerPersonId,
            // 其他字段从工作流引擎获取
        };
    }

    /// <summary>
    /// 同步实体到Neo4j
    /// </summary>
    private async Task SyncEntityToNeo4j(Guid entityId, string entityType, Dictionary<string, object> entityProperties)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            var query = $@"
                MERGE (e:Entity {{id: $id}})
                SET e.type = $type,
                    e.name = $name,
                    e.tags = $tags,
                    e.properties = $properties,
                    e.updatedTime = $updatedTime
                RETURN e";

            await session.RunAsync(query, new
            {
                id = entityId.ToString(),
                type = entityType,
                name = entityProperties.GetValueOrDefault("name")?.ToString() ?? "",
                tags = entityProperties.GetValueOrDefault("tags") as string[] ?? Array.Empty<string>(),
                properties = entityProperties,
                updatedTime = DateTime.UtcNow
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private async Task UpdateEntityInNeo4j(Guid entityId, string entityType, Dictionary<string, object> entityProperties)
    {
        await SyncEntityToNeo4j(entityId, entityType, entityProperties); // 更新逻辑与创建相同
    }

    private async Task DeleteEntityFromNeo4j(Guid entityId, string entityType)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            var query = "MATCH (e:Entity {id: $id}) DETACH DELETE e";
            await session.RunAsync(query, new { id = entityId.ToString() });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// 更新PostgreSQL全文搜索索引
    /// </summary>
    private async Task UpdateFullTextSearchIndex(Guid entityId, string entityType, Dictionary<string, object> entityProperties)
    {
        // 更新或创建图元数据记录（用于全文搜索）
        var metadata = await _graphMetadataRepository.FindAsync(entityId);
        if (metadata == null)
        {
            metadata = new KnowledgeGraphEntityGraphMetadata
            {
                EntityId = entityId,
                EntityType = entityType,
                GraphProperties = entityProperties,
                Tags = entityProperties.GetValueOrDefault("tags") as List<string> ?? new List<string>(),
                LastGraphUpdate = DateTime.UtcNow
            };
            await _graphMetadataRepository.InsertAsync(metadata);
        }
        else
        {
            metadata.GraphProperties = entityProperties;
            metadata.Tags = entityProperties.GetValueOrDefault("tags") as List<string> ?? new List<string>();
            metadata.LastGraphUpdate = DateTime.UtcNow;
            await _graphMetadataRepository.UpdateAsync(metadata);
        }
    }

    /// <summary>
    /// 删除PostgreSQL全文搜索索引
    /// </summary>
    private async Task DeleteFullTextSearchIndex(Guid entityId, string entityType)
    {
        var metadata = await _graphMetadataRepository.FindAsync(entityId);
        if (metadata != null)
        {
            await _graphMetadataRepository.DeleteAsync(metadata);
        }
    }

    /// <summary>
    /// 同步关系到Neo4j
    /// </summary>
    public async Task SyncRelationshipToNeo4jAsync(KnowledgeGraphRelationship relationship)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            // 获取源节点和目标节点的标签（根据实体类型）
            var sourceLabel = GetEntityLabel(relationship.SourceEntityType);
            var targetLabel = GetEntityLabel(relationship.TargetEntityType);
            var relType = GetNeo4jRelationshipType(relationship.Type);

            var query = $@"
                MATCH (source:{sourceLabel} {{id: $sourceId}})
                MATCH (target:{targetLabel} {{id: $targetId}})
                MERGE (source)-[r:{relType} {{id: $relId}}]->(target)
                SET r.description = $description,
                    r.weight = $weight,
                    r.properties = $properties,
                    r.createdTime = $createdTime,
                    r.updatedTime = $updatedTime
                RETURN r";

            await session.RunAsync(query, new
            {
                sourceId = relationship.SourceEntityId.ToString(),
                targetId = relationship.TargetEntityId.ToString(),
                relId = relationship.Id.ToString(),
                description = relationship.Description,
                weight = relationship.Weight,
                properties = relationship.Properties,
                createdTime = relationship.CreatedTime,
                updatedTime = relationship.UpdatedTime
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// 从Neo4j删除关系
    /// </summary>
    public async Task DeleteRelationshipFromNeo4jAsync(Guid relationshipId)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            var query = "MATCH ()-[r {id: $relId}]-() DELETE r";
            await session.RunAsync(query, new { relId = relationshipId.ToString() });
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    /// <summary>
    /// 获取Neo4j实体标签
    /// </summary>
    private string GetEntityLabel(string entityType)
    {
        return entityType switch
        {
            "Catalogue" => "Catalogue",
            "Person" => "Person",
            "Department" => "Department",
            "BusinessEntity" => "BusinessEntity",
            "Workflow" => "Workflow",
            _ => "Entity"
        };
    }

    /// <summary>
    /// 获取Neo4j关系类型（采用抽象化设计）
    /// </summary>
    private string GetNeo4jRelationshipType(RelationshipType relationshipType, string? role = null, string? semanticType = null)
    {
        return relationshipType switch
        {
            // 抽象关系类型统一使用 RELATES_TO
            RelationshipType.CatalogueRelatesToCatalogue => "RELATES_TO",
            RelationshipType.PersonRelatesToCatalogue => "RELATES_TO",
            RelationshipType.PersonRelatesToWorkflow => "RELATES_TO",
            RelationshipType.WorkflowRelatesToWorkflow => "RELATES_TO",

            // 语义明确的关系类型保留独立类型
            RelationshipType.CatalogueHasChild => "HAS_CHILD",
            RelationshipType.CatalogueReferencesBusiness => "REFERENCES",
            RelationshipType.PersonBelongsToDepartment => "BELONGS_TO",
            RelationshipType.DepartmentOwnsCatalogue => "OWNS",
            RelationshipType.DepartmentManagesCatalogue => "MANAGES",
            RelationshipType.DepartmentHasParent => "HAS_PARENT",
            RelationshipType.BusinessEntityHasCatalogue => "HAS",
            RelationshipType.BusinessEntityManagesCatalogue => "MANAGES",
            RelationshipType.CatalogueUsesWorkflow => "USES",
            RelationshipType.WorkflowManagesCatalogue => "MANAGES",
            RelationshipType.WorkflowInstanceBelongsToCatalogue => "INSTANCE_OF",
            RelationshipType.DepartmentOwnsWorkflow => "OWNS",

            _ => relationshipType.ToString().ToUpper()
        };
    }

    /// <summary>
    /// 同步关系到Neo4j（支持抽象关系类型）
    /// </summary>
    public async Task SyncRelationshipToNeo4jAsync(KnowledgeGraphRelationship relationship)
    {
        var session = _neo4jDriver.AsyncSession();
        try
        {
            // 获取源节点和目标节点的标签（根据实体类型）
            var sourceLabel = GetEntityLabel(relationship.SourceEntityType);
            var targetLabel = GetEntityLabel(relationship.TargetEntityType);
            var relType = GetNeo4jRelationshipType(relationship.Type, relationship.Role, relationship.SemanticType);

            // 构建关系属性
            var relProperties = new Dictionary<string, object>
            {
                ["id"] = relationship.Id.ToString(),
                ["description"] = relationship.Description ?? "",
                ["weight"] = relationship.Weight,
                ["createdTime"] = relationship.CreationTime,
                ["updatedTime"] = relationship.LastModificationTime ?? relationship.CreationTime
            };

            // 添加 role 或 semanticType 属性
            if (!string.IsNullOrEmpty(relationship.Role))
            {
                relProperties["role"] = relationship.Role;
            }

            if (!string.IsNullOrEmpty(relationship.SemanticType))
            {
                relProperties["semanticType"] = relationship.SemanticType;
            }

            // 添加其他扩展属性
            if (relationship.Properties != null)
            {
                foreach (var prop in relationship.Properties)
                {
                    relProperties[prop.Key] = prop.Value;
                }
            }

            var query = $@"
                MATCH (source:{sourceLabel} {{id: $sourceId}})
                MATCH (target:{targetLabel} {{id: $targetId}})
                MERGE (source)-[r:{relType} {{id: $relId}}]->(target)
                SET r += $properties
                RETURN r";

            await session.RunAsync(query, new
            {
                sourceId = relationship.SourceEntityId.ToString(),
                targetId = relationship.TargetEntityId.ToString(),
                relId = relationship.Id.ToString(),
                properties = relProperties
            });
        }
        finally
        {
            await session.CloseAsync();
        }
    }
}
```

---

## 8. 性能优化方案

### 8.1 图查询优化

-   **索引优化**：为常用查询字段创建索引（name, type, tags）
-   **查询深度限制**：默认限制查询深度为 2-3 跳
-   **节点数量限制**：单次查询最多返回 500-1000 个节点
-   **缓存策略**：热点图谱数据缓存 30 分钟

### 8.2 搜索性能优化

-   **PostgreSQL 全文搜索优化**：
    -   使用 GIN 索引提升全文搜索性能
    -   合理使用 `ts_rank_cd` 函数计算匹配度
    -   为常用搜索字段（名称、标签、描述）创建复合全文搜索索引
    -   使用 `plainto_tsquery` 进行中文分词搜索
    -   定期执行 `VACUUM ANALYZE` 优化索引
-   **结果缓存**：相同关键词搜索结果缓存 5 分钟

### 8.3 前端性能优化

-   **虚拟滚动**：搜索结果列表使用虚拟滚动
-   **图谱渲染优化**：
    -   使用 Web Worker 进行布局计算
    -   节点数量超过 1000 时使用简化渲染
    -   支持按需加载（懒加载）
-   **防抖和节流**：搜索输入防抖 300ms，滚动事件节流

---

## 9. 安全方案

### 9.1 数据安全

#### 9.1.1 密级控制

-   **密级过滤**：根据实体密级（内部、秘密、机密等）过滤查询结果
-   **密级继承**：子分类继承父分类的密级设置
-   **密级验证**：在实体详情和关系查询时自动验证用户密级权限

#### 9.1.2 访问控制

知识图谱系统集成 `AttachCatalogue.Permissions` 权限系统，实现细粒度的访问控制：

**权限检查机制**：

-   所有图谱查询和搜索接口自动应用权限过滤
-   利用 `AttachCatalogue.HasPermission()` 方法检查用户权限
-   支持权限继承：子分类继承父分类权限（通过 `HasInheritedPermission()` 方法）
-   权限优先级：`Deny` > `Allow` > `Inherit`

**权限过滤策略**：

-   **图谱查询**：只返回用户有 `Read` 权限的实体节点
-   **搜索接口**：搜索结果自动过滤无权限的实体
-   **关系查询**：只返回用户可访问的关系
-   **时间轴查询**：统计信息只包含用户可访问的实体

**权限类型支持**：

-   **用户权限（User）**：直接授予特定用户的权限
-   **角色权限（Role）**：授予角色的权限，角色成员自动继承
-   **策略权限（Policy）**：基于属性条件的动态权限

**权限缓存**：

-   访问控制结果缓存 5 分钟，提升查询性能
-   权限变更时自动失效相关缓存

#### 9.1.3 数据脱敏

-   **敏感信息脱敏**：根据用户权限级别脱敏显示敏感信息
-   **人员信息保护**：人员联系方式、身份证号等敏感信息根据权限脱敏
-   **文件内容脱敏**：OCR 内容、文件预览等根据权限控制访问

### 9.2 API 安全

-   **认证授权**：使用 JWT Token 认证，集成 ABP 框架的认证系统
-   **权限验证**：所有 API 接口自动验证用户权限，利用 `AttachCatalogue.Permissions` 进行访问控制
-   **接口限流**：防止恶意查询，使用令牌桶算法限制请求频率
-   **参数验证**：严格验证所有输入参数，防止 SQL 注入和 XSS 攻击
-   **审计日志**：记录所有权限相关的操作，支持合规性审计

---

## 10. 部署方案

### 10.1 容器化部署

```yaml
# docker-compose.yml
version: '3.8'
services:
    neo4j:
        image: neo4j:5.15
        ports:
            - '7474:7474'
            - '7687:7687'
        environment:
            - NEO4J_AUTH=neo4j/password
        volumes:
            - neo4j_data:/data

    elasticsearch:
        image: elasticsearch:8.11.0
        ports:
            - '9200:9200'
        environment:
            - discovery.type=single-node
        volumes:
            - es_data:/usr/share/elasticsearch/data

volumes:
    neo4j_data:
    es_data:
```

### 10.2 监控和日志

-   **应用监控**：集成 APM 工具（如 Application Insights）
-   **图数据库监控**：监控 Neo4j 查询性能和资源使用
-   **日志聚合**：使用 ELK Stack 进行日志收集和分析

---

## 11. 实施计划

### 11.1 第一阶段（2 周）

-   数据模型设计和数据库搭建
-   基础 API 接口开发
-   前端图谱可视化组件开发

### 11.2 第二阶段（2 周）

-   搜索功能实现
-   时间轴功能实现
-   节点详情面板开发

### 11.3 第三阶段（2 周）

-   影响分析算法实现
-   风险评估功能开发
-   风险预警功能实现

### 11.4 第四阶段（1 周）

-   数据同步方案实施
-   性能优化
-   测试和 bug 修复

---

## 12. 数据模型优化说明

### 12.1 已采纳的优化建议

基于多维知识图谱最佳实践和档案系统特点，以下优化已整合到设计中：

#### 12.1.1 文件实体增强

-   ✅ **FileSize 类型优化**：从 `int` 改为 `long`，支持大文件（>2GB）
-   ✅ **文件完整性校验**：新增 `Checksum` 字段（MD5/SHA256），用于文件完整性验证
-   ✅ **MIME 类型**：新增 `MimeType` 字段，便于文件类型识别和处理
-   ✅ **最后访问时间**：新增 `LastAccessTime` 字段，支持访问统计和分析

#### 12.1.2 关系模型优化（抽象化设计）

-   ✅ **抽象关系类型**：采用 `PersonRelatesToCatalogue` 和 `CatalogueRelatesToCatalogue` 抽象关系类型，通过 `role` 和 `semanticType` 属性描述具体语义
-   ✅ **角色枚举**：定义 `PersonRole` 枚举（Creator、Manager、ProjectManager、Reviewer、Expert、Responsible、Contact、Participant），支持灵活扩展
-   ✅ **语义类型枚举**：定义 `CatalogueSemanticType` 枚举（Temporal、Business、Version、Replaces、DependsOn、References、SimilarTo），支持灵活扩展
-   ✅ **可扩展性**：新增角色或语义类型无需修改关系类型枚举，只需扩展对应的枚举类型
-   ✅ **数据库支持**：在 `kg_relationships` 表中新增 `role` 和 `semantic_type` 字段，并创建相应索引

#### 12.1.3 状态管理优化

-   ✅ **统一状态字段**：在实体基类中新增 `Status` 字段，统一管理实体状态（ACTIVE, ARCHIVED, DELETED 等）
-   ✅ **状态索引**：为状态字段创建索引，支持按状态快速查询

#### 12.1.4 数据库优化

-   ✅ **索引增强**：
    -   新增文件校验和索引（`file_checksum_index`），支持文件去重和完整性验证
    -   新增状态索引（`entity_status_index`），提升状态查询性能
    -   新增分类状态索引（`catalogue_status_index`）
-   ✅ **风险预警表增强**：
    -   新增 `risk_category` 字段，支持风险分类（数据完整性、安全性、合规性等）
    -   新增 `due_date` 字段，支持处理截止日期管理
-   ✅ **审计轨迹表**：新增 `kg_audit_trail` 表，记录实体变更历史，支持合规性审计

### 12.2 关系型数据库设计优化

#### 12.2.1 设计原则

知识图谱的关系型数据库设计采用**引用模式**，避免数据冗余：

1. **不重复存储实体数据**：

    - 已有实体表：`APPATTACH_CATALOGUES`（分类）
    - 知识图谱表只存储图谱特有的数据（关系、图查询优化、时间轴快照等）

2. **通过实体 ID 和类型关联**：

    - `kg_relationships` 表通过 `source_entity_id` + `source_entity_type` 和 `target_entity_id` + `target_entity_type` 关联到现有实体表
    - 不创建 `kg_entities` 表，避免数据冗余和同步问题

3. **视图支持**：
    - 创建类型特定的视图（如 `v_kg_catalogue_relationships`）
    - 通过 JOIN 现有实体表提供完整的实体关系信息
    - 方便查询，无需手动 JOIN

#### 12.2.2 核心表结构

-   **kg_relationships**：关系表（核心表），存储实体间的关系
-   **kg_entity_graph_metadata**：实体图查询优化表（可选），存储图查询相关元数据
-   **kg_timeline_snapshots**：时间轴快照表，存储业务阶段的时间轴数据
-   **kg_risk_alerts**：风险预警表，存储知识图谱相关的风险预警
-   **kg_graph_audit_trail**：知识图谱审计轨迹表（仅记录图谱相关操作）

#### 12.2.3 数据同步策略

由于不重复存储实体数据，数据同步策略如下：

1. **关系数据同步**：

    - 当实体关系发生变化时（如分类包含文件、分类有子分类），同步更新 `kg_relationships` 表
    - 通过 CDC 或应用层事件监听实现

2. **图查询元数据同步**：

    - 定期计算实体的图查询相关属性（如重要性评分、中心度）
    - 更新 `kg_entity_graph_metadata` 表

3. **时间轴快照**：
    - 按业务阶段定期创建快照
    - 统计实体数量和关系数量，存储到 `kg_timeline_snapshots` 表

### 12.3 未采纳的建议及原因

#### 12.3.1 ArchiveSeries（档案系列）维度

-   ❌ **未采纳原因**：
    -   当前业务模型中没有档案系列的概念
    -   `AttachCatalogue` 已通过 `IsArchived` 字段支持归档功能
    -   如果未来业务需要，可以通过 `BusinessEntity` 维度扩展，无需新增独立维度
    -   避免过度设计，保持模型简洁

#### 12.3.2 CreatedBy/UpdatedBy 字段

-   ❌ **未采纳原因**：
    -   ABP 框架已提供完整的审计字段（`CreatorId`, `LastModifierId`, `CreationTime`, `LastModificationTime`）
    -   知识图谱实体通过 `Properties` 字段可以存储这些信息
    -   避免数据冗余，保持与 ABP 框架的一致性

#### 12.3.3 Version 字段（实体基类）

-   ❌ **未采纳原因**：
    -   不是所有实体都需要版本控制（如 Person、Facet 等）
    -   模板实体已有 `Version` 字段，文件版本通过关系管理
    -   版本控制应作为可选功能，而非所有实体的必需字段

#### 12.3.4 CatalogueCode（分类编码）

-   ❌ **未采纳原因**：
    -   当前业务模型中没有分类编码的需求
    -   分类通过 `Reference` + `ReferenceType` + `CatalogueName` 唯一标识
    -   如果未来需要，可以通过 `Properties` 字段扩展，无需修改核心模型

#### 12.3.5 ArchiveDate/ReviewDate（分类实体）

-   ❌ **未采纳原因**：
    -   归档日期可通过审计字段 `LastModificationTime` 和状态变更记录获取
    -   审核日期可通过审计轨迹表查询
    -   避免字段冗余，保持模型简洁

### 12.4 设计原则

在优化过程中遵循以下原则：

1. **与现有框架集成**：充分利用 ABP 框架的审计、权限等功能，避免重复实现
2. **业务驱动**：只添加当前业务确实需要的功能，避免过度设计
3. **扩展性**：通过 `Properties` 字段和关系模型支持未来扩展
4. **性能优化**：合理使用索引，提升查询性能
5. **合规性**：通过审计轨迹表支持合规性要求

### 12.5 未来扩展建议

如果未来业务需要以下功能，可以通过以下方式扩展：

1. **档案系列管理**：通过 `BusinessEntity` 维度或新增关系类型实现
2. **分类编码**：通过 `Properties` 字段或新增可选字段实现
3. **更细粒度的版本控制**：通过关系模型和审计轨迹表实现
4. **文件完整性状态**：通过 `Checksum` 字段和定期校验任务实现

---

## 13. 总结

本技术方案提供了多维知识图谱系统的完整设计和实现方案，涵盖了数据模型、API 设计、前端组件、核心算法等各个方面。系统采用现代化的技术栈，具有良好的扩展性和性能表现，能够满足业务需求并支持未来扩展。

### 核心特点

-   **六维实体模型**：分类、文件、模板、分面、人员、业务实体
-   **完整的关系网络**：支持复杂的业务关系建模和查询
-   **统一版本管理**：支持文件和分类的版本历史管理，包括版本创建、回滚和变更追踪
-   **细粒度访问控制**：集成 `AttachCatalogue.Permissions` 权限系统，自动过滤无权限的实体
-   **性能优化**：合理的索引设计和查询优化策略
-   **合规性支持**：完整的审计轨迹和风险预警机制
-   **扩展性强**：通过 Properties 字段和关系模型支持灵活扩展
