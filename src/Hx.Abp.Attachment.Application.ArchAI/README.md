# AI 智能分析服务架构说明

## 概述

本项目基于阿里云 OpenNLU 服务，提供智能文档分析和分类推荐能力，适用于附件管理系统的智能查询和分类推荐场景。

## 架构设计

### 核心服务接口

#### 1. IDocumentAnalysisService - 文档智能分析服务

**适用场景**: AttachCatalogue 文档内容分析
**主要功能**:

-   文档摘要生成
-   关键词提取
-   文档内容智能分析

```csharp
// 获取服务实例
var documentAnalysisService = aiServiceFactory.GetDocumentAnalysisService();

// 分析文档
var result = await documentAnalysisService.AnalyzeDocumentAsync(input);

// 单独生成摘要
var summary = await documentAnalysisService.GenerateDocumentSummaryAsync(content, 500);

// 单独提取关键词
var keywords = await documentAnalysisService.ExtractDocumentKeywordsAsync(content, 5);
```

#### 2. IIntelligentClassificationService - 智能分类推荐服务

**适用场景**: AttachCatalogueTemplate 分类推荐
**主要功能**:

-   智能分类推荐
-   批量分类推荐
-   分类置信度评估

```csharp
// 获取服务实例
var classificationService = aiServiceFactory.GetIntelligentClassificationService();

// 推荐单个文档分类
var result = await classificationService.RecommendDocumentCategoryAsync(content, categoryOptions);

// 批量推荐分类
var results = await classificationService.BatchRecommendCategoriesAsync(documents, categoryOptions);
```

#### 3. IFullStackAnalysisService - 全栈智能分析服务

**适用场景**: 需要同时进行文档分析和分类推荐的场景
**主要功能**:

-   全栈文档分析
-   批量全栈分析
-   综合分析结果

```csharp
// 获取服务实例
var fullStackService = aiServiceFactory.GetFullStackAnalysisService();

// 全栈分析单个文档
var result = await fullStackService.AnalyzeDocumentComprehensivelyAsync(
    content, categoryOptions, 500, 5);

// 批量全栈分析
var results = await fullStackService.BatchAnalyzeComprehensivelyAsync(
    documents, categoryOptions, 500, 5);
```

### 业务场景枚举

```csharp
public enum BusinessScenario
{
    /// <summary>
    /// 文档分析场景 - 适用于AttachCatalogue
    /// </summary>
    DocumentAnalysis,

    /// <summary>
    /// 分类推荐场景 - 适用于AttachCatalogueTemplate
    /// </summary>
    ClassificationRecommendation,

    /// <summary>
    /// 全栈分析场景 - 同时支持文档分析和分类推荐
    /// </summary>
    FullStackAnalysis
}
```

### 便捷访问方法

```csharp
// 根据业务场景获取合适的服务
var service = aiServiceFactory.GetAnalysisServiceByScenario(BusinessScenario.DocumentAnalysis);

// 获取默认服务（推荐使用）
var defaultService = aiServiceFactory.GetDefaultDocumentAnalysisService();
```

## 使用示例

### 场景 1: AttachCatalogue 智能查询

```csharp
public class AttachCatalogueService
{
    private readonly AIServiceFactory _aiServiceFactory;

    public AttachCatalogueService(AIServiceFactory aiServiceFactory)
    {
        _aiServiceFactory = aiServiceFactory;
    }

    public async Task<TextAnalysisDto> AnalyzeAttachmentContentAsync(string content)
    {
        // 使用文档分析服务
        var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();

        var input = new TextAnalysisInputDto
        {
            Text = content,
            MaxSummaryLength = 500,
            KeywordCount = 5
        };

        return await documentAnalysisService.AnalyzeDocumentAsync(input);
    }
}
```

### 场景 2: AttachCatalogueTemplate 智能推荐

