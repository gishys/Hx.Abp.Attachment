-- =====================================================
-- 知识图谱关系表创建与优化脚本
-- 项目：多维知识图谱系统
-- 版本：v1.0
-- 说明：本脚本用于创建和维护 APPKG_RELATIONSHIPS 表，包括表结构、索引、约束和性能优化
-- 基于：KnowledgeGraphRelationship 实体（CreationAuditedAggregateRoot<Guid>）
-- =====================================================

-- =====================================================
-- 第一部分：环境检查
-- =====================================================

-- 检查 PostgreSQL 版本（需要 12+）
DO $$
BEGIN
    IF current_setting('server_version_num')::int < 120000 THEN
        RAISE EXCEPTION 'PostgreSQL 版本需要 12.0 或更高版本';
    END IF;
END $$;

-- =====================================================
-- 第二部分：创建表结构
-- =====================================================

-- 删除表（如果存在，谨慎使用）
-- DROP TABLE IF EXISTS "APPKG_RELATIONSHIPS" CASCADE;

-- 创建知识图谱关系表
CREATE TABLE IF NOT EXISTS "APPKG_RELATIONSHIPS" (
    -- 主键
    "Id" UUID NOT NULL DEFAULT gen_random_uuid(),
    
    -- 关系核心字段
    "SOURCE_ENTITY_ID" UUID NOT NULL,
    "SOURCE_ENTITY_TYPE" VARCHAR(50) NOT NULL,
    "TARGET_ENTITY_ID" UUID NOT NULL,
    "TARGET_ENTITY_TYPE" VARCHAR(50) NOT NULL,
    "RELATIONSHIP_TYPE" VARCHAR(50) NOT NULL,
    
    -- 关系扩展字段（用于抽象关系类型）
    "ROLE" VARCHAR(50),
    "SEMANTIC_TYPE" VARCHAR(50),
    "DESCRIPTION" TEXT,
    "WEIGHT" DOUBLE PRECISION NOT NULL DEFAULT 1.0,
    
    -- ABP 创建审计字段（CreationAuditedAggregateRoot）
    "CREATION_TIME" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CREATOR_ID" UUID,
    
    -- ABP 扩展字段
    "EXTRA_PROPERTIES" JSONB,
    "CONCURRENCY_STAMP" VARCHAR(40),
    
    -- 主键约束
    CONSTRAINT "PK_KG_RELATIONSHIPS" PRIMARY KEY ("Id")
);

-- =====================================================
-- 第三部分：添加表注释和字段注释
-- =====================================================

COMMENT ON TABLE "APPKG_RELATIONSHIPS" IS '知识图谱关系表，存储实体间的关系数据，支持五维知识网络（分类、人员、部门、业务实体、工作流）';

COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."Id" IS '关系ID（主键，UUID）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."SOURCE_ENTITY_ID" IS '源实体ID（关联到现有实体表，如 AttachCatalogue.Id）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."SOURCE_ENTITY_TYPE" IS '源实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."TARGET_ENTITY_ID" IS '目标实体ID（关联到现有实体表）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."TARGET_ENTITY_TYPE" IS '目标实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."RELATIONSHIP_TYPE" IS '关系类型（PersonRelatesToCatalogue, CatalogueRelatesToCatalogue, PersonRelatesToWorkflow, WorkflowRelatesToWorkflow等）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."ROLE" IS '角色（用于抽象关系类型，如 PersonRelatesToCatalogue 中的"创建者"、"管理者"等）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."SEMANTIC_TYPE" IS '语义类型（用于抽象关系类型，如 CatalogueRelatesToCatalogue 中的"时间关系"、"业务关系"等）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."DESCRIPTION" IS '关系描述（可选，用于详细说明关系的业务含义）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."WEIGHT" IS '关系权重（默认1.0，用于影响分析和路径计算）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."CREATION_TIME" IS '创建时间（ABP审计字段）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."CREATOR_ID" IS '创建者ID（ABP审计字段，可为空）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."EXTRA_PROPERTIES" IS '扩展属性（JSONB格式，用于存储关系的动态属性）';
COMMENT ON COLUMN "APPKG_RELATIONSHIPS"."CONCURRENCY_STAMP" IS '并发控制标记（用于乐观并发控制）';

-- =====================================================
-- 第四部分：创建唯一约束
-- =====================================================

