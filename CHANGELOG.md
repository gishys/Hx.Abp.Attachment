# 更新日志 (Changelog)

本文档记录了项目的重要变更和更新内容。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [未发布] - 2025

---

## [v1.1.56] - 2025-11-21

### 新增功能

#### 权限管理系统扩展

-   **新增权限组**：添加了 6 个新的权限组，共包含 38 个子权限
    -   **知识图谱权限** (`Attachment.KnowledgeGraph`)：查看、创建、编辑、删除、查询、分析、导出
    -   **智能采集权限** (`Attachment.IntelligentCollection`)：查看、创建、编辑、删除、执行、配置、监控
    -   **档案查询权限** (`Attachment.ArchiveQuery`)：查看、查询、高级查询、导出、统计
    -   **数据驾驶舱权限** (`Attachment.DataDashboard`)：查看、配置、导出、管理组件
    -   **人工校验台权限** (`Attachment.ManualVerification`)：查看、执行校验、审批通过、拒绝审批、批量处理、统计
    -   **系统设置权限** (`Attachment.SystemSettings`)：查看、配置、管理用户、管理角色、管理权限、系统维护、查看日志、备份恢复
-   **权限常量定义**：在 `AttachmentPermissions` 类中添加了所有新权限的常量定义，便于代码中使用
-   **权限定义提供者**：在 `AttachmentPermissionDefinitionProvider` 中注册了所有新权限，支持多租户

#### 本地化资源支持

-   **新增本地化资源模块**：创建了 `HxAbpAttachmentDomainSharedModule` 模块
-   **简体中文本地化**：添加了完整的简体中文本地化文件 (`zh-CN.json`)，包含所有权限的中文名称
-   **英文本地化**：添加了英文本地化文件 (`en.json`) 作为后备
-   **本地化资源类**：创建了 `AttachmentLocalizationResource` 类用于本地化资源管理

### 改进

#### 分类查询接口优化

-   **接口合并**：将"查询当天用户最新上传分类"功能合并到现有的 `GetCataloguesTreeAsync` 接口中
    -   添加了 `creatorId` 参数（可选）：用于查询指定用户创建的分类
    -   添加了 `targetDate` 参数（可选）：用于查询指定日期创建的分类
    -   智能处理：如果指定了 `targetDate` 但没有指定 `creatorId`，自动使用当前用户 ID
    -   排序优化：当指定了创建者或日期时，按创建时间倒序排列（最新在前）
-   **仓储层优化**：
    -   在 `GetCataloguesTreeAsync` 和 `GetCataloguesTreeCountAsync` 方法中添加了用户和日期过滤支持
    -   删除了独立的 `GetTodayUserCataloguesTreeAsync` 和 `GetTodayUserCataloguesTreeCountAsync` 方法
    -   统一了查询逻辑，减少代码重复
-   **API 端点**：统一使用 `GET /api/app/attachment/tree` 端点，通过参数控制查询行为

#### ZIP 文件下载修复

-   **问题修复**：修复了 React 前端下载 ZIP 文件后无法正常解压的问题
-   **Blob 处理优化**：
    -   兼容多种响应格式（response 本身是 Blob、response.data 是 Blob 等）
    -   创建 Blob 时指定正确的 MIME 类型 (`application/zip`)
    -   添加了数据有效性验证
-   **文件名解析增强**：
    -   支持 UTF-8 编码的文件名（`filename*=UTF-8''`格式）
    -   支持标准格式的文件名（`filename="xxx"`格式）
    -   改进了文件名解码逻辑
-   **错误处理完善**：
    -   处理 Blob 格式的错误响应
    -   提供更友好的错误提示信息

### 修复

#### 权限查询问题

-   **模块依赖修复**：确保 `HxAbpAttachmentApplicationContractsModule` 正确依赖 `HxAbpAttachmentDomainSharedModule`
-   **本地化资源引用修复**：将权限定义提供者中的本地化资源引用从 `AttachmentResource` 更新为 `AttachmentLocalizationResource`
-   **权限提供者注册**：权限定义提供者会自动被 ABP 框架发现和注册（继承自 `PermissionDefinitionProvider`）

### 技术细节

#### 项目配置

-   **嵌入资源配置**：更新了 `.csproj` 文件，将 JSON 本地化文件标记为嵌入资源
-   **包引用**：添加了 `Volo.Abp.Localization` 包引用到 Domain.Shared 项目

