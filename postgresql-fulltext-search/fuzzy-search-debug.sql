-- 模糊搜索诊断脚本
-- 用于调试模糊搜索功能问题

-- 1. 检查 pg_trgm 扩展是否已安装
SELECT * FROM pg_extension WHERE extname = 'pg_trgm';

-- 2. 检查相似度函数是否可用
SELECT similarity('测试', '测试中文') as test_similarity;

-- 3. 检查表结构
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name IN ('APPATTACH_CATALOGUES', 'APPATTACHFILE')
AND column_name IN ('CATALOGUE_NAME', 'FILEALIAS')
ORDER BY table_name, column_name;

-- 4. 检查索引是否存在
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('APPATTACH_CATALOGUES', 'APPATTACHFILE')
AND indexname LIKE '%TRGM%'
ORDER BY tablename, indexname;

-- 5. 检查表中的数据量
SELECT 
    'APPATTACH_CATALOGUES' as table_name,
    COUNT(*) as total_count,
    COUNT("CATALOGUE_NAME") as non_null_count,
    COUNT(DISTINCT "CATALOGUE_NAME") as distinct_count
FROM "APPATTACH_CATALOGUES"
UNION ALL
SELECT 
    'APPATTACHFILE' as table_name,
    COUNT(*) as total_count,
    COUNT("FILEALIAS") as non_null_count,
    COUNT(DISTINCT "FILEALIAS") as distinct_count
FROM "APPATTACHFILE";

-- 6. 测试优化后的多层次搜索策略
WITH test_queries AS (
    SELECT '测试' as query_text
    UNION ALL SELECT '中文'
    UNION ALL SELECT '搜索'
    UNION ALL SELECT '文档'
),
search_results AS (
    SELECT 
        tq.query_text,
        'catalogue' as table_type,
        ac.*,
        CASE 
            -- 1. 子字符串匹配（最高优先级）
            WHEN LOWER(COALESCE(ac."CATALOGUE_NAME", '')) LIKE LOWER('%' || tq.query_text || '%') 
                 OR LOWER(COALESCE(ac."FULL_TEXT_CONTENT", '')) LIKE LOWER('%' || tq.query_text || '%')
            THEN 3
            -- 2. 分词匹配（中等优先级）
            WHEN EXISTS (
                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(ac."CATALOGUE_NAME", '')), ' ')) word
                WHERE word LIKE LOWER('%' || tq.query_text || '%')
            ) OR EXISTS (
                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(ac."FULL_TEXT_CONTENT", '')), ' ')) word
                WHERE word LIKE LOWER('%' || tq.query_text || '%')
            )
            THEN 2
            -- 3. 相似度匹配（最低优先级）
            WHEN COALESCE(similarity(ac."CATALOGUE_NAME", tq.query_text), 0) > 0.05
                 OR COALESCE(similarity(ac."FULL_TEXT_CONTENT", tq.query_text), 0) > 0.05
            THEN 1
            ELSE 0
        END as match_type,
        GREATEST(
            COALESCE(similarity(ac."CATALOGUE_NAME", tq.query_text), 0),
            COALESCE(similarity(ac."FULL_TEXT_CONTENT", tq.query_text), 0)
        ) as similarity_score
    FROM test_queries tq
    CROSS JOIN "APPATTACH_CATALOGUES" ac
    WHERE 
        (LOWER(COALESCE(ac."CATALOGUE_NAME", '')) LIKE LOWER('%' || tq.query_text || '%') 
         OR LOWER(COALESCE(ac."FULL_TEXT_CONTENT", '')) LIKE LOWER('%' || tq.query_text || '%'))
        OR
        (EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(ac."CATALOGUE_NAME", '')), ' ')) word
            WHERE word LIKE LOWER('%' || tq.query_text || '%')
        ) OR EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(ac."FULL_TEXT_CONTENT", '')), ' ')) word
            WHERE word LIKE LOWER('%' || tq.query_text || '%')
        ))
        OR
        (COALESCE(similarity(ac."CATALOGUE_NAME", tq.query_text), 0) > 0.05
         OR COALESCE(similarity(ac."FULL_TEXT_CONTENT", tq.query_text), 0) > 0.05)
    
    UNION ALL
    
    SELECT 
        tq.query_text,
        'file' as table_type,
        af.*,
        CASE 
            -- 1. 子字符串匹配（最高优先级）
            WHEN LOWER(COALESCE(af."FILEALIAS", '')) LIKE LOWER('%' || tq.query_text || '%') 
                 OR LOWER(COALESCE(af."OCR_CONTENT", '')) LIKE LOWER('%' || tq.query_text || '%')
            THEN 3
            -- 2. 分词匹配（中等优先级）
            WHEN EXISTS (
                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(af."FILEALIAS", '')), ' ')) word
                WHERE word LIKE LOWER('%' || tq.query_text || '%')
            ) OR EXISTS (
                SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(af."OCR_CONTENT", '')), ' ')) word
                WHERE word LIKE LOWER('%' || tq.query_text || '%')
            )
            THEN 2
            -- 3. 相似度匹配（最低优先级）
            WHEN COALESCE(similarity(af."FILEALIAS", tq.query_text), 0) > 0.05
                 OR COALESCE(similarity(af."OCR_CONTENT", tq.query_text), 0) > 0.05
            THEN 1
            ELSE 0
        END as match_type,
        GREATEST(
            COALESCE(similarity(af."FILEALIAS", tq.query_text), 0),
            COALESCE(similarity(af."OCR_CONTENT", tq.query_text), 0)
        ) as similarity_score
    FROM test_queries tq
    CROSS JOIN "APPATTACHFILE" af
    WHERE 
        (LOWER(COALESCE(af."FILEALIAS", '')) LIKE LOWER('%' || tq.query_text || '%') 
         OR LOWER(COALESCE(af."OCR_CONTENT", '')) LIKE LOWER('%' || tq.query_text || '%'))
        OR
        (EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(af."FILEALIAS", '')), ' ')) word
            WHERE word LIKE LOWER('%' || tq.query_text || '%')
        ) OR EXISTS (
            SELECT 1 FROM unnest(string_to_array(LOWER(COALESCE(af."OCR_CONTENT", '')), ' ')) word
            WHERE word LIKE LOWER('%' || tq.query_text || '%')
        ))
        OR
        (COALESCE(similarity(af."FILEALIAS", tq.query_text), 0) > 0.05
         OR COALESCE(similarity(af."OCR_CONTENT", tq.query_text), 0) > 0.05)
)
SELECT 
    query_text,
    table_type,
    COUNT(*) as total_matches,
    COUNT(CASE WHEN match_type = 3 THEN 1 END) as substring_matches,
    COUNT(CASE WHEN match_type = 2 THEN 1 END) as word_matches,
    COUNT(CASE WHEN match_type = 1 THEN 1 END) as similarity_matches,
    MAX(similarity_score) as max_similarity,
    AVG(similarity_score) as avg_similarity
