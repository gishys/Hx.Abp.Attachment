-- 清理 SearchVector 字段脚本
-- 由于项目使用原生SQL查询，不需要预计算的SearchVector字段

-- ========================================
-- 1. 删除 SearchVector 字段（如果存在）
-- ========================================

DO $$
BEGIN
    -- 如果SEARCH_VECTOR字段存在，则删除
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'SEARCH_VECTOR'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" DROP COLUMN "SEARCH_VECTOR";
        RAISE NOTICE 'SEARCH_VECTOR字段已删除（不再需要）';
    ELSE
        RAISE NOTICE 'SEARCH_VECTOR字段不存在，无需删除';
    END IF;
END $$;

-- ========================================
-- 2. 删除相关的索引（如果存在）
-- ========================================

DO $$
BEGIN
    -- 删除基于SEARCH_VECTOR的索引
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_SEARCH_VECTOR'
    ) THEN
        DROP INDEX "IDX_ATTACH_CATALOGUES_SEARCH_VECTOR";
        RAISE NOTICE 'SEARCH_VECTOR索引已删除';
    ELSE
        RAISE NOTICE 'SEARCH_VECTOR索引不存在，无需删除';
    END IF;
END $$;

-- ========================================
-- 3. 创建新的全文搜索索引（直接基于字段）
-- ========================================

-- 确保中文全文搜索配置存在
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_ts_config WHERE cfgname = 'chinese_fts'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese_fts
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
        RAISE NOTICE '中文全文搜索配置已创建';
    ELSE
        RAISE NOTICE '中文全文搜索配置已存在';
    END IF;
END $$;

-- 创建全文搜索索引（直接基于CATALOGUE_NAME字段）
CREATE INDEX IF NOT EXISTS "idx_attach_catalogue_name_fts" 
ON "APPATTACH_CATALOGUES" 
USING GIN (to_tsvector('chinese_fts', "CATALOGUE_NAME"));

-- 创建模糊搜索索引
CREATE INDEX IF NOT EXISTS "idx_attach_catalogue_name_trgm" 
ON "APPATTACH_CATALOGUES" 
USING GIN ("CATALOGUE_NAME" gin_trgm_ops);

-- ========================================
-- 4. 验证清理结果
-- ========================================

-- 验证字段已删除
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
AND column_name = 'SEARCH_VECTOR';

-- 验证索引
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUES' 
AND (indexname LIKE '%fts%' OR indexname LIKE '%trgm%')
ORDER BY indexname;

-- ========================================
-- 5. 测试搜索功能
-- ========================================

-- 测试全文搜索配置
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能') AS test_vector;

-- 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索') AS similarity_score;

RAISE NOTICE 'SearchVector字段清理完成！全文搜索功能使用原生SQL查询。';
