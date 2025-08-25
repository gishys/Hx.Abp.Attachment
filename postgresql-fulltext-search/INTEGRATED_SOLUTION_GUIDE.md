# 附件管理系统完整解决方案指南

## 概述

本文档整合了附件管理系统的核心功能模块，包括全文搜索、智能推荐、OCR 处理等完整解决方案，基于 ABP Framework 和 PostgreSQL 实现。

## 系统架构

### 技术栈

-   **框架**: ABP Framework (DDD 架构)
-   **数据库**: PostgreSQL (全文搜索、相似度计算)
-   **ORM**: Entity Framework Core
-   **语言**: C# .NET 8.0

### 核心组件

-   **IntelligentRecommendationAppService**: 智能推荐应用服务
-   **DefaultSemanticMatcher**: 语义匹配服务
-   **AttachCatalogueTemplateRepository**: 模板仓储层
-   **FullTextSearchRepository**: 全文搜索服务
-   **OcrService**: OCR 文本提取服务

## 核心功能模块

### 1. 全文搜索系统

#### 功能特性

-   **OCR 文本提取**: 从 PDF、图片等文件中提取文本内容
-   **全文内容存储**: 在目录级别存储所有文件的 OCR 内容
-   **全文搜索**: 基于 PostgreSQL 的全文搜索功能
-   **模糊搜索**: 基于相似度的模糊匹配
-   **组合搜索**: 结合全文搜索和模糊搜索的最佳结果

#### 数据库结构

```sql
-- AttachCatalogue 表新增字段
FULL_TEXT_CONTENT text                    -- 存储分类下所有文件的OCR提取内容
FULL_TEXT_CONTENT_UPDATED_TIME timestamp  -- 全文内容更新时间

-- AttachFile 表新增字段
OCR_CONTENT text                          -- OCR提取的文本内容
OCR_PROCESS_STATUS integer               -- OCR处理状态
OCR_PROCESSED_TIME timestamp             -- OCR处理时间
```

#### 索引配置

```sql
-- 全文搜索索引
CREATE INDEX IDX_ATTACH_CATALOGUES_FULLTEXT ON APPATTACH_CATALOGUES USING GIN (
    to_tsvector('chinese_fts',
        COALESCE(CATALOGUE_NAME, '') || ' ' ||
        COALESCE(FULL_TEXT_CONTENT, '')
    )
);

-- 模糊搜索索引
CREATE INDEX IDX_ATTACH_CATALOGUES_NAME_TRGM ON APPATTACH_CATALOGUES USING GIN (CATALOGUE_NAME gin_trgm_ops);
```

#### API 接口

```http
# OCR 处理接口
POST /api/ocr/files/{fileId}                    # 处理单个文件 OCR
POST /api/ocr/files/batch                       # 批量处理文件 OCR
POST /api/ocr/catalogues/{catalogueId}          # 处理目录下所有文件 OCR
GET /api/ocr/files/{fileId}/supported           # 检查文件是否支持 OCR
GET /api/ocr/files/{fileId}/content             # 获取文件 OCR 内容
GET /api/ocr/catalogues/{catalogueId}/content   # 获取目录全文内容

# 全文搜索接口
GET /api/fulltextsearch/catalogues?query=关键词     # 全文搜索目录
GET /api/fulltextsearch/files?query=关键词         # 全文搜索文件
GET /api/fulltextsearch/catalogues/fuzzy?query=关键词  # 模糊搜索目录
GET /api/fulltextsearch/files/fuzzy?query=关键词      # 模糊搜索文件
GET /api/fulltextsearch/catalogues/combined?query=关键词 # 组合搜索目录
GET /api/fulltextsearch/files/combined?query=关键词     # 组合搜索文件
```

### 2. 智能推荐系统

#### 核心功能

-   **智能模板推荐**: 基于语义匹配的模板推荐
-   **模板生成**: 基于现有模板生成新模板
-   **分类结构推荐**: 基于业务描述推荐分类结构
-   **关键字维护**: 自动化的关键字提取和更新

#### 推荐算法特点

-   **数据库驱动**: 直接在数据库层面进行相似度计算
-   **多层次匹配**: SemanticModel > NamePattern > RuleExpression > Name
-   **权重分配**: 不同字段采用不同权重进行匹配
-   **实时排序**: 基于 PostgreSQL 内置函数进行排序

#### 匹配类型优先级

1. **Semantic**: 基于 SemanticModel 关键字匹配 (权重 1.3)
2. **Pattern**: 基于 NamePattern 模式匹配 (权重 1.1)
3. **Rule**: 基于 RuleExpression 规则匹配 (权重 1.0)
4. **Name**: 基于模板名称匹配 (权重 0.8)

