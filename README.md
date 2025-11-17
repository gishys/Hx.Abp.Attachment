# Hx.Abp.Attachment - 智能档案管理系统

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![ABP Framework](https://img.shields.io/badge/ABP%20Framework-8.0-green.svg)](https://abp.io/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-14+-blue.svg)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 📋 项目概述

**Hx.Abp.Attachment** 是一个基于 ABP (ASP.NET Boilerplate) 框架开发的智能档案管理系统模块，其核心目标是利用人工智能技术提升传统档案管理的效率与智能化水平。

**最新更新** (2025):

-   ✨ 新增模板结构下载功能，支持将模板结构导出为 ZIP 压缩包
-   ✨ 新增模板验证服务，集中管理模板创建和更新的业务规则验证
-   🔧 优化模板名称唯一性约束，从全局唯一改为按父节点分组唯一
-   🔧 优化验证逻辑，统一验证服务，提升代码质量和可维护性

> 📖 查看 [业务介绍文档](docs/BUSINESS_INTRODUCTION.md) 了解更多业务价值和应用场景

### 🎯 核心价值

-   **智能化档案管理**: 通过 AI 技术实现文档自动分类、智能检索和内容分析
-   **提升工作效率**: 减少人工操作，自动化处理海量非结构化文档
-   **知识价值挖掘**: 从档案中提取关键信息，构建企业知识库
-   **标准化管理**: 建立统一的档案分类体系和检索标准

### 🏢 应用场景

-   企业合同文档管理
-   技术报告与设计文档归档
-   邮件与通讯记录管理
-   项目文档全生命周期管理
-   合规性文档存储与检索

## 🏗️ 系统架构

### 技术栈

| 层级        | 技术选型              | 版本 | 说明                     |
| ----------- | --------------------- | ---- | ------------------------ |
| **框架**    | ABP Framework         | 8.0  | 企业级应用开发框架       |
| **运行时**  | .NET                  | 8.0  | 跨平台开发平台           |
| **数据库**  | PostgreSQL            | 14+  | 关系型数据库，支持 JSONB |
| **ORM**     | Entity Framework Core | 8.0  | 对象关系映射框架         |
| **AI 服务** | 阿里云 OpenNLU        | v1   | 自然语言处理服务         |
| **前端**    | ASP.NET Core MVC      | 8.0  | Web 应用框架             |
| **认证**    | ABP Identity          | 8.0  | 身份认证与授权           |

### 项目结构

```
Hx.Abp.Attachment/
├── src/
│   ├── Hx.Abp.Attachment.Api/                    # API 网关层
│   ├── Hx.Abp.Attachment.Application/             # 应用服务层
│   ├── Hx.Abp.Attachment.Application.Contracts/   # 应用服务契约
│   ├── Hx.Abp.Attachment.Application.ArchAI/      # AI 应用服务层
│   ├── Hx.Abp.Attachment.Application.ArchAI.Contracts/  # AI 服务契约
│   ├── Hx.Abp.Attachment.Domain/                  # 领域层
│   ├── Hx.Abp.Attachment.Dmain.Shared/           # 共享领域层
│   ├── Hx.Abp.Attachment.EntityFrameworkCore/    # 数据访问层
│   └── Hx.Abp.Attachment.HttpApi/                # HTTP API 层
├── postgresql-fulltext-search/                    # 数据库迁移脚本
├── docs/                                          # 项目文档
└── tests/                                         # 测试项目
```

### 架构模式

-   **分层架构**: 严格遵循 DDD (领域驱动设计) 分层原则
-   **模块化设计**: 基于 ABP 模块化架构，支持功能扩展
-   **微服务就绪**: 架构设计支持未来向微服务架构演进
-   **事件驱动**: 支持领域事件和集成事件

## 🚀 核心功能模块

### 1. 智能检索模块

#### 功能特性

-   **语义查询**: 基于自然语言理解的智能检索
-   **全文检索**: 支持 PostgreSQL 全文搜索
-   **关键词提取**: AI 驱动的文档关键词自动识别
-   **摘要生成**: 自动生成文档内容摘要

#### 核心接口

```csharp
public interface IDocumentAnalysisService
{
    Task<DocumentAnalysisResult> AnalyzeDocumentAsync(string content);
    Task<List<string>> ExtractDocumentKeywordsAsync(string content, int count);
    Task<string> GenerateDocumentSummaryAsync(string content, int maxLength);
}
```

#### 技术实现

-   集成阿里云 OpenNLU 服务
-   支持中文分词和语义理解
-   基于 TF-IDF 和 TextRank 的关键词提取
-   使用 PostgreSQL 的 `tsvector` 和 `tsquery` 进行全文检索

### 2. 智能档案采集与 AI 分类入库

#### 功能特性

-   **智能分类推荐**: 基于文档内容的自动分类
-   **批量处理**: 支持大规模文档批量分类
-   **置信度评估**: 分类结果可靠性评分
-   **多维度分类**: 支持按业务、部门、项目等多维度分类

#### 核心接口

```csharp
public interface IIntelligentClassificationService
{
    Task<ClassificationResult> RecommendDocumentCategoryAsync(string content, List<string> categories);
    Task<List<ClassificationResult>> BatchRecommendCategoriesAsync(List<string> contents, List<string> categories);
    Task<double> EvaluateClassificationConfidence(string content, string category);
}
```

#### 技术实现

-   基于机器学习的文本分类算法
-   支持自定义分类体系
-   实时分类推荐和批量处理
-   分类结果可解释性分析

### 3. 档案管理核心功能

#### 实体模型

-   **AttachCatalogue**: 档案目录实体
-   **AttachCatalogueTemplate**: 档案目录模板（支持版本管理）
-   **AttachCatalogueTemplatePermission**: 模板权限管理
-   **Attachment**: 附件实体

#### 核心特性

-   **权限管理**: 基于角色的访问控制
-   **版本控制**: 支持文档和模板版本管理
-   **元数据管理**: 丰富的文档属性管理
-   **工作流支持**: 可配置的审批流程
-   **模板验证**: 集中化的业务规则验证服务
    -   根分类模板不能是动态分面
    -   同一级只能有一个动态分面模板
    -   动态分面和静态分面互斥
    -   模板名称唯一性验证（按父节点分组）
-   **模板结构导出**: 支持将模板结构导出为 ZIP 压缩包，包含完整目录结构和模板信息

## 🔧 开发环境搭建

### 前置要求

-   .NET 8.0 SDK
-   PostgreSQL 14+
-   Visual Studio 2022 或 VS Code
-   Git

### 环境配置

1. **克隆项目**

```bash
git clone <repository-url>
cd Hx.Abp.Attachment
```

2. **配置数据库连接**

```json
{
    "ConnectionStrings": {
        "Default": "Host=localhost;Database=AttachmentDB;Username=postgres;Password=your_password"
    }
}
```

3. **配置环境变量**

```bash
# 阿里云 AI 服务配置
export DASHSCOPE_API_KEY="your_api_key"
export ALIYUN_WORKSPACE_ID="your_workspace_id"
```

4. **运行数据库迁移**

```bash
dotnet ef database update --project src/Hx.Abp.Attachment.EntityFrameworkCore
```

5. **启动项目**

```bash
dotnet run --project src/Hx.Abp.Attachment.Api
```

## 📊 数据库设计

### 核心表结构

#### APPATTACH_CATALOGUES (档案目录表)

| 字段名               | 类型         | 说明     |
| -------------------- | ------------ | -------- |
| Id                   | UUID         | 主键     |
| CATALOGUE_NAME       | VARCHAR(255) | 目录名称 |
| CATALOGUE_FACET_TYPE | INTEGER      | 目录类型 |
| CATALOGUE_PURPOSE    | INTEGER      | 目录用途 |
| PERMISSIONS          | JSONB        | 权限配置 |
| TEXT_VECTOR          | JSONB        | 文本向量 |
| VECTOR_DIMENSION     | INTEGER      | 向量维度 |
| FULL_TEXT_CONTENT    | TEXT         | 全文内容 |
| REFERENCE            | VARCHAR(255) | 引用标识 |
| REFERENCE_TYPE       | INTEGER      | 引用类型 |

#### APPATTACH_CATALOGUE_TEMPLATES (档案目录模板表)

| 字段名           | 类型         | 说明                     |
| ---------------- | ------------ | ------------------------ |
| Id               | UUID         | 模板 ID（业务标识）      |
| VERSION          | INTEGER      | 版本号                   |
| TEMPLATE_NAME    | VARCHAR(255) | 模板名称                 |
| PARENT_ID        | UUID         | 父模板 ID                |
| PARENT_VERSION   | INTEGER      | 父模板版本               |
| TEMPLATE_PATH    | VARCHAR(200) | 模板路径                 |
| FACET_TYPE       | INTEGER      | 分面类型                 |
| TEMPLATE_PURPOSE | INTEGER      | 模板用途                 |
| TEMPLATE_ROLE    | INTEGER      | 模板角色（根/分支/叶子） |
| IS_STATIC        | BOOLEAN      | 是否静态分面             |
| IS_LATEST        | BOOLEAN      | 是否最新版本             |
| META_FIELDS      | JSONB        | 元数据字段集合           |
| PERMISSIONS      | JSONB        | 权限配置                 |
| TAGS             | JSONB        | 标签集合                 |

**唯一性约束**:

-   根节点：模板名称在根节点下唯一
-   子节点：模板名称在同一父节点下唯一

### 索引设计

```sql
-- 全文搜索索引
CREATE INDEX idx_catalogues_fulltext ON "APPATTACH_CATALOGUES"
USING gin(to_tsvector('chinese_fts', FULL_TEXT_CONTENT));

-- 引用索引
CREATE INDEX idx_catalogues_reference ON "APPATTACH_CATALOGUES"
(REFERENCE, REFERENCE_TYPE);

-- 模板索引
CREATE INDEX idx_templates_type_purpose ON "APPATTACH_CATALOGUE_TEMPLATES"
(CATALOGUE_FACET_TYPE, CATALOGUE_PURPOSE);
```

## 🧠 AI 能力集成

### 阿里云 OpenNLU 服务

#### 服务配置

```csharp
public class AliyunAIService
{
    private readonly string _apiKey = Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
    private readonly string _workspaceId = Environment.GetEnvironmentVariable("ALIYUN_WORKSPACE_ID");
    private readonly string _baseUrl = "https://dashscope.aliyuncs.com/api/v1/services/nlp/nlu/understanding";
}
```

#### 支持的任务类型

-   **文本分类**: 文档自动分类
-   **实体识别**: 人名、地名、组织名等实体提取
-   **关键词提取**: 核心词汇识别
-   **摘要生成**: 文档内容摘要
-   **情感分析**: 文档情感倾向分析

### AI 模型优化

#### 性能优化

-   使用 `Span<T>` 和 `MemoryExtensions` 进行文本处理
-   实现 `ArrayPool<T>` 内存池优化
-   支持批量处理提升吞吐量

#### 质量提升

-   支持中文和英文混合文本
-   自定义分类标签训练
-   用户反馈学习机制

## 🔒 安全与权限

### 权限体系

#### 权限级别

-   **系统级权限**: 管理员权限
-   **分类级权限**: 按档案分类控制访问
-   **文档级权限**: 单个文档的细粒度权限
-   **操作级权限**: 读、写、删除等操作权限

#### 权限配置

```json
{
    "permissions": [
        {
            "role": "Manager",
            "actions": ["read", "write", "delete"],
            "scope": "all"
        },
        {
            "role": "User",
            "actions": ["read"],
            "scope": "assigned"
        }
    ]
}
```

### 数据安全

-   **数据加密**: 敏感数据 AES 加密存储
-   **访问审计**: 完整的操作日志记录
-   **数据脱敏**: 敏感信息展示脱敏
-   **备份恢复**: 定期数据备份策略

## 📈 性能优化

### 数据库优化

#### 查询优化

-   使用 GIN 索引加速全文搜索
-   实现查询结果缓存
-   支持分页和懒加载

#### 存储优化

-   JSONB 类型存储非结构化数据
-   向量化存储提升相似度计算
-   分区表支持大数据量

### 应用层优化

#### 缓存策略

-   Redis 缓存热点数据
-   内存缓存减少数据库访问
-   分布式缓存支持集群部署

#### 异步处理

-   支持异步文档处理
-   后台任务队列
-   事件驱动架构

## 🧪 测试策略

### 测试类型

#### 单元测试

-   领域逻辑测试
-   服务层测试
-   工具类测试

#### 集成测试

-   API 接口测试
-   数据库集成测试
-   AI 服务集成测试

#### 性能测试

-   并发用户测试
-   大数据量处理测试
-   响应时间测试

### 测试工具

-   **xUnit**: 单元测试框架
-   **Moq**: Mock 框架
-   **Testcontainers**: 容器化测试环境
-   **BenchmarkDotNet**: 性能基准测试

## 🚀 部署指南

### 开发环境

```bash
# 1. 安装依赖
dotnet restore

# 2. 构建项目
dotnet build

# 3. 运行测试
dotnet test

# 4. 启动应用
dotnet run --project src/Hx.Abp.Attachment.Api
```

### 生产环境

#### Docker 部署

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Hx.Abp.Attachment.Api/Hx.Abp.Attachment.Api.csproj", "src/Hx.Abp.Attachment.Api/"]
RUN dotnet restore "src/Hx.Abp.Attachment.Api/Hx.Abp.Attachment.Api.csproj"
COPY . .
WORKDIR "/src/src/Hx.Abp.Attachment.Api"
RUN dotnet build "Hx.Abp.Attachment.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Hx.Abp.Attachment.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Hx.Abp.Attachment.Api.dll"]
```

#### 环境配置

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
        "Default": "Host=db;Database=AttachmentDB;Username=postgres;Password=secure_password"
    },
    "Redis": {
        "ConnectionString": "redis:6379"
    }
}
```

