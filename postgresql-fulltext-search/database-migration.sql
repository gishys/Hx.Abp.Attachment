-- 数据库迁移脚本：添加全文搜索和OCR相关字段
-- 执行前请备份数据库

-- 1. 为附件目录表添加全文内容字段
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN IF NOT EXISTS "FULL_TEXT_CONTENT" text,
ADD COLUMN IF NOT EXISTS "FULL_TEXT_CONTENT_UPDATED_TIME" timestamp without time zone;

-- 2. 为附件文件表添加OCR相关字段
ALTER TABLE "APPATTACHFILE" 
ADD COLUMN IF NOT EXISTS "OCR_CONTENT" text,
ADD COLUMN IF NOT EXISTS "OCR_PROCESS_STATUS" integer DEFAULT 0,
ADD COLUMN IF NOT EXISTS "OCR_PROCESSED_TIME" timestamp without time zone;

-- 3. 创建中文全文搜索配置（如果不存在）
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
    END IF;
END $$;

-- 4. 启用模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 5. 创建目录表的全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT" 
ON "APPATTACH_CATALOGUES" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("CATALOGUE_NAME", '') || ' ' || 
        COALESCE("FULL_TEXT_CONTENT", '')
    )
);

-- 6. 创建文件表的全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_FILES_FULLTEXT" 
ON "APPATTACHFILE" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("FILEALIAS", '') || ' ' || 
        COALESCE("OCR_CONTENT", '')
    )
);

-- 7. 创建目录名称的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_NAME_TRGM" 
ON "APPATTACH_CATALOGUES" USING GIN ("CATALOGUE_NAME" gin_trgm_ops);

-- 8. 创建文件别名的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_FILES_NAME_TRGM" 
ON "APPATTACHFILE" USING GIN ("FILEALIAS" gin_trgm_ops);

-- 9. 创建OCR处理状态索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_FILES_OCR_STATUS" 
ON "APPATTACHFILE" ("OCR_PROCESS_STATUS");

-- 10. 创建全文内容更新时间索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_FULLTEXT_TIME" 
ON "APPATTACH_CATALOGUES" ("FULL_TEXT_CONTENT_UPDATED_TIME");

-- 11. 创建OCR处理时间索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_FILES_OCR_TIME" 
ON "APPATTACHFILE" ("OCR_PROCESSED_TIME");

-- 12. 添加注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."FULL_TEXT_CONTENT" IS '全文内容 - 存储分类下所有文件的OCR提取内容';
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."FULL_TEXT_CONTENT_UPDATED_TIME" IS '全文内容更新时间';
COMMENT ON COLUMN "APPATTACHFILE"."OCR_CONTENT" IS 'OCR提取的文本内容';
COMMENT ON COLUMN "APPATTACHFILE"."OCR_PROCESS_STATUS" IS 'OCR处理状态：0-未处理，1-处理中，2-完成，3-失败，4-跳过';
COMMENT ON COLUMN "APPATTACHFILE"."OCR_PROCESSED_TIME" IS 'OCR处理时间';

-- ========================================
-- 13. 字段重命名（从驼峰命名改为下划线命名）
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
-- 14. 验证迁移结果
-- ========================================

-- 验证字段重命名结果
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
ORDER BY column_name;

-- 验证全文搜索配置
SELECT cfgname, cfgparser FROM pg_ts_config WHERE cfgname = 'chinese_fts';

-- ========================================
-- 15. 测试搜索功能
-- ========================================
-- 全文搜索测试
SELECT * FROM "APPATTACH_CATALOGUES" 
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试')
ORDER BY ts_rank(to_tsvector('chinese_fts', "CATALOGUE_NAME"), plainto_tsquery('chinese_fts', '测试')) DESC;

-- 模糊搜索测试
SELECT * FROM "APPATTACH_CATALOGUES" 
WHERE "CATALOGUE_NAME" % '测试'
ORDER BY similarity("CATALOGUE_NAME", '测试') DESC;

-- 组合搜索测试
SELECT DISTINCT * FROM "APPATTACH_CATALOGUES" 
WHERE to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试')
   OR "CATALOGUE_NAME" % '测试'
ORDER BY 
    CASE 
        WHEN to_tsvector('chinese_fts', "CATALOGUE_NAME") @@ plainto_tsquery('chinese_fts', '测试') 
        THEN ts_rank(to_tsvector('chinese_fts', "CATALOGUE_NAME"), plainto_tsquery('chinese_fts', '测试'))
        ELSE 0 
    END DESC,
    similarity("CATALOGUE_NAME", '测试') DESC;

-- ========================================
-- 16. 验证索引创建
-- ========================================
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('APPATTACH_CATALOGUES', 'APPATTACHFILE')
  AND indexname LIKE '%FULLTEXT%' OR indexname LIKE '%TRGM%' OR indexname LIKE '%OCR%'
ORDER BY tablename, indexname;
