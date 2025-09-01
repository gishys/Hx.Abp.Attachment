-- =====================================================
-- 基于ABP vNext的权限系统数据库迁移脚本
-- 简化实现，PostgreSQL直接支持JSONB集合存储
-- =====================================================

-- 检查扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- =====================================================
-- 1. 创建权限查询视图
-- =====================================================

-- 创建权限摘要视图（基于实体集合）
CREATE OR REPLACE VIEW "V_APPATTACH_PERMISSION_SUMMARY" AS
SELECT 
    t."ID" AS "TEMPLATE_ID",
    t."TEMPLATE_NAME",
    t."TEMPLATE_TYPE",
    t."TEMPLATE_PURPOSE",
    t."VERSION",
    -- 权限集合直接通过实体关联获取
    0 AS "TOTAL_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "ENABLED_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "EFFECTIVE_PERMISSIONS" -- 占位符，实际通过应用层计算
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
WHERE t."IS_DELETED" = false;

-- 添加视图注释
COMMENT ON VIEW "V_APPATTACH_PERMISSION_SUMMARY" IS '权限摘要视图 - 提供模板权限的汇总信息（通过应用层计算）';

-- =====================================================
-- 2. 创建权限统计视图
-- =====================================================

-- 创建权限统计视图（基于实体集合）
CREATE OR REPLACE VIEW "V_APPATTACH_PERMISSION_STATISTICS" AS
SELECT 
    t."ID" AS "TEMPLATE_ID",
    t."TEMPLATE_NAME",
    t."TEMPLATE_TYPE",
    t."TEMPLATE_PURPOSE",
    t."VERSION",
    0 AS "TOTAL_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "ROLE_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "USER_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "POLICY_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "ALLOW_PERMISSIONS", -- 占位符，实际通过应用层计算
    0 AS "DENY_PERMISSIONS" -- 占位符，实际通过应用层计算
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
WHERE t."IS_DELETED" = false;

-- 添加统计视图注释
COMMENT ON VIEW "V_APPATTACH_PERMISSION_STATISTICS" IS '权限统计视图 - 提供模板权限的统计信息（通过应用层计算）';

-- =====================================================
-- 3. 创建权限查询函数（简化版本）
-- =====================================================

-- 创建权限检查函数（简化版本，主要用于数据库层面的快速检查）
CREATE OR REPLACE FUNCTION "FN_CHECK_TEMPLATE_PERMISSION_SIMPLE"(
    p_template_id uuid,
    p_user_id uuid,
    p_action integer
)
RETURNS boolean
LANGUAGE plpgsql
AS $$
DECLARE
    v_has_permission boolean := false;
BEGIN
    -- 简化实现，主要权限检查通过应用层进行
    -- 这里可以添加一些基本的数据库层面检查逻辑
    
    -- 检查模板是否存在且未删除
    IF NOT EXISTS (
        SELECT 1 FROM "APPATTACH_CATALOGUE_TEMPLATES" 
        WHERE "ID" = p_template_id AND "IS_DELETED" = false
    ) THEN
        RETURN false;
    END IF;
    
    -- 默认返回false，实际权限检查通过应用层进行
    RETURN false;
END;
$$;

-- 添加函数注释
COMMENT ON FUNCTION "FN_CHECK_TEMPLATE_PERMISSION_SIMPLE" IS '简化权限检查函数 - 主要用于数据库层面的基本检查';

-- =====================================================
-- 4. 创建权限管理函数（简化版本）
-- =====================================================

-- 创建权限状态查询函数
CREATE OR REPLACE FUNCTION "FN_GET_TEMPLATE_PERMISSION_COUNT"(
    p_template_id uuid
)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
    v_permission_count integer := 0;
BEGIN
    -- 通过关联查询获取权限数量
    SELECT COUNT(*) INTO v_permission_count
    FROM "APPATTACH_TEMPLATE_PERMISSIONS" tp
    WHERE tp."TEMPLATE_ID" = p_template_id 
      AND tp."IS_DELETED" = false;
    
    RETURN COALESCE(v_permission_count, 0);
END;
$$;

-- 添加函数注释
COMMENT ON FUNCTION "FN_GET_TEMPLATE_PERMISSION_COUNT" IS '获取模板权限数量函数';

-- =====================================================
-- 5. 创建权限相关的索引优化
-- =====================================================

-- 为权限表创建索引（如果存在权限表）
-- 注意：这里假设权限是通过关联表存储的，如果没有关联表，这些索引可以忽略

-- 创建模板权限关联索引（如果存在）
-- CREATE INDEX IF NOT EXISTS "IX_APPATTACH_TEMPLATE_PERMISSIONS_TEMPLATE_ID" 
-- ON "APPATTACH_TEMPLATE_PERMISSIONS" ("TEMPLATE_ID");

-- 创建权限类型索引（如果存在）
-- CREATE INDEX IF NOT EXISTS "IX_APPATTACH_TEMPLATE_PERMISSIONS_TYPE" 
-- ON "APPATTACH_TEMPLATE_PERMISSIONS" ("PERMISSION_TYPE");

-- 创建权限操作索引（如果存在）
-- CREATE INDEX IF NOT EXISTS "IX_APPATTACH_TEMPLATE_PERMISSIONS_ACTION" 
-- ON "APPATTACH_TEMPLATE_PERMISSIONS" ("ACTION");

-- =====================================================
-- 6. 创建权限分析视图
-- =====================================================

-- 创建权限分析视图
CREATE OR REPLACE VIEW "V_APPATTACH_PERMISSION_ANALYSIS" AS
SELECT 
    t."ID" AS "TEMPLATE_ID",
    t."TEMPLATE_NAME",
    t."TEMPLATE_TYPE",
    t."TEMPLATE_PURPOSE",
    t."VERSION",
    t."CREATION_TIME",
    t."LAST_MODIFICATION_TIME",
    -- 权限相关统计（通过应用层计算）
    '通过应用层计算' AS "PERMISSION_STATUS"
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
WHERE t."IS_DELETED" = false;

-- 添加分析视图注释
COMMENT ON VIEW "V_APPATTACH_PERMISSION_ANALYSIS" IS '权限分析视图 - 提供模板权限的分析信息';

-- =====================================================
-- 迁移完成
-- =====================================================

SELECT '基于ABP vNext的权限系统数据库迁移完成！（简化版本）' AS "MIGRATION_STATUS";

-- 显示创建的对象
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public' 
  AND tablename LIKE 'APPATTACH%'
ORDER BY tablename;

-- 显示创建的索引
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE schemaname = 'public' 
  AND tablename LIKE 'APPATTACH%'
ORDER BY tablename, indexname;

-- 显示创建的视图
SELECT 
    schemaname,
    viewname,
    definition
FROM pg_views 
WHERE schemaname = 'public' 
  AND viewname LIKE 'V_APPATTACH%'
ORDER BY viewname;

-- 显示创建的函数
SELECT 
    n.nspname AS schema_name,
    p.proname AS function_name,
    pg_get_function_identity_arguments(p.oid) AS arguments
FROM pg_proc p
JOIN pg_namespace n ON p.pronamespace = n.oid
WHERE n.nspname = 'public' 
  AND p.proname LIKE 'FN_%'
ORDER BY p.proname;
