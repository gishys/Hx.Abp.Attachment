-- =====================================================
-- OCR文本块迁移脚本
-- 为系统添加OCR文本识别和文本块管理功能
-- 基于 OcrTextBlock 实体定义，支持简化的文本块结构
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 设置锁超时（避免长时间等待）
SET lock_timeout = '30s';

-- 设置语句超时（避免长时间执行）
SET statement_timeout = '300s';

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 创建OCR文本块表
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS') THEN
        CREATE TABLE "APPATTACH_OCR_TEXT_BLOCKS" (
            "ID" uuid NOT NULL,
            "ATTACH_FILE_ID" uuid NOT NULL,
            "TEXT" text NOT NULL,
            "PROBABILITY" real NOT NULL DEFAULT 0.0,
            "PAGE_INDEX" integer NOT NULL,
            "POSITION_DATA" text NOT NULL DEFAULT '{}',
            "BLOCK_ORDER" integer NOT NULL DEFAULT 0,
            "CREATION_TIME" timestamp without time zone NOT NULL,
            CONSTRAINT "PK_APPATTACH_OCR_TEXT_BLOCKS" PRIMARY KEY ("ID")
        );
        
        RAISE NOTICE '已创建 APPATTACH_OCR_TEXT_BLOCKS 表';
    ELSE
        RAISE NOTICE 'APPATTACH_OCR_TEXT_BLOCKS 表已存在';
    END IF;
END $$;

-- =====================================================
-- 2. 添加新字段（如果不存在）
-- =====================================================
DO $$
BEGIN
    -- 添加 ATTACH_FILE_ID 字段（如果存在旧的 ATTACHMENT_ID）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'ATTACHMENT_ID') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'ATTACH_FILE_ID') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "ATTACH_FILE_ID" uuid;
            RAISE NOTICE '已添加 ATTACH_FILE_ID 字段';
        END IF;
    END IF;

    -- 添加 TEXT 字段（如果存在旧的 TEXT_CONTENT）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'TEXT_CONTENT') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'TEXT') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "TEXT" text;
            RAISE NOTICE '已添加 TEXT 字段';
        END IF;
    END IF;

    -- 添加 PROBABILITY 字段（如果存在旧的 CONFIDENCE_SCORE）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'CONFIDENCE_SCORE') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'PROBABILITY') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "PROBABILITY" real DEFAULT 0.0;
            RAISE NOTICE '已添加 PROBABILITY 字段';
        END IF;
    END IF;

    -- 添加 PAGE_INDEX 字段（如果存在旧的 PAGE_NUMBER）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'PAGE_NUMBER') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'PAGE_INDEX') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "PAGE_INDEX" integer;
            RAISE NOTICE '已添加 PAGE_INDEX 字段';
        END IF;
    END IF;

    -- 添加 POSITION_DATA 字段（如果存在旧的 BOUNDING_BOX）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'BOUNDING_BOX') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'POSITION_DATA') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "POSITION_DATA" text DEFAULT '{}';
            RAISE NOTICE '已添加 POSITION_DATA 字段';
        END IF;
    END IF;

    -- 添加 BLOCK_ORDER 字段（如果存在旧的 BLOCK_INDEX）
    IF EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'BLOCK_INDEX') THEN
        IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS' AND column_name = 'BLOCK_ORDER') THEN
            ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
            ADD COLUMN "BLOCK_ORDER" integer DEFAULT 0;
            RAISE NOTICE '已添加 BLOCK_ORDER 字段';
        END IF;
    END IF;
END $$;

-- =====================================================
-- 3. 添加OCR相关字段到附件分类表
-- =====================================================

-- 为附件分类表添加OCR状态字段
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'OCR_STATUS') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" ADD COLUMN "OCR_STATUS" character varying(20) NOT NULL DEFAULT 'not_processed';
        RAISE NOTICE '已添加 OCR_STATUS 字段到附件分类表';
    ELSE
        RAISE NOTICE 'OCR_STATUS 字段已存在于附件分类表';
    END IF;
END $$;

