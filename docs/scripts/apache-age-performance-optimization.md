# Apache AGE 图数据库性能优化指南

## 📋 概述

本文档基于项目上下文，详细说明 Apache AGE 图数据库的性能优化策略和实施方法。

## 1. 项目上下文分析

### 1.1 数据规模预估

-   **分类节点（Catalogue）**：预计 1,000 - 10,000 个
-   **人员节点（Person）**：预计 100 - 1,000 个
-   **部门节点（Department）**：预计 10 - 100 个
-   **业务实体节点（BusinessEntity）**：预计 50 - 500 个
-   **工作流节点（Workflow）**：预计 20 - 200 个
-   **关系数量**：预计 5,000 - 50,000 条

### 1.2 查询模式分析

#### 高频查询模式

1. **分类树查询**（最高频）

    ```cypher
    MATCH path = (root:Catalogue {id: $rootId})-[:HAS_CHILD*]->(child:Catalogue)
    RETURN path
    ```

2. **分类关系查询**

    ```cypher
    MATCH (c:Catalogue {id: $catalogId})-[r:RELATES_TO]->(related:Catalogue)
    RETURN c, r, related
    ```

3. **人员-分类关系查询**

    ```cypher
    MATCH (p:Person {id: $personId})-[r:RELATES_TO]->(c:Catalogue)
    WHERE r.role = $role
    RETURN p, r, c
    ```

4. **影响分析查询**（路径查询）

    ```cypher
    MATCH path = (start:Catalogue {id: $startId})-[*1..3]-(affected)
    RETURN path
    ```

5. **全文搜索**
    ```sql
    SELECT * FROM "APPATTACH_CATALOGUES"
    WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', $keyword)
    ```

## 2. 索引优化策略

### 2.1 业务表索引（核心优化）

#### 分类表索引

```sql
-- 主键索引（自动创建）
-- PRIMARY KEY ("Id")

-- 单列索引
CREATE INDEX idx_catalogue_id ON "APPATTACH_CATALOGUES"("Id");
CREATE INDEX idx_catalogue_name ON "APPATTACH_CATALOGUES"("CATALOGUE_NAME");
CREATE INDEX idx_catalogue_status ON "APPATTACH_CATALOGUES"("STATUS") WHERE "STATUS" IS NOT NULL;

-- 外键索引（树形结构）
CREATE INDEX idx_catalogue_parent_id ON "APPATTACH_CATALOGUES"("PARENT_ID") WHERE "PARENT_ID" IS NOT NULL;

-- 复合索引（业务引用）
CREATE INDEX idx_catalogue_reference ON "APPATTACH_CATALOGUES"("REFERENCE", "REFERENCE_TYPE") WHERE "REFERENCE" IS NOT NULL;

-- 全文搜索索引
CREATE INDEX idx_catalogue_name_fts ON "APPATTACH_CATALOGUES"
USING gin(to_tsvector('chinese_fts', "CATALOGUE_NAME"));
```

#### 关系表索引（最关键）

```sql
-- 源实体索引（用于查找节点的出边）
CREATE INDEX idx_kg_relationships_source
ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "SOURCE_ENTITY_TYPE");

-- 目标实体索引（用于查找节点的入边）
CREATE INDEX idx_kg_relationships_target
ON "APPKG_RELATIONSHIPS"("TARGET_ENTITY_ID", "TARGET_ENTITY_TYPE");

-- 关系类型索引（用于过滤特定类型的关系）
CREATE INDEX idx_kg_relationships_type
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE");

-- 复合索引（用于快速查找特定类型的关系）
CREATE INDEX idx_kg_relationships_composite
ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "RELATIONSHIP_TYPE");

-- 角色和语义类型索引（用于抽象关系类型查询）
CREATE INDEX idx_kg_relationships_role
ON "APPKG_RELATIONSHIPS"("ROLE") WHERE "ROLE" IS NOT NULL;

CREATE INDEX idx_kg_relationships_semantic_type
ON "APPKG_RELATIONSHIPS"("SEMANTIC_TYPE") WHERE "SEMANTIC_TYPE" IS NOT NULL;

-- 组合索引（用于特定关系类型+角色的查询）
CREATE INDEX idx_kg_relationships_type_role
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "ROLE") WHERE "ROLE" IS NOT NULL;

CREATE INDEX idx_kg_relationships_type_semantic
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "SEMANTIC_TYPE") WHERE "SEMANTIC_TYPE" IS NOT NULL;
```

### 2.2 索引使用分析

#### 查询优化器利用索引的场景

1. **节点查找**

    ```cypher
    MATCH (c:Catalogue {id: $id})
    ```

    - 使用：`idx_catalogue_id`
    - 性能：O(log n)

2. **关系查找（出边）**

    ```cypher
    MATCH (c:Catalogue {id: $id})-[r]->()
    ```

    - 使用：`idx_kg_relationships_source`
    - 性能：O(log n + m)，m 为关系数量

3. **关系查找（入边）**

    ```cypher
    MATCH ()-[r]->(c:Catalogue {id: $id})
    ```

    - 使用：`idx_kg_relationships_target`
    - 性能：O(log n + m)

