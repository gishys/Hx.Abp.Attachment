-- =====================================================
-- AttachCatalogueTemplate 统一迁移脚本
-- 合并了表创建、字段添加、权限字段、索引创建、约束添加等所有功能
-- 支持复合主键 {ID, VERSION} 和完整的实体字段
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 设置锁超时（避免长时间等待）
SET lock_timeout = '30s';

-- 设置语句超时（避免长时间执行）
SET statement_timeout = '300s';

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 创建表（如果不存在）
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES') THEN
        CREATE TABLE "APPATTACH_CATALOGUE_TEMPLATES" (
            "ID" uuid NOT NULL,
            "TEMPLATE_NAME" character varying(256) NOT NULL,
            "VERSION" integer NOT NULL DEFAULT 1,
            "IS_LATEST" boolean NOT NULL DEFAULT true,
            "ATTACH_RECEIVE_TYPE" integer NOT NULL,
            "DESCRIPTION" text,
            "TAGS" jsonb DEFAULT '[]'::jsonb,
            "IS_REQUIRED" boolean NOT NULL DEFAULT false,
            "SEQUENCE_NUMBER" integer NOT NULL DEFAULT 0,
            "IS_STATIC" boolean NOT NULL DEFAULT false,
            "PARENT_ID" uuid,
            "PARENT_VERSION" integer,
            "TEMPLATE_PATH" character varying(200),
            "WORKFLOW_CONFIG" text,
            "FACET_TYPE" integer NOT NULL DEFAULT 0,
            "TEMPLATE_PURPOSE" integer NOT NULL DEFAULT 1,
            "TEXT_VECTOR" double precision[],
            "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0,
            "PERMISSIONS" jsonb NOT NULL DEFAULT '[]'::jsonb,
            "META_FIELDS" jsonb DEFAULT '[]'::jsonb,
            "EXTRA_PROPERTIES" text,
            "CONCURRENCY_STAMP" character varying(40),
            "CREATION_TIME" timestamp without time zone NOT NULL,
            "CREATOR_ID" uuid,
            "LAST_MODIFICATION_TIME" timestamp without time zone,
            "LAST_MODIFIER_ID" uuid,
            "IS_DELETED" boolean NOT NULL DEFAULT false,
            "DELETER_ID" uuid,
            "DELETION_TIME" timestamp without time zone,
            CONSTRAINT "PK_ATTACH_CATALOGUE_TEMPLATES" PRIMARY KEY ("ID", "VERSION")
        );
        
        RAISE NOTICE '已创建表 APPATTACH_CATALOGUE_TEMPLATES';
    ELSE
        RAISE NOTICE '表 APPATTACH_CATALOGUE_TEMPLATES 已存在';
    END IF;
END $$;

-- =====================================================
-- 2. 添加新字段（如果不存在）
-- =====================================================
DO $$
BEGIN
    -- 添加 DESCRIPTION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'DESCRIPTION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "DESCRIPTION" text;
        RAISE NOTICE '已添加 DESCRIPTION 字段';
    END IF;

    -- 添加 TAGS 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TAGS') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TAGS" jsonb DEFAULT '[]'::jsonb;
        RAISE NOTICE '已添加 TAGS 字段';
    END IF;

    -- 添加 PARENT_VERSION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'PARENT_VERSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "PARENT_VERSION" integer;
        RAISE NOTICE '已添加 PARENT_VERSION 字段';
    END IF;

    -- 添加 TEMPLATE_PATH 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_PATH') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_PATH" character varying(200);
        RAISE NOTICE '已添加 TEMPLATE_PATH 字段';
    END IF;

    -- 添加 WORKFLOW_CONFIG 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'WORKFLOW_CONFIG') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "WORKFLOW_CONFIG" text;
        RAISE NOTICE '已添加 WORKFLOW_CONFIG 字段';
    END IF;

    -- 添加 FACET_TYPE 字段（替换 TEMPLATE_TYPE）
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'FACET_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "FACET_TYPE" integer NOT NULL DEFAULT 0;
        RAISE NOTICE '已添加 FACET_TYPE 字段';
    END IF;

    -- 添加 TEMPLATE_PURPOSE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_PURPOSE" integer NOT NULL DEFAULT 1;
        RAISE NOTICE '已添加 TEMPLATE_PURPOSE 字段';
    END IF;

    -- 添加 TEXT_VECTOR 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEXT_VECTOR') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEXT_VECTOR" double precision[];
        RAISE NOTICE '已添加 TEXT_VECTOR 字段';
    END IF;

    -- 添加 VECTOR_DIMENSION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0;
        RAISE NOTICE '已添加 VECTOR_DIMENSION 字段';
    END IF;

    -- 添加 PERMISSIONS 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'PERMISSIONS') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "PERMISSIONS" jsonb NOT NULL DEFAULT '[]'::jsonb;
        RAISE NOTICE '已添加 PERMISSIONS 字段';
    END IF;

    -- 添加 META_FIELDS 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'META_FIELDS') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "META_FIELDS" jsonb DEFAULT '[]'::jsonb;
        RAISE NOTICE '已添加 META_FIELDS 字段';
    END IF;
