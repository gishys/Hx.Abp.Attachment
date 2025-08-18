# 仓储层重构总结

## 概述

本次重构为全文搜索功能增加了专门的仓储层，并根据实际的数据库表结构修改了 SQL 查询，使其与 `AttachCatalogueEntityTypeConfiguration` 中定义的表名和字段名保持一致。

## 主要更改

### 1. 新增仓储层

#### 仓储接口

-   **文件**: `src/Hx.Abp.Attachment.Application.Contracts/Hx/Abp/Attachment/Application/Contracts/IFullTextSearchRepository.cs`
-   **功能**: 定义全文搜索的仓储接口，包含所有搜索方法

#### 仓储实现

-   **文件**: `src/Hx.Abp.Attachment.EntityFrameworkCore/Hx/Abp/Attachment/EntityFrameworkCore/FullTextSearchRepository.cs`
-   **功能**: 实现具体的搜索逻辑，使用正确的表名和字段名

### 2. 服务层重构

#### FullTextSearchService 简化

-   **文件**: `src/Hx.Abp.Attachment.Application/Hx/Abp/Attachment/Application/FullTextSearchService.cs`
-   **更改**: 移除直接的 SQL 查询，改为调用仓储层方法
-   **优势**: 更好的关注点分离，服务层专注于业务逻辑

### 3. 数据库表名和字段名修正

根据 `AttachCatalogueEntityTypeConfiguration` 和 `AttachFileEntityTypeConfiguration` 的配置：

#### 表名映射

-   `AttachCatalogues` → `APPATTACH_CATALOGUES`
-   `AttachFiles` → `APPATTACHFILE`

#### 字段名映射

-   `Name` → `CATALOGUE_NAME` (目录表)
-   `Name` → `FILENAME` (文件表)

### 4. SQL 查询更新

#### 更新位置

1. **FullTextSearchRepository.cs** - 所有搜索 SQL
2. **AttachmentDbContext.cs** - 索引创建 SQL
3. **database-test.sql** - 测试脚本 SQL
4. **README.md** - 文档示例 SQL

#### 示例对比

**更新前:**

```sql
SELECT * FROM "AttachCatalogues"
WHERE to_tsvector('chinese_fts', "Name") @@ plainto_tsquery('chinese_fts', @query)
```

**更新后:**

```sql
SELECT * FROM "APPATTACH_CATALOGUES"
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', @query)
```

### 5. 依赖注入配置

#### 模块注册

-   **文件**: `src/Hx.Abp.Attachment.EntityFrameworkCore/Hx/Abp/Attachment/EntityFrameworkCore/HxAbpAttachmentEntityFrameworkCoreModule.cs`
-   **更改**: 添加仓储接口和实现的依赖注入注册

```csharp
// 注册全文搜索仓储
context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();
```

## 架构优势

### 1. 分层清晰

-   **仓储层**: 负责数据访问和 SQL 查询
-   **服务层**: 负责业务逻辑和流程控制
-   **控制器层**: 负责 API 接口和请求处理

### 2. 可维护性提升

-   SQL 查询集中在仓储层，便于维护
-   服务层代码更简洁，专注于业务逻辑
-   接口和实现分离，便于测试和扩展

### 3. 数据一致性

-   使用正确的表名和字段名，避免运行时错误
-   与 Entity Framework 配置保持一致

## 文件清单

### 新增文件

-   `IFullTextSearchRepository.cs` - 仓储接口
-   `FullTextSearchRepository.cs` - 仓储实现
-   `REPOSITORY_LAYER_SUMMARY.md` - 本总结文档

### 修改文件

-   `FullTextSearchService.cs` - 简化服务层
-   `AttachmentDbContext.cs` - 更新索引创建 SQL
-   `HxAbpAttachmentEntityFrameworkCoreModule.cs` - 添加依赖注入
-   `database-test.sql` - 更新测试脚本
-   `README.md` - 更新文档示例

## 使用方式

### 1. 服务层调用

```csharp
public class FullTextSearchService : DomainService
{
    private readonly IFullTextSearchRepository _searchRepository;

    public FullTextSearchService(IFullTextSearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    public async Task<List<AttachCatalogue>> SearchCataloguesAsync(string query)
    {
        return await _searchRepository.SearchCataloguesAsync(query);
    }
}
```

### 2. 控制器调用

```csharp
[HttpGet("catalogues")]
public async Task<IActionResult> SearchCatalogues([FromQuery] string query)
{
    var results = await _searchService.SearchCataloguesAsync(query);
    return Ok(results);
}
```

## 总结

通过引入仓储层和修正数据库表结构，全文搜索功能现在具有：

-   ✅ 更清晰的架构分层
-   ✅ 更好的可维护性
-   ✅ 正确的数据库映射
-   ✅ 统一的 SQL 查询管理
-   ✅ 便于测试和扩展的设计

这个重构为后续的功能扩展和维护奠定了良好的基础。
