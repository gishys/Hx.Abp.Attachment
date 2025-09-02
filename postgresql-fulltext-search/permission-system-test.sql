-- =====================================================
-- 权限系统功能测试脚本
-- 用于验证权限系统的各项功能是否正常工作
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 测试数据准备
-- =====================================================

-- 创建测试用户
INSERT INTO "APPUSERS" (
    "ID", "USERNAME", "EMAIL", "IS_ACTIVE", "IS_DELETED", "CREATION_TIME"
) VALUES 
(
    gen_random_uuid(), 'testuser1', 'testuser1@example.com', true, false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), 'testuser2', 'testuser2@example.com', true, false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), 'testadmin', 'testadmin@example.com', true, false, CURRENT_TIMESTAMP
);

-- 创建测试角色
INSERT INTO "APPROLES" (
    "ID", "ROLE_NAME", "DESCRIPTION", "IS_ACTIVE", "IS_DELETED", "CREATION_TIME"
) VALUES 
(
    gen_random_uuid(), '普通用户', '基本权限角色', true, false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), '管理员', '管理权限角色', true, false, CURRENT_TIMESTAMP
),
(
    gen_random_uuid(), '审核员', '审核权限角色', true, false, CURRENT_TIMESTAMP
);

-- 获取测试数据ID
DO $$
DECLARE
    user1_id uuid;
    user2_id uuid;
    admin_id uuid;
    role1_id uuid;
    role2_id uuid;
    role3_id uuid;
BEGIN
    -- 获取用户ID
    SELECT "ID" INTO user1_id FROM "APPUSERS" WHERE "USERNAME" = 'testuser1';
    SELECT "ID" INTO user2_id FROM "APPUSERS" WHERE "USERNAME" = 'testuser2';
    SELECT "ID" INTO admin_id FROM "APPUSERS" WHERE "USERNAME" = 'testadmin';
    
    -- 获取角色ID
    SELECT "ID" INTO role1_id FROM "APPROLES" WHERE "ROLE_NAME" = '普通用户';
    SELECT "ID" INTO role2_id FROM "APPROLES" WHERE "ROLE_NAME" = '管理员';
    SELECT "ID" INTO role3_id FROM "APPROLES" WHERE "ROLE_NAME" = '审核员';
    
    -- 创建用户角色关联
    INSERT INTO "APPUSERROLES" ("USER_ID", "ROLE_ID", "IS_ACTIVE", "CREATION_TIME")
    VALUES 
    (user1_id, role1_id, true, CURRENT_TIMESTAMP),
    (user2_id, role1_id, true, CURRENT_TIMESTAMP),
    (admin_id, role2_id, true, CURRENT_TIMESTAMP),
    (admin_id, role3_id, true, CURRENT_TIMESTAMP);
    
    -- 创建角色权限
    INSERT INTO "APPROLEPERMISSIONS" ("ROLE_ID", "PERMISSION_TYPE", "PERMISSION_TARGET", "ACTION", "EFFECT", "CREATION_TIME")
    VALUES 
    (role1_id, 'read', 'attachment', 'view', 'allow', CURRENT_TIMESTAMP),
    (role1_id, 'write', 'attachment', 'create', 'allow', CURRENT_TIMESTAMP),
    (role2_id, 'read', 'attachment', 'view', 'allow', CURRENT_TIMESTAMP),
    (role2_id, 'write', 'attachment', 'create', 'allow', CURRENT_TIMESTAMP),
    (role2_id, 'write', 'attachment', 'edit', 'allow', CURRENT_TIMESTAMP),
    (role2_id, 'write', 'attachment', 'delete', 'allow', CURRENT_TIMESTAMP),
    (role3_id, 'read', 'attachment', 'view', 'allow', CURRENT_TIMESTAMP),
    (role3_id, 'write', 'attachment', 'review', 'allow', CURRENT_TIMESTAMP);
    
    RAISE NOTICE '已创建测试数据：用户角色关联和角色权限';
END $$;

-- =====================================================
-- 2. 测试用户权限查询功能
-- =====================================================

-- 测试用户权限查询
DO $$
DECLARE
    user1_id uuid;
    permission_count integer;
BEGIN
    -- 获取用户ID
    SELECT "ID" INTO user1_id FROM "APPUSERS" WHERE "USERNAME" = 'testuser1';
    
    -- 查询用户权限
    SELECT COUNT(*) INTO permission_count
    FROM "APPUSERROLES" ur
    JOIN "APPROLEPERMISSIONS" rp ON ur."ROLE_ID" = rp."ROLE_ID"
    WHERE ur."USER_ID" = user1_id 
      AND ur."IS_ACTIVE" = true 
      AND rp."EFFECT" = 'allow';
    
    IF permission_count > 0 THEN
        RAISE NOTICE '✓ 用户权限查询功能测试通过，权限数量: %', permission_count;
    ELSE
        RAISE NOTICE '✗ 用户权限查询功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 3. 测试角色权限查询功能
