-- 添加模板使用统计功能 - 数据库迁移脚本
-- 执行时间：2024年

-- 1. 为 AttachCatalogues 表添加 TemplateId 字段（可空）
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN "TEMPLATE_ID" uuid NULL;

-- 2. 为 TemplateId 字段添加索引以提高查询性能
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TEMPLATE_ID" 
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID") 
WHERE "IS_DELETED" = false;

-- 3. 添加外键约束（可选，如果需要确保数据完整性）
-- ALTER TABLE "APPATTACH_CATALOGUES" 
-- ADD CONSTRAINT "FK_ATTACH_CATALOGUES_TEMPLATE" 
-- FOREIGN KEY ("TEMPLATE_ID") REFERENCES "APPATTACH_CATALOGUE_TEMPLATES" ("ID") 
-- ON DELETE SET NULL;

-- 4. 创建模板使用统计视图（可选，用于快速查询）
CREATE OR REPLACE VIEW "V_TEMPLATE_USAGE_STATS" AS
SELECT 
    t."Id" as template_id,
    t."TemplateName" as template_name,
    COUNT(ac."Id") as usage_count,
    COUNT(DISTINCT ac."Reference") as unique_references,
    MAX(ac."CreationTime") as last_used_time
FROM "APPATTACH_CATALOGUE_TEMPLATES" t
LEFT JOIN "APPATTACH_CATALOGUES" ac ON t."Id" = ac."TEMPLATE_ID" AND ac."IS_DELETED" = false
WHERE t."IsDeleted" = false
GROUP BY t."Id", t."TemplateName";

-- 5. 为视图添加注释
COMMENT ON VIEW "V_TEMPLATE_USAGE_STATS" IS '模板使用统计视图，用于快速查询模板的使用情况';

-- 6. 创建模板使用统计函数（可选，用于复杂统计）
CREATE OR REPLACE FUNCTION "GetTemplateUsageCount"(template_id uuid)
RETURNS integer AS $$
BEGIN
    RETURN (
        SELECT COUNT(*)
        FROM "APPATTACH_CATALOGUES" ac
        WHERE ac."TEMPLATE_ID" = template_id
          AND ac."IS_DELETED" = false
    );
END;
$$ LANGUAGE plpgsql;

-- 7. 为函数添加注释
COMMENT ON FUNCTION "GetTemplateUsageCount"(uuid) IS '获取指定模板的使用次数';

-- 8. 创建模板使用趋势统计函数（可选，用于分析使用趋势）
CREATE OR REPLACE FUNCTION "GetTemplateUsageTrend"(template_id uuid, days_back integer DEFAULT 30)
RETURNS TABLE(
    usage_date date,
    daily_count integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        DATE(ac."CreationTime") as usage_date,
        COUNT(*) as daily_count
    FROM "APPATTACH_CATALOGUES" ac
    WHERE ac."TEMPLATE_ID" = template_id
      AND ac."IS_DELETED" = false
      AND ac."CreationTime" >= CURRENT_DATE - INTERVAL '1 day' * days_back
    GROUP BY DATE(ac."CreationTime")
    ORDER BY usage_date;
END;
$$ LANGUAGE plpgsql;

-- 9. 为趋势函数添加注释
COMMENT ON FUNCTION "GetTemplateUsageTrend"(uuid, integer) IS '获取指定模板的使用趋势统计';

-- 10. 验证迁移结果
-- 检查字段是否添加成功
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
  AND column_name = 'TEMPLATE_ID';

-- 检查索引是否创建成功
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUES' 
  AND indexname LIKE '%TEMPLATE_ID%';

-- 检查视图是否创建成功
SELECT schemaname, viewname 
FROM pg_views 
WHERE viewname = 'v_template_usage_stats';

-- 检查函数是否创建成功
SELECT proname, prosrc 
FROM pg_proc 
WHERE proname IN ('gettemplateusagecount', 'gettemplateusagetrend');

-- 迁移完成提示
SELECT 'Template usage count migration completed successfully!' as migration_status;