#### 核心 SQL 查询

```sql
SELECT t.*,
       COALESCE(
           GREATEST(
               -- SemanticModel 语义匹配（权重最高）
               CASE WHEN t."SEMANTIC_MODEL" IS NOT NULL AND t."SEMANTIC_MODEL" != ''
                    THEN (
                        COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) * 0.4 +
                        COALESCE(similarity(t."SEMANTIC_MODEL", @query), 0) * 0.6
                    ) * 1.3
                    ELSE 0 END,
               -- NamePattern 模式匹配（权重中等）
               CASE WHEN t."NAME_PATTERN" IS NOT NULL AND t."NAME_PATTERN" != ''
                    THEN (
                        COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) * 0.5 +
                        COALESCE(similarity(t."NAME_PATTERN", @query), 0) * 0.5
                    ) * 1.1
                    ELSE 0 END,
               -- RuleExpression 规则匹配（权重较低）
               CASE WHEN t."RULE_EXPRESSION" IS NOT NULL AND t."RULE_EXPRESSION" != ''
                    THEN COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) * 1.0
                    ELSE 0 END,
               -- 基础名称匹配（权重最低）
               COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) * 0.8
           ), 0
       ) as match_score
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
WHERE (@onlyLatest = false OR t."IS_LATEST" = true)
  AND (
      t."TEMPLATE_NAME" ILIKE @queryPattern
      OR t."TEMPLATE_NAME" % @query
      OR @query % t."TEMPLATE_NAME"
      OR (t."SEMANTIC_MODEL" IS NOT NULL AND t."SEMANTIC_MODEL" ILIKE @queryPattern)
      OR (t."NAME_PATTERN" IS NOT NULL AND t."NAME_PATTERN" ILIKE @queryPattern)
      OR COALESCE(similarity(t."TEMPLATE_NAME", @query), 0) > @threshold
  )
ORDER BY match_score DESC, t."SEQUENCE_NUMBER" ASC
LIMIT @topN
```

### 3. 关键字维护系统

#### NamePattern（名称模式）

-   **用途**: 定义文件命名规则和模式
-   **示例**: `"项目_{ProjectName}_{Date}_{Version}"`
-   **应用场景**: 文件命名规范、版本控制、项目分类
-   **维护方式**: 从实际使用的模板实例中提取命名模式

#### SemanticModel（语义模型）

-   **用途**: 定义语义匹配的关键字和特征
-   **示例**: `"合同,协议,法律,商业,项目"`
-   **应用场景**: 智能推荐、语义搜索、相似度匹配
-   **维护方式**: 从模板使用历史、用户行为、业务场景中提取

#### 关键字维护方法

```csharp
// 更新模板的 SemanticModel 关键字
Task UpdateSemanticModelKeywordsAsync(Guid templateId, List<string> keywords);

// 更新模板的 NamePattern 模式
Task UpdateNamePatternAsync(Guid templateId, string namePattern);

// 基于使用历史自动提取 SemanticModel 关键字
Task<List<string>> ExtractSemanticKeywordsFromUsageAsync(Guid templateId);

// 基于文件命名模式自动提取 NamePattern
Task<string> ExtractNamePatternFromFilesAsync(Guid templateId);

// 智能更新模板关键字（基于使用数据）
Task UpdateTemplateKeywordsIntelligentlyAsync(Guid templateId);

// 批量关键字更新
Task<BatchKeywordUpdateResultDto> BatchUpdateTemplateKeywordsAsync(List<Guid> templateIds);
```

### 4. 模板使用统计系统

#### 核心功能

-   **真实统计**: 基于数据库的真实使用数据统计
-   **实时更新**: 每次查询都获取最新的使用情况
-   **趋势分析**: 支持模板使用趋势分析
-   **性能优化**: 专门的索引和统计方法

#### 数据库结构

```sql
-- AttachCatalogue 表新增字段
TEMPLATE_ID uuid                                -- 关联的模板ID

-- 为 TemplateId 字段添加索引
CREATE INDEX IDX_ATTACH_CATALOGUES_TEMPLATE_ID
ON APPATTACH_CATALOGUES (TEMPLATE_ID)
WHERE IS_DELETED = false;
```

#### 统计功能

```csharp
// 获取模板使用次数
Task<int> GetTemplateUsageCountAsync(Guid templateId);

// 获取模板使用统计
Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync(Guid templateId);

// 获取模板使用趋势
Task<List<TemplateUsageTrendDto>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30);
```

#### 数据库统计视图

