-- =====================================================
-- 为附件分类表添加标签字段
-- 文件: add-tags-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 Tags 字段
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- =====================================================

-- 1. 添加标签字段
DO $$
BEGIN
    -- 添加标签字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'TAGS'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "TAGS" JSONB DEFAULT '[]'::jsonb;
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TAGS" IS '分类标签（JSON数组格式，用于全文检索）';
        
        RAISE NOTICE '已添加标签字段';
    ELSE
        RAISE NOTICE '标签字段已存在，跳过';
    END IF;
END $$;

-- 2. 创建标签字段的GIN索引（用于JSON查询优化）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TAGS_GIN') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TAGS_GIN" 
        ON "APPATTACH_CATALOGUES" USING GIN ("TAGS") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建标签GIN索引';
    ELSE
        RAISE NOTICE '标签GIN索引已存在，跳过';
    END IF;
END $$;

-- 3. 创建标签字段的btree索引（用于一般查询）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TAGS') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TAGS" 
        ON "APPATTACH_CATALOGUES" ("TAGS") 
        WHERE "IS_DELETED" = false AND "TAGS" IS NOT NULL;
        RAISE NOTICE '已创建标签btree索引';
    ELSE
        RAISE NOTICE '标签btree索引已存在，跳过';
    END IF;
END $$;

-- 4. 更新全文搜索索引，包含标签内容
DO $$
BEGIN
    -- 删除旧的全文搜索索引（如果存在）
    IF EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT') THEN
        DROP INDEX "IDX_ATTACH_CATALOGUES_FULLTEXT";
        RAISE NOTICE '已删除旧全文搜索索引';
    END IF;
    
    -- 创建新的全文搜索索引，包含标签内容
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS') THEN
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
        RAISE NOTICE '已创建包含标签的全文搜索索引';
    ELSE
        RAISE NOTICE '包含标签的全文搜索索引已存在，跳过';
    END IF;
END $$;

-- 5. 验证字段添加成功
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
