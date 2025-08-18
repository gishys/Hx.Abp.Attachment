# 附件分类全文检索和语义检索功能

## 概述

本项目为附件分类系统添加了全文检索和语义检索功能，支持基于文本内容的精确搜索和基于语义的相似度搜索。

## 功能特性

### 1. 全文检索

-   基于 PostgreSQL 的全文搜索功能
-   支持中文和英文文本搜索
-   按相关性排序返回结果
-   支持业务引用过滤

### 2. 语义检索

-   基于 pgvector 扩展的向量相似度搜索
-   支持余弦相似度计算
-   可配置相似度阈值
-   支持业务引用过滤

## 数据库配置

### 1. 安装必要的扩展

在 PostgreSQL 数据库中执行：

```sql
-- 安装 pgvector 扩展
CREATE EXTENSION IF NOT EXISTS vector;

-- 安装中文分词扩展（可选，用于更好的中文搜索）
CREATE EXTENSION IF NOT EXISTS zhparser;

-- 验证安装
SELECT * FROM pg_extension WHERE extname IN ('vector', 'zhparser');
```

### 2. 配置中文分词器

```sql
-- 创建中文分词器配置
CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = zhparser);

-- 配置中文分词器
ALTER TEXT SEARCH CONFIGURATION chinese ALTER MAPPING FOR n,v,a,i,e,l,t WITH simple;
```

### 3. 数据库迁移

运行以下命令创建迁移：

```bash
dotnet ef migrations add AddSearchFields
dotnet ef database update
```

## API 使用

### 1. 全文检索

```csharp
// 搜索包含特定文本的分类
var results = await attachCatalogueAppService.SearchByFullTextAsync(
    searchText: "合同",
    reference: "project-001",
    referenceType: 1,
    limit: 10
);
```

### 2. 语义检索

```csharp
// 使用语义向量搜索相似分类
var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f, ... }; // 查询向量
var results = await attachCatalogueAppService.SearchBySemanticAsync(
    queryEmbedding: queryEmbedding,
    reference: "project-001",
    referenceType: 1,
    limit: 10,
    similarityThreshold: 0.8f
);

// 结果按相似度排序，最相似的排在前面
foreach (var result in results)
{
    Console.WriteLine($"分类: {result.CATALOGUE_NAME}, 相似度: {result.SimilarityScore}");
}
```

### 3. 更新语义向量

```csharp
// 更新分类的语义向量
var embedding = new float[] { 0.1f, 0.2f, 0.3f, ... }; // 新的语义向量
var updatedCatalogue = await attachCatalogueAppService.UpdateEmbeddingAsync(
    catalogueId: Guid.Parse("your-catalogue-id"),
    embedding: embedding
);
```

## 技术实现

### 0. EF Core 向量搜索支持分析

#### 当前状态

-   **EF Core 8.0** 目前**没有内置**的向量搜索方法
-   **pgvector** 扩展需要原生 SQL 查询
-   **EF.Functions** 不支持向量操作符（如 `<=>`）

#### 最佳实践

-   使用**原生 SQL 查询**是当前的最佳实践
-   通过 `FromSqlRaw` 方法执行向量搜索
-   保持 EF Core 的实体映射和事务管理优势

#### 未来展望

-   EF Core 团队正在考虑添加向量搜索支持
-   可能需要等待 EF Core 9.0 或更高版本

### 1. 实体字段

在 `AttachCatalogue` 实体中添加了以下字段：

```csharp
/// <summary>
/// 全文检索向量 (仅读取)
/// </summary>
[NotMapped]
public virtual NpgsqlTsVector SearchVector { get; private set; }

/// <summary>
/// 语义检索向量
/// </summary>
public virtual float[]? Embedding { get; private set; }
```

### 2. 向量搜索原理

#### 余弦相似度计算

-   使用 pgvector 的 `<=>` 操作符计算余弦距离
-   余弦距离 = 1 - 余弦相似度
-   相似度越高，距离越小
-   按距离升序排列，最相似的排在前面

#### 全文搜索 SQL 查询示例

```sql
-- 中文全文搜索
SELECT c.*, ts_rank(to_tsvector('chinese', c."CATALOGUE_NAME"), plainto_tsquery('chinese', @searchText)) as rank
FROM "ATTACH_CATALOGUES" c
WHERE to_tsvector('chinese', c."CATALOGUE_NAME") @@ plainto_tsquery('chinese', @searchText)
ORDER BY rank DESC
LIMIT @limit
```

#### 语义搜索 SQL 查询示例