-- 唯一索引：防止重复关系（考虑 role 和 semanticType）
-- 注意：使用唯一索引而不是唯一约束，因为需要使用 COALESCE 处理 NULL 值
-- PostgreSQL 的唯一约束不支持表达式，所以使用唯一索引来实现
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE indexname = 'UK_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE_ROLE_SEMANTIC'
    ) THEN
        CREATE UNIQUE INDEX "UK_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE_ROLE_SEMANTIC"
        ON "APPKG_RELATIONSHIPS" (
            "SOURCE_ENTITY_ID", 
            "TARGET_ENTITY_ID", 
            "RELATIONSHIP_TYPE", 
            COALESCE("ROLE", ''), 
            COALESCE("SEMANTIC_TYPE", '')
        );
    END IF;
END $$;

-- =====================================================
-- 第五部分：创建索引（性能优化核心）
-- =====================================================

-- 1. 源实体索引（用于查找从某个实体出发的所有关系）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_SOURCE"
    ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "SOURCE_ENTITY_TYPE");

-- 2. 目标实体索引（用于查找指向某个实体的所有关系）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_TARGET"
    ON "APPKG_RELATIONSHIPS"("TARGET_ENTITY_ID", "TARGET_ENTITY_TYPE");

-- 3. 关系类型索引（用于按类型筛选关系）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_TYPE"
    ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE");

-- 4. 源实体类型+关系类型复合索引（用于按源实体类型和关系类型查询）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_SOURCE_TYPE"
    ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_TYPE", "RELATIONSHIP_TYPE");

-- 5. 角色索引（部分索引，只索引非空值，节省空间）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_ROLE"
    ON "APPKG_RELATIONSHIPS"("ROLE")
    WHERE "ROLE" IS NOT NULL;

-- 6. 语义类型索引（部分索引，只索引非空值）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_SEMANTIC_TYPE"
    ON "APPKG_RELATIONSHIPS"("SEMANTIC_TYPE")
    WHERE "SEMANTIC_TYPE" IS NOT NULL;

-- 7. 关系类型+角色复合索引（用于按类型和角色查询）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_TYPE_ROLE"
    ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "ROLE")
    WHERE "ROLE" IS NOT NULL;

-- 8. 关系类型+语义类型复合索引（用于按类型和语义类型查询）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_TYPE_SEMANTIC"
    ON "APPKG_RELATIONSHIPS"("RELATIONSHIP_TYPE", "SEMANTIC_TYPE")
    WHERE "SEMANTIC_TYPE" IS NOT NULL;

-- 9. 创建时间索引（用于时间轴查询和排序）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_CREATION_TIME"
    ON "APPKG_RELATIONSHIPS"("CREATION_TIME" DESC);

-- 10. 创建者索引（用于按创建者查询）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_CREATOR_ID"
    ON "APPKG_RELATIONSHIPS"("CREATOR_ID")
    WHERE "CREATOR_ID" IS NOT NULL;

-- 11. GIN 索引：扩展属性（用于 JSONB 字段查询）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_EXTRA_PROPERTIES_GIN"
    ON "APPKG_RELATIONSHIPS" USING GIN ("EXTRA_PROPERTIES")
    WHERE "EXTRA_PROPERTIES" IS NOT NULL;

-- 12. 复合索引：源+目标+类型（用于快速查找特定关系）
CREATE INDEX IF NOT EXISTS "IDX_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE"
    ON "APPKG_RELATIONSHIPS"("SOURCE_ENTITY_ID", "TARGET_ENTITY_ID", "RELATIONSHIP_TYPE");

-- =====================================================
-- 第六部分：创建检查约束
-- =====================================================

-- 检查约束：关系权重范围（0.0-100.0，可根据业务调整）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'CK_KG_RELATIONSHIPS_WEIGHT'
    ) THEN
        ALTER TABLE "APPKG_RELATIONSHIPS"
            ADD CONSTRAINT "CK_KG_RELATIONSHIPS_WEIGHT"
            CHECK ("WEIGHT" >= 0.0 AND "WEIGHT" <= 100.0);
    END IF;
END $$;

