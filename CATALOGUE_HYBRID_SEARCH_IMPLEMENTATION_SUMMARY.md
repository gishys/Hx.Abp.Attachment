# AttachCatalogue 混合检索实现总结

## 概述

基于行业最佳实践，我已经成功重构了 `EfCoreAttachCatalogueRepository` 中的检索方法，实现了真正的混合检索架构：向量召回 + 全文检索加权过滤 + 分数融合。

## 实现的方法

### 1. SearchByHybridAsync（重构版）

-   **位置**: `EfCoreAttachCatalogueRepository.cs` 第 324 行
-   **特点**: 基于行业最佳实践的混合检索架构
-   **架构**: 向量召回 + 全文检索加权过滤 + 分数融合

### 2. SearchByFullTextAsync（优化版）

-   **位置**: `EfCoreAttachCatalogueRepository.cs` 第 249 行
-   **特点**: 高性能全文搜索，支持模糊搜索和前缀匹配
-   **架构**: 全文搜索 + 模糊搜索 + 综合评分

### 3. SearchByHybridAdvancedAsync（增强版）

-   **位置**: `EfCoreAttachCatalogueRepository.cs` 第 601 行
-   **特点**: 支持更灵活的配置和高级功能
-   **架构**: 可配置的向量召回 + 全文检索加权过滤 + 分数融合

## 核心架构特点

### 第一阶段：向量召回（语义检索）

```sql
-- 向量相似度计算
CASE
    WHEN c.text_vector IS NOT NULL AND @hasTextVector::boolean = true
    THEN (1 - (c.text_vector <=> @queryTextVector::text)) * @semanticWeight
    ELSE 0
END as vector_score
```

**特点**:

-   使用真正的向量相似度计算（余弦距离）
-   支持向量数据的动态检测
-   可配置的语义权重

### 第二阶段：全文检索加权过滤和重排

```sql
-- 多字段加权评分系统
COALESCE(
    GREATEST(
        -- 分类名称匹配（权重最高：1.0）
        CASE WHEN vr."CATALOGUE_NAME" ILIKE @searchPattern
             THEN COALESCE(similarity(vr."CATALOGUE_NAME", @searchText), 0) * 1.0
             ELSE 0 END,

        -- 引用字段匹配（权重较高：0.8）
        CASE WHEN vr."REFERENCE" IS NOT NULL AND vr."REFERENCE" ILIKE @searchPattern
             THEN COALESCE(similarity(vr."REFERENCE", @searchText), 0) * 0.8
             ELSE 0 END,

        -- 全文搜索分数（权重中等：0.6）
        CASE WHEN (
            setweight(to_tsvector('chinese_fts', coalesce(vr."CATALOGUE_NAME",'')), 'A') ||
            setweight(to_tsvector('chinese_fts', coalesce(vr."REFERENCE",'')), 'B')
        ) @@ to_tsquery('chinese_fts', @searchQuery)
        THEN ts_rank_cd(
            setweight(to_tsvector('chinese_fts', coalesce(vr."CATALOGUE_NAME",'')), 'A') ||
            setweight(to_tsvector('chinese_fts', coalesce(vr."REFERENCE",'')), 'B'),
            to_tsquery('chinese_fts', @searchQuery)
        ) * 0.6
        ELSE 0 END
    ), 0
) as fulltext_score
```

### 第三阶段：分数融合和最终排序

```sql
-- 线性加权融合：向量分数 + 全文分数 + 使用频率 + 时间衰减
(fs.vector_score +
 fs.fulltext_score * @textWeight +
 fs.usage_score +
 fs.time_score) as final_score
```

## 权重配置

| 分数类型 | 默认权重 | 说明                 |
| -------- | -------- | -------------------- |
| 向量分数 | 0.6      | 语义相似度，主要权重 |
| 全文分数 | 0.4      | 字面匹配度，次要权重 |
| 使用频率 | 0.05     | 基于文件数量的权重   |
| 时间衰减 | 0.1      | 最近修改时间权重     |

