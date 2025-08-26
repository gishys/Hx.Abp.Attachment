# 语义向量服务使用指南

## 概述

语义向量服务基于阿里云DashScope API，提供文本向量化、相似度计算等功能。该服务已从DeepSeek API迁移到阿里云DashScope API，提供更好的性能和稳定性。

## 主要特性

- ✅ **阿里云DashScope API集成** - 使用最新的text-embedding-v4模型
- ✅ **批量处理优化** - 支持批量向量生成，提高效率
- ✅ **智能重试机制** - 自动处理网络异常和服务器错误
- ✅ **参数验证** - 严格的输入参数验证
- ✅ **配置化管理** - 集中管理API配置和参数
- ✅ **相似度计算** - 支持余弦相似度计算
- ✅ **相似度矩阵** - 支持多文本相似度矩阵计算

## 环境配置

### 1. 环境变量设置

```bash
# 设置阿里云DashScope API密钥
DASHSCOPE_API_KEY=your_api_key_here
```

### 2. 支持的模型和维度

| 模型 | 支持维度 | 说明 |
|------|----------|------|
| text-embedding-v1 | 512, 1024, 1536 | 基础版本 |
| text-embedding-v2 | 512, 1024, 1536 | 改进版本 |
| text-embedding-v3 | 512, 1024, 1536 | 优化版本 |
| text-embedding-v4 | 512, 1024, 1536 | **最新版本（推荐）** |

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
    
    // 生成单个文本向量
    public async Task<List<double>> GenerateVector(string text)
    {
        return await _vectorService.GenerateVectorAsync(text);
    }
}
```

### 2. 批量向量生成

```csharp
// 批量生成向量
public async Task<List<List<double>>> GenerateVectors(List<string> texts)
{
    return await _vectorService.GenerateVectorsAsync(texts);
}

// 使用自定义参数
public async Task<List<List<double>>> GenerateVectorsWithCustomParams(List<string> texts)
{
    return await _vectorService.GenerateVectorsAsync(
        texts, 
        model: "text-embedding-v4", 
        dimension: 1024
    );
}
```

### 3. 相似度计算

```csharp
// 计算两个文本的相似度
public async Task<double> CalculateSimilarity(string text1, string text2)
{
    return await _vectorService.CalculateTextSimilarityAsync(text1, text2);
}

// 计算相似度矩阵
public async Task<double[,]> CalculateSimilarityMatrix(List<string> texts)
{
    return await _vectorService.CalculateSimilarityMatrixAsync(texts);
}
```

### 4. 静态方法使用

```csharp
// 直接计算向量相似度
public double CalculateVectorSimilarity(List<double> vector1, List<double> vector2)
{
    return SemanticVectorService.CalculateCosineSimilarity(vector1, vector2);
}
```

## 配置选项

### 1. 默认配置

```csharp
// 在SemanticVectorConfiguration中查看所有配置
public static class SemanticVectorConfiguration
{
    public const string DefaultModel = "text-embedding-v4";
    public const int DefaultDimension = 1024;
    public const int MaxBatchSize = 10;
    public const int RequestTimeoutSeconds = 30;
    public const int MaxRetryCount = 3;
    public const int RetryDelayMs = 1000;
}
```

### 2. 参数验证

```csharp
// 验证模型是否支持
bool isSupported = SemanticVectorConfiguration.IsModelSupported("text-embedding-v4");

// 验证维度是否支持
bool isValidDimension = SemanticVectorConfiguration.IsDimensionSupported(1024);
```

## 错误处理

### 1. 常见错误类型

| 错误类型 | 原因 | 解决方案 |
|----------|------|----------|
| API认证失败 | API密钥无效或过期 | 检查DASHSCOPE_API_KEY环境变量 |
| 请求参数错误 | 模型或维度不支持 | 使用支持的模型和维度 |
| 服务器错误 | 阿里云服务暂时不可用 | 服务会自动重试 |
| 网络超时 | 网络连接问题 | 检查网络连接，服务会自动重试 |

### 2. 错误处理示例

```csharp
try
{
    var vector = await _vectorService.GenerateVectorAsync(text);
    // 处理成功结果
}
catch (UserFriendlyException ex)
{
    // 处理业务异常
    _logger.LogError("向量生成失败: {Message}", ex.Message);
}
catch (Exception ex)
{
    // 处理其他异常
    _logger.LogError(ex, "未知错误");
}
```

## 性能优化

### 1. 批量处理

- 使用`GenerateVectorsAsync`而不是多次调用`GenerateVectorAsync`
- 单次请求最多支持10个文本（MaxBatchSize）
- 服务会自动分批处理大量文本

### 2. 缓存策略

```csharp
// 建议在应用层实现缓存
private readonly IMemoryCache _cache;

