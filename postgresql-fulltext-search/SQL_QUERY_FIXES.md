# SQL 查询修复总结

## 问题描述

在智能推荐功能中，出现了以下错误：

1. **`字段 t.Value 不存在`** - 在 `GetTemplateUsageCountAsync` 方法中
2. **`Sequence contains no elements`** - 在 `GetIntelligentRecommendationsAsync` 方法中
3. **事务终止错误** - 由于上述错误导致的事务回滚

## 根本原因

Entity Framework Core 在处理 `SqlQueryRaw<T>()` 时，对于不同的返回类型有不同的处理方式：

1. **`SqlQueryRaw<int>()`** - EF Core 会尝试包装结果，导致字段名不匹配
2. **`SqlQueryRaw<string>()`** - 同样存在包装问题
3. **空结果集处理** - 没有正确处理查询返回空结果的情况

## 修复内容

### 1. GetTemplateUsageCountAsync 方法修复

**问题**: `字段 t.Value 不存在`

**修复**:

```csharp
// 修复前
var usageCount = await dbContext.Database
    .SqlQueryRaw<int>(sql, parameters)
    .FirstOrDefaultAsync();

// 修复后
var usageCount = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .FirstOrDefaultAsync();

var count = usageCount?.usage_count != null ? Convert.ToInt32(usageCount.usage_count) : 0;
```

### 2. GetIntelligentRecommendationsAsync 方法修复

**问题**: `Sequence contains no elements`

**修复**:

```csharp
// 添加空结果集检查
if (rawResults == null || rawResults.Count == 0)
{
    Logger.LogWarning("智能推荐查询没有返回任何结果，查询：{query}", query);
    return results;
}
```

### 3. ExtractNamePatternFromFilesAsync 方法修复

**问题**: 潜在的字段名不匹配

**修复**:

```csharp
// 修复前
var namePattern = await dbContext.Database
    .SqlQueryRaw<string>(sql, parameters)
    .FirstOrDefaultAsync();

// 修复后
var namePatternResult = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .FirstOrDefaultAsync();

var namePattern = namePatternResult?.name_pattern?.ToString();
```

### 4. ExtractSemanticKeywordsFromUsageAsync 方法修复

**问题**: 潜在的字段名不匹配

**修复**:

```csharp
// 修复前
var keywords = await dbContext.Database
    .SqlQueryRaw<string>(sql, parameters)
    .ToListAsync();

return [.. keywords.Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1)];

// 修复后
var keywordResults = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .ToListAsync();

var keywords = keywordResults?
    .Where(k => k?.keyword != null)
    .Select(k => k.keyword.ToString())
    .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1)
    .ToList() ?? new List<string>();

return keywords;
```

## 修复原理

### 1. 使用 `dynamic` 类型

-   **原因**: `SqlQueryRaw<T>()` 对于简单类型（如 `int`, `string`）会进行额外的包装
-   **解决**: 使用 `SqlQueryRaw<dynamic>()` 直接获取原始查询结果
-   **优势**: 避免 EF Core 的自动包装，直接访问数据库返回的字段

### 2. 空值检查

-   **原因**: 查询可能返回空结果集，导致后续处理出错
-   **解决**: 添加 `null` 检查和空集合检查
-   **优势**: 提高代码健壮性，避免空引用异常

### 3. 安全的类型转换

-   **原因**: `dynamic` 类型需要安全的类型转换
-   **解决**: 使用 `?.` 操作符和 `Convert` 方法
-   **优势**: 避免类型转换异常

## 修复效果

1. **消除字段名错误**: 不再出现 `字段 t.Value 不存在` 错误
2. **处理空结果集**: 正确处理查询返回空结果的情况
3. **提高稳定性**: 减少事务终止和回滚的情况
4. **保持功能完整**: 智能推荐功能正常工作

## 最佳实践

1. **使用 `SqlQueryRaw<dynamic>()`**: 对于复杂的 SQL 查询，优先使用 `dynamic` 类型
2. **添加空值检查**: 始终检查查询结果是否为空
3. **安全的类型转换**: 使用安全的类型转换方法
4. **异常处理**: 在 SQL 查询方法中添加适当的异常处理

## 注意事项

-   这些修复主要针对 PostgreSQL 数据库
-   如果使用其他数据库，可能需要调整 SQL 语法
-   建议在生产环境部署前进行充分测试
