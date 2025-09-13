-- =====================================================
-- 为附件分类表添加标签字段 - 表结构修改部分
-- 文件: add-tags-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 Tags 字段
-- 作者: 系统自动生成
-- 创建时间: 2025-09-12
-- 注意: 此脚本只包含表结构修改，索引创建请单独执行
-- =====================================================

-- 添加标签字段（如果不存在）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN IF NOT EXISTS "TAGS" JSONB DEFAULT '[]'::jsonb;

-- 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TAGS" IS '分类标签（JSON数组格式，用于全文检索）';

-- 验证字段添加成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'TAGS'
    ) THEN
        RAISE NOTICE '标签字段添加成功';
    ELSE
        RAISE EXCEPTION '标签字段添加失败';
    END IF;
END $$;

-- =====================================================
-- 为附件分类表创建标签相关索引
-- 文件: create-tags-indexes.sql
-- 描述: 为 AttachCatalogue 表的 TAGS 字段创建索引
-- 作者: 系统自动生成
-- 创建时间: 2025-09-12
-- 注意: 此脚本必须在表结构修改完成后单独执行
-- =====================================================

-- 创建索引
DO $$
BEGIN
    -- 创建标签字段的GIN索引（用于JSON查询优化）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TAGS_GIN') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TAGS_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN ("TAGS") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建标签GIN索引';
    ELSE
        RAISE NOTICE '标签GIN索引已存在';
    END IF;

    -- 创建标签字段的btree索引（用于一般查询）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TAGS') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TAGS" 
        ON "APPATTACH_CATALOGUES" ("TAGS") 
        WHERE "IS_DELETED" = false AND "TAGS" IS NOT NULL;
        
        RAISE NOTICE '已创建标签btree索引';
    ELSE
        RAISE NOTICE '标签btree索引已存在';
    END IF;

    -- 更新全文搜索索引，包含标签内容
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS') THEN
        -- 先删除旧的全文搜索索引（如果存在）
        DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT";
        
        -- 创建新的全文搜索索引，包含标签内容
        CREATE INDEX "IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS" 
        ON "APPATTACH_CATALOGUES" USING GIN (
            to_tsvector('chinese_fts', 
                COALESCE("CATALOGUE_NAME", '') || ' ' || 
                COALESCE("REFERENCE", '') || ' ' ||
                COALESCE("FULL_TEXT_CONTENT", '') || ' ' ||
                COALESCE("TAGS"::text, '')
            )
        ) 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建全文搜索索引（包含标签）';
    ELSE
        RAISE NOTICE '全文搜索索引（包含标签）已存在';
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
        AND indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS'
    ) THEN
        RAISE NOTICE '全文搜索索引创建成功';
    ELSE
        RAISE WARNING '全文搜索索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_TAGS_GIN'
    ) THEN
        RAISE NOTICE 'GIN索引创建成功';
    ELSE
        RAISE WARNING 'GIN索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_TAGS'
    ) THEN
        RAISE NOTICE 'btree索引创建成功';
    ELSE
        RAISE WARNING 'btree索引创建可能失败，请检查';
    END IF;
END $$;
