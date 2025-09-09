-- =====================================================
-- AttachCatalogue Path 字段迁移脚本
-- 功能：为 AttachCatalogue 表添加 Path 字段和相关索引
-- 作者：系统自动生成
-- 日期：2024-12-19
-- 版本：1.0
-- =====================================================

DO $$
BEGIN
    -- 检查并添加 PATH 字段
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'PATH'
    ) THEN
        -- 添加 PATH 字段
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "PATH" VARCHAR(500);
        
        RAISE NOTICE '已添加 PATH 字段';
    ELSE
        RAISE NOTICE 'PATH 字段已存在，跳过';
    END IF;

    -- 创建路径索引
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_PATH'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_PATH" 
        ON "APPATTACH_CATALOGUES" ("PATH");
        
        RAISE NOTICE '已创建路径索引 IDX_ATTACH_CATALOGUES_PATH';
    ELSE
        RAISE NOTICE '路径索引 IDX_ATTACH_CATALOGUES_PATH 已存在，跳过';
    END IF;

    -- 创建复合索引：Reference + ReferenceType + Path
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_REF_TYPE_PATH'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_REF_TYPE_PATH" 
        ON "APPATTACH_CATALOGUES" ("REFERENCE", "REFERENCE_TYPE", "PATH");
        
        RAISE NOTICE '已创建复合索引 IDX_ATTACH_CATALOGUES_REF_TYPE_PATH';
    ELSE
        RAISE NOTICE '复合索引 IDX_ATTACH_CATALOGUES_REF_TYPE_PATH 已存在，跳过';
    END IF;

    -- 为现有数据生成路径（基于层级关系）
    -- 这里需要根据实际的业务逻辑来生成路径
    -- 暂时设置为 NULL，由应用程序在后续操作中生成
    
    RAISE NOTICE 'PATH 字段迁移完成';
END $$;

-- =====================================================
-- 路径相关辅助函数
-- =====================================================

-- 创建路径验证函数
CREATE OR REPLACE FUNCTION validate_catalogue_path(path_value TEXT)
RETURNS BOOLEAN AS $$
BEGIN
    -- 空路径是有效的（根节点）
    IF path_value IS NULL OR path_value = '' THEN
        RETURN TRUE;
    END IF;
    
    -- 检查格式：00001.00002.00003（5位数字，用点分隔）
    RETURN path_value ~ '^[0-9]{5}(\.[0-9]{5})*$';
END;
$$ LANGUAGE plpgsql;

-- 创建路径深度计算函数
CREATE OR REPLACE FUNCTION get_catalogue_path_depth(path_value TEXT)
RETURNS INTEGER AS $$
BEGIN
    IF path_value IS NULL OR path_value = '' THEN
        RETURN 0;
    END IF;
    
    RETURN array_length(string_to_array(path_value, '.'), 1);
END;
$$ LANGUAGE plpgsql;

-- 创建获取父路径函数
CREATE OR REPLACE FUNCTION get_parent_catalogue_path(path_value TEXT)
RETURNS TEXT AS $$
BEGIN
    IF path_value IS NULL OR path_value = '' THEN
        RETURN NULL;
    END IF;
    
    DECLARE
        parts TEXT[];
        parent_parts TEXT[];
    BEGIN
        parts := string_to_array(path_value, '.');
        
        IF array_length(parts, 1) <= 1 THEN
            RETURN NULL; -- 根节点
        END IF;
        
        parent_parts := parts[1:array_length(parts, 1) - 1];
        RETURN array_to_string(parent_parts, '.');
    END;
END;
$$ LANGUAGE plpgsql;

-- 创建计算下一个路径函数
CREATE OR REPLACE FUNCTION calculate_next_catalogue_path(current_path TEXT)
RETURNS TEXT AS $$
BEGIN
    IF current_path IS NULL OR current_path = '' THEN
        RETURN '00001';
    END IF;
    
    DECLARE
        parts TEXT[];
        last_part TEXT;
        next_number INTEGER;
        parent_path TEXT;
    BEGIN
        parts := string_to_array(current_path, '.');
        last_part := parts[array_length(parts, 1)];
        next_number := CAST(last_part AS INTEGER) + 1;
        
        IF array_length(parts, 1) = 1 THEN
            -- 根节点
            RETURN LPAD(next_number::TEXT, 5, '0');
        ELSE
            -- 子节点
            parent_path := array_to_string(parts[1:array_length(parts, 1) - 1], '.');
            RETURN parent_path || '.' || LPAD(next_number::TEXT, 5, '0');
        END IF;
    END;
END;
$$ LANGUAGE plpgsql;