## 📚 API 文档

### 核心接口

#### 档案管理

-   `POST /api/app/attachment/create` - 创建档案
-   `GET /api/app/attachment/{id}` - 获取档案详情
-   `PUT /api/app/attachment/{id}` - 更新档案
-   `DELETE /api/app/attachment/{id}` - 删除档案

#### 智能检索

-   `POST /api/app/attachment/search` - 全文搜索
-   `POST /api/app/attachment/semantic-search` - 语义搜索
-   `GET /api/app/attachment/keywords` - 获取关键词

#### AI 分类

-   `POST /api/app/attachment/classify` - 智能分类
-   `POST /api/app/attachment/batch-classify` - 批量分类
-   `GET /api/app/attachment/classification-confidence` - 分类置信度

#### 模板管理

-   `POST /api/app/attach-catalogue-template` - 创建模板
-   `GET /api/app/attach-catalogue-template/{id}` - 获取模板详情
-   `PUT /api/app/attach-catalogue-template/{id}` - 更新模板
-   `DELETE /api/app/attach-catalogue-template/{id}` - 删除模板
-   `GET /api/app/attach-catalogue-template/{id}/download-structure` - 下载模板结构为 ZIP
-   `POST /api/app/attach-catalogue-template/{id}/create-version` - 创建模板新版本
-   `GET /api/app/attach-catalogue-template/{id}/history` - 获取模板版本历史

