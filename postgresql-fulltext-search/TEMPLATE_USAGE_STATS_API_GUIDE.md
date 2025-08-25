# 模板使用统计 API 使用指南

## 概述

模板使用统计 API 提供了完整的模板使用情况统计功能，包括使用次数、使用趋势、热门模板分析等。所有功能都基于 EF Core 实现，确保高性能和类型安全。

## API 端点

### 基础 URL

```
https://localhost:7001/api/app/template-usage-stats
```

## 1. 获取模板使用次数

### 请求

```http
GET /api/app/template-usage-stats/usage-count/{templateId}
```

### 参数

-   `templateId` (Guid): 模板 ID

### 响应

```json
5
```

### 示例

```bash
curl -X GET "https://localhost:7001/api/app/template-usage-stats/usage-count/3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c"
```

## 2. 获取模板使用统计

### 请求

```http
POST /api/app/template-usage-stats/stats
Content-Type: application/json

{
  "templateId": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
  "daysBack": 30,
  "includeTrends": true
}
```

### 请求体

```json
{
    "templateId": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
    "daysBack": 30,
    "includeTrends": true
}
```

### 响应

```json
{
    "id": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
    "templateName": "合同模板",
    "usageCount": 15,
    "uniqueReferences": 8,
    "lastUsedTime": "2024-01-15T10:30:00Z",
    "averageUsagePerDay": 0.5,
    "recentUsageCount": 3
}
```

## 3. 获取模板使用趋势

### 请求

```http
GET /api/app/template-usage-stats/trend/{templateId}?daysBack=30
```

### 参数

-   `templateId` (Guid): 模板 ID
-   `daysBack` (int, 可选): 查询天数，默认 30 天

### 响应

```json
[
    {
        "usageDate": "2024-01-01T00:00:00Z",
        "dailyCount": 2,
        "cumulativeCount": 2
    },
    {
        "usageDate": "2024-01-02T00:00:00Z",
        "dailyCount": 1,
        "cumulativeCount": 3
    }
]
```

## 4. 批量获取模板使用统计

### 请求

```http
POST /api/app/template-usage-stats/batch-stats
Content-Type: application/json

{
  "templateIds": [
    "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
    "4b2cf1fc-8e6e-854g-5072-d0f1f8b6e16d"
  ],
  "daysBack": 30,
  "includeTrends": true
}
```

### 响应

```json
[
  {
    "templateId": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
    "templateName": "合同模板",
    "stats": {
      "id": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
      "templateName": "合同模板",
      "usageCount": 15,
      "uniqueReferences": 8,
      "lastUsedTime": "2024-01-15T10:30:00Z",
      "averageUsagePerDay": 0.5,
      "recentUsageCount": 3
    },
    "trends": [...],
    "isSuccess": true,
    "errorMessage": null
  }
]
```

## 5. 获取热门模板

### 请求

```http
POST /api/app/template-usage-stats/hot-templates
Content-Type: application/json

{
  "daysBack": 30,
  "topN": 10,
  "minUsageCount": 1
}
```

### 响应

```json
{
    "items": [
        {
            "templateId": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
            "templateName": "合同模板",
            "usageCount": 15,
            "rank": 1,
            "usageFrequency": 0.5
        }
    ],
    "totalCount": 1
}
```

## 6. 获取模板使用统计概览

### 请求

```http
GET /api/app/template-usage-stats/overview
```

### 响应

```json
{
    "totalTemplates": 25,
    "totalUsage": 150,
    "recentUsage": 45,
    "averageUsagePerTemplate": 6.0,
    "mostActiveTemplate": {
        "id": "3a1bf0eb-9e5d-743f-3961-c9e0e7a5d05c",
        "templateName": "合同模板",
        "usageCount": 15
    },
    "lastUpdated": "2024-01-15T10:30:00Z"
}
```

## 7. 更新模板使用统计（内部使用）

### 请求

```http
POST /api/app/template-usage-stats/update/{templateId}
```

### 响应

```json
true
```

## 技术实现

### 数据库查询优化

所有查询都使用 EF Core 的 LINQ 查询，确保：

1. **类型安全**: 编译时检查，避免运行时错误
2. **性能优化**: 自动生成优化的 SQL 查询
3. **参数化查询**: 防止 SQL 注入攻击
4. **索引利用**: 充分利用数据库索引

### 错误处理

所有 API 都包含完善的错误处理：

1. **异常捕获**: 捕获所有可能的异常
2. **日志记录**: 详细的错误日志记录
3. **降级策略**: 出错时返回默认值而不是抛出异常
4. **用户友好**: 返回有意义的错误信息

### 性能考虑

1. **异步操作**: 所有数据库操作都是异步的
2. **批量查询**: 支持批量获取多个模板的统计
3. **缓存友好**: 支持后续添加缓存机制
4. **分页支持**: 大数据量时支持分页查询

## 使用示例

### C# 客户端示例

```csharp
// 注入服务
private readonly ITemplateUsageStatsAppService _statsService;

// 获取模板使用次数
public async Task<int> GetUsageCount(Guid templateId)
{
    return await _statsService.GetTemplateUsageCountAsync(templateId);
}

// 获取模板使用统计
public async Task<TemplateUsageStatsDto> GetUsageStats(Guid templateId)
{
    var input = new TemplateUsageStatsInputDto
    {
        TemplateId = templateId,
        DaysBack = 30,
        IncludeTrends = true
    };
    return await _statsService.GetTemplateUsageStatsAsync(input);
}

// 获取热门模板
public async Task<List<HotTemplateDto>> GetHotTemplates()
{
    var input = new HotTemplatesInputDto
    {
        DaysBack = 30,
        TopN = 10,
        MinUsageCount = 1
    };
    var result = await _statsService.GetHotTemplatesAsync(input);
    return result.Items.ToList();
}
```

### JavaScript 客户端示例

```javascript
// 获取模板使用次数
async function getUsageCount(templateId) {
    const response = await fetch(
        `/api/app/template-usage-stats/usage-count/${templateId}`
    );
    return await response.json();
}

// 获取模板使用统计
async function getUsageStats(templateId) {
    const response = await fetch('/api/app/template-usage-stats/stats', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            templateId: templateId,
            daysBack: 30,
            includeTrends: true,
        }),
    });
    return await response.json();
}

// 获取热门模板
async function getHotTemplates() {
    const response = await fetch(
        '/api/app/template-usage-stats/hot-templates',
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                daysBack: 30,
                topN: 10,
                minUsageCount: 1,
            }),
        }
    );
    return await response.json();
}
```

## 最佳实践

### 1. 性能优化

-   使用批量查询减少网络请求
-   合理设置查询时间范围
-   考虑添加缓存机制

### 2. 错误处理

-   始终检查 API 响应状态
-   实现重试机制
-   提供用户友好的错误信息

### 3. 数据展示

-   使用图表展示趋势数据
-   提供数据导出功能
-   支持数据筛选和排序

### 4. 监控和日志

-   监控 API 响应时间
-   记录错误和异常
-   定期分析使用模式

## 总结

模板使用统计 API 提供了完整的模板使用情况分析功能，基于 EF Core 实现，确保高性能和类型安全。通过合理使用这些 API，可以深入了解模板的使用情况，为业务决策提供数据支持。
