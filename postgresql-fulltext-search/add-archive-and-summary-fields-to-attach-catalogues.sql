-- =====================================================
-- 为附件分类表添加归档和概要信息字段
-- 文件: add-archive-and-summary-fields-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 IsArchived 和 Summary 字段
-- 作者: 系统自动生成
-- 创建时间: 2025-09-19
-- =====================================================

-- 1. 添加归档标识字段
DO $$
BEGIN
    -- 添加归档标识字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'IS_ARCHIVED'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "IS_ARCHIVED" boolean NOT NULL DEFAULT false;
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."IS_ARCHIVED" IS '归档标识 - 标识分类是否已归档';
        
        RAISE NOTICE '已添加归档标识字段';
    ELSE
        RAISE NOTICE '归档标识字段已存在，跳过';
    END IF;
END $$;

-- 2. 添加概要信息字段
DO $$
BEGIN
    -- 添加概要信息字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'SUMMARY'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "SUMMARY" varchar(2000) NULL;
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."SUMMARY" IS '概要信息 - 分类的描述信息';
        
        RAISE NOTICE '已添加概要信息字段';
    ELSE
        RAISE NOTICE '概要信息字段已存在，跳过';
    END IF;
END $$;

-- 3. 创建归档字段索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_IS_ARCHIVED') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_IS_ARCHIVED" 
        ON "APPATTACH_CATALOGUES" ("IS_ARCHIVED") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建归档字段索引';
    ELSE
        RAISE NOTICE '归档字段索引已存在，跳过';
    END IF;
END $$;

-- 4. 创建复合索引：业务引用 + 业务类型 + 归档状态
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_REF_TYPE_ARCHIVED') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_REF_TYPE_ARCHIVED" 
        ON "APPATTACH_CATALOGUES" ("REFERENCE", "REFERENCE_TYPE", "IS_ARCHIVED") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建复合索引（业务引用+业务类型+归档状态）';
    ELSE
        RAISE NOTICE '复合索引（业务引用+业务类型+归档状态）已存在，跳过';
    END IF;
END $$;

-- 5. 更新现有记录的归档状态（维护已存在的数据）
DO $$
DECLARE
    updated_count INTEGER;
BEGIN
    -- 统计需要更新的记录数
    SELECT COUNT(*) INTO updated_count 
    FROM "APPATTACH_CATALOGUES" 
    WHERE "IS_ARCHIVED" IS NULL AND "IS_DELETED" = false;
    
    IF updated_count > 0 THEN
        -- 更新现有记录的归档状态为 false（未归档）
        UPDATE "APPATTACH_CATALOGUES" 
        SET "IS_ARCHIVED" = false 
        WHERE "IS_ARCHIVED" IS NULL AND "IS_DELETED" = false;
        
        RAISE NOTICE '已更新 % 条现有记录的归档状态为未归档', updated_count;
    ELSE
        RAISE NOTICE '没有需要更新归档状态的记录';
    END IF;
END $$;

-- 6. 创建概要信息字段的全文搜索索引
DO $$
BEGIN
    -- 创建概要信息字段的GIN索引（用于全文搜索优化）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_SUMMARY_GIN') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_SUMMARY_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN (to_tsvector('chinese_fts', "SUMMARY")) 
        WHERE "IS_DELETED" = false AND "SUMMARY" IS NOT NULL;
        
        RAISE NOTICE '已创建概要信息GIN索引';
    ELSE
        RAISE NOTICE '概要信息GIN索引已存在，跳过';
    END IF;
END $$;

-- 7. 更新综合全文搜索索引，包含概要信息
DO $$
BEGIN
    -- 先删除旧的综合全文搜索索引（如果存在）
    IF EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT') THEN
        DROP INDEX "IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT";
        RAISE NOTICE '已删除旧的综合全文搜索索引';
    END IF;
    
    -- 创建新的综合全文搜索索引，包含所有文本内容（包括概要信息和元数据）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT_WITH_SUMMARY') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT_WITH_SUMMARY" 
        ON "APPATTACH_CATALOGUES" USING GIN (
            to_tsvector('chinese_fts', 
                COALESCE("CATALOGUE_NAME", '') || ' ' || 
                COALESCE("REFERENCE", '') || ' ' ||
                COALESCE("FULL_TEXT_CONTENT", '') || ' ' ||
                COALESCE("TAGS"::text, '') || ' ' ||
                COALESCE("SUMMARY", '') || ' ' ||
                COALESCE("META_FIELDS"::text, '')
            )
        ) 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建包含概要信息和元数据的综合全文搜索索引';
    ELSE
        RAISE NOTICE '包含概要信息的综合全文搜索索引已存在，跳过';
    END IF;
END $$;

-- 8. 验证字段添加成功
DO $$
BEGIN
    -- 验证归档标识字段
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'IS_ARCHIVED'
    ) THEN
        RAISE NOTICE '归档标识字段添加成功';
    ELSE
        RAISE EXCEPTION '归档标识字段添加失败';
    END IF;
    
    -- 验证概要信息字段
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'SUMMARY'
    ) THEN
        RAISE NOTICE '概要信息字段添加成功';
    ELSE
        RAISE EXCEPTION '概要信息字段添加失败';
    END IF;
END $$;

-- 9. 验证索引创建成功
DO $$
BEGIN
    -- 验证归档字段索引
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_IS_ARCHIVED'
    ) THEN
        RAISE NOTICE '归档字段索引创建成功';
    ELSE
        RAISE WARNING '归档字段索引创建可能失败，请检查';
    END IF;
    
    -- 验证复合索引
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_REF_TYPE_ARCHIVED'
    ) THEN
        RAISE NOTICE '复合索引（业务引用+业务类型+归档状态）创建成功';
    ELSE
        RAISE WARNING '复合索引（业务引用+业务类型+归档状态）创建可能失败，请检查';
    END IF;
    
    -- 验证概要信息GIN索引
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_SUMMARY_GIN'
    ) THEN
        RAISE NOTICE '概要信息GIN索引创建成功';
    ELSE
        RAISE WARNING '概要信息GIN索引创建可能失败，请检查';
    END IF;
    
    -- 验证综合全文搜索索引
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT_WITH_SUMMARY'
    ) THEN
        RAISE NOTICE '包含概要信息和元数据的综合全文搜索索引创建成功';
    ELSE
        RAISE WARNING '包含概要信息和元数据的综合全文搜索索引创建可能失败，请检查';
    END IF;
END $$;