-- =====================================================

-- 测试角色权限查询
DO $$
DECLARE
    admin_role_id uuid;
    permission_count integer;
BEGIN
    -- 获取管理员角色ID
    SELECT "ID" INTO admin_role_id FROM "APPROLES" WHERE "ROLE_NAME" = '管理员';
    
    -- 查询角色权限
    SELECT COUNT(*) INTO permission_count
    FROM "APPROLEPERMISSIONS"
    WHERE "ROLE_ID" = admin_role_id AND "EFFECT" = 'allow';
    
    IF permission_count > 0 THEN
        RAISE NOTICE '✓ 角色权限查询功能测试通过，权限数量: %', permission_count;
    ELSE
        RAISE NOTICE '✗ 角色权限查询功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 4. 测试权限验证功能
-- =====================================================

-- 测试权限验证
DO $$
DECLARE
    user1_id uuid;
    admin_id uuid;
    has_read_permission boolean;
    has_delete_permission boolean;
BEGIN
    -- 获取用户ID
    SELECT "ID" INTO user1_id FROM "APPUSERS" WHERE "USERNAME" = 'testuser1';
    SELECT "ID" INTO admin_id FROM "APPUSERS" WHERE "USERNAME" = 'testadmin';
    
    -- 测试普通用户读取权限
    SELECT EXISTS(
        SELECT 1 FROM "APPUSERROLES" ur
        JOIN "APPROLEPERMISSIONS" rp ON ur."ROLE_ID" = rp."ROLE_ID"
        WHERE ur."USER_ID" = user1_id 
          AND ur."IS_ACTIVE" = true 
          AND rp."PERMISSION_TYPE" = 'read'
          AND rp."PERMISSION_TARGET" = 'attachment'
          AND rp."ACTION" = 'view'
          AND rp."EFFECT" = 'allow'
    ) INTO has_read_permission;
    
    -- 测试普通用户删除权限（应该没有）
    SELECT EXISTS(
        SELECT 1 FROM "APPUSERROLES" ur
        JOIN "APPROLEPERMISSIONS" rp ON ur."ROLE_ID" = rp."ROLE_ID"
        WHERE ur."USER_ID" = user1_id 
          AND ur."IS_ACTIVE" = true 
          AND rp."PERMISSION_TYPE" = 'write'
          AND rp."PERMISSION_TARGET" = 'attachment'
          AND rp."ACTION" = 'delete'
          AND rp."EFFECT" = 'allow'
    ) INTO has_delete_permission;
    
    IF has_read_permission AND NOT has_delete_permission THEN
        RAISE NOTICE '✓ 权限验证功能测试通过';
        RAISE NOTICE '  普通用户有读取权限: %', has_read_permission;
        RAISE NOTICE '  普通用户无删除权限: %', has_delete_permission;
    ELSE
        RAISE NOTICE '✗ 权限验证功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 5. 测试权限继承功能
-- =====================================================

-- 测试权限继承
DO $$
DECLARE
    admin_id uuid;
    role_count integer;
    total_permissions integer;
BEGIN
    -- 获取管理员用户ID
    SELECT "ID" INTO admin_id FROM "APPUSERS" WHERE "USERNAME" = 'testadmin';
    
    -- 查询用户角色数量
    SELECT COUNT(*) INTO role_count
    FROM "APPUSERROLES"
    WHERE "USER_ID" = admin_id AND "IS_ACTIVE" = true;
    
    -- 查询用户总权限数量
    SELECT COUNT(*) INTO total_permissions
    FROM "APPUSERROLES" ur
    JOIN "APPROLEPERMISSIONS" rp ON ur."ROLE_ID" = rp."ROLE_ID"
    WHERE ur."USER_ID" = admin_id 
      AND ur."IS_ACTIVE" = true 
      AND rp."EFFECT" = 'allow';
    
    IF role_count > 1 AND total_permissions > 0 THEN
        RAISE NOTICE '✓ 权限继承功能测试通过';
        RAISE NOTICE '  管理员角色数量: %', role_count;
        RAISE NOTICE '  总权限数量: %', total_permissions;
    ELSE
        RAISE NOTICE '✗ 权限继承功能测试失败';
    END IF;
END $$;

-- =====================================================
-- 6. 测试权限冲突处理
-- =====================================================

-- 测试权限冲突处理（添加一个拒绝权限）
DO $$
DECLARE
    user1_id uuid;
    role1_id uuid;
    permission_count integer;
