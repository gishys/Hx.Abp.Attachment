-- =====================================================
-- 添加工作流配置字段到附件目录模板表（最终版本）
-- 文件: AddWorkflowConfigField_Final.sql
-- 描述: 为 AttachCatalogueTemplate 实体添加 WorkflowConfig 字段
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- =====================================================

-- 添加工作流配置字段（如果不存在）
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD COLUMN IF NOT EXISTS "WORKFLOW_CONFIG" TEXT;

-- 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."WORKFLOW_CONFIG" IS '工作流配置（JSON格式，存储工作流引擎参数）';

-- 验证字段添加成功
SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM information_schema.columns 
            WHERE table_schema = 'public' 
            AND table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
            AND column_name = 'WORKFLOW_CONFIG'
        ) THEN '工作流配置字段添加成功'
        ELSE '工作流配置字段添加失败'
    END AS result;
