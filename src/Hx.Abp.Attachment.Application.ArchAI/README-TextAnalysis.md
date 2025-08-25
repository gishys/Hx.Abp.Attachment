# 文本分析服务

## 概述

基于 DeepSeek AI 的智能文本分析服务，用于生成文本摘要和关键词提取。该服务主要用于模板智能匹配，通过分析文本内容来判断属于哪个分类。

## 功能特性

-   **智能摘要生成**：提取文本核心信息，生成简洁准确的摘要
-   **关键词提取**：识别文本中的重要关键词，支持自定义提取数量
-   **结构化输出**：返回 JSON 格式的结构化数据，便于后续处理
-   **高精度分析**：基于先进的 AI 模型，确保分析结果的准确性
-   **错误处理**：完善的异常处理机制，提供友好的错误信息

## 实现的功能

### 1. 核心服务类

-   **TextAnalysisService**: 主要的文本分析服务类
    -   集成 DeepSeek AI API
    -   支持结构化 JSON 输出
    -   完善的错误处理机制
    -   备用解析方案

### 2. 数据传输对象 (DTO)

-   **TextAnalysisDto**: 分析结果输出

    -   Summary: 文本摘要
    -   Keywords: 关键词列表
    -   Confidence: 置信度
    -   AnalysisTime: 分析时间戳

-   **TextAnalysisInputDto**: 分析输入参数
    -   Text: 待分析文本
    -   KeywordCount: 关键词数量
    -   MaxSummaryLength: 摘要最大长度

### 3. 接口扩展

-   **IArchiveAIAppService**: 添加了 AnalyzeTextAsync 方法
-   **ArchiveAIAppService**: 实现了新的分析方法

## API 接口

### 文本分析接口

**接口地址**：`POST /api/text-analysis/analyze`

**请求参数**：

```json
{
    "text": "待分析的文本内容",
    "keywordCount": 5,
    "maxSummaryLength": 200
}
```

**参数说明**：

-   `text`：必填，待分析的文本内容（10-10000 字符）
-   `keywordCount`：可选，关键词提取数量（1-20，默认 5）
-   `maxSummaryLength`：可选，摘要最大长度（50-500 字符，默认 200）

**响应结果**：

```json
{
    "summary": "文本摘要内容",
    "keywords": ["关键词1", "关键词2", "关键词3"],
    "confidence": 0.95,
    "analysisTime": "2024-01-01T12:00:00"
}
```

## 使用示例

### C# 代码示例

```csharp
// 注入服务
public class MyService
{
    private readonly IArchiveAIAppService _archiveAIAppService;

    public MyService(IArchiveAIAppService archiveAIAppService)
    {
        _archiveAIAppService = archiveAIAppService;
    }

    public async Task<TextAnalysisDto> AnalyzeDocumentText(string text)
    {
        var input = new TextAnalysisInputDto
        {
            Text = text,
            KeywordCount = 5,
            MaxSummaryLength = 200
        };

        return await _archiveAIAppService.AnalyzeTextAsync(input);
    }
}
```

### HTTP 请求示例

```bash
curl -X POST "https://your-api-domain/api/text-analysis/analyze" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "人工智能技术在政务服务领域的应用日益广泛，通过自然语言处理、机器学习等技术，可以为用户提供智能化的政务咨询、办事指南、材料审核等服务。",
    "keywordCount": 5,
    "maxSummaryLength": 200
  }'
```

### API 测试示例

```http
### 分析文本并生成摘要和关键词
POST {{baseUrl}}/api/text-analysis/analyze
Content-Type: application/json

{
  "text": "人工智能技术在政务服务领域的应用日益广泛，通过自然语言处理、机器学习等技术，可以为用户提供智能化的政务咨询、办事指南、材料审核等服务。智能政务助手能够理解用户的自然语言表达，准确识别办事需求，并提供个性化的服务建议。",
  "keywordCount": 5,
  "maxSummaryLength": 200
}

### 测试短文本
POST {{baseUrl}}/api/text-analysis/analyze
Content-Type: application/json

{
  "text": "办理社保需要身份证、户口本、工作证明等材料。",
  "keywordCount": 3,
  "maxSummaryLength": 100
}
```

## 环境配置

### 必需的环境变量

```bash
# DeepSeek API密钥
DEEPSEEK_API_KEY=your_deepseek_api_key_here
```

### 依赖服务

-   DeepSeek AI API
-   HttpClient（已通过依赖注入配置）

## 技术实现亮点

1. **结构化输出**: 使用 JSON 格式确保结果可解析
2. **错误处理**: 多层错误处理机制，包括 API 错误、解析错误等
3. **备用方案**: 当 AI 解析失败时，提供基础的关键词提取
4. **依赖注入**: 使用 ABP 框架的依赖注入模式
5. **日志记录**: 完整的日志记录便于调试和监控
6. **参数验证**: 输入参数验证确保数据质量

## 文件结构

```
src/Hx.Abp.Attachment.Application.ArchAI/
├── Hx/Abp/Attachment/Application/ArchAI/
│   ├── TextAnalysisService.cs          # 核心分析服务
│   ├── ArchiveAIAppService.cs          # 更新的应用服务
│   └── HxAbpAttachmentApplicationArchAIModule.cs  # 模块配置
└── README-TextAnalysis.md              # 使用文档

src/Hx.Abp.Attachment.Application.ArchAI.Contracts/
└── Hx/Abp/Attachment/Application/ArchAI/Contracts/
    ├── TextAnalysisDto.cs              # 分析结果DTO
    └── IArchiveAIAppService.cs         # 更新的接口

src/Hx.Abp.Attachment.Api/
└── Controllers/
    ├── TextAnalysisController.cs       # API控制器
    └── TextAnalysis.http               # API测试文件
```

## 错误处理

服务包含完善的错误处理机制：

1. **API 密钥缺失**：提示配置环境变量
2. **网络错误**：返回友好的错误信息
3. **API 调用失败**：记录详细错误日志
4. **解析失败**：使用备用解析方案

## 性能优化

-   使用 HttpClient 工厂模式，支持连接池复用
-   合理的超时设置和重试机制
-   结构化的日志记录，便于问题排查
-   备用解析方案，确保服务可用性

## 扩展功能

该服务设计为可扩展的架构，可以轻松添加以下功能：

1. **缓存机制**：对相同文本的分析结果进行缓存
2. **批量处理**：支持批量文本分析
3. **自定义模型**：支持切换不同的 AI 模型
4. **结果存储**：将分析结果存储到数据库

## 注意事项

1. 确保 DeepSeek API 密钥有效且有足够的配额
2. 文本长度限制为 10-10000 字符
3. 关键词数量建议不超过 20 个，以保证质量
4. 摘要长度建议在 50-500 字符之间
5. 服务为同步调用，建议在异步方法中使用

## 总结

成功实现了基于 DeepSeek AI 的文本分析服务，满足了所有要求：

-   ✅ 创建了公共方法用于文本摘要和关键词提取
-   ✅ 支持模板智能匹配的后续应用
-   ✅ 实现了非流式的一次性返回
-   ✅ 优化了提示词设计
-   ✅ 遵循了代码最佳实践

该实现具有良好的可扩展性和维护性，为后续的模板智能匹配功能提供了坚实的基础。
