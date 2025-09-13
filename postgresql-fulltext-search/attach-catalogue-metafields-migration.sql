-- =====================================================
-- AttachCatalogue 表 MetaFields 字段迁移脚本
-- 为 AttachCatalogue 添加 META_FIELDS 字段，支持元数据字段管理
-- =====================================================

-- 检查扩展
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUES') THEN
        RAISE EXCEPTION '表 APPATTACH_CATALOGUES 不存在，请先创建表';
    END IF;
END $$;

-- 添加 META_FIELDS 字段
DO $$
BEGIN
    -- 添加 META_FIELDS 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'META_FIELDS') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "META_FIELDS" jsonb;
        
        RAISE NOTICE '已添加 META_FIELDS 字段';
    ELSE
        RAISE NOTICE 'META_FIELDS 字段已存在';
    END IF;
END $$;

-- 添加字段注释
DO $$
BEGIN
    -- 添加元数据字段注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'META_FIELDS' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."META_FIELDS" IS '元数据字段集合（JSONB格式，存储元数据字段信息，用于命名实体识别(NER)、前端展示和业务场景配置）';
    END IF;
END $$;

-- 创建索引
DO $$
BEGIN
    -- 创建元数据字段GIN索引（用于JSONB查询）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_META_FIELDS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_META_FIELDS_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN ("META_FIELDS" jsonb_path_ops);
        
        RAISE NOTICE '已创建元数据字段GIN索引';
    ELSE
        RAISE NOTICE '元数据字段GIN索引已存在';
    END IF;

    -- 创建元数据字段字段键名索引（使用B-tree索引，因为这是文本字段）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_META_FIELDS_FIELD_KEY') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_META_FIELDS_FIELD_KEY" 
        ON "APPATTACH_CATALOGUES" (("META_FIELDS"->>'fieldKey'));
        
        RAISE NOTICE '已创建元数据字段键名索引';
    ELSE
        RAISE NOTICE '元数据字段键名索引已存在';
    END IF;

    -- 创建元数据字段实体类型索引（使用B-tree索引）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_META_FIELDS_ENTITY_TYPE') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_META_FIELDS_ENTITY_TYPE" 
        ON "APPATTACH_CATALOGUES" (("META_FIELDS"->>'entityType'));
        
        RAISE NOTICE '已创建元数据字段实体类型索引';
    ELSE
        RAISE NOTICE '元数据字段实体类型索引已存在';
    END IF;

    -- 创建元数据字段数据类型索引（使用B-tree索引）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_META_FIELDS_DATA_TYPE') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_META_FIELDS_DATA_TYPE" 
        ON "APPATTACH_CATALOGUES" (("META_FIELDS"->>'dataType'));
        
        RAISE NOTICE '已创建元数据字段数据类型索引';
    ELSE
        RAISE NOTICE '元数据字段数据类型索引已存在';
    END IF;

    -- 创建元数据字段启用状态索引（使用B-tree索引）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_META_FIELDS_IS_ENABLED') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_META_FIELDS_IS_ENABLED" 
        ON "APPATTACH_CATALOGUES" (("META_FIELDS"->>'isEnabled'));
        
        RAISE NOTICE '已创建元数据字段启用状态索引';
    ELSE
        RAISE NOTICE '元数据字段启用状态索引已存在';
    END IF;
END $$;

-- =====================================================
-- 数据更新：将空的 META_FIELDS 字段更新为空数组
-- =====================================================

-- 更新 NULL 的 META_FIELDS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "META_FIELDS" = '[]'::jsonb 
WHERE "META_FIELDS" IS NULL;

-- 更新空字符串的 META_FIELDS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "META_FIELDS" = '[]'::jsonb 
WHERE "META_FIELDS" = '""'::jsonb;

-- 更新无效 JSON 的 META_FIELDS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "META_FIELDS" = '[]'::jsonb 
WHERE "META_FIELDS" = 'null'::jsonb;

-- 显示更新结果
SELECT 
    COUNT(*) as total_records,
    COUNT("META_FIELDS") as non_null_meta_fields,
    COUNT(CASE WHEN "META_FIELDS" = '[]'::jsonb THEN 1 END) as empty_array_meta_fields,
    COUNT(CASE WHEN "META_FIELDS" IS NULL THEN 1 END) as null_meta_fields
FROM "APPATTACH_CATALOGUES";

-- =====================================================
-- 最佳实践：优化 JSONB 字段配置
-- =====================================================

-- 设置 META_FIELDS 字段的默认值
ALTER TABLE "APPATTACH_CATALOGUES" 
ALTER COLUMN "META_FIELDS" SET DEFAULT '[]'::jsonb;

-- 添加 NOT NULL 约束（确保数据一致性）
ALTER TABLE "APPATTACH_CATALOGUES" 
ALTER COLUMN "META_FIELDS" SET NOT NULL;

-- 添加 JSONB 格式验证约束（确保是数组格式）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'CK_ATTACH_CATALOGUES_META_FIELDS_FORMAT'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_META_FIELDS_FORMAT" 
        CHECK (jsonb_typeof("META_FIELDS") = 'array');
        
        RAISE NOTICE '已添加元数据字段格式验证约束';
    ELSE
        RAISE NOTICE '元数据字段格式验证约束已存在';
    END IF;
END $$;

-- =====================================================
-- 创建有用的查询函数
-- =====================================================

