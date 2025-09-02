-- =====================================================
-- 附件分类增强功能测试脚本
-- 用于验证新增字段和功能是否正常工作
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 测试数据准备
-- =====================================================

-- 创建测试用的附件分类数据
INSERT INTO "APPATTACH_CATALOGUES" (
    "ID", "CATALOGUE_NAME", "REFERENCE", "REFERENCE_TYPE", "SEQUENCE_NUMBER",
    "CATALOGUE_TYPE", "CATALOGUE_PURPOSE", "TEXT_VECTOR", "VECTOR_DIMENSION",
    "PERMISSIONS", "IS_DELETED", "CREATION_TIME"
) VALUES 
(
    gen_random_uuid(), '测试分类1', 'test-ref-001', 1, 1,
    1, 1, ARRAY[0.1, 0.2, 0.3], 3,
    '[{"permissionType": "read", "permissionTarget": "user", "action": "view", "effect": "allow"}]'::jsonb,
    false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), '测试分类2', 'test-ref-002', 2, 2,
    2, 2, ARRAY[0.4, 0.5, 0.6], 3,
    '[{"permissionType": "write", "permissionTarget": "admin", "action": "edit", "effect": "allow"}]'::jsonb,
    false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), '测试分类3', 'test-ref-003', 3, 3,
    3, 3, ARRAY[0.7, 0.8, 0.9], 3,
    '[]'::jsonb,
    false, CURRENT_TIMESTAMP
);

RAISE NOTICE '已插入测试数据';

-- =====================================================
-- 2. 测试新增字段功能
-- =====================================================

-- 测试 CATALOGUE_TYPE 字段
DO $$
DECLARE
    test_result record;
BEGIN
    SELECT "CATALOGUE_TYPE", "CATALOGUE_PURPOSE" 
    INTO test_result
    FROM "APPATTACH_CATALOGUES" 
    WHERE "CATALOGUE_NAME" = '测试分类1';
    
    IF test_result."CATALOGUE_TYPE" = 1 AND test_result."CATALOGUE_PURPOSE" = 1 THEN
        RAISE NOTICE '✓ CATALOGUE_TYPE 和 CATALOGUE_PURPOSE 字段测试通过';
    ELSE
        RAISE NOTICE '✗ CATALOGUE_TYPE 和 CATALOGUE_PURPOSE 字段测试失败';
    END IF;
END $$;

-- 测试 TEXT_VECTOR 字段
DO $$
DECLARE
    test_result record;
BEGIN
    SELECT "TEXT_VECTOR", "VECTOR_DIMENSION" 
    INTO test_result
    FROM "APPATTACH_CATALOGUES" 
    WHERE "CATALOGUE_NAME" = '测试分类1';
    
    IF test_result."TEXT_VECTOR" IS NOT NULL AND test_result."VECTOR_DIMENSION" = 3 THEN
        RAISE NOTICE '✓ TEXT_VECTOR 和 VECTOR_DIMENSION 字段测试通过';
    ELSE
        RAISE NOTICE '✗ TEXT_VECTOR 和 VECTOR_DIMENSION 字段测试失败';
    END IF;
END $$;

-- 测试 PERMISSIONS 字段
DO $$
DECLARE
    test_result record;
BEGIN
    SELECT "PERMISSIONS" 
    INTO test_result
    FROM "APPATTACH_CATALOGUES" 
    WHERE "CATALOGUE_NAME" = '测试分类1';
    
    IF jsonb_array_length(test_result."PERMISSIONS") > 0 THEN
        RAISE NOTICE '✓ PERMISSIONS 字段测试通过';
    ELSE
        RAISE NOTICE '✗ PERMISSIONS 字段测试失败';
    END IF;
END $$;

-- =====================================================
-- 3. 测试索引功能
-- =====================================================

-- 测试模板类型索引
DO $$
DECLARE
    start_time timestamp;
    end_time timestamp;
    execution_time interval;
BEGIN
    start_time := clock_timestamp();
    
    -- 执行查询
    PERFORM COUNT(*) FROM "APPATTACH_CATALOGUES" 
    WHERE "CATALOGUE_TYPE" = 1 AND "IS_DELETED" = false;
    
    end_time := clock_timestamp();
    execution_time := end_time - start_time;
    
    RAISE NOTICE '模板类型索引查询执行时间: %', execution_time;
END $$;

-- 测试权限索引
DO $$
DECLARE
    start_time timestamp;
    end_time timestamp;
    execution_time interval;