FROM search_results
GROUP BY query_text, table_type
ORDER BY query_text, table_type;

-- 7. 显示一些示例数据
SELECT 
    'APPATTACH_CATALOGUES' as table_name,
    "CATALOGUE_NAME",
    "FULL_TEXT_CONTENT",
    GREATEST(
        COALESCE(similarity("CATALOGUE_NAME", '测试'), 0),
        COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0)
    ) as similarity_score
FROM "APPATTACH_CATALOGUES"
WHERE COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.1
   OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.1
ORDER BY GREATEST(
    COALESCE(similarity("CATALOGUE_NAME", '测试'), 0),
    COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0)
) DESC
LIMIT 10;

SELECT 
    'APPATTACHFILE' as table_name,
    "FILEALIAS",
    "OCR_CONTENT",
    GREATEST(
        COALESCE(similarity("FILEALIAS", '测试'), 0),
        COALESCE(similarity("OCR_CONTENT", '测试'), 0)
    ) as similarity_score
FROM "APPATTACHFILE"
WHERE COALESCE(similarity("FILEALIAS", '测试'), 0) > 0.1
   OR COALESCE(similarity("OCR_CONTENT", '测试'), 0) > 0.1
ORDER BY GREATEST(
    COALESCE(similarity("FILEALIAS", '测试'), 0),
    COALESCE(similarity("OCR_CONTENT", '测试'), 0)
) DESC
LIMIT 10;

-- 8. 测试 % 操作符 vs similarity 函数
SELECT 
    'Using % operator (CATALOGUE_NAME)' as method,
    COUNT(*) as match_count
FROM "APPATTACH_CATALOGUES"
WHERE "CATALOGUE_NAME" % '测试'
UNION ALL
SELECT 
    'Using % operator (FULL_TEXT_CONTENT)' as method,
    COUNT(*) as match_count
FROM "APPATTACH_CATALOGUES"
WHERE "FULL_TEXT_CONTENT" % '测试'
UNION ALL
SELECT 
    'Using similarity > 0.3 (CATALOGUE_NAME)' as method,
    COUNT(*) as match_count
FROM "APPATTACH_CATALOGUES"
WHERE COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.3
UNION ALL
SELECT 
    'Using similarity > 0.3 (FULL_TEXT_CONTENT)' as method,
    COUNT(*) as match_count
FROM "APPATTACH_CATALOGUES"
WHERE COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.3
UNION ALL
SELECT 
    'Using similarity > 0.1 (combined)' as method,
    COUNT(*) as match_count
FROM "APPATTACH_CATALOGUES"
WHERE COALESCE(similarity("CATALOGUE_NAME", '测试'), 0) > 0.1
   OR COALESCE(similarity("FULL_TEXT_CONTENT", '测试'), 0) > 0.1;