-- 为附件分类表添加OCR完成时间字段
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'OCR_COMPLETED_TIME') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" ADD COLUMN "OCR_COMPLETED_TIME" timestamp without time zone;
        RAISE NOTICE '已添加 OCR_COMPLETED_TIME 字段到附件分类表';
    ELSE
        RAISE NOTICE 'OCR_COMPLETED_TIME 字段已存在于附件分类表';
    END IF;
END $$;

-- 为附件分类表添加OCR文本块数量字段
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'OCR_TEXT_BLOCK_COUNT') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" ADD COLUMN "OCR_TEXT_BLOCK_COUNT" integer NOT NULL DEFAULT 0;
        RAISE NOTICE '已添加 OCR_TEXT_BLOCK_COUNT 字段到附件分类表';
    ELSE
        RAISE NOTICE 'OCR_TEXT_BLOCK_COUNT 字段已存在于附件分类表';
    END IF;
END $$;

-- 为附件分类表添加OCR平均置信度字段
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUES' AND column_name = 'OCR_AVERAGE_CONFIDENCE') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" ADD COLUMN "OCR_AVERAGE_CONFIDENCE" numeric(5,4);
        RAISE NOTICE '已添加 OCR_AVERAGE_CONFIDENCE 字段到附件分类表';
    ELSE
        RAISE NOTICE 'OCR_AVERAGE_CONFIDENCE 字段已存在于附件分类表';
    END IF;
END $$;

-- =====================================================
-- 4. 创建索引
-- =====================================================

-- 创建OCR文本块索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_APPATTACH_OCR_TEXT_BLOCKS_ATTACH_FILE_PAGE') THEN
        CREATE INDEX "IX_APPATTACH_OCR_TEXT_BLOCKS_ATTACH_FILE_PAGE" 
        ON "APPATTACH_OCR_TEXT_BLOCKS" ("ATTACH_FILE_ID", "PAGE_INDEX", "BLOCK_ORDER");
        
        RAISE NOTICE '已创建附件文件页面块索引';
    ELSE
        RAISE NOTICE '附件文件页面块索引已存在';
    END IF;
END $$;

-- 创建OCR文本块概率索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_APPATTACH_OCR_TEXT_BLOCKS_PROBABILITY') THEN
        CREATE INDEX "IX_APPATTACH_OCR_TEXT_BLOCKS_PROBABILITY" 
        ON "APPATTACH_OCR_TEXT_BLOCKS" ("PROBABILITY");
        
        RAISE NOTICE '已创建概率索引';
    ELSE
        RAISE NOTICE '概率索引已存在';
    END IF;
END $$;

-- 创建OCR文本块页面索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_APPATTACH_OCR_TEXT_BLOCKS_PAGE_INDEX') THEN
        CREATE INDEX "IX_APPATTACH_OCR_TEXT_BLOCKS_PAGE_INDEX" 
        ON "APPATTACH_OCR_TEXT_BLOCKS" ("PAGE_INDEX");
        
        RAISE NOTICE '已创建页面索引';
    ELSE
        RAISE NOTICE '页面索引已存在';
    END IF;
END $$;

-- 创建OCR文本块全文搜索索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_APPATTACH_OCR_TEXT_BLOCKS_FULL_TEXT') THEN
        CREATE INDEX "IX_APPATTACH_OCR_TEXT_BLOCKS_FULL_TEXT" 
        ON "APPATTACH_OCR_TEXT_BLOCKS" USING GIN (to_tsvector('chinese_fts', "TEXT"));
        
        RAISE NOTICE '已创建文本内容全文搜索索引';
    ELSE
        RAISE NOTICE '文本内容全文搜索索引已存在';
    END IF;
END $$;


-- =====================================================
-- 5. 创建约束
-- =====================================================

-- 添加OCR状态约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_APPATTACH_CATALOGUES_OCR_STATUS') THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD CONSTRAINT "CK_APPATTACH_CATALOGUES_OCR_STATUS" 
        CHECK ("OCR_STATUS" IN ('not_processed', 'processing', 'completed', 'failed', 'partially_completed'));
        
        RAISE NOTICE '已添加OCR状态约束';
    ELSE
        RAISE NOTICE 'OCR状态约束已存在';
    END IF;
