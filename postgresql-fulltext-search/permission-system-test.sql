-- 权限系统测试脚本
-- 用于验证RBAC + ABAC + PBAC混合模型的权限管理功能

-- 设置测试环境
SET client_min_messages TO notice;

-- 测试数据准备
DO $$
DECLARE
    test_user_id uuid := '11111111-1111-1111-1111-111111111111';
    template_id uuid := '55555555-5555-5555-5555-555555555555';
BEGIN
    -- 插入测试用户
    INSERT INTO "APPATTACH_CATALOGUE_TEMPLATES" ("ID", "TEMPLATE_NAME", "RUNTIME_PERMISSION_CONTEXT") VALUES
    (test_user_id, 'TestUser', jsonb_build_object(
        'userRoles', jsonb_build_array('Staff', 'Editor'),
        'userGroups', jsonb_build_array('11111111-1111-1111-1111-111111111112')
    ))
    ON CONFLICT (id) DO UPDATE SET 
        "RUNTIME_PERMISSION_CONTEXT" = EXCLUDED."RUNTIME_PERMISSION_CONTEXT";
    
    -- 插入测试模板
    INSERT INTO "APPATTACH_CATALOGUE_TEMPLATES" ("ID", "TEMPLATE_NAME", "TEMPLATE_TYPE", "TEMPLATE_PURPOSE") VALUES
    (template_id, '测试模板', 1, 1)
    ON CONFLICT (id) DO NOTHING;
    
    RAISE NOTICE '测试数据准备完成';
END $$;

-- 插入测试权限
DO $$
DECLARE
    template_id uuid := '55555555-5555-5555-5555-555555555555';
    user_id uuid := '11111111-1111-1111-1111-111111111111';
BEGIN
    -- 插入RBAC权限
    INSERT INTO "APPATTACH_ATTACH_CATALOGUE_TEMPLATE_PERMISSIONS" (
        "ID", "TEMPLATE_ID", "TEMPLATE_VERSION", "PERMISSION_TYPE", "ROLE_NAME", "ACTION", "EFFECT", "DESCRIPTION"
    ) VALUES (
        uuid_generate_v4(), template_id, 1, 'RBAC', 'Admin', 1, 1, '管理员可以查看'
    );
    
    -- 插入ABAC权限
    INSERT INTO "APPATTACH_ATTACH_CATALOGUE_TEMPLATE_PERMISSIONS" (
        "ID", "TEMPLATE_ID", "TEMPLATE_VERSION", "PERMISSION_TYPE", "ATTRIBUTE_CONDITIONS", "ACTION", "EFFECT", "DESCRIPTION"
    ) VALUES (
        uuid_generate_v4(), template_id, 1, 'ABAC', 
        '{"department": "IT", "securityLevel": "High"}', 2, 1, 'IT部门高安全级别用户可以创建'
    );
    
    -- 插入用户特定权限
    INSERT INTO "APPATTACH_ATTACH_CATALOGUE_TEMPLATE_PERMISSIONS" (
        "ID", "TEMPLATE_ID", "TEMPLATE_VERSION", "PERMISSION_TYPE", "USER_ID", "ACTION", "EFFECT", "DESCRIPTION"
    ) VALUES (
        uuid_generate_v4(), template_id, 1, 'RBAC', user_id, 4, 1, '测试用户可以删除'
    );
    
    RAISE NOTICE '测试权限插入完成';
END $$;

-- 测试权限检查功能
DO $$
DECLARE
    template_id uuid := '55555555-5555-5555-5555-555555555555';
    user_id uuid := '11111111-1111-1111-1111-111111111111';
    has_permission boolean;
BEGIN
    -- 测试查看权限
    SELECT "FN_CHECK_USER_PERMISSION"(user_id, template_id, 1) INTO has_permission;
    RAISE NOTICE '用户查看权限: %', has_permission;
    
    -- 测试删除权限
    SELECT "FN_CHECK_USER_PERMISSION"(user_id, template_id, 4) INTO has_permission;
    RAISE NOTICE '用户删除权限: %', has_permission;
END $$;

-- 测试权限视图
SELECT 
    "TEMPLATE_ID",
    "PERMISSION_TYPE",
    "ACTION",
    "EFFECT",
    "IS_EFFECTIVE"
FROM "V_APPATTACH_PERMISSION_SUMMARY"
WHERE "TEMPLATE_ID" = '55555555-5555-5555-5555-555555555555'
ORDER BY "ACTION";

-- 清理测试数据
DELETE FROM "APPATTACH_ATTACH_CATALOGUE_TEMPLATE_PERMISSIONS"
WHERE "TEMPLATE_ID" = '55555555-5555-5555-5555-555555555555';

DELETE FROM "APPATTACH_CATALOGUE_TEMPLATES"
WHERE "ID" IN ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111');

SELECT '权限系统测试完成！' AS "TEST_STATUS";
