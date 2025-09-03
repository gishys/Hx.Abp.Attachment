# 开发指南

## 概述

本文档为 Hx.Abp.Attachment 项目的开发指南，包含开发环境搭建、编码规范、开发流程、测试策略等核心内容。

## 开发环境搭建

### 前置要求

#### 必需软件

-   **.NET 8.0 SDK**: [下载地址](https://dotnet.microsoft.com/download/dotnet/8.0)
-   **PostgreSQL 14+**: [下载地址](https://www.postgresql.org/download/)
-   **Git**: [下载地址](https://git-scm.com/downloads)
-   **IDE**: Visual Studio 2022 或 VS Code

#### 推荐软件

-   **Docker Desktop**: 容器化开发环境
-   **Postman**: API 测试工具
-   **pgAdmin**: PostgreSQL 管理工具

### 环境配置

#### 1. 克隆项目

```bash
git clone <repository-url>
cd Hx.Abp.Attachment
```

#### 2. 安装依赖

```bash
dotnet restore
```

#### 3. 配置数据库

```json
// appsettings.Development.json
{
    "ConnectionStrings": {
        "Default": "Host=localhost;Database=AttachmentDB;Username=postgres;Password=your_password;Port=5432"
    }
}
```

#### 4. 运行数据库迁移

```bash
dotnet ef database update --project src/Hx.Abp.Attachment.EntityFrameworkCore
```

#### 5. 配置环境变量

```bash
# Windows
set DASHSCOPE_API_KEY=your_api_key
set ALIYUN_WORKSPACE_ID=your_workspace_id

# Linux/macOS
export DASHSCOPE_API_KEY=your_api_key
export ALIYUN_WORKSPACE_ID=your_workspace_id
```

#### 6. 启动项目

```bash
dotnet run --project src/Hx.Abp.Attachment.Api
```

## 编码规范

### C# 编码规范

#### 命名规范

**类名**: 使用 PascalCase，名词

```csharp
public class AttachCatalogueService
public class DocumentAnalysisResult
```

**方法名**: 使用 PascalCase，动词开头

```csharp
public async Task<AttachCatalogue> CreateAsync(CreateAttachCatalogueDto input)
public List<string> ExtractKeywords(string content)
```

**变量名**: 使用 camelCase

```csharp
var catalogueName = "项目合同";
var isActive = true;
```

**常量名**: 使用 PascalCase

```csharp
public const string DefaultCategory = "未分类";
public const int MaxRetryCount = 3;
```

**私有字段**: 使用 \_camelCase

```csharp
private readonly IAttachCatalogueRepository _repository;
private readonly ILogger<AttachCatalogueService> _logger;
```

#### 代码格式

**缩进**: 使用 4 个空格，不使用 Tab
**行长度**: 建议不超过 120 字符
**空行**: 使用空行分隔逻辑块

```csharp
public class AttachCatalogueService
{
    private readonly IAttachCatalogueRepository _repository;
    private readonly ILogger<AttachCatalogueService> _logger;

    public AttachCatalogueService(
        IAttachCatalogueRepository repository,
        ILogger<AttachCatalogueService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AttachCatalogue> CreateAsync(CreateAttachCatalogueDto input)
    {
        // 参数验证
        if (input == null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        // 业务逻辑
        var catalogue = new AttachCatalogue
        {
            CatalogueName = input.CatalogueName,
            CatalogueType = input.CatalogueType,
            IsActive = true
        };

        // 数据持久化
        await _repository.InsertAsync(catalogue);

        return catalogue;
    }
}
```

#### 注释规范

**类注释**: 使用 XML 文档注释

```csharp
/// <summary>
/// 档案目录服务
/// 负责档案目录的创建、更新、删除等操作
/// </summary>
public class AttachCatalogueService
```

**方法注释**: 详细描述参数、返回值和异常

```csharp
/// <summary>
/// 创建档案目录
/// </summary>
/// <param name="input">创建档案目录的输入参数</param>
/// <returns>创建的档案目录实体</returns>
/// <exception cref="ArgumentNullException">当输入参数为null时抛出</exception>
/// <exception cref="BusinessException">当业务规则验证失败时抛出</exception>
public async Task<AttachCatalogue> CreateAsync(CreateAttachCatalogueDto input)
```

**复杂逻辑注释**: 解释业务逻辑和算法

```csharp
// 使用 TF-IDF 算法提取关键词
// 1. 计算词频 (TF)
// 2. 计算逆文档频率 (IDF)
// 3. 计算 TF-IDF 值
// 4. 选择值最高的前N个词作为关键词
var keywords = ExtractKeywordsUsingTfIdf(content, maxCount);
```

### ABP 框架规范

#### 服务接口定义

```csharp
public interface IAttachCatalogueService
{
    Task<AttachCatalogue> CreateAsync(CreateAttachCatalogueDto input);
    Task<AttachCatalogue> UpdateAsync(Guid id, UpdateAttachCatalogueDto input);
    Task DeleteAsync(Guid id);
    Task<AttachCatalogue> GetAsync(Guid id);
    Task<PagedResultDto<AttachCatalogueDto>> GetListAsync(GetAttachCatalogueListDto input);
}
```

#### 应用服务实现

```csharp
public class AttachCatalogueService : AttachCatalogueAppService, IAttachCatalogueService
{
    private readonly IAttachCatalogueRepository _repository;
    private readonly IAttachCatalogueManager _manager;

    public AttachCatalogueService(
        IAttachCatalogueRepository repository,
        IAttachCatalogueManager manager)
    {
        _repository = repository;
        _manager = manager;
    }

    public override async Task<AttachCatalogueDto> CreateAsync(CreateAttachCatalogueDto input)
    {
        // 业务规则验证
        await _manager.ValidateCreationAsync(input);

        // 创建实体
        var catalogue = await _manager.CreateAsync(input);

        // 保存到数据库
        await _repository.InsertAsync(catalogue);

        // 返回DTO
        return ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(catalogue);
    }
}
```

#### 领域服务

```csharp
public class AttachCatalogueManager : DomainService
{
    public async Task ValidateCreationAsync(CreateAttachCatalogueDto input)
    {
        // 验证业务规则
        if (string.IsNullOrWhiteSpace(input.CatalogueName))
        {
            throw new BusinessException(AttachCatalogueErrorCodes.CatalogueNameCannotBeEmpty);
        }

        // 验证唯一性
        var exists = await Repository.AnyAsync(x => x.CatalogueName == input.CatalogueName);
        if (exists)
        {
            throw new BusinessException(AttachCatalogueErrorCodes.CatalogueNameAlreadyExists);
        }
    }

    public async Task<AttachCatalogue> CreateAsync(CreateAttachCatalogueDto input)
    {
        var catalogue = new AttachCatalogue(
            GuidGenerator.Create(),
            input.CatalogueName,
            input.CatalogueType,
            input.CataloguePurpose
        );

        return catalogue;
    }
}
```

## 开发流程

### 功能开发流程

#### 1. 需求分析

-   理解业务需求
-   确定功能范围
-   设计数据模型
-   定义接口规范

#### 2. 技术设计

-   设计领域模型
-   定义服务接口
-   设计数据访问层
-   规划测试策略

#### 3. 编码实现

-   实现领域实体
-   实现应用服务
-   实现数据访问
-   编写单元测试

#### 4. 测试验证

-   运行单元测试
-   进行集成测试
-   性能测试验证
-   代码审查

#### 5. 部署上线

-   代码合并到主分支
-   自动化构建部署
-   生产环境验证
-   监控告警配置

### Git 工作流

#### 分支管理

```bash
# 主分支
main          # 生产环境代码
develop       # 开发分支，集成测试代码

# 功能分支
feature/user-management     # 用户管理功能
feature/ai-classification  # AI分类功能
feature/search-optimization # 搜索优化功能

# 修复分支
hotfix/critical-bug        # 紧急Bug修复
hotfix/security-patch      # 安全补丁
```

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

#### 代码审查

-   所有代码变更必须通过 Pull Request
-   至少需要一名团队成员审查
-   审查通过后才能合并到主分支
-   使用自动化工具检查代码质量

## 测试策略

### 测试类型

#### 单元测试

-   **测试范围**: 领域逻辑、应用服务、工具类
-   **测试框架**: xUnit
-   **测试原则**: 快速、独立、可重复、自验证

```csharp
[Fact]
public async Task CreateAsync_WithValidInput_ShouldCreateCatalogue()
{
    // Arrange
    var input = new CreateAttachCatalogueDto
    {
        CatalogueName = "测试目录",
        CatalogueType = CatalogueType.Project,
        CataloguePurpose = CataloguePurpose.Contract
    };

    var mockRepository = new Mock<IAttachCatalogueRepository>();
    var mockManager = new Mock<IAttachCatalogueManager>();

    var service = new AttachCatalogueService(mockRepository.Object, mockManager.Object);

    // Act
    var result = await service.CreateAsync(input);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(input.CatalogueName, result.CatalogueName);
    Assert.Equal(input.CatalogueType, result.CatalogueType);
}
```

#### 集成测试

-   **测试范围**: API 接口、数据库集成、外部服务集成
-   **测试环境**: 使用 Testcontainers 创建隔离的测试环境
-   **测试数据**: 使用测试数据工厂创建测试数据

```csharp
[Fact]
public async Task CreateCatalogue_ShouldPersistToDatabase()
{
    // Arrange
    var client = CreateClient();
    var input = new CreateAttachCatalogueDto
    {
        CatalogueName = "集成测试目录",
        CatalogueType = CatalogueType.Project
    };

    // Act
    var response = await client.PostAsJsonAsync("/api/app/attachment/create", input);

    // Assert
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<AttachCatalogueDto>();
    Assert.NotNull(result);
    Assert.NotEqual(Guid.Empty, result.Id);
}
```

#### 性能测试

-   **测试范围**: API 响应时间、数据库查询性能、并发处理能力
-   **测试工具**: BenchmarkDotNet、JMeter
-   **测试指标**: 响应时间、吞吐量、资源使用率

```csharp
[Benchmark]
public async Task SearchPerformance_Benchmark()
{
    var input = new SearchAttachCatalogueDto
    {
        Keyword = "合同",
        PageSize = 20
    };

    await _service.SearchAsync(input);
}
```

### 测试数据管理

#### 测试数据工厂

```csharp
public static class AttachCatalogueTestDataFactory
{
    public static CreateAttachCatalogueDto CreateValidInput()
    {
        return new CreateAttachCatalogueDto
        {
            CatalogueName = $"测试目录_{Guid.NewGuid():N}",
            CatalogueType = CatalogueType.Project,
            CataloguePurpose = CataloguePurpose.Contract
        };
    }

    public static AttachCatalogue CreateValidEntity()
    {
        return new AttachCatalogue(
            Guid.NewGuid(),
            "测试目录",
            CatalogueType.Project,
            CataloguePurpose.Contract
        );
    }
}
```

#### 测试数据清理

```csharp
public class AttachCatalogueTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public AttachCatalogueTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TestMethod()
    {
        // 测试逻辑
    }

    public async Task DisposeAsync()
    {
        // 清理测试数据
        await _fixture.CleanupAsync();
    }
}
```

## 性能优化

### 数据库优化

#### 查询优化

```csharp
// 使用 Include 避免 N+1 查询问题
var catalogues = await _repository
    .Include(x => x.Template)
    .Include(x => x.Permissions)
    .ToListAsync();

// 使用投影减少数据传输
var catalogueNames = await _repository
    .Select(x => x.CatalogueName)
    .ToListAsync();

// 使用分页避免大量数据查询
var pagedResult = await _repository
    .Skip(input.SkipCount)
    .Take(input.MaxResultCount)
    .ToListAsync();
```

#### 索引优化

```sql
-- 为常用查询字段创建索引
CREATE INDEX idx_catalogues_name ON "APPATTACH_CATALOGUES" (CATALOGUE_NAME);
CREATE INDEX idx_catalogues_type ON "APPATTACH_CATALOGUES" (CATALOGUE_FACET_TYPE);
CREATE INDEX idx_catalogues_reference ON "APPATTACH_CATALOGUES" (REFERENCE, REFERENCE_TYPE);

-- 为全文搜索创建 GIN 索引
CREATE INDEX idx_catalogues_fulltext ON "APPATTACH_CATALOGUES"
USING gin(to_tsvector('chinese_fts', FULL_TEXT_CONTENT));
```

### 应用层优化

#### 缓存策略

```csharp
public class AttachCatalogueService
{
    private readonly IDistributedCache _cache;
    private const string CacheKeyPrefix = "AttachCatalogue:";

    public async Task<AttachCatalogueDto> GetAsync(Guid id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        // 尝试从缓存获取
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<AttachCatalogueDto>(cached);
        }

        // 从数据库获取
        var entity = await _repository.GetAsync(id);
        var dto = ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(entity);

        // 存入缓存
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        });

        return dto;
    }
}
```

#### 异步处理

```csharp
public async Task<BatchClassificationResult> BatchClassifyAsync(List<string> contents)
{
    // 并行处理多个文档
    var tasks = contents.Select(async content =>
    {
        try
        {
            return await _aiService.ClassifyAsync(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分类失败: {Content}", content);
            return new ClassificationResult { Success = false, Error = ex.Message };
        }
    });

    var results = await Task.WhenAll(tasks);

    return new BatchClassificationResult
    {
        Results = results.ToList(),
        TotalCount = contents.Count,
        SuccessCount = results.Count(r => r.Success)
    };
}
```

## 错误处理

### 异常处理策略

#### 业务异常

```csharp
public class AttachCatalogueErrorCodes
{
    public const string CatalogueNameCannotBeEmpty = "AttachCatalogue:CatalogueNameCannotBeEmpty";
    public const string CatalogueNameAlreadyExists = "AttachCatalogue:CatalogueNameAlreadyExists";
    public const string CatalogueNotFound = "AttachCatalogue:CatalogueNotFound";
}

public class AttachCatalogueService
{
    public async Task<AttachCatalogueDto> GetAsync(Guid id)
    {
        var entity = await _repository.FindAsync(id);
        if (entity == null)
        {
            throw new BusinessException(AttachCatalogueErrorCodes.CatalogueNotFound);
        }

        return ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(entity);
    }
}
```

#### 全局异常处理

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is BusinessException businessException)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "业务错误",
                Detail = businessException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }

        return false;
    }
}
```

### 日志记录

#### 结构化日志

```csharp
public class AttachCatalogueService
{
    private readonly ILogger<AttachCatalogueService> _logger;