END $$;

-- =====================================================
-- 3. 数据迁移和清理
-- =====================================================
DO $$
BEGIN
    -- 如果存在旧的 TEMPLATE_TYPE 字段，迁移到 FACET_TYPE
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_TYPE') THEN
        -- 更新 FACET_TYPE 字段（如果为空）
        UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
        SET "FACET_TYPE" = "TEMPLATE_TYPE" 
        WHERE "FACET_TYPE" IS NULL OR "FACET_TYPE" = 0;
        
        RAISE NOTICE '已迁移 TEMPLATE_TYPE 到 FACET_TYPE';
    END IF;

    -- 如果存在旧的 TEMPLATE_ID 字段，迁移到 ID
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_ID') THEN
        -- 更新 ID 字段（如果为空）
        UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
        SET "ID" = "TEMPLATE_ID" 
        WHERE "ID" IS NULL;
        
        RAISE NOTICE '已迁移 TEMPLATE_ID 到 ID';
    END IF;
END $$;

-- =====================================================
-- 4. 数据清理和标准化（JSONB 字段）
-- =====================================================
DO $$
BEGIN
    -- 更新 NULL 的 PERMISSIONS 字段为空数组
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "PERMISSIONS" = '[]'::jsonb 
    WHERE "PERMISSIONS" IS NULL;

    -- 更新空字符串的 PERMISSIONS 字段为空数组
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "PERMISSIONS" = '[]'::jsonb 
    WHERE "PERMISSIONS" = '""'::jsonb;

    -- 更新无效 JSON 的 PERMISSIONS 字段为空数组
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "PERMISSIONS" = '[]'::jsonb 
    WHERE "PERMISSIONS" = 'null'::jsonb;

    -- 更新 NULL 的 META_FIELDS 字段为空数组
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "META_FIELDS" = '[]'::jsonb 
    WHERE "META_FIELDS" IS NULL;

    -- 更新 NULL 的 TAGS 字段为空数组
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "TAGS" = '[]'::jsonb 
    WHERE "TAGS" IS NULL;

    RAISE NOTICE '已完成 JSONB 字段数据清理';
END $$;

-- =====================================================
-- 5. 设置字段约束和默认值
-- =====================================================
DO $$
BEGIN
    -- 设置 PERMISSIONS 字段的默认值
    ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    ALTER COLUMN "PERMISSIONS" SET DEFAULT '[]'::jsonb;

    -- 添加 NOT NULL 约束（确保数据一致性）
    ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    ALTER COLUMN "PERMISSIONS" SET NOT NULL;

    RAISE NOTICE '已设置 PERMISSIONS 字段约束和默认值';
END $$;

-- =====================================================
-- 6. 添加字段约束
-- =====================================================
DO $$
BEGIN
    -- 添加向量维度约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION" 
        CHECK ("VECTOR_DIMENSION" >= 0 AND "VECTOR_DIMENSION" <= 2048);
        RAISE NOTICE '已添加向量维度约束';
    END IF;

    -- 添加分面类型约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE" 
        CHECK ("FACET_TYPE" IN (0, 1, 2, 3, 4, 5, 6, 99));
        RAISE NOTICE '已添加分面类型约束';
    END IF;

    -- 添加模板用途约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE" 
        CHECK ("TEMPLATE_PURPOSE" IN (1, 2, 3, 4, 99));
        RAISE NOTICE '已添加模板用途约束';
    END IF;

    -- 添加 JSONB 格式验证约束（确保是数组格式）
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_FORMAT') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_FORMAT" 
        CHECK (jsonb_typeof("PERMISSIONS") = 'array');
        RAISE NOTICE '已添加权限格式约束';
    END IF;
END $$;

-- =====================================================
-- 7. 创建索引
-- =====================================================
DO $$
BEGIN
    -- 基础索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST') THEN
        CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "IS_LATEST") 
        WHERE "IS_DELETED" = false AND "IS_LATEST" = true;
        RAISE NOTICE '已创建名称最新状态唯一索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_ID_LATEST') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_ID_LATEST" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("ID", "IS_LATEST") 
        WHERE "IS_DELETED" = false AND "IS_LATEST" = true;
        RAISE NOTICE '已创建ID最新状态索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PARENT_ID') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PARENT_ID" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("PARENT_ID");
        RAISE NOTICE '已创建父级ID索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_SEQUENCE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_SEQUENCE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("SEQUENCE_NUMBER");
        RAISE NOTICE '已创建序号索引';
    END IF;

    -- 模板标识索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建分面类型索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建模板用途索引';
    END IF;

    -- 复合索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE", "TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建模板标识复合索引';
    END IF;

    -- 向量维度索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("VECTOR_DIMENSION") 
        WHERE "IS_DELETED" = false AND "VECTOR_DIMENSION" > 0;
        RAISE NOTICE '已创建向量维度索引';
    END IF;

    -- 权限集合GIN索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_GIN" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("PERMISSIONS" jsonb_path_ops);
        RAISE NOTICE '已创建权限集合GIN索引';
    END IF;

    -- 元数据字段GIN索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUE_TEMPLATES_META_FIELDS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUE_TEMPLATES_META_FIELDS_GIN" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("META_FIELDS" jsonb_path_ops);
        RAISE NOTICE '已创建元数据字段GIN索引';
    END IF;

    -- 标签GIN索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUE_TEMPLATES_TAGS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUE_TEMPLATES_TAGS_GIN" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("TAGS" jsonb_path_ops);
        RAISE NOTICE '已创建标签GIN索引';
    END IF;

    -- 模板路径索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PATH") 
        WHERE "IS_DELETED" = false AND "TEMPLATE_PATH" IS NOT NULL;
        RAISE NOTICE '已创建模板路径索引';
    END IF;

    -- 模板路径和最新状态复合索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_LATEST') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_LATEST" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PATH", "IS_LATEST") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建模板路径最新状态复合索引';
    END IF;
