-- AttachCatalogue 增强功能测试脚本
-- 测试新添加的 CatalogueType、CataloguePurpose、TextVector、VectorDimension、Permissions 字段

-- 设置测试环境
SET client_min_messages TO notice;

-- 测试数据准备
DO $$
DECLARE
    test_catalogue_id uuid := '66666666-6666-6666-6666-666666666666';
BEGIN
    -- 插入测试分类
    INSERT INTO "APPATTACH_CATALOGUES" (
        "ID", "CATALOGUE_NAME", "ATTACH_RECEIVE_TYPE", "SEQUENCE_NUMBER", 
        "REFERENCE", "REFERENCE_TYPE", "CATALOGUE_TYPE", "CATALOGUE_PURPOSE",
        "TEXT_VECTOR", "VECTOR_DIMENSION", "PERMISSIONS"
    ) VALUES (
        test_catalogue_id, '测试分类', 0, 1, 'TEST001', 1, 1, 1,
        ARRAY[0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0]::double precision[],
        10,
        '[
            {
                "permissionType": "Role",
                "permissionTarget": "Admin",
                "action": 1,
                "effect": 1,
                "isEnabled": true,
                "description": "管理员可以查看"
            },
            {
                "permissionType": "User",
                "permissionTarget": "11111111-1111-1111-1111-111111111111",
                "action": 2,
                "effect": 1,
                "isEnabled": true,
                "description": "特定用户可以创建"
            }
        ]'::jsonb
    )
    ON CONFLICT (id) DO UPDATE SET 
        "CATALOGUE_TYPE" = EXCLUDED."CATALOGUE_TYPE",
        "CATALOGUE_PURPOSE" = EXCLUDED."CATALOGUE_PURPOSE",
        "TEXT_VECTOR" = EXCLUDED."TEXT_VECTOR",
        "VECTOR_DIMENSION" = EXCLUDED."VECTOR_DIMENSION",
        "PERMISSIONS" = EXCLUDED."PERMISSIONS";
    
    RAISE NOTICE '测试数据准备完成';
END $$;

-- 测试分类标识描述函数
DO $$
DECLARE
    test_catalogue_id uuid := '66666666-6666-6666-6666-666666666666';
    description text;
BEGIN
    SELECT "FN_GET_CATALOGUE_IDENTIFIER_DESCRIPTION"("CATALOGUE_TYPE", "CATALOGUE_PURPOSE") INTO description
    FROM "APPATTACH_CATALOGUES"
    WHERE "ID" = test_catalogue_id;
    
    RAISE NOTICE '分类标识描述: %', description;
END $$;

-- 测试权限检查函数
DO $$
DECLARE
    test_catalogue_id uuid := '66666666-6666-6666-6666-666666666666';
    test_user_id uuid := '11111111-1111-1111-1111-111111111111';
    has_permission boolean;
BEGIN
    -- 测试查看权限
    SELECT "FN_CHECK_CATALOGUE_PERMISSION"(test_catalogue_id, test_user_id, 1) INTO has_permission;
    RAISE NOTICE '用户查看权限: %', has_permission;
    
    -- 测试创建权限
    SELECT "FN_CHECK_CATALOGUE_PERMISSION"(test_catalogue_id, test_user_id, 2) INTO has_permission;
    RAISE NOTICE '用户创建权限: %', has_permission;
END $$;

-- 测试分类标识统计视图
SELECT 
    "CATALOGUE_TYPE",
    "CATALOGUE_PURPOSE",
    "CATALOGUE_COUNT",
    "ACTIVE_CATALOGUE_COUNT",
    "AVERAGE_VECTOR_DIMENSION"
FROM "V_ATTACH_CATALOGUES_BY_IDENTIFIER"
ORDER BY "CATALOGUE_TYPE", "CATALOGUE_PURPOSE";

-- 测试向量维度统计视图
SELECT 
    "VECTOR_DIMENSION_RANGE",
    "CATALOGUE_COUNT",
    "ACTIVE_CATALOGUE_COUNT"
FROM "V_ATTACH_CATALOGUES_BY_VECTOR_DIMENSION"
ORDER BY "VECTOR_DIMENSION_RANGE";

-- 测试新字段查询
SELECT 
    "ID",
    "CATALOGUE_NAME",
    "CATALOGUE_TYPE",
    "CATALOGUE_PURPOSE",
    "VECTOR_DIMENSION",
    "PERMISSIONS"
FROM "APPATTACH_CATALOGUES"
WHERE "ID" = '66666666-6666-6666-6666-666666666666';

-- 测试分类标识查询
SELECT 
    "ID",
    "CATALOGUE_NAME",
    "CATALOGUE_TYPE",
    "CATALOGUE_PURPOSE"
FROM "APPATTACH_CATALOGUES"
WHERE "CATALOGUE_TYPE" = 1 AND "CATALOGUE_PURPOSE" = 1
  AND "IS_DELETED" = false;

-- 测试向量维度查询
SELECT 
    "ID",
    "CATALOGUE_NAME",
    "VECTOR_DIMENSION"
FROM "APPATTACH_CATALOGUES"
WHERE "VECTOR_DIMENSION" > 0 AND "VECTOR_DIMENSION" <= 100
  AND "IS_DELETED" = false;

-- 测试权限JSONB查询
SELECT 
    "ID",
    "CATALOGUE_NAME",
    jsonb_array_length("PERMISSIONS") AS "PERMISSION_COUNT"
FROM "APPATTACH_CATALOGUES"
WHERE "PERMISSIONS" IS NOT NULL
  AND "IS_DELETED" = false;

-- 清理测试数据
DELETE FROM "APPATTACH_CATALOGUES"
WHERE "ID" = '66666666-6666-6666-6666-666666666666';

SELECT 'AttachCatalogue 增强功能测试完成！' AS "TEST_STATUS";
