# 文本分析功能完整指南

## 🎯 功能概述

本项目实现了完整的文本分析和文本分类功能，支持：

-   **单个文档分析** - 提取摘要、关键词、实体信息
-   **文本分类特征提取** - 分析多个样本，提取通用特征
-   **语义向量生成** - 生成文本向量，计算相似度
-   **HTTP API 接口** - 完整的 RESTful API 支持

## 📁 项目结构

```
src/Hx.Abp.Attachment.Application.ArchAI/
├── Hx/Abp/Attachment/Application/ArchAI/
│   ├── TextAnalysisService.cs              # 文本分析服务
│   ├── TextClassificationService.cs        # 文本分类服务
│   ├── SemanticVectorService.cs            # 语义向量服务
│   ├── ArchiveAIAppService.cs              # 应用服务实现
│   └── HxAbpAttachmentApplicationArchAIModule.cs  # 模块配置
├── Contracts/
│   ├── IArchiveAIAppService.cs             # 应用服务接口
│   ├── TextAnalysisDto.cs                  # 文本分析DTO
│   ├── TextAnalysisInputDto.cs             # 文本分析输入DTO
│   └── TextClassificationInputDto.cs       # 文本分类输入DTO
└── README.md                               # 本文档

src/Hx.Abp.Attachment.Api/
├── Controllers/
│   └── ArchiveAIController.cs              # API控制器
└── TextAnalysis.http                       # HTTP测试文件
```

## 🌐 API 接口

### 基础 URL

```
http://localhost:5000/api/app/attachmentai
```

### 接口列表

| 接口             | 方法 | 路径                               | 描述             |
| ---------------- | ---- | ---------------------------------- | ---------------- |
| 单个文档分析     | POST | `/analyze-text`                    | 分析单个文档内容 |
| 文本分类特征提取 | POST | `/extract-classification-features` | 提取文本分类特征 |
| OCR 全文识别     | GET  | `/ocrfulltext`                     | OCR 文字识别     |

## 💻 使用示例

### 1. 单个文档分析

#### HTTP 请求

```bash
curl -X POST "http://localhost:5000/api/app/attachmentai/analyze-text" \
  -H "Content-Type: application/json" \
  -d '{
    "text": "准格尔旗信力机械工程有限责任公司办理的1300万元贷款已于2023年5月31日结清全部本金及利息。",
    "keywordCount": 5,
    "maxSummaryLength": 200,
    "analysisType": 1,
    "generateSemanticVector": true,
    "extractEntities": true
  }'
```

#### C# 服务端使用

```csharp
public class MyService
{
    private readonly IArchiveAIAppService _archiveAIAppService;

    public MyService(IArchiveAIAppService archiveAIAppService)
    {
        _archiveAIAppService = archiveAIAppService;
    }

    public async Task<TextAnalysisDto> AnalyzeDocumentAsync(string text)
    {
        var input = new TextAnalysisInputDto
        {
            Text = text,
            KeywordCount = 5,
            MaxSummaryLength = 200,
            AnalysisType = TextAnalysisType.SingleDocument,
            GenerateSemanticVector = true,
            ExtractEntities = true
        };

        return await _archiveAIAppService.AnalyzeTextAsync(input);
    }
}
```

#### JavaScript 客户端使用

```javascript
async function analyzeText(text) {
    const response = await fetch('/api/app/attachmentai/analyze-text', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            text: text,
            keywordCount: 5,
            maxSummaryLength: 200,
            analysisType: 1,
            generateSemanticVector: true,
            extractEntities: true,
        }),
    });

    return await response.json();
}
```

#### 响应示例

