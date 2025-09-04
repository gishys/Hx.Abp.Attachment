-- =====================================================
-- 创建工作流配置相关索引
-- 文件: CreateWorkflowConfigIndexes.sql
-- 描述: 为 WorkflowConfig 字段创建相关索引
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- =====================================================

-- 创建工作流配置索引（如果不存在）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_WORKFLOW_CONFIG" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("WORKFLOW_CONFIG") 
WHERE "IS_DELETED" = false AND "WORKFLOW_CONFIG" IS NOT NULL;

-- 创建全文搜索索引（如果不存在）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT_WITH_WORKFLOW" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("TEMPLATE_NAME", '') || ' ' || 
        COALESCE("DESCRIPTION", '') || ' ' ||
        COALESCE("WORKFLOW_CONFIG", '')
    )
) 
WHERE "IS_DELETED" = false;

-- 验证索引创建成功
SELECT 
    indexname,
    CASE 
        WHEN indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_WORKFLOW_CONFIG' THEN '工作流配置索引'
        WHEN indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT_WITH_WORKFLOW' THEN '全文搜索索引'
        ELSE '其他索引'
    END AS index_type
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND indexname IN (
    'IDX_ATTACH_CATALOGUE_TEMPLATES_WORKFLOW_CONFIG',
    'IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT_WITH_WORKFLOW'
)
ORDER BY indexname;
