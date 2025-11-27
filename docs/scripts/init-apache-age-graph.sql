-- =====================================================
-- Apache AGE 图数据库初始化与优化脚本
-- 项目：多维知识图谱系统
-- 版本：v1.0
-- 说明：本脚本用于初始化 Apache AGE 图数据库，创建图结构，同步业务数据，并进行性能优化
-- =====================================================

-- =====================================================
-- 第一部分：环境检查与扩展安装
-- =====================================================

-- 检查 PostgreSQL 版本（需要 12+）
DO $$
BEGIN
    IF current_setting('server_version_num')::int < 120000 THEN
        RAISE EXCEPTION 'PostgreSQL 版本需要 12.0 或更高版本';
    END IF;
END $$;

-- 检查是否已安装 Apache AGE 扩展
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_extension WHERE extname = 'age'
    ) THEN
        RAISE NOTICE '正在创建 Apache AGE 扩展...';
        CREATE EXTENSION IF NOT EXISTS age;
    ELSE
        RAISE NOTICE 'Apache AGE 扩展已存在';
    END IF;
END $$;

-- 加载 AGE 扩展
LOAD 'age';

-- =====================================================
-- 第二部分：创建图数据库
-- =====================================================

-- 检查图数据库是否已存在
DO $$
DECLARE
    graph_exists BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM ag_graph WHERE name = 'kg_graph'
    ) INTO graph_exists;
    
    IF NOT graph_exists THEN
        PERFORM create_graph('kg_graph');
        RAISE NOTICE '图数据库 kg_graph 创建成功';
    ELSE
        RAISE NOTICE '图数据库 kg_graph 已存在';
    END IF;
END $$;

-- =====================================================
-- 第三部分：创建性能优化索引
-- =====================================================

-- 注意：Apache AGE 使用 PostgreSQL 的索引机制
-- 在业务表上创建索引，AGE 查询会自动利用这些索引

-- 1. 分类表索引
CREATE INDEX IF NOT EXISTS idx_catalogue_id 
ON "APPATTACH_CATALOGUES"("Id");

CREATE INDEX IF NOT EXISTS idx_catalogue_name 
ON "APPATTACH_CATALOGUES"("CATALOGUE_NAME");

CREATE INDEX IF NOT EXISTS idx_catalogue_status 
ON "APPATTACH_CATALOGUES"("STATUS") 
WHERE "STATUS" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_catalogue_parent_id 
ON "APPATTACH_CATALOGUES"("PARENT_ID") 
WHERE "PARENT_ID" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_catalogue_reference 
ON "APPATTACH_CATALOGUES"("REFERENCE", "REFERENCE_TYPE") 
WHERE "REFERENCE" IS NOT NULL;

-- 2. 关系表索引（核心性能优化）
CREATE INDEX IF NOT EXISTS idx_kg_relationships_source 
ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "SOURCE_ENTITY_TYPE");

CREATE INDEX IF NOT EXISTS idx_kg_relationships_target 
ON "APPKG_RELATIONSHIPS"("TARGET_ENTITY_ID", "TARGET_ENTITY_TYPE");

CREATE INDEX IF NOT EXISTS idx_kg_relationships_type 
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE");

CREATE INDEX IF NOT EXISTS idx_kg_relationships_source_type 
ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_TYPE", "RELATIONSHIP_TYPE");

CREATE INDEX IF NOT EXISTS idx_kg_relationships_target_type 
ON "APPKG_RELATIONSHIPS"("TARGET_ENTITY_TYPE", "RELATIONSHIP_TYPE");

-- 复合索引：用于快速查找特定类型的关系
CREATE INDEX IF NOT EXISTS idx_kg_relationships_composite 
ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "RELATIONSHIP_TYPE");

-- 角色和语义类型索引（用于抽象关系类型查询）
CREATE INDEX IF NOT EXISTS idx_kg_relationships_role 
ON "APPKG_RELATIONSHIPS"("ROLE") 
WHERE "ROLE" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_kg_relationships_semantic_type 
ON "APPKG_RELATIONSHIPS"("SEMANTIC_TYPE") 
WHERE "SEMANTIC_TYPE" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_kg_relationships_type_role 
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "ROLE") 
WHERE "ROLE" IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_kg_relationships_type_semantic 
ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "SEMANTIC_TYPE") 
WHERE "SEMANTIC_TYPE" IS NOT NULL;

