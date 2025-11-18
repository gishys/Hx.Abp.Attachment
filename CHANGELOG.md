# 更新日志 (Changelog)

本文档记录了项目的重要变更和更新内容。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/)。

## [未发布] - 2025

### 改进

#### JSON 序列化字段命名优化

-   **DTO 更新**：为所有 DTO 属性添加 `[JsonPropertyName]` 特性，支持 camelCase（首字母小写）命名
    -   `FileFacetMappingDto`：`fileName`、`fileIndex`、`fileSize`、`dynamicFacetCatalogueName`
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