public async Task<List<double>> GetCachedVector(string text)
{
    var cacheKey = $"vector_{text.GetHashCode()}";
    
    if (_cache.TryGetValue(cacheKey, out List<double> cachedVector))
    {
        return cachedVector;
    }
    
    var vector = await _vectorService.GenerateVectorAsync(text);
    _cache.Set(cacheKey, vector, TimeSpan.FromHours(24));
    
    return vector;
}
```

## 迁移说明

### 从DeepSeek API迁移

1. **环境变量更新**
   ```bash
   # 旧配置
   DEEPSEEK_API_KEY=xxx
   
   # 新配置
   DASHSCOPE_API_KEY=xxx
   ```

2. **API端点变更**
   - 旧端点：`https://api.deepseek.com/embeddings`
   - 新端点：`https://dashscope.aliyuncs.com/compatible-mode/v1/embeddings`

3. **模型名称变更**
   - 旧模型：`deepseek-embedding`
   - 新模型：`text-embedding-v4`（推荐）

### 兼容性

- ✅ 所有现有方法签名保持不变
- ✅ 返回值格式保持一致
- ✅ 错误处理机制增强
- ✅ 新增批量处理和相似度矩阵功能

## 最佳实践

### 1. 性能优化

```csharp
// ✅ 推荐：批量处理
var vectors = await _vectorService.GenerateVectorsAsync(texts);

// ❌ 不推荐：逐个处理
foreach (var text in texts)
{
    var vector = await _vectorService.GenerateVectorAsync(text);
}
```

### 2. 错误处理

```csharp
// ✅ 推荐：使用UserFriendlyException
try
{
    var vector = await _vectorService.GenerateVectorAsync(text);
}
catch (UserFriendlyException ex)
{
    // 处理业务异常
}

// ❌ 不推荐：捕获所有异常
try
{
    var vector = await _vectorService.GenerateVectorAsync(text);
}
catch (Exception ex)
{
    // 可能掩盖重要错误
}
```

### 3. 参数验证

```csharp
// ✅ 推荐：使用配置验证
if (!SemanticVectorConfiguration.IsModelSupported(model))
{
    throw new ArgumentException($"不支持的模型: {model}");
}

// ❌ 不推荐：硬编码验证
if (model != "text-embedding-v4")
{
    throw new ArgumentException("模型不支持");
}
```

## 监控和日志

### 1. 日志级别

- **Debug**: 详细的API调用信息
- **Warning**: 重试和临时错误
- **Error**: 严重错误和异常

### 2. 关键指标

- API调用成功率
- 平均响应时间
- 重试次数
- 错误类型分布

## 故障排除

### 1. 常见问题

**Q: API调用失败，提示认证错误**
A: 检查DASHSCOPE_API_KEY环境变量是否正确设置

**Q: 批量处理时部分文本失败**
A: 检查文本是否为空，服务会自动跳过空文本

**Q: 相似度计算结果异常**
A: 确保两个向量的维度相同

### 2. 调试技巧

```csharp
// 启用详细日志
_logger.LogDebug("生成向量，文本: {Text}", text);

// 检查向量维度
_logger.LogDebug("向量维度: {Dimension}", vector.Count);

// 验证相似度计算
_logger.LogDebug("相似度: {Similarity}", similarity);
```

## 更新日志

### v2.0.0 (当前版本)
- ✅ 迁移到阿里云DashScope API
- ✅ 新增批量处理优化
- ✅ 增强错误处理和重试机制
- ✅ 新增配置管理类
- ✅ 新增相似度矩阵功能
- ✅ 改进参数验证

### v1.0.0 (旧版本)
- 基于DeepSeek API
- 基础向量生成功能
- 简单的相似度计算
