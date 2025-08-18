-- 完整的数据库迁移脚本
-- 包含字段重命名、全文搜索配置、索引创建等所有必要的更新

-- ========================================
-- 1. 字段重命名（从驼峰命名改为下划线命名）
-- ========================================

-- 业务字段重命名
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ATTACHRECEIVETYPE"    TO "ATTACH_RECEIVE_TYPE";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "CATALOGUENAME"        TO "CATALOGUE_NAME";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "REFERENCETYPE"        TO "REFERENCE_TYPE";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ATTACHCOUNT"          TO "ATTACH_COUNT";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "PAGECOUNT"            TO "PAGE_COUNT";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ISVERIFICATION"       TO "IS_VERIFICATION";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "VERIFICATIONPASSED"   TO "VERIFICATION_PASSED";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ISREQUIRED"           TO "IS_REQUIRED";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "SEQUENCENUMBER"       TO "SEQUENCE_NUMBER";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "PARENTID"             TO "PARENT_ID";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ISSTATIC"             TO "IS_STATIC";

-- 审计字段重命名
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "EXTRAPROPERTIES"      TO "EXTRA_PROPERTIES";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "CONCURRENCYSTAMP"     TO "CONCURRENCY_STAMP";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "CREATIONTIME"         TO "CREATION_TIME";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "CREATORID"            TO "CREATOR_ID";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "LASTMODIFICATIONTIME" TO "LAST_MODIFICATION_TIME";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "LASTMODIFIERID"       TO "LAST_MODIFIER_ID";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "ISDELETED"            TO "IS_DELETED";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "DELETERID"            TO "DELETER_ID";
ALTER TABLE public."APPATTACH_CATALOGUES" RENAME COLUMN "DELETIONTIME"         TO "DELETION_TIME";

-- ========================================
-- 2. 创建中文全文搜索配置
-- ========================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'chinese_fts'
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

-- ========================================
-- 3. 启用必要的扩展
-- ========================================

-- 启用pg_trgm扩展（用于模糊搜索）
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ========================================
-- 4. 清理旧的全文搜索向量字段（如果存在）
-- ========================================

DO $$
BEGIN
    -- 如果SEARCH_VECTOR字段存在，则删除（不再需要）
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
-- 5. 创建索引
-- ========================================

-- 删除旧的索引（如果存在）
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUES_SEARCH_VECTOR";
DROP INDEX IF EXISTS "idx_attach_catalogue_name_fts";
DROP INDEX IF EXISTS "idx_attach_catalogue_name_trgm";

-- 创建全文搜索索引（直接基于字段，不使用SEARCH_VECTOR）
CREATE INDEX "idx_attach_catalogue_name_fts" 
ON "APPATTACH_CATALOGUES" 
USING GIN (to_tsvector('chinese_fts', "CATALOGUE_NAME"));

-- 创建模糊搜索索引
CREATE INDEX "idx_attach_catalogue_name_trgm" 
ON "APPATTACH_CATALOGUES" 
USING GIN ("CATALOGUE_NAME" gin_trgm_ops);

-- ========================================
-- 6. 验证迁移结果
-- ========================================

-- 验证字段重命名结果
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
ORDER BY column_name;

-- 验证索引
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUES' 
AND (indexname LIKE '%fts%' OR indexname LIKE '%trgm%')
ORDER BY indexname;

-- 验证全文搜索配置
SELECT cfgname, cfgparser FROM pg_ts_config WHERE cfgname = 'chinese_fts';

-- ========================================
-- 7. 测试搜索功能
-- ========================================

-- 测试全文搜索配置
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能') AS test_vector;

-- 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索') AS similarity_score;

-- 测试实际搜索（如果有数据）
-- SELECT * FROM "APPATTACH_CATALOGUES" 
-- WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试')
-- ORDER BY ts_rank(to_tsvector('chinese_fts', "CATALOGUE_NAME"), plainto_tsquery('chinese_fts', '测试')) DESC;

RAISE NOTICE '数据库迁移完成！所有字段已重命名，全文搜索功能已配置。';