```json
{
    "summary": "准格尔旗信力机械工程有限责任公司办理的1300万元贷款已于2023年5月31日结清全部本金及利息。",
    "keywords": [
        "准格尔旗信力机械工程有限责任公司",
        "1300万元",
        "贷款结清",
        "2023年5月31日",
        "准格尔旗农村信用合作联社"
    ],
    "confidence": 0.92,
    "documentType": "结清证明",
    "businessDomain": "金融服务",
    "semanticVector": [0.1, 0.2, 0.3, 0.4, 0.5],
    "entities": [
        {
            "name": "准格尔旗信力机械工程有限责任公司",
            "type": "Organization",
            "value": "准格尔旗信力机械工程有限责任公司",
            "confidence": 0.8
        }
    ],
    "analysisTime": "2024-01-15T10:30:00Z",
    "metadata": {
        "textLength": 120,
        "processingTimeMs": 1500,
        "model": "deepseek-chat",
        "apiUsage": {
            "promptTokens": 800,
            "completionTokens": 300,
            "totalTokens": 1100
        }
    }
}
```

### 2. 文本分类特征提取

#### HTTP 请求

```bash
curl -X POST "http://localhost:5000/api/app/attachmentai/extract-classification-features" \
  -H "Content-Type: application/json" \
  -d '{
    "classificationName": "结清证明",
    "textSamples": [
      "准格尔旗信力机械工程有限责任公司办理的1300万元贷款已于2023年5月31日结清全部本金及利息。",
      "内蒙古某建筑公司在我行办理的500万元贷款已于2024年1月15日结清全部本金及利息。",
      "某科技公司在农村信用社办理的200万元贷款已于2023年12月31日结清全部本金及利息。"
    ],
    "keywordCount": 5,
    "maxSummaryLength": 200,
    "generateSemanticVector": true
  }'
```

#### C# 服务端使用

```csharp
public async Task<TextAnalysisDto> CreateTemplateAsync(string classificationName, List<string> samples)
{
    var input = new TextClassificationInputDto
    {
        ClassificationName = classificationName,
        TextSamples = samples,
        KeywordCount = 5,
        MaxSummaryLength = 200,
        GenerateSemanticVector = true
    };

    return await _archiveAIAppService.ExtractClassificationFeaturesAsync(input);
}
```

#### JavaScript 客户端使用

```javascript
async function extractClassificationFeatures(classificationName, textSamples) {
    const response = await fetch(
        '/api/app/attachmentai/extract-classification-features',
        {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                classificationName: classificationName,
                textSamples: textSamples,
                keywordCount: 5,
                maxSummaryLength: 200,
                generateSemanticVector: true,
            }),
        }
    );

    return await response.json();
}
```

#### 响应示例

```json
{
    "summary": "结清证明类文档的通用特征：金融机构出具的证明文件，确认借款人在该机构办理的贷款已全部结清本金及利息，包含借款人信息、贷款金额、结清日期等关键信息。",
    "keywords": ["结清证明", "贷款结清", "金融机构", "本金利息", "结清日期"],
    "confidence": 0.95,
    "documentType": "结清证明",
    "businessDomain": "金融服务",
    "semanticVector": [0.1, 0.2, 0.3, 0.4, 0.5],
    "entities": [],
    "analysisTime": "2024-01-15T10:30:00Z",
    "metadata": {
        "textLength": 450,
        "processingTimeMs": 2000,
        "model": "deepseek-chat",
        "apiUsage": {
            "promptTokens": 1200,
            "completionTokens": 400,
            "totalTokens": 1600
        }
    }
}
```

## ⚙️ 配置要求

### 环境变量

```bash
# DeepSeek API配置（必需）
DEEPSEEK_API_KEY=your_api_key_here

# 阿里云OCR配置（可选，用于OCR功能）
ALIBABA_CLOUD_ACCESS_KEY_ID=your_access_key_id
ALIBABA_CLOUD_ACCESS_KEY_SECRET=your_access_key_secret
```

### 依赖注入配置

所有服务已在 `HxAbpAttachmentApplicationArchAIModule` 中自动注册：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.AddHttpClient();
    context.Services.AddScoped<TextAnalysisService>();
    context.Services.AddScoped<TextClassificationService>();
    context.Services.AddScoped<SemanticVectorService>();
}
```

## 🧪 测试

### HTTP 测试文件

使用 `src/Hx.Abp.Attachment.Api/TextAnalysis.http` 文件进行 API 测试：

1. 在 VS Code 中安装 REST Client 扩展
2. 打开 `.http` 文件
3. 设置环境变量 `@baseUrl = http://localhost:5000`
4. 点击"Send Request"按钮测试各个接口

