# 模板使用次数功能优化总结

## 优化概述

本次优化针对 `GetTemplateUsageCount` 方法进行了全面重构，从简单的返回 0 改为基于数据库的真实统计，并添加了完整的模板使用统计功能。

## 主要问题分析

### 1. 缺少模板使用统计

-   **问题**: `GetTemplateUsageCount` 方法返回固定值 0
-   **影响**: 无法提供真实的模板使用情况，影响推荐算法的准确性
-   **解决**: 实现基于数据库的真实统计功能

### 2. 缺少模板与分类的关联

-   **问题**: AttachCatalogue 实体中没有 TemplateId 字段
-   **影响**: 无法追踪模板的使用情况
-   **解决**: 添加 TemplateId 字段建立关联关系

### 3. 缺少数据库层面的统计支持

-   **问题**: 没有专门的统计查询和索引
-   **影响**: 统计查询性能差
-   **解决**: 添加专门的统计方法和数据库优化

## 优化内容

### 1. 实体层优化

#### 添加 TemplateId 字段到 AttachCatalogue 实体

```csharp
/// <summary>
/// 关联的模板ID
/// </summary>
public virtual Guid? TemplateId { get; private set; }
```

#### 更新构造函数

```csharp
public AttachCatalogue(
    Guid id,
    AttachReceiveType attachReceiveType,
    string catologueName,
    int sequenceNumber,
    string reference,
    int referenceType,
    Guid? parentId = null,
    bool isRequired = false,
    bool isVerification = false,
    bool verificationPassed = false,
    bool isStatic = false,
    int attachCount = 0,
    int pageCount = 0,
    Guid? templateId = null  // 新增参数
)
```

#### 添加设置方法

```csharp
/// <summary>
/// 设置关联的模板ID
/// </summary>
/// <param name="templateId">模板ID</param>
public virtual void SetTemplateId(Guid? templateId) => TemplateId = templateId;
```

### 2. 数据库层优化

#### 更新数据库配置

```csharp
builder.Property(d => d.TemplateId).HasColumnName("TEMPLATE_ID");
```

#### 添加数据库迁移脚本

```sql
-- 为 AttachCatalogues 表添加 TemplateId 字段
ALTER TABLE "APPATTACH_CATALOGUES"
ADD COLUMN "TEMPLATE_ID" uuid;

-- 为 TemplateId 字段添加索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TEMPLATE_ID"
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID")
WHERE "IS_DELETED" = false;
```

### 3. 仓储层优化

#### 添加获取模板使用次数方法

```csharp
/// <summary>
/// 获取模板使用次数
/// </summary>
public async Task<int> GetTemplateUsageCountAsync(Guid templateId)
{
    var dbContext = await GetDbContextAsync();

    // 统计基于该模板创建的分类数量
    var sql = @"
        SELECT COUNT(*) as usage_count
        FROM ""AttachCatalogues"" ac
        WHERE ac.""TemplateId"" = @templateId
          AND ac.""IsDeleted"" = false";

    var parameters = new[] { new Npgsql.NpgsqlParameter("@templateId", templateId) };

    var usageCount = await dbContext.Database
        .SqlQueryRaw<int>(sql, parameters)
        .FirstOrDefaultAsync();

    Logger.LogInformation("获取模板使用次数完成，模板ID：{templateId}，使用次数：{usageCount}",
        templateId, usageCount);

    return usageCount;
}
```

#### 更新仓储接口

```csharp
/// <summary>
/// 获取模板使用次数
/// </summary>
Task<int> GetTemplateUsageCountAsync(Guid templateId);
```

### 4. 应用服务层优化

#### 更新 GetTemplateUsageCount 方法

```csharp
/// <summary>
/// 获取模板使用次数
/// </summary>
private async Task<int> GetTemplateUsageCountAsync(Guid templateId)
{
    try
    {
        return await _templateRepository.GetTemplateUsageCountAsync(templateId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取模板使用次数失败，模板ID：{templateId}", templateId);
        return 0; // 出错时返回0
    }
}
```

#### 更新推荐模板构建方法