-- 3. 全文搜索索引（PostgreSQL 内置）
CREATE INDEX IF NOT EXISTS idx_catalogue_name_fts 
ON "APPATTACH_CATALOGUES" 
USING gin(to_tsvector('chinese_fts', "CATALOGUE_NAME"));

-- 4. 时间戳索引（用于时间轴查询）
CREATE INDEX IF NOT EXISTS idx_kg_relationships_creation_time 
ON "APPKG_RELATIONSHIPS"("CreationTime" DESC);

CREATE INDEX IF NOT EXISTS idx_kg_relationships_modification_time 
ON "APPKG_RELATIONSHIPS"("LastModificationTime" DESC) 
WHERE "LastModificationTime" IS NOT NULL;

-- =====================================================
-- 第四部分：数据同步函数（从业务表同步到图数据库）
-- =====================================================

-- 函数：同步单个分类节点到图数据库
CREATE OR REPLACE FUNCTION sync_catalogue_to_graph(catalogue_id UUID)
RETURNS VOID AS $$
DECLARE
    catalogue_record RECORD;
BEGIN
    -- 获取分类数据
    SELECT 
        "Id",
        "CATALOGUE_NAME",
        "STATUS",
        "PARENT_ID",
        "REFERENCE",
        "REFERENCE_TYPE",
        "CATALOGUE_FACET_TYPE",
        "CATALOGUE_PURPOSE"
    INTO catalogue_record
    FROM "APPATTACH_CATALOGUES"
    WHERE "Id" = catalogue_id;
    
    IF NOT FOUND THEN
        RAISE NOTICE '分类不存在: %', catalogue_id;
        RETURN;
    END IF;
    
    -- 在图中创建或更新节点
    PERFORM * FROM cypher('kg_graph', format($cypher$
        MERGE (c:Catalogue {id: $id})
        SET c.name = $name,
            c.status = $status,
            c.parentId = $parentId,
            c.reference = $reference,
            c.referenceType = $referenceType,
            c.facetType = $facetType,
            c.purpose = $purpose,
            c.updatedTime = $updatedTime
        RETURN c
    $cypher$, jsonb_build_object(
        'id', catalogue_record."Id"::text,
        'name', catalogue_record."CATALOGUE_NAME",
        'status', catalogue_record."STATUS",
        'parentId', catalogue_record."PARENT_ID"::text,
        'reference', catalogue_record."REFERENCE",
        'referenceType', catalogue_record."REFERENCE_TYPE",
        'facetType', catalogue_record."CATALOGUE_FACET_TYPE",
        'purpose', catalogue_record."CATALOGUE_PURPOSE",
        'updatedTime', now()::text
    )));
    
    RAISE NOTICE '分类节点同步成功: %', catalogue_id;
END;
$$ LANGUAGE plpgsql;

-- 函数：同步单个关系到图数据库
CREATE OR REPLACE FUNCTION sync_relationship_to_graph(relationship_id UUID)
RETURNS VOID AS $$
DECLARE
    rel_record RECORD;
    rel_type_name TEXT;
    cypher_query TEXT;