-- 检查约束：源实体和目标实体不能相同（防止自引用，根据业务需求可选）
-- 注意：如果业务需要支持自引用关系，请注释掉此约束
-- DO $$
-- BEGIN
--     IF NOT EXISTS (
--         SELECT 1 FROM pg_constraint 
--         WHERE conname = 'CK_KG_RELATIONSHIPS_NO_SELF_REFERENCE'
--     ) THEN
--         ALTER TABLE "APPKG_RELATIONSHIPS"
--             ADD CONSTRAINT "CK_KG_RELATIONSHIPS_NO_SELF_REFERENCE"
--             CHECK ("SOURCE_ENTITY_ID" != "TARGET_ENTITY_ID" OR 
--                    "SOURCE_ENTITY_TYPE" != "TARGET_ENTITY_TYPE");
--     END IF;
-- END $$;

-- =====================================================
-- 第七部分：性能优化建议和维护脚本
-- =====================================================

-- 1. 定期执行 VACUUM ANALYZE 以优化查询性能
-- 建议：每天执行一次，或在大量数据变更后执行
-- VACUUM ANALYZE "APPKG_RELATIONSHIPS";

-- 2. 更新表统计信息（用于查询优化器）
-- ANALYZE "APPKG_RELATIONSHIPS";

-- 3. 重建索引（如果索引碎片化严重）
-- REINDEX TABLE "APPKG_RELATIONSHIPS";

-- 4. 监控索引使用情况（查询未使用的索引）
-- SELECT 
--     schemaname,
--     tablename,
--     indexname,
--     idx_scan as index_scans,
--     idx_tup_read as tuples_read,
--     idx_tup_fetch as tuples_fetched
-- FROM pg_stat_user_indexes
-- WHERE tablename = 'APPKG_RELATIONSHIPS'
-- ORDER BY idx_scan ASC;

-- 5. 监控表大小和索引大小
-- SELECT 
--     pg_size_pretty(pg_total_relation_size('"APPKG_RELATIONSHIPS"')) AS total_size,
--     pg_size_pretty(pg_relation_size('"APPKG_RELATIONSHIPS"')) AS table_size,
--     pg_size_pretty(pg_total_relation_size('"APPKG_RELATIONSHIPS"') - pg_relation_size('"APPKG_RELATIONSHIPS"')) AS indexes_size;

-- =====================================================
-- 第八部分：查询性能优化建议
-- =====================================================

-- 1. 常用查询模式优化建议：
-- 
-- a) 查找从某个实体出发的所有关系：
--    SELECT * FROM "APPKG_RELATIONSHIPS" 
--    WHERE "SOURCE_ENTITY_ID" = ? AND "SOURCE_ENTITY_TYPE" = ?;
--    使用索引：IDX_KG_RELATIONSHIPS_SOURCE
--
-- b) 查找指向某个实体的所有关系：
--    SELECT * FROM "APPKG_RELATIONSHIPS" 
--    WHERE "TARGET_ENTITY_ID" = ? AND "TARGET_ENTITY_TYPE" = ?;
--    使用索引：IDX_KG_RELATIONSHIPS_TARGET
--
-- c) 查找特定类型的关系：
--    SELECT * FROM "APPKG_RELATIONSHIPS" 
--    WHERE "RELATIONSHIP_TYPE" = ?;
--    使用索引：IDX_KG_RELATIONSHIPS_TYPE
--
-- d) 查找特定角色或语义类型的关系：
--    SELECT * FROM "APPKG_RELATIONSHIPS" 
--    WHERE "RELATIONSHIP_TYPE" = ? AND "ROLE" = ?;
--    使用索引：IDX_KG_RELATIONSHIPS_TYPE_ROLE
--
-- e) 查找扩展属性中包含特定键值的关系：
--    SELECT * FROM "APPKG_RELATIONSHIPS" 
--    WHERE "EXTRA_PROPERTIES" @> '{"key": "value"}'::jsonb;
--    使用索引：IDX_KG_RELATIONSHIPS_EXTRA_PROPERTIES_GIN

-- 2. 分区建议（如果数据量非常大，超过千万级）：
--    可以考虑按 CREATION_TIME 进行范围分区，或按 RELATIONSHIP_TYPE 进行列表分区

-- 3. 归档策略建议：
--    对于历史关系数据，可以考虑：
--    a) 创建归档表：APPKG_RELATIONSHIPS_ARCHIVE
--    b) 定期将超过一定时间的关系数据迁移到归档表
--    c) 在归档表上创建相同的索引结构

-- =====================================================
-- 第九部分：数据维护函数（可选）
-- =====================================================

