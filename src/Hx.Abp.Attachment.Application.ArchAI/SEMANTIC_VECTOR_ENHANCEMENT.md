# 语义向量生成功能增强

## 概述

为 `AliyunAIService` 和相关服务添加了完整的语义向量生成功能，支持文本的向量化表示，可用于文档相似度计算、语义搜索等高级 AI 应用场景。

## 新增功能

### 1. AliyunAIService 向量生成

**新增方法：**

```csharp
public async Task<List<double>?> GenerateSemanticVectorAsync(string summary, List<string> keywords)
```

**功能：**

-   将摘要和关键词组合生成语义向量
-   使用 SemanticVectorService 进行向量化处理
-   异常安全，失败时返回 null 并记录警告日志

### 2. 综合分析向量支持

**增强方法：**

```csharp
public async Task<ComprehensiveAnalysisResult> AnalyzeComprehensivelyAsync(
    string content,
    List<string> categoryOptions,
    int maxSummaryLength = 500,
    int keywordCount = 5,
    bool generateSemanticVector = false) // 新增参数
```

**功能：**

-   可选择性生成语义向量
-   向量结果包含在 `ComprehensiveAnalysisResult.SemanticVector` 中
-   不影响现有 API 调用的兼容性

### 3. 文档分析服务向量支持

**增强类：**

-   `AliyunDocumentAnalysisService.AnalyzeDocumentAsync`

**功能：**

-   根据 `TextAnalysisInputDto.GenerateSemanticVector` 参数决定是否生成向量
-   向量结果包含在 `TextAnalysisDto.SemanticVector` 中
-   完全向后兼容

### 4. 数据模型更新

**ComprehensiveAnalysisResult 新增字段：**

```csharp
[JsonPropertyName("semantic_vector")]
public List<double>? SemanticVector { get; set; }
```

## 使用示例

### 直接向量生成

```csharp
var vector = await aliyunAIService.GenerateSemanticVectorAsync(
    summary: "房产证明文件，证明张三的房产信息",
    keywords: new List<string> { "房产证", "张三", "不动产" }
);
```

### 综合分析带向量

```csharp
var result = await aliyunAIService.AnalyzeComprehensivelyAsync(
    content: documentContent,
    categoryOptions: categories,
    generateSemanticVector: true  // 启用向量生成
);
// 使用 result.SemanticVector
```

### 文档分析带向量

```csharp
var input = new TextAnalysisInputDto
{
    Text = content,
    GenerateSemanticVector = true  // 启用向量生成
};
var result = await documentAnalysisService.AnalyzeDocumentAsync(input);
// 使用 result.SemanticVector
```

## 应用场景

### 1. 文档相似度计算

```csharp
var similarity = CalculateCosineSimilarity(vector1, vector2);
```

### 2. 语义搜索

-   将查询文本向量化
-   与文档库中的向量进行相似度比较
-   返回最相关的文档

### 3. 文档聚类

-   批量处理文档生成向量
-   使用聚类算法对相似文档分组
-   自动化文档分类

### 4. 智能推荐

-   基于用户历史文档的向量
-   推荐相似类型的文档
-   个性化内容发现

## 性能特性

### 优势

-   **可选性**: 向量生成为可选功能，不影响现有性能
-   **异步处理**: 完全异步，不阻塞主流程
-   **错误容错**: 向量生成失败不影响其他功能
-   **内存效率**: 仅在需要时生成向量

### 建议

-   仅在需要语义搜索/相似度计算时启用向量生成
-   大批量处理时考虑分批处理避免内存压力
-   可将生成的向量持久化存储以提高查询效率

## 依赖要求

-   **SemanticVectorService**: 语义向量生成服务
-   **适当的向量化模型**: 确保向量质量和维度一致性
-   **足够的计算资源**: 向量生成需要额外的计算开销

## 兼容性

-   ✅ **完全向后兼容**: 现有 API 调用无需修改
-   ✅ **渐进式增强**: 可选择性启用新功能
-   ✅ **类型安全**: 向量字段为可空类型，避免空值异常

## 测试建议

1. **功能测试**: 验证向量生成的正确性
2. **性能测试**: 评估向量生成对整体性能的影响
3. **相似度测试**: 验证生成向量的语义表示质量
4. **边界测试**: 测试异常情况下的容错处理

## 扩展可能

-   支持不同的向量化模型选择
-   向量维度自定义配置
-   向量压缩和存储优化
-   实时向量更新和缓存机制