-- 创建函数：根据字段键名查找分类
CREATE OR REPLACE FUNCTION find_catalogues_by_meta_field_key(field_key text)
RETURNS TABLE (
    id uuid,
    catalogue_name text,
    meta_field jsonb
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        jsonb_array_elements(c."META_FIELDS") as meta_field
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."META_FIELDS" @> '[{"fieldKey": "' || field_key || '"}]'::jsonb
    AND c."IS_DELETED" = false;
END;
$$ LANGUAGE plpgsql;

-- 创建函数：根据实体类型查找分类
CREATE OR REPLACE FUNCTION find_catalogues_by_meta_entity_type(entity_type text)
RETURNS TABLE (
    id uuid,
    catalogue_name text,
    meta_fields jsonb
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."META_FIELDS"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."META_FIELDS" @> '[{"entityType": "' || entity_type || '"}]'::jsonb
    AND c."IS_DELETED" = false;
END;
$$ LANGUAGE plpgsql;

-- 创建函数：根据数据类型查找分类
CREATE OR REPLACE FUNCTION find_catalogues_by_meta_data_type(data_type text)
RETURNS TABLE (
    id uuid,
    catalogue_name text,
    meta_fields jsonb
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        c."Id",
        c."CATALOGUE_NAME",
        c."META_FIELDS"
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."META_FIELDS" @> '[{"dataType": "' || data_type || '"}]'::jsonb
    AND c."IS_DELETED" = false;
END;
$$ LANGUAGE plpgsql;

-- 创建函数：获取启用的元数据字段
CREATE OR REPLACE FUNCTION get_enabled_meta_fields(catalogue_id uuid)
RETURNS TABLE (
    field_key text,
    field_name text,
    data_type text,
    is_required boolean,
    description text
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        (jsonb_array_elements(c."META_FIELDS")->>'fieldKey')::text as field_key,
        (jsonb_array_elements(c."META_FIELDS")->>'fieldName')::text as field_name,
        (jsonb_array_elements(c."META_FIELDS")->>'dataType')::text as data_type,
        (jsonb_array_elements(c."META_FIELDS")->>'isRequired')::boolean as is_required,
        (jsonb_array_elements(c."META_FIELDS")->>'description')::text as description
    FROM "APPATTACH_CATALOGUES" c
    WHERE c."Id" = catalogue_id
    AND c."IS_DELETED" = false
    AND (jsonb_array_elements(c."META_FIELDS")->>'isEnabled')::boolean = true;
END;
$$ LANGUAGE plpgsql;

-- 显示最终结果
DO $$
BEGIN
    RAISE NOTICE '=====================================================';
    RAISE NOTICE 'MetaFields 字段迁移完成！';
    RAISE NOTICE '已创建以下功能：';
    RAISE NOTICE '1. META_FIELDS 字段（JSONB格式）';
    RAISE NOTICE '2. 元数据字段数据清理和标准化';
    RAISE NOTICE '3. 元数据字段格式验证约束（数组格式）';
    RAISE NOTICE '4. 默认值设置为空数组';
    RAISE NOTICE '5. NOT NULL 约束确保数据一致性';
    RAISE NOTICE '6. 多个索引支持高效查询：';
    RAISE NOTICE '   - GIN索引用于JSONB查询';
    RAISE NOTICE '   - B-tree索引用于文本字段查询';
    RAISE NOTICE '7. 实用查询函数：';
    RAISE NOTICE '   - find_catalogues_by_meta_field_key(field_key)';
    RAISE NOTICE '   - find_catalogues_by_meta_entity_type(entity_type)';
    RAISE NOTICE '   - find_catalogues_by_meta_data_type(data_type)';
    RAISE NOTICE '   - get_enabled_meta_fields(catalogue_id)';
    RAISE NOTICE '=====================================================';
END $$;

-- =====================================================
-- 示例查询和使用方法
-- =====================================================

/*
-- 示例1：查找包含特定字段键名的分类
SELECT * FROM find_catalogues_by_meta_field_key('projectName');

-- 示例2：查找包含特定实体类型的分类
SELECT * FROM find_catalogues_by_meta_entity_type('Project');

-- 示例3：查找包含特定数据类型的分类
SELECT * FROM find_catalogues_by_meta_data_type('string');

-- 示例4：获取指定分类的所有启用元数据字段
SELECT * FROM get_enabled_meta_fields('your-catalogue-id-here');

-- 示例5：直接查询包含特定元数据字段的分类
SELECT 
    "Id",
    "CATALOGUE_NAME",
    "META_FIELDS"
FROM "APPATTACH_CATALOGUES"
WHERE "META_FIELDS" @> '[{"fieldKey": "projectName", "isEnabled": true}]'::jsonb
AND "IS_DELETED" = false;

-- 示例6：查询包含必填字段的分类
SELECT 
    "Id",
    "CATALOGUE_NAME",
    "META_FIELDS"
FROM "APPATTACH_CATALOGUES"
WHERE "META_FIELDS" @> '[{"isRequired": true}]'::jsonb
AND "IS_DELETED" = false;

-- 示例7：统计元数据字段使用情况
SELECT 
    jsonb_array_elements("META_FIELDS")->>'entityType' as entity_type,
    jsonb_array_elements("META_FIELDS")->>'dataType' as data_type,
    COUNT(*) as usage_count
FROM "APPATTACH_CATALOGUES"
WHERE "META_FIELDS" != '[]'::jsonb
AND "IS_DELETED" = false
GROUP BY 
    jsonb_array_elements("META_FIELDS")->>'entityType',
    jsonb_array_elements("META_FIELDS")->>'dataType'
ORDER BY usage_count DESC;
*/
