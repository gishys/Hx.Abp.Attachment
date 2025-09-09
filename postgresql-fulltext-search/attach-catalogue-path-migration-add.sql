-- =====================================================
-- AttachCatalogue Path 字段添加脚本
-- 功能：为 AttachCatalogue 表添加 Path 字段和相关索引
-- 作者：系统自动生成
-- 日期：2025-09-09
-- 版本：1.5.11
-- =====================================================

DO $$
BEGIN
    -- 检查并添加 PATH 字段
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'PATH'
    ) THEN
        -- 添加 PATH 字段
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "PATH" VARCHAR(500);
        
        RAISE NOTICE '已添加 PATH 字段';
    ELSE
        RAISE NOTICE 'PATH 字段已存在，跳过';
    END IF;

    -- 创建路径索引
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_PATH'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_PATH" 
        ON "APPATTACH_CATALOGUES" ("PATH");
        
        RAISE NOTICE '已创建路径索引 IDX_ATTACH_CATALOGUES_PATH';
    ELSE
        RAISE NOTICE '路径索引 IDX_ATTACH_CATALOGUES_PATH 已存在，跳过';
    END IF;

    -- 创建复合索引：Reference + ReferenceType + Path
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_REF_TYPE_PATH'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_REF_TYPE_PATH" 
        ON "APPATTACH_CATALOGUES" ("REFERENCE", "REFERENCE_TYPE", "PATH");
        
        RAISE NOTICE '已创建复合索引 IDX_ATTACH_CATALOGUES_REF_TYPE_PATH';
    ELSE
        RAISE NOTICE '复合索引 IDX_ATTACH_CATALOGUES_REF_TYPE_PATH 已存在，跳过';
    END IF;

    RAISE NOTICE 'PATH 字段和索引添加完成';
END $$;

-- 输出完成信息
SELECT 'AttachCatalogue Path 字段和索引添加完成！' AS message;