#### 代码质量

-   遵循 ABP Framework 权限管理最佳实践
-   遵循 ABP Framework 本地化资源管理最佳实践
-   代码结构清晰，职责分离明确
-   所有权限均设置为 `MultiTenancySides.Both`，支持多租户场景

---

## [v1.1.55] - 2025-11-18

### 新增功能

#### 动态子分类创建功能

-   **新增服务**：`DynamicSubCatalogueCreationService`
    -   根据文件夹路径递归创建子分类，支持嵌套文件夹结构
    -   自动匹配模板：如果子文件夹名称与模板名称匹配，使用模板属性创建分类
    -   动态创建：如果没有匹配的模板，根据父模板属性动态创建子分类
    -   避免重复：自动检查已存在的分类，避免重复创建
-   **DTO 扩展**：
    -   `FileFacetMappingDto` 新增 `subFolderPath` 属性，支持指定子文件夹路径
    -   例如：如果文件在 "案卷 A/材料类型/正本" 路径下，可指定 `subFolderPath` 为 "材料类型/正本"
-   **功能特性**：
    -   支持多级嵌套文件夹结构（如 "材料类型/正本/扫描件"）
    -   自动计算分类路径（Path）和序号（SequenceNumber）
    -   自动继承父分类的引用信息（Reference、ReferenceType）
    -   支持模板匹配和动态创建两种模式
-   **使用场景**：
    -   批量上传文件时，根据文件夹结构自动创建对应的分类层级
    -   支持复杂的文件组织结构，无需预先创建所有分类

### 改进

#### JSON 序列化字段命名优化

-   **DTO 更新**：为所有 DTO 属性添加 `[JsonPropertyName]` 特性，支持 camelCase（首字母小写）命名
    -   `FileFacetMappingDto`：`fileName`、`fileIndex`、`fileSize`、`dynamicFacetCatalogueName`、`subFolderPath`
    -   `DynamicFacetInfoDto`：`catalogueName`、`description`、`sequenceNumber`、`tags`、`metadata`
-   **优势**：
    -   符合前端 JavaScript/TypeScript 命名规范
    -   提高前后端数据交互的一致性
    -   无需额外的 JSON 序列化配置

#### 动态分面验证逻辑增强

-   **新增验证**：当子模板中存在动态分面时，验证所有文件都必须分配动态分面分类名称
    -   检查每个文件的 `DynamicFacetCatalogueName` 是否已设置
    -   如果存在未分配的文件，抛出明确的错误提示，列出前 5 个文件名
-   **验证逻辑优化**：
    -   如果提供了 `dynamicFacetInfoList`，验证其中的 `catalogueName` 是否与文件中的 `DynamicFacetCatalogueName` 匹配
    -   如果未提供 `dynamicFacetInfoList`，从文件的 `DynamicFacetCatalogueName` 中提取动态分面信息
-   **错误提示优化**：
    -   明确提示缺少动态分面分类名称的文件
    -   提示如何修复（通过 `fileFacetMapping` 为每个文件指定 `dynamicFacetCatalogueName`）

### 技术细节

#### 依赖注入

-   注册了 `DynamicSubCatalogueCreationService` 到 DI 容器

#### 代码质量

-   遵循单一职责原则，将复杂的子分类创建逻辑隔离到独立服务
-   支持事务管理，确保数据一致性
-   完善的日志记录，便于问题排查

---

## [v1.1.53] - 2025

### 改进

#### 文件上传匹配机制优化

-   **问题**：文件夹嵌套上传时，同名文件无法通过文件路径匹配（`IFormFile.FileName` 只包含文件名，不包含路径）
-   **解决方案**：实现基于文件索引的匹配机制
    -   **主要匹配方式**：文件索引（`FileIndex`）- 前端按顺序上传，后端按索引位置匹配
    -   **备选匹配方式**：文件名+大小组合（`FileName + FileSize`）- 提高匹配准确性
-   **DTO 变更**：
    -   移除了 `FilePath` 属性（后端无法获取文件路径）
    -   添加了 `FileIndex` 属性（必需，最可靠的匹配方式）
    -   添加了 `FileSize` 属性（可选，用于辅助匹配）
    -   保留了 `FileName` 属性（用于备选匹配）