### 测试用例

-   单个文档分析测试
-   文本分类特征提取测试
-   不同文档类型测试（合同、发票、证明等）
-   错误处理测试

## 🚀 部署指南

### 1. 环境准备

```bash
# 安装.NET 8.0 SDK
# 配置环境变量
# 准备数据库连接字符串
```

### 2. 构建和运行

```bash
# 构建项目
dotnet build

# 运行API服务
dotnet run --project src/Hx.Abp.Attachment.Api
```

### 3. 验证部署

```bash
# 测试API健康状态
curl http://localhost:5000/api/app/attachmentai/analyze-text

# 查看Swagger文档
http://localhost:5000/swagger
```

## 📊 参数说明

### TextAnalysisInputDto 参数

| 参数                   | 类型                       | 必填 | 默认值         | 说明                             |
| ---------------------- | -------------------------- | ---- | -------------- | -------------------------------- |
| text                   | string                     | 是   | -              | 要分析的文本内容                 |
| keywordCount           | int                        | 否   | 5              | 关键词数量，范围 1-20            |
| maxSummaryLength       | int                        | 否   | 200            | 摘要最大长度，范围 50-500        |
| analysisType           | TextAnalysisType           | 否   | SingleDocument | 分析类型：1=单个文档，2=文本分类 |
| generateSemanticVector | bool                       | 否   | true           | 是否生成语义向量                 |
| extractEntities        | bool                       | 否   | true           | 是否提取实体信息                 |
| context                | Dictionary<string, object> | 否   | null           | 业务上下文信息                   |

### TextClassificationInputDto 参数

| 参数                   | 类型                       | 必填 | 默认值 | 说明                      |
| ---------------------- | -------------------------- | ---- | ------ | ------------------------- |
| classificationName     | string                     | 是   | -      | 分类名称，最大 100 字符   |
| textSamples            | List<string>               | 是   | -      | 文本样本列表，1-50 个样本 |
| keywordCount           | int                        | 否   | 5      | 关键词数量，范围 1-20     |
| maxSummaryLength       | int                        | 否   | 200    | 摘要最大长度，范围 50-500 |
| generateSemanticVector | bool                       | 否   | true   | 是否生成语义向量          |
| context                | Dictionary<string, object> | 否   | null   | 业务上下文信息            |

## 🎯 最佳实践

### 1. 使用建议

-   **单个文档分析**：用于分析具体文档内容，提取具体信息
-   **文本分类分析**：用于建立文档分类模板，提取通用特征
-   **样本选择**：选择具有代表性的样本，建议 3-10 个
-   **参数调优**：根据文档复杂度调整关键词数量和摘要长度

### 2. 错误处理

-   实现适当的重试机制
-   提供友好的错误信息
-   监控 API 调用性能和错误率

### 3. 性能优化

-   缓存常用结果
-   批量处理文档
-   异步处理大量数据
-   合理控制 API 调用频率

### 4. 安全考虑

-   使用环境变量存储敏感信息
-   验证输入参数的有效性
-   实现适当的身份验证和授权
-   定期轮换 API 密钥

## 📈 监控和维护

### 1. 日志监控

-   监控 API 调用日志
-   跟踪错误和异常

### 2. 性能监控

-   监控响应时间
-   跟踪 API 使用量

### 3. 定期维护

-   更新 API 密钥
-   优化提示词
-   更新模型版本

## ✅ 完成状态

-   [x] 文本分析服务实现
-   [x] 文本分类服务实现
-   [x] 语义向量服务实现
-   [x] API 接口暴露
-   [x] 依赖注入配置
-   [x] 错误处理
-   [x] 参数验证
-   [x] 文档和示例
-   [x] 测试用例
-   [x] 部署指南

所有功能已完整实现并可以投入使用！