    public async Task<AttachCatalogueDto> CreateAsync(CreateAttachCatalogueDto input)
    {
        _logger.LogInformation("开始创建档案目录: {CatalogueName}", input.CatalogueName);

        try
        {
            var entity = await _manager.CreateAsync(input);
            await _repository.InsertAsync(entity);

            _logger.LogInformation("档案目录创建成功: {Id}, {CatalogueName}",
                entity.Id, entity.CatalogueName);

            return ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建档案目录失败: {CatalogueName}", input.CatalogueName);
            throw;
        }
    }
}
```

## 部署与运维

### 环境配置

#### 配置文件管理

```json
// appsettings.json (基础配置)
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}

// appsettings.Development.json (开发环境)
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=AttachmentDB_Dev;Username=postgres;Password=dev_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}

// appsettings.Production.json (生产环境)
{
  "ConnectionStrings": {
    "Default": "Host=prod-db;Database=AttachmentDB_Prod;Username=prod_user;Password=prod_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

#### 环境变量配置

```bash
# 数据库连接
export ConnectionStrings__Default="Host=db;Database=AttachmentDB;Username=user;Password=pass"

# AI服务配置
export DASHSCOPE_API_KEY="your_api_key"
export ALIYUN_WORKSPACE_ID="your_workspace_id"

# 缓存配置
export Redis__ConnectionString="redis:6379"

# 日志配置
export Serilog__MinimumLevel__Default="Information"
```

### 监控与告警

#### 健康检查

```csharp
public class HealthCheck : IHealthCheck
{
    private readonly IAttachCatalogueRepository _repository;

    public HealthCheck(IAttachCatalogueRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查数据库连接
            await _repository.GetCountAsync();

            return HealthCheckResult.Healthy("数据库连接正常");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("数据库连接异常", ex);
        }
    }
}
```

#### 性能监控

```csharp
public class AttachCatalogueService
{
    private readonly ILogger<AttachCatalogueService> _logger;
    private readonly IMetrics _metrics;

    public async Task<AttachCatalogueDto> GetAsync(Guid id)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _repository.GetAsync(id);
            var dto = ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(result);

            // 记录性能指标
            _metrics.Record("attach_catalogue_get_duration", stopwatch.ElapsedMilliseconds);
            _metrics.Increment("attach_catalogue_get_total");

            return dto;
        }
        catch (Exception ex)
        {
            _metrics.Increment("attach_catalogue_get_errors");
            throw;
        }
    }
}
```

## 总结

本开发指南涵盖了 Hx.Abp.Attachment 项目开发的核心内容，包括环境搭建、编码规范、开发流程、测试策略、性能优化、错误处理、部署运维等方面。

通过遵循这些指南，开发团队可以：

1. **提高代码质量**: 统一的编码规范和最佳实践
2. **提升开发效率**: 清晰的工作流程和工具支持
3. **保证系统性能**: 全面的性能优化策略
4. **确保系统稳定**: 完善的测试和监控体系
5. **简化部署运维**: 标准化的部署流程和配置管理

建议团队成员仔细阅读并遵循这些指南，在开发过程中不断优化和完善，为项目的成功交付提供有力保障。