```sql
-- 创建统计视图
CREATE OR REPLACE VIEW "V_TEMPLATE_USAGE_STATS" AS
SELECT
    t."Id" as template_id,
    t."TemplateName" as template_name,
    COUNT(ac."Id") as usage_count,
    COUNT(DISTINCT ac."Reference") as unique_references,
    MAX(ac."CreationTime") as last_used_time
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
LEFT JOIN "APPATTACH_CATALOGUES" ac ON t."Id" = ac."TEMPLATE_ID" AND ac."IS_DELETED" = false
WHERE t."IsDeleted" = false
GROUP BY t."Id", t."TemplateName";

-- 创建统计函数
CREATE OR REPLACE FUNCTION "GetTemplateUsageCount"(template_id uuid)
RETURNS integer AS $$
BEGIN
    RETURN (
        SELECT COUNT(*)
        FROM "APPATTACH_CATALOGUES" ac
        WHERE ac."TEMPLATE_ID" = template_id
          AND ac."IS_DELETED" = false
    );
END;
$$ LANGUAGE plpgsql;
```

#### 应用场景

-   **智能推荐**: 基于模板使用频率进行推荐
-   **热门模板**: 识别最受欢迎的模板
-   **趋势分析**: 分析模板使用趋势
-   **业务分析**: 了解用户对模板的偏好

### 5. 性能优化

#### 数据库优化

-   **索引策略**: 为关键字段建立合适的索引
-   **查询优化**: 使用数据库内置函数
-   **参数化查询**: 防止 SQL 注入
-   **批量处理**: 合理设置批量大小

#### 代码优化

-   **数据库驱动**: 避免内存中的大量计算
-   **分数存储**: 将数据库计算的分数存储在实体扩展属性中
-   **方法拆分**: 将复杂逻辑拆分为独立的辅助方法
-   **异步处理**: 所有 I/O 操作都是异步的

#### 推荐置信度计算

```csharp
private static double CalculateRecommendationConfidence(List<RecommendedTemplateDto> templates)
{
    if (templates.Count == 0)
        return 0.0;

    // 基于最高分数和平均分数计算置信度
    var maxScore = templates.Max(t => t.Score);
    var avgScore = templates.Average(t => t.Score);

    // 考虑匹配类型的影响
    var semanticCount = templates.Count(t => t.MatchType == "Semantic");
    var semanticFactor = semanticCount > 0 ? 0.1 : 0.0;

    // 考虑结果数量的影响（适度数量更可信）
    var countFactor = templates.Count switch
    {
        0 => 0.0,
        1 => 0.05,
        2 => 0.08,
        3 => 0.1,
        _ => 0.1 // 超过3个结果，置信度不再增加
    };

    return Math.Min(1.0, maxScore * 0.6 + avgScore * 0.25 + semanticFactor + countFactor);
}
```

## 部署配置

### 1. 数据库迁移

执行数据库迁移脚本：

```sql
-- 执行 database-migration.sql 文件中的所有SQL语句
-- 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 创建中文全文搜索配置
CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
ALTER TEXT SEARCH CONFIGURATION chinese_fts
    ALTER MAPPING FOR
        asciiword, asciihword, hword_asciipart,
        word, hword, hword_part
    WITH simple;
```

### 2. 依赖注入配置

```csharp
// 在 HxAbpAttachmentApplicationModule.cs 中
public override void ConfigureServices(ServiceConfigurationContext context)
{
    // 注册OCR服务
    context.Services.AddScoped<IOcrService, OcrService>();

    // 注册全文搜索仓储
    context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();

    // 注册语义匹配服务
    context.Services.AddScoped<ISemanticMatcher, DefaultSemanticMatcher>();
}
```

### 3. DbContext 配置

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);

    // 启用 pg_trgm 扩展
    builder.HasPostgresExtension("pg_trgm");

    // 配置全文搜索索引
    ConfigureFullTextSearch(builder);
}