-- 函数：清理孤立关系（源实体或目标实体不存在的关系）
-- 注意：此函数需要根据实际业务需求调整
CREATE OR REPLACE FUNCTION cleanup_orphaned_relationships()
RETURNS TABLE(deleted_count BIGINT) AS $$
DECLARE
    deleted_rows BIGINT;
BEGIN
    -- 删除源实体不存在的关系
    DELETE FROM "APPKG_RELATIONSHIPS" r
    WHERE NOT EXISTS (
        SELECT 1 FROM "APPATTACH_CATALOGUES" c
        WHERE c."Id" = r."SOURCE_ENTITY_ID" AND r."SOURCE_ENTITY_TYPE" = 'Catalogue'
    )
    AND r."SOURCE_ENTITY_TYPE" = 'Catalogue';
    
    GET DIAGNOSTICS deleted_rows = ROW_COUNT;
    
    -- 可以根据需要添加其他实体类型的清理逻辑
    
    RETURN QUERY SELECT deleted_rows;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION cleanup_orphaned_relationships() IS '清理孤立关系（源实体或目标实体不存在的关系）';

-- 函数：统计关系数据
CREATE OR REPLACE FUNCTION get_relationship_statistics()
RETURNS TABLE(
    total_relationships BIGINT,
    relationships_by_type JSONB,
    relationships_by_source_type JSONB,
    relationships_by_target_type JSONB
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*)::BIGINT AS total_relationships,
        jsonb_object_agg("RELATIONSHIP_TYPE", type_count) AS relationships_by_type,
        jsonb_object_agg("SOURCE_ENTITY_TYPE", source_count) AS relationships_by_source_type,
        jsonb_object_agg("TARGET_ENTITY_TYPE", target_count) AS relationships_by_target_type
    FROM (
        SELECT 
            COUNT(*) AS total_relationships,
            (SELECT jsonb_object_agg(rel_type, cnt) 
             FROM (SELECT "RELATIONSHIP_TYPE" AS rel_type, COUNT(*) AS cnt 
                   FROM "APPKG_RELATIONSHIPS" GROUP BY "RELATIONSHIP_TYPE") t) AS relationships_by_type,
            (SELECT jsonb_object_agg(src_type, cnt) 
             FROM (SELECT "SOURCE_ENTITY_TYPE" AS src_type, COUNT(*) AS cnt 
                   FROM "APPKG_RELATIONSHIPS" GROUP BY "SOURCE_ENTITY_TYPE") t) AS relationships_by_source_type,
            (SELECT jsonb_object_agg(tgt_type, cnt) 
             FROM (SELECT "TARGET_ENTITY_TYPE" AS tgt_type, COUNT(*) AS cnt 
                   FROM "APPKG_RELATIONSHIPS" GROUP BY "TARGET_ENTITY_TYPE") t) AS relationships_by_target_type
        FROM "APPKG_RELATIONSHIPS"
    ) stats;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION get_relationship_statistics() IS '获取关系数据统计信息';

-- =====================================================
-- 第十部分：触发器（可选，用于数据一致性维护）
-- =====================================================

-- 触发器函数：自动更新 CONCURRENCY_STAMP
CREATE OR REPLACE FUNCTION update_kg_relationships_concurrency_stamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW."CONCURRENCY_STAMP" = gen_random_uuid()::TEXT;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 创建触发器（在更新时自动更新并发标记）
DROP TRIGGER IF EXISTS trg_kg_relationships_update_concurrency_stamp ON "APPKG_RELATIONSHIPS";
CREATE TRIGGER trg_kg_relationships_update_concurrency_stamp
    BEFORE UPDATE ON "APPKG_RELATIONSHIPS"
    FOR EACH ROW
    EXECUTE FUNCTION update_kg_relationships_concurrency_stamp();

-- =====================================================
-- 脚本执行完成
-- =====================================================

-- 输出执行结果
DO $$
BEGIN
    RAISE NOTICE '知识图谱关系表创建脚本执行完成！';
    RAISE NOTICE '表名：APPKG_RELATIONSHIPS';
    RAISE NOTICE '主键约束：PK_KG_RELATIONSHIPS';
    RAISE NOTICE '唯一约束：UK_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE_ROLE_SEMANTIC';
    RAISE NOTICE '索引数量：12个';
    RAISE NOTICE '建议：执行 ANALYZE "APPKG_RELATIONSHIPS" 以更新统计信息';
END $$;

