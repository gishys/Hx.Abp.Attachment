# EF Core 迁移总结

## 迁移背景

由于 SQL 查询在处理复杂类型时出现的问题（如 `字段 t.Value 不存在`、`Sequence contains no elements` 等），我们将相关的 SQL 查询迁移到 EF Core 查询，以提高代码的稳定性和可维护性。

## 迁移的方法

### 1. ExtractSemanticKeywordsFromUsageAsync 方法

**迁移前（SQL 查询）**:

```csharp
var sql = @"
    SELECT DISTINCT
        unnest(string_to_array(t.""SEMANTIC_MODEL"", ',')) as keyword
    FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
    WHERE t.""ID"" = @templateId
       OR t.""PARENT_ID"" = @templateId
    UNION
    SELECT DISTINCT
        unnest(string_to_array(t.""TEMPLATE_NAME"", ' ')) as keyword
    FROM ""APPATTACH_CATALOGUE_TEMPLATES"" t
    WHERE t.""ID"" = @templateId
       OR t.""PARENT_ID"" = @templateId
    LIMIT 10";

var keywordResults = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .ToListAsync();

var keywords = keywordResults?
    .Where(k => k?.keyword != null)
    .Select(k => k.keyword.ToString())
    .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1)
    .ToList() ?? new List<string>();
```

**迁移后（EF Core 查询）**:

```csharp
var templates = await dbSet
    .Where(t => t.Id == templateId || t.ParentId == templateId)
    .Select(t => new { t.SemanticModel, t.TemplateName })
    .ToListAsync();

var keywords = new List<string>();

foreach (var template in templates)
{
    // 从 SemanticModel 提取关键字
    if (!string.IsNullOrEmpty(template.SemanticModel))
    {
        var semanticKeywords = template.SemanticModel
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1);

        keywords.AddRange(semanticKeywords);
    }

    // 从 TemplateName 提取关键字
    if (!string.IsNullOrEmpty(template.TemplateName))
    {
        var nameKeywords = template.TemplateName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(k => k.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k) && k.Length > 1);

        keywords.AddRange(nameKeywords);
    }
}

return keywords.Distinct().Take(10).ToList();
```

### 2. ExtractNamePatternFromFilesAsync 方法

**迁移前（SQL 查询）**:

```csharp
var sql = @"
    SELECT DISTINCT
        CASE
            WHEN af.""FILE_NAME"" LIKE '%{ProjectName}%' THEN '项目_{ProjectName}_{Date}_{Version}'
            WHEN af.""FILE_NAME"" LIKE '%{Date}%' THEN '{Type}_{Date}_{Version}'
            WHEN af.""FILE_NAME"" LIKE '%{Version}%' THEN '{Type}_{ProjectName}_{Version}'
            ELSE '{Type}_{ProjectName}_{Date}'
        END as name_pattern
    FROM ""APPATTACH_FILES"" af
    INNER JOIN ""APPATTACH_CATALOGUES"" ac ON af.""CATALOGUE_ID"" = ac.""ID""
    WHERE ac.""TEMPLATE_ID"" IS NOT NULL
      AND ac.""TEMPLATE_ID"" = @templateId
    LIMIT 1";

var namePatternResult = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .FirstOrDefaultAsync();

var namePattern = namePatternResult?.name_pattern?.ToString();
```

**迁移后（EF Core 查询）**:

```csharp
var catalogues = await dbContext.Set<AttachCatalogue>()
    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted)
    .Include(ac => ac.AttachFiles)
    .Select(ac => new { ac.AttachFiles })
    .ToListAsync();

var fileNames = catalogues
    .SelectMany(c => c.AttachFiles)
    .Select(af => af.FileName)
    .Where(fn => !string.IsNullOrEmpty(fn))
    .ToList();

if (!fileNames.Any())
{
    return "{Type}_{ProjectName}_{Date}";
}

var namePattern = DetermineNamePattern(fileNames);
return namePattern;
```

### 3. GetTemplateUsageCountAsync 方法

**迁移前（SQL 查询）**:

```csharp
var sql = @"
    SELECT COUNT(*) as usage_count
    FROM ""APPATTACH_CATALOGUES"" ac
    WHERE ac.""TEMPLATE_ID"" IS NOT NULL
      AND ac.""TEMPLATE_ID"" = @templateId
      AND ac.""IS_DELETED"" = false";

var usageCount = await dbContext.Database
    .SqlQueryRaw<dynamic>(sql, parameters)
    .FirstOrDefaultAsync();

var count = usageCount?.usage_count != null ? Convert.ToInt32(usageCount.usage_count) : 0;
```

**迁移后（EF Core 查询）**:

```csharp
var usageCount = await dbContext.Set<AttachCatalogue>()
    .Where(ac => ac.TemplateId == templateId && !ac.IsDeleted)
    .CountAsync();

return usageCount;
```

## 迁移的优势

### 1. 类型安全

-   **EF Core**: 编译时类型检查，避免运行时类型错误
-   **SQL**: 运行时类型转换，容易出现 `字段 t.Value 不存在` 等错误

### 2. 代码可读性

-   **EF Core**: 强类型的 LINQ 查询，代码更清晰易懂
-   **SQL**: 字符串拼接，容易出现语法错误

### 3. 维护性

-   **EF Core**: 实体关系清晰，修改实体时查询自动适应
-   **SQL**: 硬编码字段名，修改实体时需要同步修改 SQL

### 4. 性能优化

-   **EF Core**: 自动生成优化的 SQL，支持查询缓存
-   **SQL**: 手动编写，需要手动优化

### 5. 错误处理

-   **EF Core**: 更好的异常处理和调试信息
-   **SQL**: 错误信息不够详细，调试困难

## 新增的辅助方法

### DetermineNamePattern 方法

```csharp
private static string DetermineNamePattern(List<string> fileNames)
{
    if (!fileNames.Any())
        return "{Type}_{ProjectName}_{Date}";

    var sampleFileName = fileNames.First();

    if (sampleFileName.Contains("{ProjectName}") || sampleFileName.Contains("项目"))
    {
        return "{Type}_{ProjectName}_{Date}_{Version}";
    }
    else if (sampleFileName.Contains("{Date}") || sampleFileName.Contains("日期"))
    {
        return "{Type}_{Date}_{Version}";
    }
    else if (sampleFileName.Contains("{Version}") || sampleFileName.Contains("版本"))
    {
        return "{Type}_{ProjectName}_{Version}";
    }
    else
    {
        return "{Type}_{ProjectName}_{Date}";
    }
}
```

## 注意事项

1. **性能考虑**: EF Core 查询在某些复杂场景下可能不如原生 SQL 高效，但提供了更好的可维护性
2. **数据库特定功能**: 某些 PostgreSQL 特定功能（如 `similarity` 函数）仍需要使用 SQL 查询
3. **迁移测试**: 建议在生产环境部署前进行充分的测试，确保功能正常

## 总结

通过将 SQL 查询迁移到 EF Core 查询，我们：

1. **解决了类型安全问题**: 消除了 `字段 t.Value 不存在` 等错误
2. **提高了代码质量**: 更好的类型安全和可读性
3. **增强了可维护性**: 实体关系清晰，修改更容易
4. **改善了错误处理**: 更好的异常信息和调试支持

这次迁移为智能推荐功能提供了更稳定、更可维护的基础。