private void ConfigureFullTextSearch(ModelBuilder builder)
{
    builder.Entity<AttachCatalogue>(entity =>
    {
        entity.HasIndex(e => e.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    });
}
```

## 使用示例

### 1. 智能推荐使用

```csharp
// 注入服务
private readonly IIntelligentRecommendationAppService _recommendationService;

// 智能推荐模板
public async Task<IntelligentRecommendationResultDto> RecommendTemplates(string query)
{
    var input = new IntelligentRecommendationInputDto
    {
        Query = query,
        TopN = 5,
        Threshold = 0.6,
        IncludeHistory = false
    };

    return await _recommendationService.RecommendTemplatesAsync(input);
}
```

### 2. 全文搜索使用

```csharp
// 注入服务
private readonly IFullTextSearchRepository _searchRepository;

// 搜索目录
public async Task<List<AttachCatalogue>> SearchCatalogues(string query)
{
    return await _searchRepository.SearchCataloguesAsync(query);
}

// 组合搜索
public async Task<List<AttachCatalogue>> CombinedSearch(string query)
{
    return await _searchRepository.CombinedSearchCataloguesAsync(query);
}
```

### 3. OCR 处理使用

```csharp
// 注入服务
private readonly IOcrService _ocrService;

// 处理目录下所有文件
public async Task<CatalogueOcrResult> ProcessCatalogueOcr(Guid catalogueId)
{
    var result = await _ocrService.ProcessCatalogueAsync(catalogueId);

    // 获取并保存目录
    var catalogue = await _catalogueRepository.GetAsync(catalogueId);
    await _catalogueRepository.UpdateAsync(catalogue);

    return result;
}
```

### 4. 模板使用统计使用

```csharp
// 注入服务
private readonly IAttachCatalogueTemplateRepository _templateRepository;

// 获取模板使用次数
public async Task<int> GetTemplateUsageCount(Guid templateId)
{
    return await _templateRepository.GetTemplateUsageCountAsync(templateId);
}

// 获取模板使用统计
public async Task<TemplateUsageStatsDto> GetTemplateUsageStats(Guid templateId)
{
    return await _templateRepository.GetTemplateUsageStatsAsync(templateId);
}

// 获取模板使用趋势
public async Task<List<TemplateUsageTrendDto>> GetTemplateUsageTrend(Guid templateId, int daysBack = 30)
{
    return await _templateRepository.GetTemplateUsageTrendAsync(templateId, daysBack);
}
```

## 监控和日志

### 性能指标

-   **处理时间**: 每个操作的处理时间
-   **成功率**: 各种操作的成功率
-   **资源使用**: CPU 和内存使用情况

### 日志记录

```csharp
// 在 appsettings.json 中配置
{
  "Logging": {
    "LogLevel": {
      "Hx.Abp.Attachment": "Debug"
    }
  }
}
```

## 故障排除

### 常见问题

#### OCR 处理失败

-   检查文件格式是否支持
-   检查文件是否损坏
-   检查 OCR 服务是否可用

#### 搜索无结果

-   检查全文内容是否已生成
-   检查搜索关键词是否正确
-   检查数据库索引是否正常

#### 性能问题

-   检查数据库索引是否创建
-   检查查询是否使用了索引
-   检查数据库配置是否合理

### 调试方法

```sql
-- 测试全文搜索
SELECT to_tsvector('chinese_fts', '测试文本') @@ plainto_tsquery('chinese_fts', '测试');

-- 测试模糊搜索
SELECT similarity('测试文本', '测试');

-- 测试智能推荐查询
SELECT similarity('合同模板', '合同');
```

## 扩展功能

### 1. 真实 OCR 服务集成

替换模拟 OCR 处理为真实的 OCR 服务：

-   Tesseract OCR
-   Azure Computer Vision
-   Google Cloud Vision API
-   阿里云 OCR

### 2. 语义搜索

集成向量数据库进行语义搜索：

-   PostgreSQL pgvector 扩展
-   Milvus 向量数据库
-   Elasticsearch 向量搜索

### 3. 搜索建议

实现搜索建议功能：

-   基于历史搜索记录
-   基于热门搜索词
-   基于内容相似度

## 最佳实践

### 1. 代码组织

-   **DDD 架构**: 清晰的分层架构
-   **依赖注入**: 使用 ABP 的 DI 容器
-   **异步编程**: 所有 I/O 操作都是异步的

### 2. 数据库设计

-   **索引策略**: 为关键字段建立合适的索引
-   **查询优化**: 使用数据库内置函数
-   **参数化查询**: 防止 SQL 注入

### 3. 性能优化

-   **批量处理**: 合理设置批量大小
-   **并发控制**: 避免过度占用系统资源
-   **缓存策略**: 合理使用缓存提高性能

## 总结

通过这个完整的解决方案，我们实现了：

1. **完整的 OCR 处理流程** - 从文件上传到文本提取
2. **高效的全文搜索** - 基于 PostgreSQL 原生功能
3. **智能推荐系统** - 基于语义匹配的模板推荐
4. **灵活的关键字维护** - 自动化的关键字提取和更新
5. **完善的模板使用统计** - 基于数据库的真实使用数据统计
6. **良好的扩展性** - 易于集成真实 OCR 服务
7. **完善的文档** - 详细的使用说明和示例

这个解决方案不仅提供了强大的搜索和推荐能力，还具有良好的可扩展性和可维护性，为未来的功能扩展奠定了良好的基础。