### 接口规范

#### 请求格式

```json
{
    "catalogueName": "项目合同",
    "catalogueType": 1,
    "cataloguePurpose": 2,
    "reference": "PROJ-2024-001",
    "referenceType": 1,
    "permissions": {
        "roles": ["Manager", "ProjectLead"],
        "actions": ["read", "write"]
    }
}
```

#### 响应格式

```json
{
    "success": true,
    "data": {
        "id": "uuid",
        "catalogueName": "项目合同",
        "creationTime": "2024-01-01T00:00:00Z"
    },
    "error": null
}
```

## 🔄 开发工作流

### Git 工作流

#### 分支策略

-   **main**: 主分支，生产环境代码
-   **develop**: 开发分支，集成测试代码
-   **feature/\***: 功能分支，新功能开发
-   **hotfix/\***: 热修复分支，紧急问题修复

#### 提交规范

```
feat: 添加用户登录功能
fix: 修复订单计算精度问题
docs: 更新 API 文档
style: 代码格式调整
refactor: 重构用户服务
test: 添加单元测试
chore: 更新依赖包
```

### 代码质量

#### 代码规范

-   遵循 C# 编码规范
-   使用 EditorConfig 统一配置
-   集成 StyleCop 代码风格检查
-   支持 Prettier 格式化

