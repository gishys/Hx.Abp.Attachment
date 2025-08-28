# Hx.Abp.Attachment.Application.ArchAI

## 项目概述

AI 文本分析服务模块，支持多 AI 提供商（DeepSeek、阿里云）的灵活切换，提供文本摘要生成、关键词提取、语义向量计算等功能。

## 核心功能

-   **多 AI 提供商支持**：DeepSeek（高质量）、阿里云（快速响应）
-   **文本分析**：摘要生成、关键词提取、实体识别
-   **语义向量**：文本向量化、相似度计算
-   **文本分类**：基于样本的智能分类
-   **工厂模式**：灵活的 AI 服务切换

## 环境配置

### 必需环境变量

```bash
# DeepSeek配置
DEEPSEEK_API_KEY=your_deepseek_api_key_here

# 阿里云配置
ALIYUN_API_KEY=your_aliyun_api_key_here
ALIYUN_WORKSPACE_ID=ws_QTggmeAxxxxx

# 默认AI服务类型
DEFAULT_AI_SERVICE_TYPE=DeepSeek
```

## 快速使用

### 基本文本分析

```csharp
// 注入服务
private readonly TextAnalysisService _textAnalysisService;

// 分析文本
var input = new TextAnalysisInputDto
{
    Text = "这是一份贷款结清证明，证明借款人已按时还清所有贷款本息。",
    KeywordCount = 5,
    MaxSummaryLength = 200,
    GenerateSemanticVector = true,
    ExtractEntities = true
};

var result = await _textAnalysisService.AnalyzeTextAsync(input);
```

### 指定 AI 服务

```csharp
// 使用阿里云AI（快速）
input.PreferredAIService = AIServiceType.Aliyun;

// 使用DeepSeek（高质量）
input.PreferredAIService = AIServiceType.DeepSeek;
```

### 文本分类

```csharp
// 注入服务
private readonly TextClassificationService _textClassificationService;

// 提取分类特征
var input = new TextClassificationInputDto
{
    ClassificationName = "结清证明",
    TextSamples = new List<string> { "样本1", "样本2" },
    KeywordCount = 5,
    MaxSummaryLength = 200,
    GenerateSemanticVector = true
};

var result = await _textClassificationService.ExtractClassificationFeaturesAsync(input);
```

## 服务特点对比

| 特性       | DeepSeek             | 阿里云 AI          |
| ---------- | -------------------- | ------------------ |
| 响应速度   | 10-15 秒             | 2-5 秒             |
| 摘要质量   | 高质量，结构化       | 高质量，简洁       |
| 关键词提取 | 智能提取，上下文相关 | 准确提取，数量可控 |
| 成本       | 按 token 计费        | 按调用次数计费     |
| 适用场景   | 复杂文档，高质量要求 | 快速处理，简单文档 |

## 架构设计

### 核心组件

-   **AIServiceFactory**：AI 服务工厂，管理不同 AI 提供商
-   **ITextAnalysisProvider**：统一文本分析接口
-   **AliyunAIService**：阿里云 AI 服务实现（HTTP API）
-   **DeepSeekTextAnalysisProvider**：DeepSeek 服务适配器
-   **TextAnalysisService**：主文本分析服务
-   **TextClassificationService**：文本分类服务
-   **SemanticVectorService**：语义向量服务

### 设计模式

-   **工厂模式**：AIServiceFactory 管理 AI 服务实例
-   **策略模式**：通过接口实现 AI 服务策略切换
-   **适配器模式**：将不同 AI 服务 API 适配到统一接口
-   **依赖注入**：使用构造函数注入，避免服务定位器

## 性能优化

-   **并行处理**：摘要和关键词提取并行调用
-   **HTTP 连接复用**：使用 HttpClient 连接池
-   **智能缓存**：语义向量计算结果缓存
-   **错误重试**：网络异常自动重试机制

## 扩展性

### 添加新 AI 提供商

1. 实现`ITextAnalysisProvider`接口
2. 在`AIServiceType`枚举中添加新类型
3. 在`AIServiceFactory`中添加创建逻辑

```csharp
public class NewAITextAnalysisProvider : ITextAnalysisProvider
{
    public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
    {
        // 实现具体AI服务调用逻辑
    }
}
```

## 故障排除

### 常见错误

1. **缺少环境变量**

    - 错误：`缺少环境变量 DEEPSEEK_API_KEY`
    - 解决：设置正确的 API 密钥

2. **阿里云认证失败**

    - 错误：`缺少环境变量 ALIYUN_API_KEY`
    - 解决：设置正确的阿里云 API 密钥

3. **服务不可用**
    - 错误：`AI服务暂时不可用`
    - 解决：检查网络连接和 API 配额

## 测试接口

```bash
# 测试阿里云AI服务
POST /api/aliyun-ai-test/test

# 测试文本分析服务
POST /api/text-analysis/test

# 测试文本分类服务
POST /api/text-classification/test
```

## 最佳实践

1. **服务选择**：对速度要求高用阿里云 AI，对质量要求高用 DeepSeek
2. **参数设置**：关键词数量建议 5-10 个，摘要长度建议 100-300 字符
3. **批量处理**：大量文档考虑批量处理提高效率
4. **错误处理**：实现服务降级机制，添加重试和超时控制

## 依赖包

```xml
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
<PackageReference Include="Volo.Abp.Core" Version="8.1.1" />
<PackageReference Include="Volo.Abp.Ddd.Application" Version="8.1.1" />
```

## 更新日志

### v2.0.0 - HTTP API 重构

-   移除阿里云 SDK 依赖，改用 HTTP API 调用
-   提升响应速度，从 10-15 秒降低到 2-5 秒
-   简化项目依赖，提高稳定性

### v1.0.0 - 初始版本

-   支持 DeepSeek 和阿里云 AI 服务
-   实现文本分析、分类、语义向量功能
-   采用工厂模式和策略模式设计
