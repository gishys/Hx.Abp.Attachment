-- =====================================================
-- AttachCatalogue 表增强功能迁移脚本
-- 为 AttachCatalogue 添加 CatalogueType、CataloguePurpose、TextVector、VectorDimension、Permissions 字段
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

-- 添加新字段
DO $$
BEGIN
    -- 添加 CATALOGUE_FACET_TYPE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'CATALOGUE_FACET_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "CATALOGUE_FACET_TYPE" integer NOT NULL DEFAULT 99;
        
        RAISE NOTICE '已添加 CATALOGUE_FACET_TYPE 字段';
    ELSE
        RAISE NOTICE 'CATALOGUE_FACET_TYPE 字段已存在';
    END IF;

    -- 添加 CATALOGUE_PURPOSE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'CATALOGUE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "CATALOGUE_PURPOSE" integer NOT NULL DEFAULT 1;
        
        RAISE NOTICE '已添加 CATALOGUE_PURPOSE 字段';
    ELSE
        RAISE NOTICE 'CATALOGUE_PURPOSE 字段已存在';
    END IF;

    -- 添加 TEXT_VECTOR 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'TEXT_VECTOR') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "TEXT_VECTOR" double precision[];
        
        RAISE NOTICE '已添加 TEXT_VECTOR 字段';
    ELSE
        RAISE NOTICE 'TEXT_VECTOR 字段已存在';
    END IF;

    -- 添加 VECTOR_DIMENSION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0;
        
        RAISE NOTICE '已添加 VECTOR_DIMENSION 字段';
    ELSE
        RAISE NOTICE 'VECTOR_DIMENSION 字段已存在';
    END IF;

    -- 添加 PERMISSIONS 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'PERMISSIONS') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "PERMISSIONS" jsonb;
        
        RAISE NOTICE '已添加 PERMISSIONS 字段';
    ELSE
        RAISE NOTICE 'PERMISSIONS 字段已存在';
    END IF;
END $$;

-- 添加约束
DO $$
BEGIN
    -- 添加向量维度约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUES_VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_VECTOR_DIMENSION" 
        CHECK ("VECTOR_DIMENSION" >= 0 AND "VECTOR_DIMENSION" <= 2048);
        
        RAISE NOTICE '已添加向量维度约束';
    ELSE
        RAISE NOTICE '向量维度约束已存在';
    END IF;

    -- 添加分类类型约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUES_CATALOGUE_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_CATALOGUE_TYPE" 
        CHECK ("CATALOGUE_FACET_TYPE" IN (1, 2, 3, 4, 99));
        
        RAISE NOTICE '已添加分类类型约束';
    ELSE
        RAISE NOTICE '分类类型约束已存在';
    END IF;

    -- 添加分类用途约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUES_CATALOGUE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_CATALOGUE_PURPOSE" 
        CHECK ("CATALOGUE_PURPOSE" IN (1, 2, 3, 4, 99));
        
        RAISE NOTICE '已添加分类用途约束';
    ELSE
        RAISE NOTICE '分类用途约束已存在';
    END IF;
END $$;

-- 添加字段注释
DO $$
BEGIN
    -- 添加分类类型注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'CATALOGUE_FACET_TYPE' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."CATALOGUE_FACET_TYPE" IS '分类类型：1=项目级,2=阶段级,3=业务分类,4=专业领域,99=通用';
    END IF;

    -- 添加分类用途注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'CATALOGUE_PURPOSE' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."CATALOGUE_PURPOSE" IS '分类用途：1=分类管理,2=文档管理,3=流程管理,4=权限管理,99=其他';
    END IF;

    -- 添加文本向量注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'TEXT_VECTOR' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TEXT_VECTOR" IS '文本向量（64-2048维）';
    END IF;

    -- 添加向量维度注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'VECTOR_DIMENSION' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."VECTOR_DIMENSION" IS '向量维度';
    END IF;

    -- 添加权限集合注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'PERMISSIONS' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."PERMISSIONS" IS '权限集合（JSONB格式，存储权限值对象数组）';
    END IF;
END $$;

