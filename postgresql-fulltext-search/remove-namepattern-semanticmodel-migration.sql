-- =====================================================
-- 删除 NamePattern 和 SemanticModel 字段迁移脚本
-- 移除不再使用的字段，简化表结构
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- 设置错误处理
DO $$
BEGIN
    RAISE NOTICE '开始执行删除 NamePattern 和 SemanticModel 字段迁移...';

    -- =====================================================
    -- 1. 删除 NamePattern 字段
    -- =====================================================
    -- 检查字段是否存在
    IF EXISTS (SELECT FROM information_schema.columns 
               WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
               AND column_name = 'NAME_PATTERN') THEN
        
        -- 删除字段
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
            DROP COLUMN "NAME_PATTERN";
        
        RAISE NOTICE '已删除 NAME_PATTERN 字段';
    ELSE
        RAISE NOTICE 'NAME_PATTERN 字段不存在，跳过删除';
    END IF;

    -- =====================================================
    -- 2. 删除 SemanticModel 字段
    -- =====================================================
    -- 检查字段是否存在
    IF EXISTS (SELECT FROM information_schema.columns 
               WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
               AND column_name = 'SEMANTIC_MODEL') THEN
        
        -- 删除字段
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
            DROP COLUMN "SEMANTIC_MODEL";
        
        RAISE NOTICE '已删除 SEMANTIC_MODEL 字段';
    ELSE
        RAISE NOTICE 'SEMANTIC_MODEL 字段不存在，跳过删除';
    END IF;

    -- =====================================================
    -- 3. 更新相关索引（如果需要）
    -- =====================================================
    -- 检查是否有基于这些字段的索引需要删除
    -- 这里可以根据实际情况添加索引删除逻辑

    -- =====================================================
    -- 4. 验证迁移结果
    -- =====================================================
    -- 检查字段是否已删除
    IF EXISTS (SELECT FROM information_schema.columns 
               WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
               AND column_name IN ('NAME_PATTERN', 'SEMANTIC_MODEL')) THEN
        RAISE EXCEPTION '字段删除失败：仍有相关字段存在';
    END IF;
    
    RAISE NOTICE '字段删除成功：NAME_PATTERN 和 SEMANTIC_MODEL 已移除';
    RAISE NOTICE '迁移执行完成，准备提交事务...';

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION '迁移过程中发生错误：%', SQLERRM;
END $$;

-- =====================================================
-- 5. 提交事务
-- =====================================================
COMMIT;

-- =====================================================
-- 6. 最终验证
-- =====================================================
-- 检查表结构
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES'
ORDER BY ordinal_position;

-- =====================================================
-- 迁移完成后的注意事项
-- =====================================================
/*
1. 更新应用程序代码中的字段引用
2. 更新相关的 DTO 和映射配置
3. 更新业务逻辑中的字段判断
4. 测试所有相关功能
5. 更新文档和注释

已删除的字段：
- NAME_PATTERN: 分类名称规则
- SEMANTIC_MODEL: AI语义匹配模型名称

保留的核心字段：
- TEMPLATE_NAME: 模板名称
- RULE_EXPRESSION: 规则引擎表达式
- FACET_TYPE: 分面类型
- TEMPLATE_PURPOSE: 模板用途
*/