END $$;

-- 添加概率约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_APPATTACH_OCR_TEXT_BLOCKS_PROBABILITY') THEN
        ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_APPATTACH_OCR_TEXT_BLOCKS_PROBABILITY" 
        CHECK ("PROBABILITY" BETWEEN 0.0 AND 1.0);
        
        RAISE NOTICE '已添加概率约束';
    ELSE
        RAISE NOTICE '概率约束已存在';
    END IF;
END $$;

-- 添加页面索引约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_APPATTACH_OCR_TEXT_BLOCKS_PAGE_INDEX') THEN
        ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_APPATTACH_OCR_TEXT_BLOCKS_PAGE_INDEX" 
        CHECK ("PAGE_INDEX" >= 0);
        
        RAISE NOTICE '已添加页面索引约束';
    ELSE
        RAISE NOTICE '页面索引约束已存在';
    END IF;
END $$;

-- 添加块顺序约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_APPATTACH_OCR_TEXT_BLOCKS_BLOCK_ORDER') THEN
        ALTER TABLE "APPATTACH_OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_APPATTACH_OCR_TEXT_BLOCKS_BLOCK_ORDER" 
        CHECK ("BLOCK_ORDER" >= 0);
        
        RAISE NOTICE '已添加块顺序约束';
    ELSE
        RAISE NOTICE '块顺序约束已存在';
    END IF;
END $$;

-- =====================================================
-- 6. 添加表和字段注释
-- =====================================================
DO $$
BEGIN
    -- 表注释
    COMMENT ON TABLE "APPATTACH_OCR_TEXT_BLOCKS" IS 'OCR文本块表';
    
    -- 字段注释
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."ID" IS '文本块ID';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."ATTACH_FILE_ID" IS '关联的文件ID';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."TEXT" IS '文本内容';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."PROBABILITY" IS '置信度（0.0-1.0）';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."PAGE_INDEX" IS '页面索引（PDF多页时使用）';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."POSITION_DATA" IS '文本位置信息（JSON格式）';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."BLOCK_ORDER" IS '文本块在文档中的位置（用于排序）';
    COMMENT ON COLUMN "APPATTACH_OCR_TEXT_BLOCKS"."CREATION_TIME" IS '创建时间';
    
    RAISE NOTICE '已添加表和字段注释';
END $$;

-- =====================================================
-- 7. 验证迁移结果
-- =====================================================

-- 显示表结构
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_OCR_TEXT_BLOCKS'
ORDER BY ordinal_position;

-- 显示统计信息
SELECT 
    COUNT(*) as total_ocr_blocks,
    COUNT(DISTINCT "ATTACH_FILE_ID") as unique_files,
    AVG("PROBABILITY") as avg_probability,
    MIN("PROBABILITY") as min_probability,
    MAX("PROBABILITY") as max_probability,
    COUNT(CASE WHEN "TEXT" IS NOT NULL AND "TEXT" != '' THEN 1 END) as non_empty_text_blocks
FROM "APPATTACH_OCR_TEXT_BLOCKS";

-- 显示索引信息
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_OCR_TEXT_BLOCKS'
ORDER BY indexname;

-- 显示完成消息
DO $$
BEGIN
    RAISE NOTICE '=====================================================';
    RAISE NOTICE 'OCR文本块迁移完成！';
    RAISE NOTICE '已创建以下功能：';
    RAISE NOTICE '1. APPATTACH_OCR_TEXT_BLOCKS表（基于OcrTextBlock实体）';
    RAISE NOTICE '2. 字段添加和检查';
    RAISE NOTICE '3. 完整的索引体系';
    RAISE NOTICE '4. 约束和验证规则';
    RAISE NOTICE '5. 表和字段注释';
    RAISE NOTICE '6. 统计信息查询';
    RAISE NOTICE '=====================================================';
END $$;

-- 提交事务
COMMIT;