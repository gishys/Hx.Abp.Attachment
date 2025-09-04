# 混合检索架构设计文档

## 概述

本文档描述了 `GetIntelligentRecommendationsAsync` 方法中实现的混合检索架构，该架构结合了向量检索和全文检索的优势，提供了更准确和全面的搜索结果。

## 架构设计

### 1. 混合检索流程

```
查询输入 → 向量召回 → 全文检索加权 → 分数融合 → 结果排序 → 输出
```

### 2. 三阶段处理

#### 第一阶段：向量召回（Vector Recall）

-   **目的**：基于语义相似度进行初步筛选
-   **技术**：使用 `TEXT_VECTOR` 字段进行向量相似度计算
-   **特点**：
    -   语义泛化能力强
    -   容错性好
    -   召回更多相关结果
-   **参数**：`vectorTopN = max(topN * 3, 50)` - 召回更多候选

#### 第二阶段：全文检索加权过滤（Fulltext Scoring）

-   **目的**：对向量召回的结果进行精确匹配和重排
-   **技术**：使用 PostgreSQL 的全文检索功能
-   **特点**：
    -   字面精准匹配
    -   布尔过滤
    -   多字段加权评分

#### 第三阶段：分数线性融合（Score Fusion）

-   **目的**：综合多种信号进行最终排序
-   **技术**：线性加权组合
-   **特点**：
    -   平衡语义和字面匹配
    -   考虑使用频率和时间衰减

## 字段权重设计

### 全文检索字段权重

| 字段           | 权重 | 说明                 |
| -------------- | ---- | -------------------- |
| TemplateName   | 1.0  | 模板名称，权重最高   |
| Description    | 0.8  | 模板描述，权重较高   |
| Tags           | 0.6  | 标签匹配，权重中等   |
| MetaFields     | 0.5  | 元数据字段，权重中等 |
| WorkflowConfig | 0.3  | 工作流配置，权重较低 |
| 模糊匹配       | 0.2  | 模糊匹配，权重最低   |

### 最终分数融合权重

| 分数类型 | 权重 | 说明                 |
| -------- | ---- | -------------------- |
| 向量分数 | 0.6  | 语义相似度，主要权重 |
| 全文分数 | 0.4  | 字面匹配度，次要权重 |
| 使用频率 | 0.05 | 使用次数权重         |
| 时间衰减 | 0.1  | 最近使用时间权重     |

## 技术实现

### 1. 向量检索

```sql
-- 向量相似度计算（占位符，实际应使用向量操作符）
CASE
    WHEN t."TEXT_VECTOR" IS NOT NULL AND t."VECTOR_DIMENSION" > 0
    THEN COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) * 0.9
    ELSE 0
END as vector_score
```

**注意**：当前使用文本相似度作为占位符，实际部署时应替换为真正的向量相似度计算：

```sql
-- 实际向量相似度计算示例
1 - (t."TEXT_VECTOR" <-> @queryVector::vector)
```

### 2. 全文检索

```sql
-- 多字段加权评分
COALESCE(
    GREATEST(
        -- 模板名称匹配
        CASE WHEN vr."TEMPLATE_NAME" ILIKE @queryPattern
             THEN COALESCE(similarity(vr."TEMPLATE_NAME", @query), 0) * 1.0
             ELSE 0 END,

        -- 描述字段匹配
        CASE WHEN vr."DESCRIPTION" IS NOT NULL AND vr."DESCRIPTION" ILIKE @queryPattern
             THEN COALESCE(similarity(vr."DESCRIPTION", @query), 0) * 0.8
             ELSE 0 END,

        -- 标签匹配
        CASE WHEN vr."TAGS" IS NOT NULL AND vr."TAGS" != '[]'::jsonb
             THEN (
                 SELECT COALESCE(MAX(similarity(tag, @query)), 0) * 0.6
                 FROM jsonb_array_elements_text(vr."TAGS") AS tag
             )
             ELSE 0 END,

        -- 元数据字段匹配
        CASE WHEN vr."META_FIELDS" IS NOT NULL AND vr."META_FIELDS" != '[]'::jsonb
             THEN (
                 SELECT COALESCE(MAX(similarity(meta_field->>'FieldName', @query)), 0) * 0.5
                 FROM jsonb_array_elements(vr."META_FIELDS") AS meta_field
             )
             ELSE 0 END
    ), 0
) as fulltext_score
```

### 3. 分数融合

```sql
-- 线性加权融合
(
    -- 向量分数权重（语义）
    COALESCE(vector_score, 0) * 0.6 +
    -- 全文检索分数权重（字面）
    COALESCE(fulltext_score, 0) * 0.4 +
    -- 使用频率权重
    (usage_count * 0.05) +
    -- 时间衰减权重
    CASE WHEN last_used_time IS NOT NULL
         THEN GREATEST(0, 1 - EXTRACT(EPOCH FROM (NOW() - last_used_time)) / (30 * 24 * 3600)) * 0.1
         ELSE 0 END
) as final_score
```

## 索引策略

### 1. 倒排索引（全文检索）

-   **用途**：字面精准匹配和布尔过滤
-   **字段**：TemplateName, Description, Tags, MetaFields, WorkflowConfig
-   **类型**：GIN 索引（JSONB）、B-tree 索引（文本字段）

### 2. 向量索引（语义检索）

-   **用途**：语义泛化和容错匹配
-   **字段**：TextVector
-   **类型**：HNSW 索引（高维向量）

## 性能优化

### 1. 查询优化

-   使用 CTE（Common Table Expression）分阶段处理
-   向量召回阶段限制候选数量
-   全文检索阶段使用索引加速

### 2. 参数调优

-   `vectorTopN`：向量召回数量，默认 `max(topN * 3, 50)`
-   `vectorThreshold`：向量阈值，默认 `threshold * 0.5`
-   `fulltextThreshold`：全文阈值，默认 `threshold`

### 3. 降级策略

-   主查询失败时自动降级到简化搜索
-   简化搜索使用内存计算，保证服务可用性

## 扩展性考虑

### 1. 向量模型升级

-   支持不同维度的向量模型
-   支持多种向量相似度算法
-   支持向量模型版本管理

### 2. 权重动态调整

-   支持运行时权重调整
-   支持用户个性化权重
-   支持 A/B 测试权重配置

### 3. 多语言支持

-   支持不同语言的向量模型
-   支持多语言全文检索
-   支持跨语言语义匹配

## 监控指标

### 1. 性能指标

-   查询响应时间
-   向量召回数量
-   全文检索命中率
-   分数分布统计

### 2. 质量指标

-   用户点击率
-   结果相关性评分
-   用户反馈评分
-   搜索成功率

## 部署建议

### 1. 数据库配置

-   启用 PostgreSQL 扩展：`pg_trgm`, `vector`
-   配置合适的索引参数
-   优化内存和缓存设置

### 2. 应用配置

-   配置向量模型服务
-   设置合适的阈值参数
-   配置监控和日志

### 3. 测试验证

-   准备测试数据集
-   验证向量相似度计算
-   测试混合检索效果
-   性能压力测试

## 总结

混合检索架构通过结合向量检索和全文检索的优势，提供了更准确、更全面的搜索结果。该架构具有良好的扩展性和可维护性，能够适应不同的业务需求和性能要求。