4. **特定类型关系查找**

    ```cypher
    MATCH (c:Catalogue {id: $id})-[r:RELATES_TO]->()
    ```

    - 使用：`idx_kg_relationships_composite`
    - 性能：O(log n + m')

5. **角色过滤查询**
    ```cypher
    MATCH (p:Person)-[r:RELATES_TO {role: 'Manager'}]->(c:Catalogue)
    ```
    - 使用：`idx_kg_relationships_type_role`
    - 性能：O(log n + m')

## 3. PostgreSQL 配置优化

### 3.1 内存配置

```sql
-- 在 postgresql.conf 中配置（需要重启 PostgreSQL）

-- 共享缓冲区（建议设置为系统内存的 25%）
shared_buffers = 4GB

-- 工作内存（用于排序和哈希操作）
work_mem = 256MB

-- 维护工作内存（用于 VACUUM、CREATE INDEX 等操作）
maintenance_work_mem = 1GB

-- 有效缓存大小（建议设置为系统内存的 50-75%）
effective_cache_size = 12GB
```

### 3.2 查询优化配置

```sql
-- 并行查询配置
max_parallel_workers_per_gather = 4
max_parallel_workers = 8
max_worker_processes = 8

-- 查询计划器配置
random_page_cost = 1.1  -- SSD 存储
effective_io_concurrency = 200  -- SSD 存储

-- 连接配置
max_connections = 200
```

### 3.3 会话级别配置（临时优化）

```sql
-- 在应用连接时设置
SET work_mem = '256MB';
SET max_parallel_workers_per_gather = 4;
SET enable_seqscan = on;  -- 允许顺序扫描（某些场景下可能更快）
SET enable_indexscan = on;  -- 启用索引扫描
SET enable_bitmapscan = on;  -- 启用位图扫描
```

## 4. 查询优化技巧

### 4.1 Cypher 查询优化

#### ✅ 好的做法

```cypher
-- 1. 使用索引字段进行过滤
MATCH (c:Catalogue {id: $id})  -- ✅ 使用主键
RETURN c

-- 2. 限制查询深度
MATCH path = (start:Catalogue {id: $id})-[*1..3]-(related)  -- ✅ 限制深度
RETURN path
LIMIT 100  -- ✅ 限制结果数量

-- 3. 使用 WHERE 子句提前过滤
MATCH (c:Catalogue)
WHERE c.status = 'ACTIVE' AND c.id = $id  -- ✅ 提前过滤
RETURN c

-- 4. 使用投影减少数据传输
MATCH (c:Catalogue {id: $id})-[r]->(related)
RETURN c.id, c.name, type(r), related.id, related.name  -- ✅ 只返回需要的字段
```

#### ❌ 避免的做法

```cypher
-- 1. 避免全图扫描
MATCH (c:Catalogue)  -- ❌ 全图扫描
WHERE c.id = $id
RETURN c

-- 2. 避免过深的路径查询
MATCH path = (start)-[*]-(end)  -- ❌ 无深度限制，可能导致性能问题
RETURN path

-- 3. 避免返回大量数据
MATCH (c:Catalogue)-[r]->(related)
RETURN c, r, related  -- ❌ 返回完整对象，数据量大
```

### 4.2 混合查询优化（PostgreSQL + AGE）

```sql
-- 场景：需要同时查询业务数据和图数据

-- ✅ 好的做法：先过滤业务数据，再查询图数据
WITH filtered_catalogues AS (
    SELECT "Id" FROM "APPATTACH_CATALOGUES"
    WHERE "STATUS" = 'ACTIVE'
    LIMIT 100
)
SELECT * FROM cypher('kg_graph', $$
    MATCH (c:Catalogue)
    WHERE c.id IN $ids
    MATCH (c)-[r]->(related)
    RETURN c, r, related
$$, jsonb_build_object('ids', array_agg("Id")::text[])) AS (c agtype, r agtype, related agtype)
FROM filtered_catalogues;

-- ❌ 避免：在图数据库中过滤大量数据
SELECT * FROM cypher('kg_graph', $$
    MATCH (c:Catalogue)
    WHERE c.status = 'ACTIVE'  -- ❌ 图数据库中没有业务状态索引
    RETURN c
$$) AS (c agtype);
```

## 5. 物化视图优化

### 5.1 创建统计物化视图

```sql
-- 分类关系统计视图（用于快速获取分类的关系数量）
CREATE MATERIALIZED VIEW mv_catalogue_relationship_stats AS
SELECT
    "SOURCE_ENTITY_ID" AS catalogue_id,
    "SOURCE_ENTITY_TYPE",
    COUNT(*) AS relationship_count,
    COUNT(DISTINCT "TARGET_ENTITY_TYPE") AS target_type_count,
    MAX("CreationTime") AS last_relationship_time
FROM "APPKG_RELATIONSHIPS"
WHERE "SOURCE_ENTITY_TYPE" = 'Catalogue'
    AND "IsDeleted" = FALSE
GROUP BY "SOURCE_ENTITY_ID", "SOURCE_ENTITY_TYPE";

CREATE UNIQUE INDEX idx_mv_catalogue_rel_stats_id
ON mv_catalogue_relationship_stats(catalogue_id);
```

### 5.2 定期刷新物化视图

```sql
-- 创建刷新函数
CREATE OR REPLACE FUNCTION refresh_graph_statistics()
RETURNS VOID AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_catalogue_relationship_stats;
    ANALYZE "APPATTACH_CATALOGUES";
    ANALYZE "APPKG_RELATIONSHIPS";
END;
$$ LANGUAGE plpgsql;

-- 使用 pg_cron 扩展定期刷新（如果可用）
-- SELECT cron.schedule('refresh-graph-stats', '0 2 * * *', 'SELECT refresh_graph_statistics();');
```

## 6. 连接池优化

### 6.1 Npgsql 连接池配置

```csharp
// 在应用启动时配置
services.AddNpgsql<AttachmentDbContext>(
    connectionString,
    options => options
        .MaxPoolSize(100)           // 最大连接数
        .MinPoolSize(10)            // 最小连接数
        .ConnectionIdleLifetime(TimeSpan.FromMinutes(5))  // 空闲连接生命周期
        .ConnectionPruningInterval(TimeSpan.FromSeconds(10))  // 连接清理间隔
);
```

### 6.2 查询超时配置

```csharp
// 设置命令超时（防止长时间运行的查询）
options.CommandTimeout(30);  // 30 秒超时
```

## 7. 监控和诊断

### 7.1 查询性能监控

```sql
-- 启用查询日志（在 postgresql.conf 中）
log_min_duration_statement = 1000  -- 记录执行时间超过 1 秒的查询
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '

-- 查看慢查询
SELECT
    query,
    calls,
    total_time,
    mean_time,
    max_time
FROM pg_stat_statements
WHERE mean_time > 1000  -- 平均执行时间超过 1 秒
ORDER BY mean_time DESC
LIMIT 20;
```

### 7.2 索引使用情况监控

```sql
-- 查看索引使用统计
SELECT
    schemaname,
    tablename,
    indexname,
    idx_scan,  -- 索引扫描次数
    idx_tup_read,  -- 通过索引读取的元组数
    idx_tup_fetch  -- 通过索引获取的元组数
FROM pg_stat_user_indexes
WHERE schemaname = 'public'
ORDER BY idx_scan DESC;
```

### 7.3 图数据库统计信息

```sql
-- 使用自定义函数获取统计信息
SELECT * FROM get_graph_statistics();

-- 验证数据完整性
SELECT * FROM validate_graph_data();
```

## 8. 性能测试建议

### 8.1 测试场景

1. **单节点查询**

    - 测试：查询单个分类及其直接关系
    - 目标：< 50ms

2. **树形查询**

    - 测试：查询分类树（深度 3-5 层）
    - 目标：< 200ms

3. **路径查询**

    - 测试：查找两个节点之间的路径（深度 1-3）
    - 目标：< 500ms

4. **影响分析查询**

    - 测试：计算节点的影响范围（深度 2-3）
    - 目标：< 1s

5. **全文搜索**
    - 测试：搜索分类名称
    - 目标：< 100ms

### 8.2 压力测试

```sql
-- 使用 pgbench 进行压力测试
pgbench -c 10 -j 2 -T 60 -f test_queries.sql your_database
```

## 9. 常见性能问题及解决方案

### 9.1 问题：查询速度慢

**原因**：

-   缺少索引
-   查询深度过深
-   返回数据量过大

**解决方案**：

-   检查并创建缺失的索引
-   限制查询深度（使用 `[*1..3]` 而不是 `[*]`）
-   使用 `LIMIT` 限制结果数量
-   使用投影只返回需要的字段

### 9.2 问题：内存使用过高

**原因**：

-   `work_mem` 设置过大
-   并行查询过多
-   物化视图过大

**解决方案**：

-   调整 `work_mem` 设置
-   限制并行查询数量
-   定期清理物化视图
-   使用 `EXPLAIN ANALYZE` 分析查询计划

### 9.3 问题：索引未使用

**原因**：

-   统计信息过期
-   查询条件不匹配索引
-   索引选择性低

**解决方案**：

-   运行 `ANALYZE` 更新统计信息
-   检查查询条件是否匹配索引
-   考虑创建复合索引

## 10. 最佳实践总结

1. ✅ **索引优先**：为常用查询字段创建索引
2. ✅ **限制深度**：路径查询限制深度和结果数量
3. ✅ **使用投影**：只返回需要的字段
4. ✅ **定期维护**：定期运行 `ANALYZE` 和 `VACUUM`
5. ✅ **监控性能**：使用 `pg_stat_statements` 监控查询性能
6. ✅ **连接池**：合理配置连接池大小
7. ✅ **物化视图**：对常用统计查询使用物化视图
8. ✅ **混合查询**：结合 PostgreSQL 和 AGE 的优势

---

**文档版本**：v1.0  
**最后更新**：2024 年  
**维护者**：开发团队