-   **后端匹配逻辑**：
    -   优先级 1：文件索引匹配（最可靠）
    -   优先级 2：文件名+大小组合匹配（备选方案）
-   **优势**：
    -   即使文件名重复也能准确匹配
    -   支持文件夹嵌套批量上传
    -   性能开销小，无需额外计算

### 文档

#### 前端示例文档更新

-   **更新**：`react-upload-example.md`
    -   移除了文件路径相关的说明和代码
    -   更新为基于文件索引的匹配机制
    -   添加了详细的匹配优先级说明
    -   更新了代码示例，自动添加文件索引和文件大小

---

## [v1.1.52] - 2025

### 新增功能

#### 模板结构下载功能

-   **新增接口**：`DownloadTemplateStructureAsZipAsync`
    -   支持下载分类模板的完整目录结构为 ZIP 压缩包
    -   压缩包包含完整的目录结构
    -   动态分面模板在名称后添加 `(动态分类)` 标记
    -   每个模板文件夹包含 `模板信息.txt` 文件，记录模板详细信息

#### 模板验证服务

-   **新增服务**：`AttachCatalogueTemplateValidationService`
    -   集中管理模板创建和更新的业务规则验证
    -   实现了以下业务规则：
        1. **根分类模板不能是动态分面**：确保根模板的稳定性
        2. **同一级只能有一个动态分面模板**：避免同级多个动态分面导致结构混乱
        3. **动态分面和静态分面互斥**：同一级不能同时存在动态和静态分面模板，保持分类结构清晰
        4. **模板名称唯一性验证**：同一父节点下模板名称不能重复，根节点下也不能重复

### 改进

#### 模板名称唯一性约束优化

-   **修改前**：全局唯一性约束 `TemplateName + IsLatest`（所有模板名称全局唯一）
-   **修改后**：
    -   根节点唯一性约束：`TemplateName + IsLatest`（仅当 `ParentId IS NULL` 时）
    -   子节点唯一性约束：`TemplateName + ParentId + IsLatest`（仅当 `ParentId IS NOT NULL` 时）
-   **业务规则**：
    -   根节点下模板名称唯一
    -   同一父节点下模板名称唯一
    -   不同父节点下可以有相同的模板名称
-   **数据库迁移脚本**：`AttachCatalogueTemplate_UpdateUniqueIndexes.sql`
    -   删除旧的全局唯一性约束
    -   创建新的分组唯一性约束

#### 验证逻辑优化

-   **统一验证**：将所有业务规则验证集中到 `AttachCatalogueTemplateValidationService`
-   **减少冗余**：移除了应用服务中重复的名称检查代码
-   **性能优化**：仅在模板名称、分面类型或父模板变化时进行验证
-   **错误信息优化**：所有错误消息都包含操作类型（创建/更新/创建新版本/更新版本）

### 修复

#### 控制器继承问题

-   **问题**：`AttachCatalogueTemplateController` 未继承 `AbpControllerBase`，导致无法使用 `File` 方法
-   **修复**：添加 `AbpControllerBase` 基类继承

### 文档

#### API 文档

-   **更新**：`AttachCatalogueTemplate_API_Documentation.md`
    -   添加了下载模板结构为压缩包的接口文档
    -   包含前端调用示例（React Axios、Fetch、React Hook、TypeScript）

#### 数据库脚本

-   **新增**：`AttachCatalogueTemplate_UpdateUniqueIndexes.sql` - 模板名称唯一性约束更新脚本
    -   幂等性设计，可重复执行
    -   包含数据验证查询示例

### 技术细节

#### 依赖注入

-   注册了 `AttachCatalogueTemplateValidationService` 到 DI 容器

#### 异步操作

-   所有异步方法都添加了 `CancellationToken` 参数支持

#### 代码质量

-   遵循 DDD（领域驱动设计）最佳实践
-   遵循 ABP Framework 最佳实践
-   代码结构清晰，职责分离明确
-   减少代码冗余，提高可维护性

---

## 变更类型说明

-   **新增功能**：新添加的功能
-   **改进**：对现有功能的改进和优化
-   **修复**：问题修复
-   **文档**：文档更新
-   **技术细节**：技术实现细节

## 版本号说明

当前版本标记为 `[未发布]`，待正式发布时将更新为具体的版本号（如 `v1.1.54`）。
