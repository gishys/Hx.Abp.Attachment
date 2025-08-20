# AttachCatalogueTemplate 分类模板功能指南

## 概述

`AttachCatalogueTemplate` 是一个完整的分类模板系统，允许用户创建、管理和使用模板来快速生成附件分类结构。该系统支持版本管理、语义匹配、规则引擎和模板继承等高级功能。

## 核心功能

### 1. 模板管理

-   **创建模板**: 支持创建带有规则和语义配置的模板
-   **版本控制**: 完整的版本管理，支持创建新版本和回滚
-   **模板继承**: 支持父子模板关系，构建模板树结构
-   **模板验证**: 内置配置验证，确保模板格式正确

### 2. 智能匹配

-   **语义匹配**: 基于 AI 模型的语义相似度匹配
-   **规则引擎**: 支持复杂的业务规则配置
-   **多维度搜索**: 支持按名称、规则、语义等多维度查找模板

### 3. 分类生成

-   **模板实例化**: 从模板快速生成分类结构
-   **动态命名**: 支持基于上下文的动态分类命名
-   **批量生成**: 支持批量从模板生成分类

## 架构设计

### 领域层 (Domain)

```
AttachCatalogueTemplate (实体)
├── 基础属性 (TemplateName, Version, IsLatest)
├── 配置属性 (NamePattern, RuleExpression, SemanticModel)
├── 业务属性 (IsRequired, SequenceNumber, IsStatic)
├── 关系属性 (ParentId, Children)
└── 业务方法 (ValidateConfiguration, CopyFrom, GetPath)
```

### 仓储层 (Repository)

```
IAttachCatalogueTemplateRepository
├── 基础CRUD操作
├── 语义匹配查询 (FindBySemanticMatchAsync)
├── 规则匹配查询 (FindByRuleMatchAsync)
├── 版本管理 (GetLatestVersionAsync, GetTemplateHistoryAsync)
└── 树结构查询 (GetChildrenAsync)
```

### 应用层 (Application)

```
AttachCatalogueTemplateAppService
├── 模板CRUD操作
├── 模板匹配 (FindMatchingTemplatesAsync)
├── 模板结构 (GetTemplateStructureAsync)
├── 分类生成 (GenerateCatalogueFromTemplateAsync)
└── 版本管理 (CreateNewVersionAsync, RollbackToVersionAsync)
```

### 接口层 (HttpApi)

```
AttachCatalogueTemplateController
├── RESTful API 接口
├── 模板管理端点
├── 模板匹配端点
├── 分类生成端点
└── 版本管理端点
```

## API 接口

### 基础 CRUD 操作

```http
POST   /api/app/attachment-template                    # 创建模板
GET    /api/app/attachment-template                    # 获取模板列表
GET    /api/app/attachment-template/{id}               # 获取模板详情
PUT    /api/app/attachment-template/{id}               # 更新模板
DELETE /api/app/attachment-template/{id}               # 删除模板
```

### 模板匹配

```http
POST   /api/app/attachment-template/find-matching      # 查找匹配的模板
```

### 模板结构

```http
GET    /api/app/attachment-template/{id}/structure     # 获取模板结构
```

### 分类生成

```http
POST   /api/app/attachment-template/generate-catalogue # 从模板生成分类
```

### 版本管理

```http
POST   /api/app/attachment-template/{id}/versions      # 创建新版本
PUT    /api/app/attachment-template/{id}/set-latest    # 设为最新版本
GET    /api/app/attachment-template/{id}/history       # 获取版本历史
POST   /api/app/attachment-template/{id}/rollback      # 回滚到指定版本
```

## 使用示例

### 1. 创建模板

```csharp
var templateDto = new CreateUpdateAttachCatalogueTemplateDto
{
    TemplateName = "合同文档模板",
    AttachReceiveType = AttachReceiveType.Copy,
    NamePattern = "合同_{ContractType}_{ContractNumber}",
    RuleExpression = @"{
        ""WorkflowName"": ""ContractWorkflow"",
        ""Rules"": [
            {
                ""RuleName"": ""ContractTypeRule"",
                ""Expression"": ""ContractType != null""
            }
        ]
    }",
    SemanticModel = "contract_model",
    IsRequired = true,
    SequenceNumber = 1,
    IsStatic = false
};

var result = await templateService.CreateAsync(templateDto);
```

### 2. 查找匹配模板

```csharp
var matchInput = new TemplateMatchInput
{
    SemanticQuery = "合同文档",
    OnlyLatest = true
};

var templates = await templateService.FindMatchingTemplatesAsync(matchInput);
```