## 技术优势

### 1. 行业最佳实践

-   **向量召回**: 先进行语义检索，召回 Top-N 候选结果
-   **全文过滤**: 使用倒排索引进行精确匹配和布尔过滤
-   **分数融合**: 线性加权融合多种分数，平衡语义和字面匹配

### 2. 性能优化

-   **分阶段处理**: 避免全表扫描，先召回再过滤
-   **索引利用**: 充分利用 PostgreSQL 的 GIN 索引和向量索引
-   **参数化查询**: 防止 SQL 注入，提高查询效率
-   **动态 SQL 构建**: 根据配置动态生成查询，避免不必要的计算

### 3. 灵活配置

-   **动态权重**: 支持调整语义和文本权重比例
-   **阈值控制**: 可配置相似度阈值
-   **功能开关**: 增强版支持启用/禁用各种搜索功能
-   **搜索策略**: 支持前缀匹配、模糊搜索等多种策略

### 4. 错误处理

-   **参数验证**: 严格的输入参数验证
-   **异常捕获**: 完整的异常处理和日志记录
-   **降级策略**: 当向量搜索失败时，可降级到纯文本搜索

## 使用示例

### 基础版混合检索

```csharp
var results = await repository.SearchByHybridAsync(
    searchText: "合同文档",
    reference: "CONTRACT_001",
    referenceType: 1,
    limit: 20,
    queryTextVector: "[0.1, 0.2, 0.3, ...]",
    similarityThreshold: 0.7f,
    textWeight: 0.4,
    semanticWeight: 0.6
);
```

### 优化版全文检索

```csharp
var results = await repository.SearchByFullTextAsync(
    searchText: "合同文档",
    reference: "CONTRACT_001",
    referenceType: 1,
    limit: 20,
    enableFuzzy: true,
    enablePrefix: true
);
```

### 增强版混合检索

```csharp
var results = await repository.SearchByHybridAdvancedAsync(
    searchText: "合同文档",
    reference: "CONTRACT_001",
    referenceType: 1,
    limit: 20,
    queryTextVector: "[0.1, 0.2, 0.3, ...]",
    similarityThreshold: 0.7f,
    textWeight: 0.4,
    semanticWeight: 0.6,
    enableVectorSearch: true,
    enableFullTextSearch: true,
    enableFuzzySearch: true,
    enablePrefixMatch: true
);
```

## 与模板检索的对比

| 特性     | AttachCatalogue           | AttachCatalogueTemplate     |
| -------- | ------------------------- | --------------------------- |
| 向量字段 | text_vector               | TEXT_VECTOR                 |
| 全文内容 | FULL_TEXT_CONTENT         | 多字段组合                  |
| 业务过滤 | reference + referenceType | facetType + templatePurpose |
| 使用频率 | ATTACH_FILES_COUNT        | USAGE_COUNT                 |
| 时间衰减 | LAST_MODIFICATION_TIME    | LAST_USED_TIME              |

## 部署注意事项

1. **向量相似度计算**: 使用真正的向量相似度计算（余弦距离）
2. **索引优化**: 确保 PostgreSQL 中已创建相应的 GIN 索引和向量索引
3. **性能监控**: 建议监控查询性能，根据实际数据量调整向量召回数量
4. **权重调优**: 根据业务需求调整各权重参数，优化检索效果

## 总结

重构后的 `EfCoreAttachCatalogueRepository` 检索方法完全符合行业最佳实践，提供了：

-   完整的向量召回 + 全文检索架构
-   灵活的权重配置和参数调优
-   优秀的性能优化和错误处理
-   易于维护和扩展的代码结构
-   与模板检索方法的一致性设计

这些改进将显著提升附件分类检索的准确性和用户体验，是现代化搜索系统的标准实现方案。