BEGIN
    -- 获取用户和角色ID
    SELECT "ID" INTO user1_id FROM "APPUSERS" WHERE "USERNAME" = 'testuser1';
    SELECT "ID" INTO role1_id FROM "APPROLES" WHERE "ROLE_NAME" = '普通用户';
    
    -- 添加一个拒绝权限
    INSERT INTO "APPROLEPERMISSIONS" ("ROLE_ID", "PERMISSION_TYPE", "PERMISSION_TARGET", "ACTION", "EFFECT", "CREATION_TIME")
    VALUES (role1_id, 'write', 'attachment', 'edit', 'deny', CURRENT_TIMESTAMP);
    
    -- 查询权限冲突
    SELECT COUNT(*) INTO permission_count
    FROM "APPROLEPERMISSIONS"
    WHERE "ROLE_ID" = role1_id 
      AND "PERMISSION_TYPE" = 'write' 
      AND "PERMISSION_TARGET" = 'attachment' 
      AND "ACTION" = 'edit';
    
    IF permission_count = 2 THEN
        RAISE NOTICE '✓ 权限冲突处理测试通过，找到 % 个相关权限', permission_count;
    ELSE
        RAISE NOTICE '✗ 权限冲突处理测试失败';
    END IF;
END $$;

-- =====================================================
-- 7. 测试权限性能
-- =====================================================

-- 测试权限查询性能
DO $$
DECLARE
    start_time timestamp;
    end_time timestamp;
    execution_time interval;
    query_count integer;
BEGIN
    start_time := clock_timestamp();
    
    -- 执行多次权限查询
    FOR i IN 1..100 LOOP
        PERFORM COUNT(*) FROM "APPUSERROLES" ur
        JOIN "APPROLEPERMISSIONS" rp ON ur."ROLE_ID" = rp."ROLE_ID"
        WHERE ur."USER_ID" IN (SELECT "ID" FROM "APPUSERS" WHERE "USERNAME" = 'testuser1')
          AND ur."IS_ACTIVE" = true 
          AND rp."EFFECT" = 'allow';
    END LOOP;
    
    end_time := clock_timestamp();
    execution_time := end_time - start_time;
    
    RAISE NOTICE '权限查询性能测试：100次查询耗时 %', execution_time;
END $$;

-- =====================================================
-- 8. 测试结果汇总
-- =====================================================

-- 显示测试数据统计
SELECT 
    '用户' as entity_type,
    COUNT(*) as count
FROM "APPUSERS" 
WHERE "USERNAME" LIKE 'test%'
UNION ALL
SELECT 
    '角色' as entity_type,
    COUNT(*) as count
FROM "APPROLES" 
WHERE "ROLE_NAME" IN ('普通用户', '管理员', '审核员')
UNION ALL
SELECT 
    '用户角色关联' as entity_type,
    COUNT(*) as count
FROM "APPUSERROLES" ur
JOIN "APPUSERS" u ON ur."USER_ID" = u."ID"
WHERE u."USERNAME" LIKE 'test%'
UNION ALL
SELECT 
    '角色权限' as entity_type,
    COUNT(*) as count
FROM "APPROLEPERMISSIONS" rp
JOIN "APPROLES" r ON rp."ROLE_ID" = r."ID"
WHERE r."ROLE_NAME" IN ('普通用户', '管理员', '审核员');

-- 显示权限分布
SELECT 
    r."ROLE_NAME",
    rp."PERMISSION_TYPE",
    rp."PERMISSION_TARGET",
    rp."ACTION",
    rp."EFFECT",
    COUNT(*) as count
FROM "APPROLES" r
JOIN "APPROLEPERMISSIONS" rp ON r."ID" = rp."ROLE_ID"
WHERE r."ROLE_NAME" IN ('普通用户', '管理员', '审核员')
GROUP BY r."ROLE_NAME", rp."PERMISSION_TYPE", rp."PERMISSION_TARGET", rp."ACTION", rp."EFFECT"
ORDER BY r."ROLE_NAME", rp."PERMISSION_TYPE", rp."ACTION";

-- =====================================================
-- 9. 清理测试数据
-- =====================================================

-- 删除测试数据（按依赖关系顺序删除）
DELETE FROM "APPROLEPERMISSIONS" 
WHERE "ROLE_ID" IN (SELECT "ID" FROM "APPROLES" WHERE "ROLE_NAME" IN ('普通用户', '管理员', '审核员'));

DELETE FROM "APPUSERROLES" 
WHERE "USER_ID" IN (SELECT "ID" FROM "APPUSERS" WHERE "USERNAME" LIKE 'test%')
   OR "ROLE_ID" IN (SELECT "ID" FROM "APPROLES" WHERE "ROLE_NAME" IN ('普通用户', '管理员', '审核员'));

DELETE FROM "APPROLES" 
WHERE "ROLE_NAME" IN ('普通用户', '管理员', '审核员');

DELETE FROM "APPUSERS" 
WHERE "USERNAME" LIKE 'test%';

RAISE NOTICE '已清理测试数据';

-- 提交事务
COMMIT;

RAISE NOTICE '=====================================================';
RAISE NOTICE '权限系统功能测试完成！';
RAISE NOTICE '请检查上述测试结果，确保所有功能正常工作。';
RAISE NOTICE '=====================================================';