```sql
-- 向量相似度搜索
SELECT c.*, 1 - (c.embedding <=> @queryEmbedding) as similarity_score
FROM "ATTACH_CATALOGUES" c
WHERE c.embedding IS NOT NULL
AND 1 - (c.embedding <=> @queryEmbedding) >= @similarityThreshold
ORDER BY c.embedding <=> @queryEmbedding
LIMIT @limit
```

### 3. 数据库配置

-   使用 `NpgsqlTsVector` 类型支持全文检索
-   使用 `vector` 类型支持语义向量存储
-   在 DbContext 中启用 pgvector 扩展

### 4. 搜索实现

-   全文检索：使用 PostgreSQL 的 `to_tsvector` 和 `plainto_tsquery` 函数，通过原生 SQL 查询支持中文分词
-   语义检索：使用 pgvector 的 `<=>` 操作符进行余弦距离计算，通过原生 SQL 查询实现真正的向量相似度搜索

## 注意事项

1. **pgvector 扩展**：确保 PostgreSQL 数据库已安装 pgvector 扩展
2. **中文分词器**：建议安装 zhparser 扩展以获得更好的中文搜索效果
3. **向量维度**：语义向量的维度需要与训练模型保持一致
4. **性能优化**：建议为向量字段创建适当的索引
5. **相似度阈值**：语义检索的相似度阈值建议设置在 0.7-0.9 之间
6. **中文模型**：语义搜索需要使用支持中文的 AI 模型（如 text-embedding-3-small）
7. **原生 SQL**：由于 EF Core 对向量搜索的支持限制，使用原生 SQL 是最佳实践

## 依赖包

-   `Npgsql.EntityFrameworkCore.PostgreSQL` - PostgreSQL 支持
-   `Pgvector.EntityFrameworkCore` - pgvector 扩展支持

## 完整示例

### 1. 生成语义向量

```csharp
// 使用支持中文的 AI 模型生成文本的语义向量
public async Task<float[]> GenerateEmbeddingAsync(string text)
{
    // 推荐使用支持中文的模型
    // OpenAI: text-embedding-3-small (支持中文)
    // 百度: text-embedding-v1
    // 阿里云: text-embedding-v1

    var client = new HttpClient();
    var request = new
    {
        input = text,
        model = "text-embedding-3-small" // 支持中文的模型
    };

    var response = await client.PostAsJsonAsync("https://api.openai.com/v1/embeddings", request);
    var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();

    return result.Data[0].Embedding;
}
```

### 2. 更新分类向量

```csharp
// 为分类生成并更新语义向量
public async Task UpdateCatalogueEmbeddingAsync(Guid catalogueId)
{
    var catalogue = await attachCatalogueAppService.GetAsync(catalogueId);
    var embedding = await GenerateEmbeddingAsync(catalogue.CatalogueName);

    await attachCatalogueAppService.UpdateEmbeddingAsync(catalogueId, embedding);
}
```

### 3. 中文搜索示例

```csharp
// 中文全文搜索
public async Task<List<AttachCatalogueDto>> SearchChineseCataloguesAsync(string queryText)
{
    // 搜索包含中文关键词的分类
    var results = await attachCatalogueAppService.SearchByFullTextAsync(
        searchText: queryText, // 例如："合同文件"
        reference: "project-001",
        referenceType: 1,
        limit: 10
    );

    return results;
}

// 中文语义搜索
public async Task<List<AttachCatalogueDto>> SearchSimilarChineseCataloguesAsync(string queryText)
{
    // 生成查询文本的向量（支持中文）
    var queryEmbedding = await GenerateEmbeddingAsync(queryText);

    // 执行语义搜索
    var results = await attachCatalogueAppService.SearchBySemanticAsync(
        queryEmbedding: queryEmbedding,
        limit: 10,
        similarityThreshold: 0.7f
    );

    return results;
}

// 混合搜索（全文 + 语义）
public async Task<List<AttachCatalogueDto>> HybridSearchAsync(string queryText)
{
    var tasks = new List<Task<List<AttachCatalogueDto>>>
    {
        SearchChineseCataloguesAsync(queryText),
        SearchSimilarChineseCataloguesAsync(queryText)
    };

    var results = await Task.WhenAll(tasks);

    // 合并结果并去重
    var allResults = results.SelectMany(r => r).Distinct().ToList();

    return allResults;
}
```

## 示例索引

```sql
-- 为语义向量创建索引（可选，用于提高查询性能）
CREATE INDEX ON attach_catalogues USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
```
