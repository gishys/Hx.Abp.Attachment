# 现有搜索和推荐方法优化总结

## 概述

基于用户要求"依据上述修改，请同步修改领域层、服务层、接口等相关内容：模板实体添加 Description、Tags 字段...上述查找排序等方法（以及仓储中其他牵扯语义查询、全文（字面）检索）需要修改并优化"，我们已经完成了对现有搜索和推荐方法的全面优化，让它们能够利用新添加的 `Description` 和 `Tags` 字段以及混合检索能力。

## 已优化的方法

### 1. GetIntelligentRecommendationsAsync 方法

**优化内容：**

-   更新了 SQL 查询逻辑，加入了新的字段匹配
-   模板名称匹配（权重最高：1.2）
-   描述字段匹配（权重较高：1.0）
-   标签匹配（权重中等：0.8）
-   规则表达式匹配（权重较低：0.6）
-   基础文本匹配（权重最低：0.4）

**SQL 查询优化：**

```sql
-- 标签匹配（权重中等）
CASE WHEN t."TAGS" IS NOT NULL AND t."TAGS" != '[]'
     THEN (
         SELECT COALESCE(MAX(similarity(tag, @query), 0) * 0.8
         FROM jsonb_array_elements_text(t."TAGS") AS tag
     )
 ELSE 0 END
```

**降级方案优化：**

-   在内存中进行混合检索计算
-   综合利用所有新字段进行评分
-   标签匹配逻辑优化

### 2. GetRecommendationsByBusinessAsync 方法

**优化内容：**

-   动态关键词匹配权重重新分配：
    -   模板名称：25%
    -   描述字段：25%
    -   规则表达式：25%
    -   标签匹配：25%
-   文件类型匹配扩展到新字段
-   基础文本相似度计算综合多个字段

**SQL 查询优化：**

```sql
-- 标签匹配（权重中等）
CASE WHEN t."TAGS" IS NOT NULL AND t."TAGS" != '[]'
     THEN (
         SELECT COALESCE(MAX(similarity(tag, @businessQuery), 0) * 0.25
         FROM jsonb_array_elements_text(t."TAGS") AS tag
     )
 ELSE 0 END

-- 文件类型匹配扩展到新字段
CASE WHEN t."TAGS" @> ANY(@fileTypeJsonArray) THEN 0.2 ELSE 0 END
```

### 3. GetFallbackRecommendationsAsync 方法

**优化内容：**

-   模板名称匹配（权重：0.4）
-   描述字段匹配（权重：0.3）
-   标签匹配（权重：0.2）
-   规则表达式匹配（权重：0.1）
-   文件类型匹配扩展到新字段

**评分逻辑优化：**

```csharp
// 标签匹配（权重中等）
if (template.Tags != null && template.Tags.Count > 0)
{
    foreach (var tag in template.Tags)
    {
        if (tag.Contains(businessDescription, StringComparison.OrdinalIgnoreCase))
        {
            score += 0.2;
            break; // 只计算一次标签匹配
        }
    }
}
```

### 4. ExtractDynamicKeywordsAsync 方法

**优化内容：**

-   从模板名称、描述、规则表达式提取关键词
-   新增从模板标签中提取关键词
-   关键词来源更加丰富和准确

**实现优化：**

```csharp
// 从现有模板中提取高频关键词（利用新字段）
var templateKeywords = await dbContext.Set<AttachCatalogueTemplate>()
    .Where(t => !t.IsDeleted)
    .SelectMany(t => new[]
    {
        t.TemplateName,
        t.Description,
        t.RuleExpression
    }.Where(x => !string.IsNullOrEmpty(x)))
    .ToListAsync();

// 从模板标签中提取关键词
var tagKeywords = await dbContext.Set<AttachCatalogueTemplate>()
    .Where(t => !t.IsDeleted && t.Tags != null && t.Tags.Count > 0)
    .SelectMany(t => t.Tags)
    .ToListAsync();
```

## 新增的混合检索方法

### 1. SearchTemplatesHybridAsync

-   结合字面检索和语义检索
-   支持权重配置
-   综合利用所有字段

### 2. SearchTemplatesByTextAsync

-   基于倒排索引的全文检索
-   支持模糊匹配和前缀匹配
-   在模板名称、描述、标签中搜索

### 3. SearchTemplatesByTagsAsync

-   专门的标签检索
-   支持全部匹配和任意匹配模式

### 4. SearchTemplatesBySemanticAsync

-   基于向量的语义检索
-   支持相似度阈值配置

## 技术特点

### 1. 权重分配策略

-   **模板名称**：权重最高，因为这是最直接的标识
-   **描述字段**：权重较高，提供详细的语义信息
-   **标签**：权重中等，提供分类和关键词信息
-   **规则表达式**：权重较低，作为辅助匹配

### 2. 混合检索策略

-   先用向量召回 Top-N（语义）
-   再用全文检索加权过滤或重排（字面）
-   支持线性融合两种分数

### 3. 降级方案

-   当复杂 SQL 查询失败时，自动降级到内存计算
-   保持搜索功能的可用性和准确性
-   渐进式优化，确保系统稳定性

### 4. 性能优化

-   利用 PostgreSQL 的 JSONB 操作符进行标签查询
-   使用 GIN 索引优化全文检索
-   向量索引优化语义检索

## 数据库索引支持

### 1. 全文检索索引

-   `FULL_TEXT_VECTOR` 字段的 GIN 索引
-   支持中文分词和权重配置

### 2. 标签索引

-   `TAGS` 字段的 GIN 索引
-   支持 JSON 数组的高效查询

### 3. 向量索引

-   `TEXT_VECTOR` 字段的向量索引
-   使用 `vector_cosine_ops` 操作符

### 4. 复合索引

-   分面类型、用途、最新版本的复合索引
-   优化多条件查询性能

## 使用示例

### 1. 智能推荐

```csharp
var recommendations = await _templateRepository.GetIntelligentRecommendationsAsync(
    query: "工程合同",
    threshold: 0.3,
    topN: 10,
    onlyLatest: true
);
```

### 2. 业务推荐

```csharp
var businessRecommendations = await _templateRepository.GetRecommendationsByBusinessAsync(
    businessDescription: "建筑工程施工合同",
    fileTypes: ["PDF", "DOC"],
    expectedLevels: 3,
    onlyLatest: true
);
```

### 3. 混合检索

```csharp
var hybridResults = await _templateRepository.SearchTemplatesHybridAsync(
    keyword: "合同",
    semanticQuery: "建筑工程",
    facetType: FacetType.ProjectType,
    tags: ["施工", "监理"],
    maxResults: 20
);
```

## 总结

通过这次优化，我们实现了：

1. **全面的字段利用**：所有搜索方法都能充分利用新添加的 `Description` 和 `Tags` 字段
2. **智能权重分配**：根据不同字段的重要性分配不同的权重
3. **混合检索能力**：结合字面检索和语义检索的优势
4. **降级保障**：确保在复杂查询失败时仍能提供基本的搜索功能
5. **性能优化**：利用数据库索引和高效的查询策略
6. **向后兼容**：保持现有接口的兼容性，同时扩展新功能

这些优化让系统的搜索和推荐能力得到了显著提升，能够更准确地匹配用户需求，提供更好的用户体验。