BEGIN
    start_time := clock_timestamp();
    
    -- 执行查询
    PERFORM COUNT(*) FROM "APPATTACH_CATALOGUES" 
    WHERE "PERMISSIONS" @> '[{"permissionType": "read"}]'::jsonb;
    
    end_time := clock_timestamp();
    execution_time := end_time - start_time;
    
    RAISE NOTICE '权限索引查询执行时间: %', execution_time;
END $$;

-- =====================================================
-- 4. 测试约束功能
-- =====================================================

-- 测试向量维度约束
DO $$
BEGIN
    BEGIN
        UPDATE "APPATTACH_CATALOGUES" 
        SET "VECTOR_DIMENSION" = 3000 
        WHERE "CATALOGUE_NAME" = '测试分类1';
        
        RAISE NOTICE '✗ 向量维度约束测试失败（应该被拒绝）';
    EXCEPTION
        WHEN check_violation THEN
            RAISE NOTICE '✓ 向量维度约束测试通过（正确拒绝无效值）';
        WHEN OTHERS THEN
            RAISE NOTICE '✗ 向量维度约束测试异常: %', SQLERRM;
    END;
END $$;

-- 测试模板类型约束
DO $$
BEGIN
    BEGIN
        UPDATE "APPATTACH_CATALOGUES" 
        SET "CATALOGUE_TYPE" = 999 
        WHERE "CATALOGUE_NAME" = '测试分类1';
        
        RAISE NOTICE '✗ 模板类型约束测试失败（应该被拒绝）';
    EXCEPTION
        WHEN check_violation THEN
            RAISE NOTICE '✓ 模板类型约束测试通过（正确拒绝无效值）';
        WHEN OTHERS THEN
            RAISE NOTICE '✗ 模板类型约束测试异常: %', SQLERRM;
    END;
END $$;

-- =====================================================
-- 5. 测试权限功能
-- =====================================================

-- 测试权限查询
DO $$
DECLARE
    permission_count integer;
BEGIN
    SELECT COUNT(*) INTO permission_count
    FROM "APPATTACH_CATALOGUES" ac,
         jsonb_array_elements(ac."PERMISSIONS") AS perm
    WHERE ac."CATALOGUE_NAME" = '测试分类1'
      AND perm->>'permissionType' = 'read';
    
    IF permission_count > 0 THEN
        RAISE NOTICE '✓ 权限查询功能测试通过';
    ELSE
        RAISE NOTICE '✗ 权限查询功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 6. 测试全文搜索功能
-- =====================================================

-- 测试全文搜索
DO $$
DECLARE
    search_result record;
BEGIN
    SELECT "CATALOGUE_NAME" INTO search_result
    FROM "APPATTACH_CATALOGUES" 
    WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试分类')
    LIMIT 1;
    
    IF search_result."CATALOGUE_NAME" IS NOT NULL THEN
        RAISE NOTICE '✓ 全文搜索功能测试通过';
    ELSE
        RAISE NOTICE '✗ 全文搜索功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 7. 测试结果汇总
-- =====================================================

-- 显示测试数据统计
SELECT 
    COUNT(*) as total_test_records,
    COUNT("CATALOGUE_TYPE") as catalogue_type_count,
    COUNT("CATALOGUE_PURPOSE") as catalogue_purpose_count,
    COUNT("TEXT_VECTOR") as text_vector_count,
    COUNT("VECTOR_DIMENSION") as vector_dimension_count,
    COUNT("PERMISSIONS") as permissions_count
FROM "APPATTACH_CATALOGUES" 
WHERE "CATALOGUE_NAME" LIKE '测试分类%';

-- 显示权限统计
SELECT 
    perm->>'permissionType' as permission_type,
    COUNT(*) as count
FROM "APPATTACH_CATALOGUES" ac,
     jsonb_array_elements(ac."PERMISSIONS") AS perm
WHERE ac."CATALOGUE_NAME" LIKE '测试分类%'
GROUP BY perm->>'permissionType';

-- =====================================================
-- 8. 清理测试数据
-- =====================================================

-- 删除测试数据
DELETE FROM "APPATTACH_CATALOGUES" 
WHERE "CATALOGUE_NAME" LIKE '测试分类%';

RAISE NOTICE '已清理测试数据';

-- 提交事务
COMMIT;

RAISE NOTICE '=====================================================';
RAISE NOTICE '附件分类增强功能测试完成！';
RAISE NOTICE '请检查上述测试结果，确保所有功能正常工作。';
RAISE NOTICE '=====================================================';