BEGIN
    -- 获取关系数据
    SELECT 
        "Id",
        "SOURCE_ENTITY_ID",
        "SOURCE_ENTITY_TYPE",
        "TARGET_ENTITY_ID",
        "TARGET_ENTITY_TYPE",
        "RELATIONSHIP_TYPE",
        "ROLE",
        "SEMANTIC_TYPE",
        "DESCRIPTION",
        "WEIGHT"
    INTO rel_record
    FROM "APPKG_RELATIONSHIPS"
    WHERE "Id" = relationship_id;
    
    IF NOT FOUND THEN
        RAISE NOTICE '关系不存在: %', relationship_id;
        RETURN;
    END IF;
    
    -- 映射关系类型到 Cypher 关系名称
    rel_type_name := CASE rel_record."RELATIONSHIP_TYPE"
        WHEN 'CatalogueHasChild' THEN 'HAS_CHILD'
        WHEN 'CatalogueRelatesToCatalogue' THEN 'RELATES_TO'
        WHEN 'PersonRelatesToCatalogue' THEN 'RELATES_TO'
        WHEN 'PersonBelongsToDepartment' THEN 'BELONGS_TO'
        WHEN 'DepartmentOwnsCatalogue' THEN 'OWNS'
        WHEN 'DepartmentManagesCatalogue' THEN 'MANAGES'
        WHEN 'DepartmentHasParent' THEN 'HAS_PARENT'
        WHEN 'BusinessEntityHasCatalogue' THEN 'HAS'
        WHEN 'BusinessEntityManagesCatalogue' THEN 'MANAGES'
        WHEN 'CatalogueReferencesBusiness' THEN 'REFERENCES'
        WHEN 'CatalogueUsesWorkflow' THEN 'USES'
        WHEN 'WorkflowManagesCatalogue' THEN 'MANAGES'
        WHEN 'WorkflowInstanceBelongsToCatalogue' THEN 'INSTANCE_OF'
        WHEN 'PersonRelatesToWorkflow' THEN 'RELATES_TO'
        WHEN 'DepartmentOwnsWorkflow' THEN 'OWNS'
        WHEN 'WorkflowRelatesToWorkflow' THEN 'RELATES_TO'
        ELSE 'RELATES_TO'
    END;
    
    -- 构建 Cypher 查询
    cypher_query := format($cypher$
        MATCH (source:%I {id: $sourceId})
        MATCH (target:%I {id: $targetId})
        MERGE (source)-[r:%I]->(target)
        SET r.relationshipId = $relationshipId,
            r.type = $type,
            r.role = $role,
            r.semanticType = $semanticType,
            r.description = $description,
            r.weight = $weight,
            r.updatedTime = $updatedTime
        RETURN r
    $cypher$,
        rel_record."SOURCE_ENTITY_TYPE",
        rel_record."TARGET_ENTITY_TYPE",
        rel_type_name
    );
    
    -- 执行 Cypher 查询
    PERFORM * FROM cypher('kg_graph', cypher_query, jsonb_build_object(
        'sourceId', rel_record."SOURCE_ENTITY_ID"::text,
        'targetId', rel_record."TARGET_ENTITY_ID"::text,
        'relationshipId', rel_record."Id"::text,
        'type', rel_record."RELATIONSHIP_TYPE",
        'role', rel_record."ROLE",
        'semanticType', rel_record."SEMANTIC_TYPE",
        'description', rel_record."DESCRIPTION",
        'weight', rel_record."WEIGHT",
        'updatedTime', now()::text
    ));
    
    RAISE NOTICE '关系同步成功: %', relationship_id;
END;
$$ LANGUAGE plpgsql;

-- 函数：批量同步所有分类节点
CREATE OR REPLACE FUNCTION sync_all_catalogues_to_graph()
RETURNS TABLE(synced_count BIGINT, error_count BIGINT) AS $$
DECLARE
    catalogue_record RECORD;
    synced BIGINT := 0;
    errors BIGINT := 0;
BEGIN
    FOR catalogue_record IN 
        SELECT "Id" FROM "APPATTACH_CATALOGUES"
        WHERE "IsDeleted" = FALSE
    LOOP
        BEGIN
            PERFORM sync_catalogue_to_graph(catalogue_record."Id");
            synced := synced + 1;
        EXCEPTION WHEN OTHERS THEN
            errors := errors + 1;
            RAISE WARNING '同步分类失败: %, 错误: %', catalogue_record."Id", SQLERRM;
        END;
    END LOOP;
    
    RETURN QUERY SELECT synced, errors;
END;
$$ LANGUAGE plpgsql;

-- 函数：批量同步所有关系到图数据库
CREATE OR REPLACE FUNCTION sync_all_relationships_to_graph()
RETURNS TABLE(synced_count BIGINT, error_count BIGINT) AS $$
DECLARE
    rel_record RECORD;
    synced BIGINT := 0;
    errors BIGINT := 0;