```csharp
public class AttachCatalogueTemplateService
{
    private readonly AIServiceFactory _aiServiceFactory;

    public AttachCatalogueTemplateService(AIServiceFactory aiServiceFactory)
    {
        _aiServiceFactory = aiServiceFactory;
    }

    public async Task<ClassificationResult> RecommendTemplateCategoryAsync(string content, List<string> availableCategories)
    {
        // 使用智能分类推荐服务
        var classificationService = _aiServiceFactory.GetIntelligentClassificationService();

        return await classificationService.RecommendDocumentCategoryAsync(content, availableCategories);
    }

    public async Task<List<ClassificationResult>> BatchRecommendCategoriesAsync(List<string> documents, List<string> availableCategories)
    {
        // 批量分类推荐
        var classificationService = _aiServiceFactory.GetIntelligentClassificationService();

        return await classificationService.BatchRecommendCategoriesAsync(documents, availableCategories);
    }
}
```

### 场景 3: 综合分析场景

```csharp
public class ComprehensiveAnalysisService
{
    private readonly AIServiceFactory _aiServiceFactory;

    public ComprehensiveAnalysisService(AIServiceFactory aiServiceFactory)
    {
        _aiServiceFactory = aiServiceFactory;
    }

    public async Task<ComprehensiveAnalysisResult> AnalyzeComprehensivelyAsync(string content, List<string> categories)
    {
        // 使用全栈智能分析服务
        var fullStackService = _aiServiceFactory.GetFullStackAnalysisService();

        return await fullStackService.AnalyzeDocumentComprehensivelyAsync(
            content, categories, 500, 5);
    }
}
```

## 阿里云 OpenNLU 服务配置

### 环境变量配置

```bash
# 阿里云API密钥
DASHSCOPE_API_KEY=your_api_key_here

# 阿里云工作空间ID
ALIYUN_WORKSPACE_ID=your_workspace_id_here
```

### API 参数说明

-   **Model**: `opennlu-v1` (开箱即用的文本理解大模型)
-   **Task**:
    -   `extraction` - 信息抽取任务
    -   `classification` - 文本分类任务
-   **Labels**: 根据任务类型设置相应的标签

## 错误处理

所有服务都实现了优雅的错误处理：

1. **AI 服务调用失败**: 返回默认结果，不中断业务流程
2. **网络异常**: 自动降级到本地处理
3. **API 限流**: 返回缓存结果或默认值

## 性能优化

1. **并行处理**: 摘要生成和关键词提取并行执行
2. **批量处理**: 支持批量文档分析，提高处理效率
3. **缓存机制**: 可配置的结果缓存，减少重复 API 调用

## 扩展性

### 添加新的 AI 服务提供商

```csharp
public class NewAIProvider : IDocumentAnalysisService
{
    public string ServiceName => "新AI服务提供商";
    public string ServiceDescription => "新AI服务的描述";

    public async Task<TextAnalysisDto> AnalyzeDocumentAsync(TextAnalysisInputDto input)
    {
        // 实现新的AI服务逻辑
    }

    // 实现其他接口方法...
}
```

### 自定义业务场景

```csharp
public enum CustomBusinessScenario
{
    DocumentAnalysis,
    ClassificationRecommendation,
    FullStackAnalysis,
    CustomScenario // 新增自定义场景
}
```

## 最佳实践

1. **服务选择**: 根据具体业务场景选择最合适的服务
2. **错误处理**: 始终处理可能的异常情况
3. **性能考虑**: 对于大量文档，使用批量处理方法
4. **配置管理**: 通过配置文件管理 AI 服务参数
5. **监控日志**: 记录 AI 服务调用情况和性能指标

## 兼容性说明

为了保持向后兼容，旧版本的接口仍然可用，但已标记为过时：

```csharp
[Obsolete("请使用 IDocumentAnalysisService 替代")]
public interface ITextAnalysisProvider
{
    Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input);
}
```

建议逐步迁移到新的接口设计。