-- 创建路径查询函数：根据路径前缀查找子分类
CREATE OR REPLACE FUNCTION find_catalogues_by_path_prefix(
    path_prefix TEXT,
    reference_filter TEXT DEFAULT NULL,
    reference_type_filter INTEGER DEFAULT NULL
)
RETURNS TABLE (
    "Id" UUID,
    "CATALOGUE_NAME" VARCHAR(128),
    "PATH" VARCHAR(500),
    "PARENT_ID" UUID,
    "REFERENCE" VARCHAR(100),
    "REFERENCE_TYPE" INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."PATH",
        c."PARENT_ID",
        c."REFERENCE",
        c."REFERENCE_TYPE"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."IS_DELETED" = false
        AND (path_prefix IS NULL OR c."PATH" LIKE path_prefix || '.%' OR c."PATH" = path_prefix)
        AND (reference_filter IS NULL OR c."REFERENCE" = reference_filter)
        AND (reference_type_filter IS NULL OR c."REFERENCE_TYPE" = reference_type_filter)
    ORDER BY c."PATH";
END;
$$ LANGUAGE plpgsql;

-- 创建路径查询函数：根据路径深度查找分类
CREATE OR REPLACE FUNCTION find_catalogues_by_path_depth(
    depth INTEGER,
    reference_filter TEXT DEFAULT NULL,
    reference_type_filter INTEGER DEFAULT NULL
)
RETURNS TABLE (
    "Id" UUID,
    "CATALOGUE_NAME" VARCHAR(128),
    "PATH" VARCHAR(500),
    "PARENT_ID" UUID,
    "REFERENCE" VARCHAR(100),
    "REFERENCE_TYPE" INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."PATH",
        c."PARENT_ID",
        c."REFERENCE",
        c."REFERENCE_TYPE"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."IS_DELETED" = false
        AND get_catalogue_path_depth(c."PATH") = depth
        AND (reference_filter IS NULL OR c."REFERENCE" = reference_filter)
        AND (reference_type_filter IS NULL OR c."REFERENCE_TYPE" = reference_type_filter)
    ORDER BY c."PATH";
END;
$$ LANGUAGE plpgsql;

-- 创建路径查询函数：查找根分类
CREATE OR REPLACE FUNCTION find_root_catalogues(
    reference_filter TEXT DEFAULT NULL,
    reference_type_filter INTEGER DEFAULT NULL
)
RETURNS TABLE (
    "Id" UUID,
    "CATALOGUE_NAME" VARCHAR(128),
    "PATH" VARCHAR(500),
    "REFERENCE" VARCHAR(100),
    "REFERENCE_TYPE" INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."PATH",
        c."REFERENCE",
        c."REFERENCE_TYPE"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."IS_DELETED" = false
        AND (c."PATH" IS NULL OR c."PATH" = '' OR get_catalogue_path_depth(c."PATH") = 1)
        AND (reference_filter IS NULL OR c."REFERENCE" = reference_filter)
        AND (reference_type_filter IS NULL OR c."REFERENCE_TYPE" = reference_type_filter)
    ORDER BY c."PATH";
END;
$$ LANGUAGE plpgsql;

-- 创建路径查询函数：查找叶子分类
CREATE OR REPLACE FUNCTION find_leaf_catalogues(
    reference_filter TEXT DEFAULT NULL,
    reference_type_filter INTEGER DEFAULT NULL
)
RETURNS TABLE (
    "Id" UUID,
    "CATALOGUE_NAME" VARCHAR(128),
    "PATH" VARCHAR(500),
    "PARENT_ID" UUID,
    "REFERENCE" VARCHAR(100),
    "REFERENCE_TYPE" INTEGER
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."PATH",
        c."PARENT_ID",
        c."REFERENCE",
        c."REFERENCE_TYPE"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."IS_DELETED" = false
        AND NOT EXISTS (
            SELECT 1 FROM "APPATTACH_CATALOGUES" child
            WHERE child."PARENT_ID" = c."Id"
            AND child."IS_DELETED" = false
        )
        AND (reference_filter IS NULL OR c."REFERENCE" = reference_filter)
        AND (reference_type_filter IS NULL OR c."REFERENCE_TYPE" = reference_type_filter)
    ORDER BY c."PATH";
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

-- 创建路径更新触发器函数
CREATE OR REPLACE FUNCTION update_catalogue_path_trigger()
RETURNS TRIGGER AS $$
BEGIN
    -- 当父级ID改变时，自动更新路径
    IF OLD."PARENT_ID" IS DISTINCT FROM NEW."PARENT_ID" THEN
        IF NEW."PARENT_ID" IS NULL THEN
            -- 成为根节点
            NEW."PATH" := calculate_next_catalogue_path(NULL);
        ELSE
            -- 成为子节点，需要获取父级路径
            DECLARE
                parent_path TEXT;
            BEGIN
                SELECT "PATH" INTO parent_path 
                FROM "APPATTACH_CATALOGUES" 
                WHERE "Id" = NEW."PARENT_ID" AND "IS_DELETED" = false;
                
                IF parent_path IS NOT NULL THEN
                    NEW."PATH" := calculate_next_catalogue_path(parent_path);
                END IF;
            END;
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 创建路径更新触发器
DO $$
BEGIN
    -- 检查触发器是否已存在
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.triggers 
        WHERE trigger_name = 'trg_update_catalogue_path'
    ) THEN
        -- 创建触发器
        CREATE TRIGGER "trg_update_catalogue_path"
        BEFORE UPDATE ON "APPATTACH_CATALOGUES"
        FOR EACH ROW
        EXECUTE FUNCTION update_catalogue_path_trigger();
        
        RAISE NOTICE '已创建路径更新触发器';
    ELSE
        RAISE NOTICE '路径更新触发器已存在，跳过';
    END IF;
END $$;

-- 输出完成信息
SELECT 'AttachCatalogue Path 字段迁移完成！' AS message;
