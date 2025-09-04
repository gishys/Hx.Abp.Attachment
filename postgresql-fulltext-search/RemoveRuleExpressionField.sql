-- =====================================================
-- 删除规则表达式字段从附件目录模板表
-- 文件: RemoveRuleExpressionField.sql
-- 描述: 从 AttachCatalogueTemplate 表中删除 RuleExpression 字段
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- =====================================================

-- 开始事务
BEGIN;

-- 删除规则表达式字段
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
DROP COLUMN IF EXISTS "RULE_EXPRESSION";

-- 提交事务
COMMIT;

-- 更新全文搜索索引（移除规则表达式字段）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT_WITHOUT_RULE" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("TEMPLATE_NAME", '') || ' ' || 
        COALESCE("DESCRIPTION", '') || ' ' ||
        COALESCE("WORKFLOW_CONFIG", '')
    )
) 
WHERE "IS_DELETED" = false;

-- 删除旧的全文搜索索引（如果存在）
DROP INDEX CONCURRENTLY IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT";

-- 验证字段删除成功
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'RULE_EXPRESSION'
    ) THEN
        RAISE NOTICE '规则表达式字段删除成功';
    ELSE
        RAISE EXCEPTION '规则表达式字段删除失败';
    END IF;
END $$;
