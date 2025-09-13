-- =====================================================
-- 为附件分类表添加全文内容字段 - 表结构修改部分
-- 文件: add-fulltext-fields-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 FullTextContent 和 FullTextContentUpdatedTime 字段
-- 作者: 系统自动生成
-- 创建时间: 2025-09-12
-- 注意: 此脚本只包含表结构修改，索引创建请单独执行
-- =====================================================

-- 检查并安装必要的扩展
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- 检查并创建中文全文搜索配置
DO $$
BEGIN
    -- 检查 chinese_fts 配置是否存在
    IF NOT EXISTS (
        SELECT 1 FROM pg_ts_config 
        WHERE cfgname = 'chinese_fts'
    ) THEN
        -- 创建中文全文搜索配置
        CREATE TEXT SEARCH CONFIGURATION chinese_fts (COPY = simple);
        
        -- 配置中文分词器（使用pg_trgm扩展）
        ALTER TEXT SEARCH CONFIGURATION chinese_fts 
        ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part 
        WITH simple;
        
        RAISE NOTICE '已创建中文全文搜索配置 chinese_fts';
    ELSE
        RAISE NOTICE '中文全文搜索配置 chinese_fts 已存在';
    END IF;
END $$;

-- 添加全文内容字段（如果不存在）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN IF NOT EXISTS "FULL_TEXT_CONTENT" TEXT;

-- 添加全文内容更新时间字段（如果不存在）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN IF NOT EXISTS "FULL_TEXT_CONTENT_UPDATED_TIME" TIMESTAMP;

-- 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."FULL_TEXT_CONTENT" IS '全文内容（用于全文检索）';
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."FULL_TEXT_CONTENT_UPDATED_TIME" IS '全文内容更新时间';

-- 验证字段添加成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'FULL_TEXT_CONTENT'
    ) THEN
        RAISE NOTICE '全文内容字段添加成功';
    ELSE
        RAISE EXCEPTION '全文内容字段添加失败';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'FULL_TEXT_CONTENT_UPDATED_TIME'
    ) THEN
        RAISE NOTICE '全文内容更新时间字段添加成功';
    ELSE
        RAISE EXCEPTION '全文内容更新时间字段添加失败';
    END IF;
END $$;

-- =====================================================
-- 为附件分类表创建全文内容相关索引
-- 文件: add-fulltext-fields-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 表的全文内容字段创建索引
-- 作者: 系统自动生成
-- 创建时间: 2025-09-12
-- 注意: 此脚本必须在表结构修改完成后单独执行
-- =====================================================

-- 创建索引
DO $$
BEGIN
    -- 创建全文内容字段的GIN索引（用于全文搜索优化）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_GIN') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN (to_tsvector('chinese_fts', "FULL_TEXT_CONTENT")) 
        WHERE "IS_DELETED" = false AND "FULL_TEXT_CONTENT" IS NOT NULL;
        
        RAISE NOTICE '已创建全文内容GIN索引';
    ELSE
        RAISE NOTICE '全文内容GIN索引已存在';
    END IF;

    -- 创建全文内容更新时间字段的btree索引（用于时间范围查询）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_UPDATED_TIME') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_FULLTEXT_UPDATED_TIME" 
        ON "APPATTACH_CATALOGUES" ("FULL_TEXT_CONTENT_UPDATED_TIME") 
        WHERE "IS_DELETED" = false AND "FULL_TEXT_CONTENT_UPDATED_TIME" IS NOT NULL;
        
        RAISE NOTICE '已创建全文内容更新时间索引';
    ELSE
        RAISE NOTICE '全文内容更新时间索引已存在';
    END IF;

    -- 创建复合索引（全文内容 + 更新时间）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_TIME') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_TIME" 
        ON "APPATTACH_CATALOGUES" ("FULL_TEXT_CONTENT_UPDATED_TIME", "FULL_TEXT_CONTENT") 
        WHERE "IS_DELETED" = false AND "FULL_TEXT_CONTENT" IS NOT NULL;
        
        RAISE NOTICE '已创建全文内容复合索引';
    ELSE
        RAISE NOTICE '全文内容复合索引已存在';
    END IF;

    -- 更新或创建综合全文搜索索引，包含所有文本字段
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT') THEN
        -- 先删除旧的全文搜索索引（如果存在）
        DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT";
        DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS";
        
        -- 创建新的综合全文搜索索引，包含所有文本内容
        CREATE INDEX "IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT" 
        ON "APPATTACH_CATALOGUES" USING GIN (
            to_tsvector('chinese_fts', 
                COALESCE("CATALOGUE_NAME", '') || ' ' || 
                COALESCE("REFERENCE", '') || ' ' ||
                COALESCE("FULL_TEXT_CONTENT", '') || ' ' ||
                COALESCE("TAGS"::text, '')
            )
        ) 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建综合全文搜索索引（包含所有文本字段）';
    ELSE
        RAISE NOTICE '综合全文搜索索引已存在';
    END IF;
END $$;

-- 验证索引创建成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_COMPREHENSIVE_FULLTEXT'
    ) THEN
        RAISE NOTICE '综合全文搜索索引创建成功';
    ELSE
        RAISE WARNING '综合全文搜索索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_GIN'
    ) THEN
        RAISE NOTICE '全文内容GIN索引创建成功';
    ELSE
        RAISE WARNING '全文内容GIN索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_UPDATED_TIME'
    ) THEN
        RAISE NOTICE '全文内容更新时间索引创建成功';
    ELSE
        RAISE WARNING '全文内容更新时间索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_CONTENT_TIME'
    ) THEN
        RAISE NOTICE '全文内容复合索引创建成功';
    ELSE
        RAISE WARNING '全文内容复合索引创建可能失败，请检查';
    END IF;
END $$;