END $$;

-- =====================================================
-- 8. 创建全文搜索索引
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (
            to_tsvector('chinese_fts', 
                COALESCE("TEMPLATE_NAME", '') || ' ' || 
                COALESCE("DESCRIPTION", '') || ' ' ||
                COALESCE("WORKFLOW_CONFIG", '')
            )
        );
        RAISE NOTICE '已创建全文搜索索引';
    END IF;
END $$;

-- =====================================================
-- 9. 创建外键约束
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'FK_ATTACH_CATALOGUE_TEMPLATES_PARENT') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT" 
        FOREIGN KEY ("PARENT_ID", "PARENT_VERSION") REFERENCES "APPATTACH_CATALOGUE_TEMPLATES" ("ID", "VERSION") 
        ON DELETE CASCADE;
        RAISE NOTICE '已创建父级外键约束（复合键）';
    END IF;
END $$;

-- =====================================================
-- 10. 添加表和字段注释
-- =====================================================
DO $$
BEGIN
    -- 表注释
    COMMENT ON TABLE "APPATTACH_CATALOGUE_TEMPLATES" IS '附件分类模板表';
    
    -- 字段注释
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."ID" IS '模板ID（业务标识）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_NAME" IS '模板名称';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."VERSION" IS '模板版本号';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."IS_LATEST" IS '是否为最新版本';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."ATTACH_RECEIVE_TYPE" IS '附件接收类型';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."DESCRIPTION" IS '模板描述';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TAGS" IS '模板标签（JSONB格式）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."PARENT_ID" IS '父模板ID';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."PARENT_VERSION" IS '父模板版本号';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_PATH" IS '模板路径（层级标识）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."WORKFLOW_CONFIG" IS '工作流配置（JSON格式）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."FACET_TYPE" IS '分面类型';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_PURPOSE" IS '模板用途';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEXT_VECTOR" IS '文本向量';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."VECTOR_DIMENSION" IS '向量维度';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."PERMISSIONS" IS '权限集合（JSONB格式）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."META_FIELDS" IS '元数据字段集合（JSONB格式）';
    
    RAISE NOTICE '已添加表和字段注释';
END $$;

-- =====================================================
-- 11. 验证结果
-- =====================================================
DO $$
BEGIN
    -- 显示数据统计
    RAISE NOTICE '=====================================================';
    RAISE NOTICE 'AttachCatalogueTemplate 统一迁移完成！';
    RAISE NOTICE '已创建以下功能：';
    RAISE NOTICE '1. 完整的表结构和字段';
    RAISE NOTICE '2. 权限字段数据清理和标准化';
    RAISE NOTICE '3. 字段约束和验证';
    RAISE NOTICE '4. 默认值设置';
    RAISE NOTICE '5. 完整的索引体系';
    RAISE NOTICE '6. 全文搜索支持';
    RAISE NOTICE '7. 外键约束';
    RAISE NOTICE '8. 表和字段注释';
    RAISE NOTICE '=====================================================';
END $$;

-- 显示最终统计结果
SELECT 
    COUNT(*) as total_records,
    COUNT("PERMISSIONS") as non_null_permissions,
    COUNT(CASE WHEN "PERMISSIONS" = '[]'::jsonb THEN 1 END) as empty_array_permissions,
    COUNT(CASE WHEN "PERMISSIONS" IS NULL THEN 1 END) as null_permissions,
    COUNT("META_FIELDS") as non_null_meta_fields,
    COUNT(CASE WHEN "META_FIELDS" = '[]'::jsonb THEN 1 END) as empty_array_meta_fields,
    COUNT("TAGS") as non_null_tags,
    COUNT(CASE WHEN "TAGS" = '[]'::jsonb THEN 1 END) as empty_array_tags
FROM "APPATTACH_CATALOGUE_TEMPLATES";

-- 显示表结构
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default,
    character_maximum_length
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
ORDER BY ordinal_position;

-- 提交事务
COMMIT;
