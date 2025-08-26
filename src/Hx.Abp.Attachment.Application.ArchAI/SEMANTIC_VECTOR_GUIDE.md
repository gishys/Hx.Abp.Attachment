# 语义向量服务使用指南

## 概述

语义向量服务已成功迁移到阿里云DashScope API，提供更稳定和高效的文本向量化服务。

## 主要改进

- ✅ **API迁移**: 从DeepSeek迁移到阿里云DashScope
- ✅ **批量处理**: 支持批量向量生成，提高效率
- ✅ **重试机制**: 智能重试，处理网络异常
- ✅ **参数验证**: 严格的输入验证
- ✅ **配置管理**: 集中化配置管理

## 环境配置

```bash
# 设置阿里云API密钥
DASHSCOPE_API_KEY=your_api_key_here
```

## 使用示例

### 1. 基本用法

```csharp
// 注入服务
public class MyService
{
    private readonly SemanticVectorService _vectorService;
    
    public MyService(SemanticVectorService vectorService)
    {
        _vectorService = vectorService;
    }
    
    // 生成单个向量
    public async Task<List<double>> GenerateVector(string text)
    {
        return await _vectorService.GenerateVectorAsync(text);
    }
    
    // 批量生成向量
    public async Task<List<List<double>>> GenerateVectors(List<string> texts)
    {
        return await _vectorService.GenerateVectorsAsync(texts);
    }
    
    // 计算相似度
    public async Task<double> CalculateSimilarity(string text1, string text2)
    {
        return await _vectorService.CalculateTextSimilarityAsync(text1, text2);
    }
}
```

### 2. 配置选项

```csharp
// 支持的模型
var models = ["text-embedding-v1", "text-embedding-v2", "text-embedding-v3", "text-embedding-v4"];

// 支持的维度
var dimensions = [512, 1024, 1536];

// 验证参数
bool isValidModel = SemanticVectorConfiguration.IsModelSupported("text-embedding-v4");
bool isValidDimension = SemanticVectorConfiguration.IsDimensionSupported(1024);
```

## 错误处理

```csharp
try
{
    var vector = await _vectorService.GenerateVectorAsync(text);
}
catch (UserFriendlyException ex)
{
    // 处理业务异常
    _logger.LogError("向量生成失败: {Message}", ex.Message);
}
```

## 最佳实践

1. **使用批量处理** - 提高效率
2. **实现缓存** - 避免重复计算
3. **参数验证** - 确保输入有效
4. **错误处理** - 优雅处理异常

## 迁移说明

- 环境变量: `DEEPSEEK_API_KEY` → `DASHSCOPE_API_KEY`
- API端点: 自动更新
- 模型名称: `deepseek-embedding` → `text-embedding-v4`
- 所有方法签名保持不变