BEGIN
    FOR rel_record IN 
        SELECT "Id" FROM "APPKG_RELATIONSHIPS"
        WHERE "IsDeleted" = FALSE
    LOOP
        BEGIN
            PERFORM sync_relationship_to_graph(rel_record."Id");
            synced := synced + 1;
        EXCEPTION WHEN OTHERS THEN
            errors := errors + 1;
            RAISE WARNING '同步关系失败: %, 错误: %', rel_record."Id", SQLERRM;
        END;
    END LOOP;
    
    RETURN QUERY SELECT synced, errors;
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 第五部分：性能优化配置
-- =====================================================

-- 1. 更新表统计信息（用于查询优化器）
ANALYZE "APPATTACH_CATALOGUES";
ANALYZE "APPKG_RELATIONSHIPS";

-- 2. 设置 PostgreSQL 性能参数（针对图查询优化）
-- 注意：这些设置需要在 postgresql.conf 中配置，或在会话级别设置

-- 增加 work_mem（用于排序和哈希操作）
SET work_mem = '256MB';

-- 增加 shared_buffers（如果服务器内存充足）
-- SET shared_buffers = '4GB';  -- 需要在 postgresql.conf 中设置

-- 启用并行查询
SET max_parallel_workers_per_gather = 4;
SET max_parallel_workers = 8;

-- 3. 创建物化视图（用于常用查询优化）
-- 分类关系统计视图
CREATE MATERIALIZED VIEW IF NOT EXISTS mv_catalogue_relationship_stats AS
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

CREATE UNIQUE INDEX IF NOT EXISTS idx_mv_catalogue_rel_stats_id 
ON mv_catalogue_relationship_stats(catalogue_id);

-- 4. 创建刷新物化视图的函数
CREATE OR REPLACE FUNCTION refresh_graph_statistics()
RETURNS VOID AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_catalogue_relationship_stats;
    ANALYZE "APPATTACH_CATALOGUES";
    ANALYZE "APPKG_RELATIONSHIPS";
    RAISE NOTICE '图数据库统计信息已刷新';
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 第六部分：数据验证与监控查询
-- =====================================================

-- 函数：验证图数据库数据完整性
CREATE OR REPLACE FUNCTION validate_graph_data()
RETURNS TABLE(
    check_type TEXT,
    expected_count BIGINT,
    actual_count BIGINT,
    status TEXT
) AS $$
BEGIN
    RETURN QUERY
    -- 检查分类节点数量
    SELECT 
        'Catalogue Nodes'::TEXT,
        (SELECT COUNT(*) FROM "APPATTACH_CATALOGUES" WHERE "IsDeleted" = FALSE)::BIGINT,
        (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (c:Catalogue) RETURN count(c)$$) AS (count agtype)),
        CASE 
            WHEN (SELECT COUNT(*) FROM "APPATTACH_CATALOGUES" WHERE "IsDeleted" = FALSE) = 
                 (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (c:Catalogue) RETURN count(c)$$) AS (count agtype))
            THEN 'OK'::TEXT
            ELSE 'MISMATCH'::TEXT
        END
    
    UNION ALL
    
    -- 检查关系数量
    SELECT 
        'Relationships'::TEXT,
        (SELECT COUNT(*) FROM "APPKG_RELATIONSHIPS" WHERE "IsDeleted" = FALSE)::BIGINT,
        (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH ()-[r]->() RETURN count(r)$$) AS (count agtype)),
        CASE 
            WHEN (SELECT COUNT(*) FROM "APPKG_RELATIONSHIPS" WHERE "IsDeleted" = FALSE) = 
                 (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH ()-[r]->() RETURN count(r)$$) AS (count agtype))
            THEN 'OK'::TEXT
            ELSE 'MISMATCH'::TEXT
        END;
END;
$$ LANGUAGE plpgsql;

-- 函数：获取图数据库统计信息
CREATE OR REPLACE FUNCTION get_graph_statistics()
RETURNS TABLE(
    metric_name TEXT,
    metric_value BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 'Total Nodes'::TEXT, 
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (n) RETURN count(n)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'Total Relationships'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH ()-[r]->() RETURN count(r)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'Catalogue Nodes'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (c:Catalogue) RETURN count(c)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'Person Nodes'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (p:Person) RETURN count(p)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'Department Nodes'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (d:Department) RETURN count(d)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'BusinessEntity Nodes'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (b:BusinessEntity) RETURN count(b)$$) AS (count agtype))
    
    UNION ALL
    
    SELECT 'Workflow Nodes'::TEXT,
           (SELECT COUNT(*)::BIGINT FROM cypher('kg_graph', $$MATCH (w:Workflow) RETURN count(w)$$) AS (count agtype));