-- 创建索引
DO $$
BEGIN
    -- 创建分类类型索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_CATALOGUE_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_CATALOGUE_TYPE" 
        ON "APPATTACH_CATALOGUES" ("CATALOGUE_FACET_TYPE");
        
        RAISE NOTICE '已创建分类类型索引';
    ELSE
        RAISE NOTICE '分类类型索引已存在';
    END IF;

    -- 创建分类用途索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_CATALOGUE_PURPOSE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_CATALOGUE_PURPOSE" 
        ON "APPATTACH_CATALOGUES" ("CATALOGUE_PURPOSE");
        
        RAISE NOTICE '已创建分类用途索引';
    ELSE
        RAISE NOTICE '分类用途索引已存在';
    END IF;

    -- 创建向量维度索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_VECTOR_DIMENSION') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_VECTOR_DIMENSION" 
        ON "APPATTACH_CATALOGUES" ("VECTOR_DIMENSION");
        
        RAISE NOTICE '已创建向量维度索引';
    ELSE
        RAISE NOTICE '向量维度索引已存在';
    END IF;

    -- 创建复合索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TYPE_PURPOSE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TYPE_PURPOSE" 
        ON "APPATTACH_CATALOGUES" ("CATALOGUE_FACET_TYPE", "CATALOGUE_PURPOSE");
        
        RAISE NOTICE '已创建类型用途复合索引';
    ELSE
        RAISE NOTICE '类型用途复合索引已存在';
    END IF;

    -- 创建父级类型复合索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_PARENT_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_PARENT_TYPE" 
        ON "APPATTACH_CATALOGUES" ("PARENT_ID", "CATALOGUE_FACET_TYPE");
        
        RAISE NOTICE '已创建父级类型复合索引';
    ELSE
        RAISE NOTICE '父级类型复合索引已存在';
    END IF;

    -- 创建权限集合GIN索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUES_PERMISSIONS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUES_PERMISSIONS_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN ("PERMISSIONS" jsonb_path_ops);
        
        RAISE NOTICE '已创建权限集合GIN索引';
    ELSE
        RAISE NOTICE '权限集合GIN索引已存在';
    END IF;
END $$;

-- =====================================================
-- 数据更新：将空的 PERMISSIONS 字段更新为空数组
-- =====================================================

-- 更新 NULL 的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "PERMISSIONS" = '[]'::jsonb 
WHERE "PERMISSIONS" IS NULL;

-- 更新空字符串的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "PERMISSIONS" = '""'::jsonb 
WHERE "PERMISSIONS" = '""'::jsonb;

-- 更新无效 JSON 的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUES" 
SET "PERMISSIONS" = '[]'::jsonb 
WHERE "PERMISSIONS" = 'null'::jsonb;

-- 显示更新结果
SELECT 
    COUNT(*) as total_records,
    COUNT("PERMISSIONS") as non_null_permissions,
    COUNT(CASE WHEN "PERMISSIONS" = '[]'::jsonb THEN 1 END) as empty_array_permissions,
    COUNT(CASE WHEN "PERMISSIONS" IS NULL THEN 1 END) as null_permissions
FROM "APPATTACH_CATALOGUES";

-- =====================================================
-- 最佳实践：优化 JSONB 字段配置
-- =====================================================

-- 设置 PERMISSIONS 字段的默认值
ALTER TABLE "APPATTACH_CATALOGUES" 
ALTER COLUMN "PERMISSIONS" SET DEFAULT '[]'::jsonb;

-- 添加 NOT NULL 约束（确保数据一致性）
ALTER TABLE "APPATTACH_CATALOGUES" 
ALTER COLUMN "PERMISSIONS" SET NOT NULL;

-- 添加 JSONB 格式验证约束（确保是数组格式）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD CONSTRAINT "CK_ATTACH_CATALOGUES_PERMISSIONS_FORMAT" 
CHECK (jsonb_typeof("PERMISSIONS") = 'array');

-- 显示最终结果
RAISE NOTICE '=====================================================';
RAISE NOTICE '权限字段优化完成！';
RAISE NOTICE '已创建以下功能：';
RAISE NOTICE '1. 权限字段数据清理和标准化';
RAISE NOTICE '2. 权限字段格式验证约束（数组格式）';
RAISE NOTICE '3. 默认值设置为空数组';
RAISE NOTICE '4. NOT NULL 约束确保数据一致性';
RAISE NOTICE '=====================================================';