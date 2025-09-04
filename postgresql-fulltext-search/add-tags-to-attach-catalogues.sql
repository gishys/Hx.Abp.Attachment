-- =====================================================
-- 为附件分类表添加标签字段
-- 文件: add-tags-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 Tags 字段
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- =====================================================

BEGIN;

-- 添加标签字段（如果不存在）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN IF NOT EXISTS "TAGS" JSONB DEFAULT '[]'::jsonb;

-- 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TAGS" IS '分类标签（JSON数组格式，用于全文检索）';

-- 创建标签字段的GIN索引（用于JSON查询优化）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TAGS_GIN" 
ON "APPATTACH_CATALOGUES" USING GIN ("TAGS") 
WHERE "IS_DELETED" = false;

-- 创建标签字段的btree索引（用于一般查询）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TAGS" 
ON "APPATTACH_CATALOGUES" ("TAGS") 
WHERE "IS_DELETED" = false AND "TAGS" IS NOT NULL;

-- 更新全文搜索索引，包含标签内容
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT";
CREATE INDEX CONCURRENTLY "IDX_ATTACH_CATALOGUES_FULLTEXT_WITH_TAGS" 
ON "APPATTACH_CATALOGUES" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("CATALOGUE_NAME", '') || ' ' || 
        COALESCE("REFERENCE", '') || ' ' ||
        COALESCE("FULL_TEXT_CONTENT", '') || ' ' ||
        COALESCE(array_to_string(
            CASE 
                WHEN jsonb_typeof("TAGS") = 'array' THEN 
                    ARRAY(SELECT jsonb_array_elements_text("TAGS"))
                ELSE ARRAY[]::text[]
            END, 
            ' '
        ), '')
    )
) 
WHERE "IS_DELETED" = false;

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

COMMIT;