END;
$$ LANGUAGE plpgsql;

-- =====================================================
-- 第七部分：触发器（自动同步数据到图数据库）
-- =====================================================

-- 触发器函数：分类变更时自动同步到图数据库
CREATE OR REPLACE FUNCTION trigger_sync_catalogue_to_graph()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        -- 删除图数据库中的节点
        PERFORM * FROM cypher('kg_graph', format($cypher$
            MATCH (c:Catalogue {id: $id})
            DETACH DELETE c
        $cypher$, jsonb_build_object('id', OLD."Id"::text)));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' OR TG_OP = 'INSERT' THEN
        -- 同步节点到图数据库
        PERFORM sync_catalogue_to_graph(NEW."Id");
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- 创建触发器（可选，建议在数据稳定后启用）
-- CREATE TRIGGER trg_sync_catalogue_to_graph
-- AFTER INSERT OR UPDATE OR DELETE ON "APPATTACH_CATALOGUES"
-- FOR EACH ROW
-- EXECUTE FUNCTION trigger_sync_catalogue_to_graph();

-- 触发器函数：关系变更时自动同步到图数据库
CREATE OR REPLACE FUNCTION trigger_sync_relationship_to_graph()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        -- 删除图数据库中的关系
        PERFORM * FROM cypher('kg_graph', format($cypher$
            MATCH ()-[r {relationshipId: $relationshipId}]-()
            DELETE r
        $cypher$, jsonb_build_object('relationshipId', OLD."Id"::text)));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' OR TG_OP = 'INSERT' THEN
        -- 同步关系到图数据库
        PERFORM sync_relationship_to_graph(NEW."Id");
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- 创建触发器（可选，建议在数据稳定后启用）
-- CREATE TRIGGER trg_sync_relationship_to_graph
-- AFTER INSERT OR UPDATE OR DELETE ON "APPKG_RELATIONSHIPS"
-- FOR EACH ROW
-- EXECUTE FUNCTION trigger_sync_relationship_to_graph();

-- =====================================================
-- 第八部分：执行初始数据同步
-- =====================================================

-- 执行初始数据同步（可选，根据需要启用）
-- 注意：如果数据量很大，建议分批执行或使用后台作业

DO $$
DECLARE
    sync_result RECORD;
BEGIN
    RAISE NOTICE '开始同步分类数据到图数据库...';
    SELECT * INTO sync_result FROM sync_all_catalogues_to_graph();
    RAISE NOTICE '分类同步完成: 成功 %, 失败 %', sync_result.synced_count, sync_result.error_count;
    
    RAISE NOTICE '开始同步关系数据到图数据库...';
    SELECT * INTO sync_result FROM sync_all_relationships_to_graph();
    RAISE NOTICE '关系同步完成: 成功 %, 失败 %', sync_result.synced_count, sync_result.error_count;
    
    RAISE NOTICE '刷新统计信息...';
    PERFORM refresh_graph_statistics();
    
    RAISE NOTICE '图数据库初始化完成！';
END $$;

-- =====================================================
-- 第九部分：验证和报告
-- =====================================================

-- 显示图数据库统计信息
SELECT * FROM get_graph_statistics();

-- 验证数据完整性
SELECT * FROM validate_graph_data();

-- =====================================================
-- 脚本执行完成
-- =====================================================

RAISE NOTICE '========================================';
RAISE NOTICE 'Apache AGE 图数据库初始化完成！';
RAISE NOTICE '========================================';
RAISE NOTICE '下一步操作：';
RAISE NOTICE '1. 检查统计信息：SELECT * FROM get_graph_statistics();';
RAISE NOTICE '2. 验证数据完整性：SELECT * FROM validate_graph_data();';
RAISE NOTICE '3. 测试查询：SELECT * FROM cypher(''kg_graph'', $$MATCH (n) RETURN n LIMIT 10$$) AS (n agtype);';
RAISE NOTICE '4. 如需启用自动同步，请取消注释触发器创建语句';
RAISE NOTICE '========================================';