```csharp
/// <summary>
/// 基于数据库结果构建推荐模板
/// </summary>
private async Task<List<RecommendedTemplateDto>> BuildRecommendedTemplatesFromDatabaseAsync(
    List<AttachCatalogueTemplate> templates,
    string query)
{
    var recommendedTemplates = new List<RecommendedTemplateDto>();

    foreach (var template in templates)
    {
        // 使用数据库计算的相似度分数
        var score = GetDatabaseCalculatedScore(template, templates.IndexOf(template));
        var matchType = DetermineMatchType(template, query);
        var reason = GenerateRecommendationReason(template, query, score, matchType);

        var recommendedTemplate = new RecommendedTemplateDto
        {
            Template = ObjectMapper.Map<AttachCatalogueTemplate, AttachCatalogueTemplateDto>(template),
            Score = score,
            MatchType = matchType,
            Reason = reason,
            IsNewTemplate = template.Version == 1,
            UsageCount = await GetTemplateUsageCountAsync(template.Id)  // 真实的使用次数
        };

        recommendedTemplates.Add(recommendedTemplate);
    }

    return recommendedTemplates;
}
```

### 5. 数据库统计功能增强

#### 创建统计视图

```sql
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
```

#### 创建统计函数

```sql
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

#### 创建趋势分析函数

```sql
CREATE OR REPLACE FUNCTION "GetTemplateUsageTrend"(template_id uuid, days_back integer DEFAULT 30)
RETURNS TABLE(
    usage_date date,
    daily_count integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        DATE(ac."CreationTime") as usage_date,
        COUNT(*) as daily_count
    FROM "APPATTACH_CATALOGUES" ac
    WHERE ac."TEMPLATE_ID" = template_id
      AND ac."IS_DELETED" = false
      AND ac."CreationTime" >= CURRENT_DATE - INTERVAL '1 day' * days_back
    GROUP BY DATE(ac."CreationTime")
    ORDER BY usage_date;
END;
$$ LANGUAGE plpgsql;
```

## 技术优势

### 1. 数据准确性

-   **真实统计**: 基于数据库的真实使用数据
-   **实时更新**: 每次查询都获取最新的使用情况
-   **错误处理**: 完善的异常处理机制

### 2. 性能优化

-   **索引支持**: 为 TemplateId 字段添加专门索引
-   **参数化查询**: 使用参数化查询确保安全性
-   **缓存友好**: 支持后续添加缓存机制

### 3. 功能扩展

-   **统计视图**: 提供快速查询模板使用情况
-   **趋势分析**: 支持模板使用趋势分析
-   **灵活查询**: 支持多种统计维度

### 4. 代码质量

-   **异步支持**: 所有数据库操作都是异步的
-   **错误隔离**: 单个模板统计失败不影响整体
-   **日志记录**: 完整的操作日志记录

## 应用场景

### 1. 智能推荐

-   **使用频率**: 基于模板使用频率进行推荐
-   **热门模板**: 识别最受欢迎的模板
-   **趋势分析**: 分析模板使用趋势

### 2. 模板管理

-   **使用统计**: 了解模板的实际使用情况
-   **性能评估**: 评估模板的受欢迎程度
-   **优化决策**: 基于使用数据优化模板

### 3. 业务分析

-   **用户行为**: 分析用户对模板的偏好
-   **业务趋势**: 了解业务模板的使用趋势
-   **资源规划**: 基于使用情况规划资源

## 最佳实践

### 1. 数据库设计

-   **索引策略**: 为关键字段建立合适的索引
-   **外键约束**: 确保数据完整性（可选）
-   **软删除**: 支持软删除不影响统计

### 2. 性能优化

-   **批量查询**: 支持批量获取多个模板的使用次数
-   **缓存策略**: 考虑添加缓存提高性能
-   **异步处理**: 所有 I/O 操作都是异步的

### 3. 错误处理

-   **异常捕获**: 完善的异常处理机制
-   **降级策略**: 出错时返回默认值
-   **日志记录**: 详细的错误日志记录

### 4. 扩展性

-   **统计维度**: 支持多种统计维度扩展
-   **时间范围**: 支持不同时间范围的统计
-   **聚合函数**: 支持多种聚合统计

## 总结

通过这次优化，我们实现了：

1. **真实统计**: 从固定值 0 改为基于数据库的真实统计
2. **数据关联**: 建立了模板与分类的关联关系
3. **性能优化**: 添加了专门的索引和统计方法
4. **功能扩展**: 提供了丰富的统计和分析功能
5. **代码质量**: 提高了代码的可维护性和扩展性

这次优化不仅解决了现有问题，还为未来的功能扩展奠定了良好的基础，使模板使用统计功能更加完善和实用。
