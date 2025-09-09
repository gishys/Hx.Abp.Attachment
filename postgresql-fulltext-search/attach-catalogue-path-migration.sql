-- =====================================================
-- AttachCatalogue Path 现有数据生成路径
-- 功能：为 AttachCatalogue 表现有数据生成路径
-- 作者：系统自动生成
-- 日期：2025-09-09
-- 版本：1.5.11
-- =====================================================

DO $$
BEGIN
    -- 为现有数据生成路径（基于层级关系）
    RAISE NOTICE '开始为现有数据生成路径...';
    
    -- 直接执行路径生成逻辑
    DECLARE
        root_record RECORD;
        child_record RECORD;
        current_path_number INTEGER;
        parent_record RECORD;
        current_child_number INTEGER;
        new_path TEXT;
        processed_count INTEGER := 0;
    BEGIN
        RAISE NOTICE '开始为所有数据生成全局唯一路径...';
        
        -- 处理所有根节点（ParentId 为 NULL）
        current_path_number := 1;
        FOR root_record IN 
            SELECT "Id", "CATALOGUE_NAME", "REFERENCE", "REFERENCE_TYPE", "CREATION_TIME"
            FROM "APPATTACH_CATALOGUES" 
            WHERE "IS_DELETED" = false
                AND "PARENT_ID" IS NULL
                AND ("PATH" IS NULL OR "PATH" = '')
            ORDER BY "REFERENCE", "REFERENCE_TYPE", "CREATION_TIME"
        LOOP
            -- 生成根节点路径
            new_path := LPAD(current_path_number::TEXT, 7, '0');
            
            UPDATE "APPATTACH_CATALOGUES" 
            SET "PATH" = new_path
            WHERE "Id" = root_record."Id";
            
            RAISE NOTICE '根节点: % (业务: %-%) -> %', 
                root_record."CATALOGUE_NAME", 
                root_record."REFERENCE", 
                root_record."REFERENCE_TYPE",
                new_path;
            processed_count := processed_count + 1;
            current_path_number := current_path_number + 1;
        END LOOP;
        
        -- 递归处理所有子节点
        -- 使用循环方式逐层处理，避免复杂的递归查询
        DECLARE
            max_depth INTEGER := 10; -- 假设最大深度为10层
            current_depth INTEGER := 1;
            has_more_levels BOOLEAN := true;
        BEGIN
            WHILE has_more_levels AND current_depth <= max_depth LOOP
                has_more_levels := false;
                
                -- 处理当前深度的所有节点
                FOR parent_record IN 
                    SELECT "Id", "CATALOGUE_NAME", "PATH", "REFERENCE", "REFERENCE_TYPE", "CREATION_TIME"
                    FROM "APPATTACH_CATALOGUES" 
                    WHERE "IS_DELETED" = false
                        AND "PATH" IS NOT NULL
                        AND "PATH" != ''
                        AND array_length(string_to_array("PATH", '.'), 1) = current_depth
                    ORDER BY "REFERENCE", "REFERENCE_TYPE", "CREATION_TIME"
                LOOP
                    -- 处理当前父节点的所有子节点
                    current_child_number := 1;
                    FOR child_record IN 
                        SELECT "Id", "CATALOGUE_NAME", "REFERENCE", "REFERENCE_TYPE", "CREATION_TIME"
                        FROM "APPATTACH_CATALOGUES" 
                        WHERE "IS_DELETED" = false
                            AND "PARENT_ID" = parent_record."Id"
                            AND ("PATH" IS NULL OR "PATH" = '')
                        ORDER BY "CREATION_TIME"
                    LOOP
                        -- 生成子节点路径
                        new_path := parent_record."PATH" || '.' || LPAD(current_child_number::TEXT, 7, '0');
                        
                        UPDATE "APPATTACH_CATALOGUES" 
                        SET "PATH" = new_path
                        WHERE "Id" = child_record."Id";
                        
                        RAISE NOTICE '  子节点: % (业务: %-%) -> %', 
                            child_record."CATALOGUE_NAME", 
                            child_record."REFERENCE", 
                            child_record."REFERENCE_TYPE",
                            new_path;
                        processed_count := processed_count + 1;
                        current_child_number := current_child_number + 1;
                        has_more_levels := true;
                    END LOOP;
                END LOOP;
                
                current_depth := current_depth + 1;
            END LOOP;
        END;
        
        RAISE NOTICE '路径生成完成，共处理 % 条记录', processed_count;
    END;
    
    RAISE NOTICE 'PATH 字段迁移完成';
END $$;

-- 创建路径验证函数（7位数字）
CREATE OR REPLACE FUNCTION validate_catalogue_path(path_value TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    -- 空路径是有效的（根节点）
    IF path_value IS NULL OR path_value = '' THEN
        RETURN TRUE;
    END IF;
    
    -- 检查格式：0000001.0000002.0000003（7位数字，用点分隔）
    RETURN path_value ~ '^[0-9]{7}(\.[0-9]{7})*$';
END;
$$ LANGUAGE plpgsql;

-- 添加路径验证约束
DO $$
BEGIN
    -- 检查约束是否已存在
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'CK_ATTACH_CATALOGUES_PATH_FORMAT'
    ) THEN
        -- 添加路径格式验证约束
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_PATH_FORMAT" 
        CHECK (validate_catalogue_path("PATH"));
        
        RAISE NOTICE '已添加路径格式验证约束';
    ELSE
        RAISE NOTICE '路径格式验证约束已存在，跳过';
    END IF;
END $$;

-- 输出完成信息
SELECT 'AttachCatalogue Path 字段迁移完成！' AS message;
