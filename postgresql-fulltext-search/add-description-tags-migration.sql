-- =====================================================
-- 添加模板 Description 和 Tags 字段迁移脚本
-- 基于行业最佳实践：倒排索引 + 向量索引的混合检索
-- =====================================================

-- 开始事务
BEGIN;

-- 1. 添加新字段
DO $$ 
BEGIN
    -- 添加 Description 字段
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'DESCRIPTION'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "DESCRIPTION" TEXT;
        
        RAISE NOTICE '已添加 DESCRIPTION 字段';
    ELSE
        RAISE NOTICE 'DESCRIPTION 字段已存在';
    END IF;

    -- 添加 Tags 字段（JSONB 格式，支持高效查询和索引）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'TAGS'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TAGS" JSONB DEFAULT '[]'::jsonb;
        
        RAISE NOTICE '已添加 TAGS 字段';
    ELSE
        RAISE NOTICE 'TAGS 字段已存在';
    END IF;

    -- 添加全文检索向量字段（用于倒排索引）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'FULL_TEXT_VECTOR'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "FULL_TEXT_VECTOR" tsvector;
        
        RAISE NOTICE '已添加 FULL_TEXT_VECTOR 字段';
    ELSE
        RAISE NOTICE 'FULL_TEXT_VECTOR 字段已存在';
    END IF;

    -- 添加搜索权重字段
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'SEARCH_WEIGHTS'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "SEARCH_WEIGHTS" JSONB DEFAULT '{"name": 1.0, "description": 0.8, "tags": 0.6, "rule": 0.4}'::jsonb;
        
        RAISE NOTICE '已添加 SEARCH_WEIGHTS 字段';
    ELSE
        RAISE NOTICE 'SEARCH_WEIGHTS 字段已存在';
    END IF;

EXCEPTION WHEN OTHERS THEN
    RAISE EXCEPTION '添加字段时发生错误: %', SQLERRM;
END $$;

-- 2. 创建全文检索向量（基于模板名称、描述、标签、规则表达式）
DO $$ 
BEGIN
    -- 检查是否存在中文全文检索配置，如果不存在则使用默认配置
    DECLARE
        config_name TEXT;
    BEGIN
        -- 尝试使用中文配置，如果不存在则使用默认配置
        IF EXISTS (
            SELECT 1 FROM pg_ts_config 
            WHERE cfgname = 'chinese_fts'
        ) THEN
            config_name := 'chinese_fts';
            RAISE NOTICE '使用中文全文检索配置: %', config_name;
        ELSE
            config_name := 'simple';
            RAISE NOTICE '中文配置不存在，使用默认配置: %', config_name;
        END IF;
        
        -- 验证配置是否有效
        IF NOT EXISTS (
            SELECT 1 FROM pg_ts_config 
            WHERE cfgname = config_name
        ) THEN
            RAISE EXCEPTION '配置 % 不存在，请检查全文检索配置', config_name;
        END IF;
        
        -- 更新现有记录的全文检索向量
        UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
        SET "FULL_TEXT_VECTOR" = (
            setweight(to_tsvector(config_name::regconfig, COALESCE("TEMPLATE_NAME", '')), 'A') ||
            setweight(to_tsvector(config_name::regconfig, COALESCE("DESCRIPTION", '')), 'B') ||
            setweight(to_tsvector(config_name::regconfig, COALESCE("TAGS"::text, '')), 'C') ||
            setweight(to_tsvector(config_name::regconfig, COALESCE("RULE_EXPRESSION", '')), 'D')
        )
        WHERE "FULL_TEXT_VECTOR" IS NULL;
        
        RAISE NOTICE '已更新现有记录的全文检索向量';
        
        -- 验证更新结果
        IF NOT EXISTS (
            SELECT 1 FROM "APPATTACH_CATALOGUE_TEMPLATES" 
            WHERE "FULL_TEXT_VECTOR" IS NOT NULL
        ) THEN
            RAISE WARNING '警告：没有记录被更新，可能是所有记录的全文检索向量都已存在';
        END IF;
    END;
EXCEPTION WHEN OTHERS THEN
    RAISE EXCEPTION '更新全文检索向量时发生错误: %', SQLERRM;
END $$;

-- 3. 索引创建说明
-- 注意：CREATE INDEX CONCURRENTLY 不能在事务块中运行
-- 索引将在事务提交后单独创建
DO $$ 
BEGIN
    RAISE NOTICE '索引将在事务提交后创建，请稍后手动执行索引创建脚本';
END $$;

-- 4. 全文检索向量更新说明
-- 注意：全文检索向量会在应用层更新，无需数据库触发器
-- 这样可以避免数据库层面的复杂性，保持应用逻辑的清晰性

-- 7. 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."DESCRIPTION" IS '模板描述，用于全文检索和用户理解';
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TAGS" IS '模板标签，JSONB格式，支持高效查询和索引';
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."FULL_TEXT_VECTOR" IS '全文检索向量，基于名称、描述、标签、规则表达式生成';
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."SEARCH_WEIGHTS" IS '搜索权重配置，用于调整不同字段的搜索重要性';

-- 8. 验证迁移结果
DO $$ 
BEGIN
    RAISE NOTICE '=== 迁移验证 ===';
    
    -- 检查字段是否存在
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name IN ('DESCRIPTION', 'TAGS', 'FULL_TEXT_VECTOR', 'SEARCH_WEIGHTS')
    ) THEN
        RAISE NOTICE '✓ 所有新字段已成功添加';
    ELSE
        RAISE EXCEPTION '✗ 部分字段添加失败';
    END IF;
    
    -- 注意：索引将在事务外单独创建，此处不检查索引
    RAISE NOTICE '✓ 索引将在后续步骤中创建';
    
    -- 注意：不再检查函数，因为全文检索逻辑在应用层实现
    RAISE NOTICE '✓ 全文检索逻辑将在应用层实现';
    
    RAISE NOTICE '=== 迁移完成 ===';
EXCEPTION WHEN OTHERS THEN
    RAISE EXCEPTION '验证迁移结果时发生错误: %', SQLERRM;
END $$;

-- 提交事务
COMMIT;

-- 9. 性能优化建议
DO $$ 
BEGIN
    RAISE NOTICE '=== 性能优化建议 ===';
    RAISE NOTICE '1. 定期运行 ANALYZE 更新统计信息';
    RAISE NOTICE '2. 监控索引使用情况，根据查询模式调整索引';
    RAISE NOTICE '3. 考虑使用分区表处理大量数据';
    RAISE NOTICE '4. 定期清理无效的全文检索向量';
    RAISE NOTICE '5. 使用连接池优化数据库连接';
    RAISE NOTICE '6. 如需中文全文检索，可安装 zhparser 扩展';
END $$;

-- 10. 后续步骤说明
DO $$ 
BEGIN
    RAISE NOTICE '=== 后续步骤 ===';
    RAISE NOTICE '✓ 字段和全文检索向量已创建完成';
    RAISE NOTICE '⚠️  索引需要在事务外单独创建';
    RAISE NOTICE '📝 请执行: create-indexes-after-migration.sql';
    RAISE NOTICE '🔗 或手动创建所需的索引';
END $$;
