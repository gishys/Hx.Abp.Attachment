-- 动态搜索功能数据库迁移脚本
-- 支持基于数据库内容的动态关键词提取和智能匹配

-- 1. 启用必要的PostgreSQL扩展
CREATE EXTENSION IF NOT EXISTS pg_trgm;  -- 模糊搜索扩展
CREATE EXTENSION IF NOT EXISTS pg_similarity;  -- 相似度计算扩展（如果可用）

-- 2. 创建文本搜索配置（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'chinese'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
    END IF;
END $$;

-- 3. 创建模板表的全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_NAME_FULLTEXT" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (
    to_tsvector('chinese', 
        COALESCE("TEMPLATE_NAME", '') || ' ' || 
        COALESCE("SEMANTIC_MODEL", '') || ' ' ||
        COALESCE("NAME_PATTERN", '')
    )
);

-- 4. 创建模板名称的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_NAME_TRGM" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("TEMPLATE_NAME" gin_trgm_ops);

-- 5. 创建语义模型的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_SEMANTIC_TRGM" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("SEMANTIC_MODEL" gin_trgm_ops);

-- 6. 创建名称模式的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_PATTERN_TRGM" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("NAME_PATTERN" gin_trgm_ops);

-- 7. 创建目录表的全文搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_CATALOGUE_NAME_FULLTEXT" 
ON "APPATTACH_CATALOGUES" USING GIN (
    to_tsvector('chinese', 
        COALESCE("CATALOGUE_NAME", '') || ' ' || 
        COALESCE("FULL_TEXT_CONTENT", '')
    )
);

-- 8. 创建目录名称的模糊搜索索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_CATALOGUE_NAME_TRGM" 
ON "APPATTACH_CATALOGUES" USING GIN ("CATALOGUE_NAME" gin_trgm_ops);

-- 9. 创建模板使用统计的复合索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_USAGE_STATS" 
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID", "CREATION_TIME", "IS_DELETED")
WHERE "TEMPLATE_ID" IS NOT NULL;

-- 10. 创建模板活跃度统计索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_TEMPLATE_ACTIVITY" 
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID", "CREATION_TIME")
WHERE "TEMPLATE_ID" IS NOT NULL AND "IS_DELETED" = false;

-- 11. 创建函数：计算模板相似度
CREATE OR REPLACE FUNCTION calculate_template_similarity(
    template_name text,
    semantic_model text,
    name_pattern text,
    query_text text
) RETURNS float AS $$
BEGIN
    RETURN GREATEST(
        COALESCE(similarity(template_name, query_text), 0),
        COALESCE(similarity(semantic_model, query_text), 0),
        COALESCE(similarity(name_pattern, query_text), 0)
    );
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- 12. 创建函数：获取模板使用统计
CREATE OR REPLACE FUNCTION get_template_usage_stats(template_id uuid)
RETURNS TABLE(
    usage_count bigint,
    last_used_time timestamp,
    recent_usage_count bigint
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*)::bigint as usage_count,
        MAX(ac."CREATION_TIME") as last_used_time,
        COUNT(CASE WHEN ac."CREATION_TIME" >= NOW() - INTERVAL '30 days' THEN 1 END)::bigint as recent_usage_count
    FROM "APPATTACH_CATALOGUES" ac
    WHERE ac."TEMPLATE_ID" = template_id 
    AND ac."IS_DELETED" = false;
END;
$$ LANGUAGE plpgsql STABLE;

-- 13. 创建视图：模板推荐统计视图
CREATE OR REPLACE VIEW template_recommendation_stats AS
SELECT 
    t."ID" as template_id,
    t."TEMPLATE_NAME",
    t."SEMANTIC_MODEL",
    t."NAME_PATTERN",
    t."SEQUENCE_NUMBER",
    t."IS_LATEST",
    COALESCE(usage_stats.usage_count, 0) as usage_count,
    usage_stats.last_used_time,
    COALESCE(usage_stats.recent_usage_count, 0) as recent_usage_count,
    CASE 
        WHEN usage_stats.last_used_time IS NOT NULL 
        THEN GREATEST(0, 1 - EXTRACT(EPOCH FROM (NOW() - usage_stats.last_used_time)) / (30 * 24 * 3600))
        ELSE 0 
    END as time_decay_factor
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
LEFT JOIN LATERAL get_template_usage_stats(t."ID") usage_stats ON true
WHERE t."IS_DELETED" = false;

-- 14. 添加注释
COMMENT ON FUNCTION calculate_template_similarity IS '计算模板与查询文本的相似度';
COMMENT ON FUNCTION get_template_usage_stats IS '获取模板使用统计信息';
COMMENT ON VIEW template_recommendation_stats IS '模板推荐统计视图，包含使用频率和时间衰减因子';

-- 15. 验证索引创建
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('APPATTACH_CATALOGUE_TEMPLATES', 'APPATTACH_CATALOGUES')
AND indexname LIKE '%TRGM%' OR indexname LIKE '%FULLTEXT%'
ORDER BY tablename, indexname;

-- 16. 测试相似度函数
SELECT 
    '测试相似度函数' as test_name,
    calculate_template_similarity('工程合同模板', '工程,合同,建设', '{Type}_{ProjectName}_{Date}', '工程') as similarity_score;