#### 质量检查

-   集成 SonarQube 代码质量分析
-   自动化单元测试覆盖率检查
-   代码审查流程
-   持续集成/持续部署 (CI/CD)

## 📋 开发计划

### 短期目标 (1-3 个月)

-   [x] 基础档案管理功能
-   [x] 智能检索模块
-   [x] AI 分类功能
-   [x] 模板管理功能（版本管理、结构导出）
-   [x] 模板验证服务（业务规则验证）
-   [x] 动态/静态分面管理
-   [ ] 基础权限控制优化

### 中期目标 (3-6 个月)

-   [ ] 知识图谱可视化
-   [ ] 数据驾驶舱
-   [ ] 数据治理体系
-   [ ] AI 模型优化
-   [ ] 性能监控

### 长期目标 (6-12 个月)

-   [ ] 零信任权限体系
-   [ ] 全生命周期流程引擎
-   [ ] 数字资产管理
-   [ ] 高级安全防护
-   [ ] 企业级部署支持

## 🤝 贡献指南

### 参与贡献

1. Fork 项目仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

### 问题反馈

-   使用 GitHub Issues 报告 Bug
-   提出新功能建议
-   讨论技术实现方案

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📚 相关文档

-   [业务介绍文档](docs/BUSINESS_INTRODUCTION.md) - 从业务角度介绍项目价值和应用场景
-   [技术栈详解](项目技术栈详解.md) - 详细的技术栈说明和架构设计
-   [更新日志](CHANGELOG.md) - 项目变更记录和版本历史
-   [API 文档](src/Hx.Abp.Attachment.HttpApi/Scripts/) - 完整的 API 接口文档

## 📞 联系我们

-   **项目维护者**: Hx.Abp.Attachment Team
-   **邮箱**: [your-email@example.com]
-   **项目地址**: [GitHub Repository URL]
-   **文档地址**: [Documentation URL]

## 🙏 致谢

感谢以下开源项目和技术社区的支持：

-   [ABP Framework](https://abp.io/) - 企业级应用开发框架
-   [.NET](https://dotnet.microsoft.com/) - 跨平台开发平台
-   [PostgreSQL](https://www.postgresql.org/) - 强大的开源数据库
-   [阿里云](https://www.aliyun.com/) - AI 服务提供商

---

**Hx.Abp.Attachment** - 让档案管理更智能，让知识价值更凸显 🚀