### 3. 从模板生成分类

```csharp
var generateInput = new GenerateCatalogueInput
{
    TemplateId = templateId,
    Reference = "CONTRACT_001",
    ReferenceType = 1,
    ContextData = new Dictionary<string, object>
    {
        ["ContractType"] = "销售合同",
        ["ContractNumber"] = "SALE-2024-001"
    }
};

await templateService.GenerateCatalogueFromTemplateAsync(generateInput);
```

### 4. 版本管理

```csharp
// 创建新版本
var newVersion = await templateService.CreateNewVersionAsync(templateId, updateDto);

// 获取版本历史
var history = await templateService.GetTemplateHistoryAsync(templateId);

// 回滚到指定版本
var rollbackVersion = await templateService.RollbackToVersionAsync(versionId);
```

## 数据库设计

### 表结构

```sql
CREATE TABLE "APPATTACH_CATALOGUE_TEMPLATES" (
    "ID" uuid NOT NULL,
    "TEMPLATE_NAME" character varying(256) NOT NULL,
    "VERSION" integer NOT NULL DEFAULT 1,
    "IS_LATEST" boolean NOT NULL DEFAULT true,
    "ATTACH_RECEIVE_TYPE" integer NOT NULL,
    "NAME_PATTERN" character varying(512),
    "RULE_EXPRESSION" text,
    "SEMANTIC_MODEL" character varying(128),
    "IS_REQUIRED" boolean NOT NULL DEFAULT false,
    "SEQUENCE_NUMBER" integer NOT NULL DEFAULT 0,
    "IS_STATIC" boolean NOT NULL DEFAULT false,
    "PARENT_ID" uuid,
    -- 审计字段
    "CREATION_TIME" timestamp without time zone NOT NULL,
    "CREATOR_ID" uuid,
    "LAST_MODIFICATION_TIME" timestamp without time zone,
    "LAST_MODIFIER_ID" uuid,
    "IS_DELETED" boolean NOT NULL DEFAULT false,
    "DELETER_ID" uuid,
    "DELETION_TIME" timestamp without time zone,
    CONSTRAINT "PK_ATTACH_CATALOGUE_TEMPLATES" PRIMARY KEY ("ID")
);
```

### 索引设计

-   **唯一索引**: (TemplateName, Version) - 确保版本唯一性
-   **查询索引**: (TemplateName, IsLatest) - 快速查找最新版本
-   **关系索引**: (ParentId) - 支持树结构查询
-   **排序索引**: (SequenceNumber) - 支持顺序排序
-   **全文索引**: 支持模板内容的全文搜索

## 最佳实践

### 1. 模板设计

-   **命名规范**: 使用清晰的模板名称，便于识别和管理
-   **版本管理**: 定期创建新版本，保持模板的演进历史
-   **规则设计**: 设计简洁有效的业务规则，避免过于复杂
-   **语义模型**: 选择合适的语义模型，提高匹配准确性

### 2. 性能优化

-   **索引使用**: 合理使用数据库索引，提高查询性能
-   **缓存策略**: 对频繁访问的模板进行缓存
-   **批量操作**: 使用批量操作减少数据库交互次数
-   **异步处理**: 对耗时操作使用异步处理

### 3. 安全考虑

-   **权限控制**: 实现基于角色的模板访问控制
-   **数据验证**: 严格验证模板配置，防止恶意输入
-   **审计日志**: 记录模板操作日志，便于追踪和审计
-   **软删除**: 使用软删除保护重要模板数据

### 4. 扩展性设计

-   **插件架构**: 支持自定义规则引擎和语义模型
-   **配置驱动**: 通过配置文件控制模板行为
-   **事件机制**: 使用事件机制实现松耦合的扩展
-   **API 版本**: 支持 API 版本管理，确保向后兼容

## 故障排除

### 常见问题

1. **模板匹配失败**

    - 检查语义模型配置是否正确
    - 验证规则表达式格式
    - 确认上下文数据是否完整

2. **版本冲突**

    - 确保版本号唯一性
    - 检查并发操作是否导致冲突
    - 验证版本状态是否正确

3. **性能问题**

    - 检查数据库索引是否有效
    - 优化查询语句
    - 考虑使用缓存机制

4. **数据一致性问题**
    - 使用事务确保数据一致性
    - 实现乐观锁机制
    - 定期数据校验

## 总结

`AttachCatalogueTemplate` 系统提供了一个完整、灵活、可扩展的分类模板解决方案。通过合理的架构设计和最佳实践，可以构建出高效、稳定、易维护的模板管理系统，为业务提供强大的分类生成能力。
