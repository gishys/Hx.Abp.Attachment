-- =====================================================
-- EF Core 迁移脚本 - 为 AttachCatalogueTemplate 添加新字段
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES') THEN
        RAISE EXCEPTION '表 APPATTACH_CATALOGUE_TEMPLATES 不存在，请先创建表';
    END IF;
END $$;

-- 添加新字段
DO $$
BEGIN
    -- 添加 TEMPLATE_TYPE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_TYPE" integer NOT NULL DEFAULT 99;
        
        RAISE NOTICE '已添加 TEMPLATE_TYPE 字段';
    ELSE
        RAISE NOTICE 'TEMPLATE_TYPE 字段已存在';
    END IF;

    -- 添加 TEMPLATE_PURPOSE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_PURPOSE" integer NOT NULL DEFAULT 1;
        
        RAISE NOTICE '已添加 TEMPLATE_PURPOSE 字段';
    ELSE
        RAISE NOTICE 'TEMPLATE_PURPOSE 字段已存在';
    END IF;

    -- 添加 TEXT_VECTOR 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEXT_VECTOR') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEXT_VECTOR" double precision[];
        
        RAISE NOTICE '已添加 TEXT_VECTOR 字段';
    ELSE
        RAISE NOTICE 'TEXT_VECTOR 字段已存在';
    END IF;

    -- 添加 VECTOR_DIMENSION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0;
        
        RAISE NOTICE '已添加 VECTOR_DIMENSION 字段';
    ELSE
        RAISE NOTICE 'VECTOR_DIMENSION 字段已存在';
    END IF;
END $$;

-- 创建索引
DO $$
BEGIN
    -- 创建模板类型索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建模板类型索引';
    ELSE
        RAISE NOTICE '模板类型索引已存在';
    END IF;

    -- 创建模板用途索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建模板用途索引';
    ELSE
        RAISE NOTICE '模板用途索引已存在';
    END IF;

    -- 创建复合标识索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE", "TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建复合标识索引';
    ELSE
        RAISE NOTICE '复合标识索引已存在';
    END IF;

    -- 创建向量维度索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("VECTOR_DIMENSION") 
        WHERE "IS_DELETED" = false AND "VECTOR_DIMENSION" > 0;
        
        RAISE NOTICE '已创建向量维度索引';
    ELSE
        RAISE NOTICE '向量维度索引已存在';
    END IF;
END $$;

-- 添加约束
DO $$
BEGIN
    -- 添加向量维度约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION" 
        CHECK ("VECTOR_DIMENSION" >= 0 AND "VECTOR_DIMENSION" <= 2048);
        
        RAISE NOTICE '已添加向量维度约束';
    ELSE
        RAISE NOTICE '向量维度约束已存在';
    END IF;

    -- 添加模板类型约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE" 
        CHECK ("TEMPLATE_TYPE" IN (1, 2, 3, 4, 99));
        
        RAISE NOTICE '已添加模板类型约束';
    ELSE
        RAISE NOTICE '模板类型约束已存在';
    END IF;

    -- 添加模板用途约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE" 
        CHECK ("TEMPLATE_PURPOSE" IN (1, 2, 3, 4, 99));
        
        RAISE NOTICE '已添加模板用途约束';
    ELSE
        RAISE NOTICE '模板用途约束已存在';
    END IF;
END $$;

-- 添加字段注释
DO $$
BEGIN
    -- 添加模板类型注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'TEMPLATE_TYPE' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_TYPE" IS '模板类型：1=项目级,2=阶段级,3=业务分类,4=专业领域,99=通用';
    END IF;

    -- 添加模板用途注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'TEMPLATE_PURPOSE' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_PURPOSE" IS '模板用途：1=分类管理,2=文档管理,3=流程管理,4=权限管理,99=其他';
    END IF;

    -- 添加文本向量注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'TEXT_VECTOR' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEXT_VECTOR" IS '文本向量（64-2048维）';
    END IF;

    -- 添加向量维度注释
    IF NOT EXISTS (SELECT FROM pg_description WHERE objoid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES') AND objsubid = (SELECT attnum FROM pg_attribute WHERE attname = 'VECTOR_DIMENSION' AND attrelid = (SELECT oid FROM pg_class WHERE relname = 'APPATTACH_CATALOGUE_TEMPLATES'))) THEN
        COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."VECTOR_DIMENSION" IS '向量维度';
    END IF;
END $$;

-- 更新现有数据的默认值
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "TEMPLATE_TYPE" = 99, "TEMPLATE_PURPOSE" = 1, "VECTOR_DIMENSION" = 0
WHERE "TEMPLATE_TYPE" IS NULL OR "TEMPLATE_PURPOSE" IS NULL OR "VECTOR_DIMENSION" IS NULL;

-- 验证迁移结果
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default,
    character_maximum_length
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
  AND column_name IN ('TEMPLATE_TYPE', 'TEMPLATE_PURPOSE', 'TEXT_VECTOR', 'VECTOR_DIMENSION')
ORDER BY ordinal_position;

-- 验证索引创建
SELECT 
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
  AND indexname LIKE '%TEMPLATE_TYPE%' 
   OR indexname LIKE '%TEMPLATE_PURPOSE%' 
   OR indexname LIKE '%VECTOR_DIM%'
   OR indexname LIKE '%IDENTIFIER%';

-- 验证约束创建
SELECT 
    constraint_name,
    constraint_type,
    check_clause
FROM information_schema.check_constraints 
WHERE constraint_name LIKE '%TEMPLATE_TYPE%' 
   OR constraint_name LIKE '%TEMPLATE_PURPOSE%' 
   OR constraint_name LIKE '%VECTOR_DIMENSION%';

RAISE NOTICE 'EF Core 迁移脚本执行完成！';
