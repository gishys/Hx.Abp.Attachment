-- 测试PostgreSQL内置全文搜索功能
-- 这个脚本测试我们新的解决方案，不依赖zhparser扩展

-- 1. 创建中文全文搜索配置
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

-- 2. 启用pg_trgm扩展（用于模糊搜索）
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- 3. 测试全文搜索配置
SELECT to_tsvector('chinese_fts', '测试中文全文搜索功能');

-- 4. 测试模糊搜索
SELECT similarity('测试中文', '测试中文搜索');

-- 5. 创建测试数据（如果表存在）
-- 注意：这里假设APPATTACH_CATALOGUES表已经存在
-- 如果没有数据，可以插入一些测试数据

-- 6. 创建全文搜索索引
CREATE INDEX IF NOT EXISTS idx_attach_catalogue_name_fts 
ON "APPATTACH_CATALOGUES" USING gin(to_tsvector('chinese_fts', "CATALOGUE_NAME"));

CREATE INDEX IF NOT EXISTS idx_attach_file_name_fts 
ON "APPATTACHFILE" USING gin(to_tsvector('chinese_fts', "FILENAME"));

-- 7. 创建模糊搜索索引
CREATE INDEX IF NOT EXISTS idx_attach_catalogue_name_trgm 
ON "APPATTACH_CATALOGUES" USING gin("CATALOGUE_NAME" gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_attach_file_name_trgm 
ON "APPATTACHFILE" USING gin("FILENAME" gin_trgm_ops);

-- 8. 测试搜索功能
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

-- 9. 显示配置信息
SELECT cfgname, cfgparser FROM pg_ts_config WHERE cfgname = 'chinese_fts';

-- 10. 显示索引信息
SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'APPATTACH_CATALOGUES' AND indexname LIKE '%fts%' OR indexname LIKE '%trgm%';
