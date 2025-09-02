-- =====================================================
-- OCR文本块迁移脚本
-- 为系统添加OCR文本识别和文本块管理功能
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 创建OCR文本块表
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'OCR_TEXT_BLOCKS') THEN
        CREATE TABLE "OCR_TEXT_BLOCKS" (
            "ID" uuid NOT NULL,
            "ATTACHMENT_ID" uuid NOT NULL,
            "PAGE_NUMBER" integer NOT NULL,
            "BLOCK_INDEX" integer NOT NULL,
            "BLOCK_TYPE" character varying(50) NOT NULL DEFAULT 'text',
            "CONFIDENCE_SCORE" numeric(5,4) NOT NULL DEFAULT 0.0,
            "BOUNDING_BOX" jsonb NOT NULL,
            "TEXT_CONTENT" text NOT NULL,
            "TEXT_LANGUAGE" character varying(10) DEFAULT 'zh-CN',
            "TEXT_DIRECTION" character varying(10) DEFAULT 'ltr',
            "FONT_FAMILY" character varying(100),
            "FONT_SIZE" numeric(5,2),
            "FONT_WEIGHT" character varying(20),
            "TEXT_COLOR" character varying(20),
            "BACKGROUND_COLOR" character varying(20),
            "IS_HEADER" boolean NOT NULL DEFAULT false,
            "IS_FOOTER" boolean NOT NULL DEFAULT false,
            "IS_TABLE_CELL" boolean NOT NULL DEFAULT false,
            "TABLE_ROW_INDEX" integer,
            "TABLE_COLUMN_INDEX" integer,
            "IS_DELETED" boolean NOT NULL DEFAULT false,
            "CREATION_TIME" timestamp without time zone NOT NULL,
            "CREATOR_ID" uuid,
            "LAST_MODIFICATION_TIME" timestamp without time zone,
            "LAST_MODIFIER_ID" uuid,
            "DELETION_TIME" timestamp without time zone,
            "DELETER_ID" uuid,
            "CONCURRENCY_STAMP" character varying(40),
            "EXTRA_PROPERTIES" text,
            CONSTRAINT "PK_OCR_TEXT_BLOCKS" PRIMARY KEY ("ID")
        );
        
        RAISE NOTICE '已创建 OCR_TEXT_BLOCKS 表';
    ELSE
        RAISE NOTICE 'OCR_TEXT_BLOCKS 表已存在';
    END IF;
END $$;

-- =====================================================
-- 2. 创建OCR处理历史表
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'OCR_PROCESSING_HISTORIES') THEN
        CREATE TABLE "OCR_PROCESSING_HISTORIES" (
            "ID" uuid NOT NULL,
            "ATTACHMENT_ID" uuid NOT NULL,
            "PROCESSING_STATUS" character varying(20) NOT NULL DEFAULT 'pending',
            "PROCESSING_START_TIME" timestamp without time zone,
            "PROCESSING_END_TIME" timestamp without time zone,
            "PROCESSING_DURATION" interval,
            "TOTAL_PAGES" integer NOT NULL DEFAULT 0,
            "PROCESSED_PAGES" integer NOT NULL DEFAULT 0,
            "SUCCESSFUL_PAGES" integer NOT NULL DEFAULT 0,
            "FAILED_PAGES" integer NOT NULL DEFAULT 0,
            "TOTAL_TEXT_BLOCKS" integer NOT NULL DEFAULT 0,
            "AVERAGE_CONFIDENCE" numeric(5,4),
            "OCR_ENGINE" character varying(100),
            "OCR_ENGINE_VERSION" character varying(50),
            "PROCESSING_PARAMETERS" jsonb,
            "ERROR_MESSAGES" text[],
            "IS_DELETED" boolean NOT NULL DEFAULT false,
            "CREATION_TIME" timestamp without time zone NOT NULL,
            "CREATOR_ID" uuid,
            "LAST_MODIFICATION_TIME" timestamp without time zone,
            "LAST_MODIFIER_ID" uuid,
            "DELETION_TIME" timestamp without time zone,
            "DELETER_ID" uuid,
            "CONCURRENCY_STAMP" character varying(40),
            "EXTRA_PROPERTIES" text,
            CONSTRAINT "PK_OCR_PROCESSING_HISTORIES" PRIMARY KEY ("ID")
        );
        
        RAISE NOTICE '已创建 OCR_PROCESSING_HISTORIES 表';
    ELSE
        RAISE NOTICE 'OCR_PROCESSING_HISTORIES 表已存在';
    END IF;
END $$;

-- =====================================================
-- 3. 创建文本块索引表
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'TEXT_BLOCK_INDEXES') THEN
        CREATE TABLE "TEXT_BLOCK_INDEXES" (
            "ID" uuid NOT NULL,
            "TEXT_BLOCK_ID" uuid NOT NULL,
            "INDEX_TYPE" character varying(50) NOT NULL,
            "INDEX_VALUE" text NOT NULL,
            "INDEX_WEIGHT" numeric(5,4) NOT NULL DEFAULT 1.0,
            "IS_ACTIVE" boolean NOT NULL DEFAULT true,
            "IS_DELETED" boolean NOT NULL DEFAULT false,
            "CREATION_TIME" timestamp without time zone NOT NULL,
            "CREATOR_ID" uuid,
            "LAST_MODIFICATION_TIME" timestamp without time zone,
            "LAST_MODIFIER_ID" uuid,
            "DELETION_TIME" timestamp without time zone,
            "DELETER_ID" uuid,
            "CONCURRENCY_STAMP" character varying(40),
            "EXTRA_PROPERTIES" text,
            CONSTRAINT "PK_TEXT_BLOCK_INDEXES" PRIMARY KEY ("ID")
        );
        
        RAISE NOTICE '已创建 TEXT_BLOCK_INDEXES 表';
    ELSE
        RAISE NOTICE 'TEXT_BLOCK_INDEXES 表已存在';
    END IF;
END $$;

-- =====================================================
-- 4. 添加OCR相关字段到附件分类表
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
-- 5. 创建索引
-- =====================================================

-- 创建OCR文本块索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_TEXT_BLOCKS_ATTACHMENT_PAGE') THEN
        CREATE INDEX "IX_OCR_TEXT_BLOCKS_ATTACHMENT_PAGE" 
        ON "OCR_TEXT_BLOCKS" ("ATTACHMENT_ID", "PAGE_NUMBER", "BLOCK_INDEX");
        
        RAISE NOTICE '已创建附件页面块索引';
    ELSE
        RAISE NOTICE '附件页面块索引已存在';
    END IF;
END $$;

-- 创建OCR文本块类型索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_TEXT_BLOCKS_BLOCK_TYPE') THEN
        CREATE INDEX "IX_OCR_TEXT_BLOCKS_BLOCK_TYPE" 
        ON "OCR_TEXT_BLOCKS" ("BLOCK_TYPE");
        
        RAISE NOTICE '已创建块类型索引';
    ELSE
        RAISE NOTICE '块类型索引已存在';
    END IF;
END $$;

-- 创建OCR文本块置信度索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_TEXT_BLOCKS_CONFIDENCE') THEN
        CREATE INDEX "IX_OCR_TEXT_BLOCKS_CONFIDENCE" 
        ON "OCR_TEXT_BLOCKS" ("CONFIDENCE_SCORE");
        
        RAISE NOTICE '已创建置信度索引';
    ELSE
        RAISE NOTICE '置信度索引已存在';
    END IF;
END $$;

-- 创建OCR文本块全文搜索索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_TEXT_BLOCKS_FULL_TEXT') THEN
        CREATE INDEX "IX_OCR_TEXT_BLOCKS_FULL_TEXT" 
        ON "OCR_TEXT_BLOCKS" USING GIN (to_tsvector('chinese_fts', "TEXT_CONTENT"));
        
        RAISE NOTICE '已创建文本内容全文搜索索引';
    ELSE
        RAISE NOTICE '文本内容全文搜索索引已存在';
    END IF;
END $$;

-- 创建OCR处理历史索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_PROCESSING_HISTORIES_ATTACHMENT') THEN
        CREATE INDEX "IX_OCR_PROCESSING_HISTORIES_ATTACHMENT" 
        ON "OCR_PROCESSING_HISTORIES" ("ATTACHMENT_ID");
        
        RAISE NOTICE '已创建OCR处理历史附件索引';
    ELSE
        RAISE NOTICE 'OCR处理历史附件索引已存在';
    END IF;
END $$;

-- 创建OCR处理状态索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_OCR_PROCESSING_HISTORIES_STATUS') THEN
        CREATE INDEX "IX_OCR_PROCESSING_HISTORIES_STATUS" 
        ON "OCR_PROCESSING_HISTORIES" ("PROCESSING_STATUS");
        
        RAISE NOTICE '已创建OCR处理状态索引';
    ELSE
        RAISE NOTICE 'OCR处理状态索引已存在';
    END IF;
END $$;

-- 创建文本块索引表索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_TEXT_BLOCK_INDEXES_BLOCK_ID') THEN
        CREATE INDEX "IX_TEXT_BLOCK_INDEXES_BLOCK_ID" 
        ON "TEXT_BLOCK_INDEXES" ("TEXT_BLOCK_ID");
        
        RAISE NOTICE '已创建文本块索引表块ID索引';
    ELSE
        RAISE NOTICE '文本块索引表块ID索引已存在';
    END IF;
END $$;

-- 创建文本块索引类型索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_TEXT_BLOCK_INDEXES_INDEX_TYPE') THEN
        CREATE INDEX "IX_TEXT_BLOCK_INDEXES_INDEX_TYPE" 
        ON "TEXT_BLOCK_INDEXES" ("INDEX_TYPE");
        
        RAISE NOTICE '已创建文本块索引类型索引';
    ELSE
        RAISE NOTICE '文本块索引类型索引已存在';
    END IF;
END $$;

-- =====================================================
-- 6. 创建约束
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

-- 添加置信度约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_OCR_TEXT_BLOCKS_CONFIDENCE') THEN
        ALTER TABLE "OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_OCR_TEXT_BLOCKS_CONFIDENCE" 
        CHECK ("CONFIDENCE_SCORE" BETWEEN 0.0 AND 1.0);
        
        RAISE NOTICE '已添加置信度约束';
    ELSE
        RAISE NOTICE '置信度约束已存在';
    END IF;
END $$;

-- 添加页面号约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_OCR_TEXT_BLOCKS_PAGE_NUMBER') THEN
        ALTER TABLE "OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_OCR_TEXT_BLOCKS_PAGE_NUMBER" 
        CHECK ("PAGE_NUMBER" > 0);
        
        RAISE NOTICE '已添加页面号约束';
    ELSE
        RAISE NOTICE '页面号约束已存在';
    END IF;
END $$;

-- 添加块索引约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_OCR_TEXT_BLOCKS_BLOCK_INDEX') THEN
        ALTER TABLE "OCR_TEXT_BLOCKS" 
        ADD CONSTRAINT "CK_OCR_TEXT_BLOCKS_BLOCK_INDEX" 
        CHECK ("BLOCK_INDEX" >= 0);
        
        RAISE NOTICE '已添加块索引约束';
    ELSE
        RAISE NOTICE '块索引约束已存在';
    END IF;
END $$;

-- 添加处理状态约束
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'CK_OCR_PROCESSING_HISTORIES_STATUS') THEN
        ALTER TABLE "OCR_PROCESSING_HISTORIES" 
        ADD CONSTRAINT "CK_OCR_PROCESSING_HISTORIES_STATUS" 
        CHECK ("PROCESSING_STATUS" IN ('pending', 'processing', 'completed', 'failed', 'cancelled'));
        
        RAISE NOTICE '已添加处理状态约束';
    ELSE
        RAISE NOTICE '处理状态约束已存在';
    END IF;
END $$;

-- =====================================================
-- 7. 创建函数
-- =====================================================

-- 创建OCR文本搜索函数
CREATE OR REPLACE FUNCTION search_ocr_text(
    search_query text,
    attachment_id uuid DEFAULT NULL,
    min_confidence numeric(5,4) DEFAULT 0.5,
    page_number integer DEFAULT NULL
)
RETURNS TABLE(
    "ID" uuid,
    "ATTACHMENT_ID" uuid,
    "PAGE_NUMBER" integer,
    "BLOCK_INDEX" integer,
    "BLOCK_TYPE" character varying(50),
    "CONFIDENCE_SCORE" numeric(5,4),
    "TEXT_CONTENT" text,
    "RELEVANCE_SCORE" numeric(10,4)
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        otb."ID",
        otb."ATTACHMENT_ID",
        otb."PAGE_NUMBER",
        otb."BLOCK_INDEX",
        otb."BLOCK_TYPE",
        otb."CONFIDENCE_SCORE",
        otb."TEXT_CONTENT",
        ts_rank(
            to_tsvector('chinese_fts', otb."TEXT_CONTENT"),
            plainto_tsquery('chinese_fts', search_query)
        ) * otb."CONFIDENCE_SCORE" as relevance_score
    FROM "OCR_TEXT_BLOCKS" otb
    WHERE otb."IS_DELETED" = false
      AND otb."CONFIDENCE_SCORE" >= min_confidence
      AND to_tsvector('chinese_fts', otb."TEXT_CONTENT") @@ plainto_tsquery('chinese_fts', search_query)
      AND (attachment_id IS NULL OR otb."ATTACHMENT_ID" = attachment_id)
      AND (page_number IS NULL OR otb."PAGE_NUMBER" = page_number)
    ORDER BY relevance_score DESC, otb."CONFIDENCE_SCORE" DESC;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '已创建OCR文本搜索函数';

-- 创建OCR统计函数
CREATE OR REPLACE FUNCTION get_ocr_statistics(attachment_id uuid)
RETURNS TABLE(
    "TOTAL_PAGES" integer,
    "PROCESSED_PAGES" integer,
    "TOTAL_TEXT_BLOCKS" integer,
    "AVERAGE_CONFIDENCE" numeric(5,4),
    "LOW_CONFIDENCE_BLOCKS" integer,
    "HIGH_CONFIDENCE_BLOCKS" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(DISTINCT otb."PAGE_NUMBER") as total_pages,
        COUNT(DISTINCT CASE WHEN otb."CONFIDENCE_SCORE" > 0 THEN otb."PAGE_NUMBER" END) as processed_pages,
        COUNT(*) as total_text_blocks,
        AVG(otb."CONFIDENCE_SCORE") as average_confidence,
        COUNT(CASE WHEN otb."CONFIDENCE_SCORE" < 0.7 THEN 1 END) as low_confidence_blocks,
        COUNT(CASE WHEN otb."CONFIDENCE_SCORE" >= 0.9 THEN 1 END) as high_confidence_blocks
    FROM "OCR_TEXT_BLOCKS" otb
    WHERE otb."ATTACHMENT_ID" = attachment_id
      AND otb."IS_DELETED" = false;
END;
$$ LANGUAGE plpgsql;

RAISE NOTICE '已创建OCR统计函数';

-- =====================================================
-- 8. 插入示例数据
-- =====================================================

-- 插入示例OCR文本块
DO $$
DECLARE
    sample_attachment_id uuid := gen_random_uuid();
    sample_block_id uuid;
BEGIN
    -- 插入示例附件分类
    IF NOT EXISTS (SELECT 1 FROM "APPATTACH_CATALOGUES" WHERE "REFERENCE" = 'sample-ocr-attachment') THEN
        INSERT INTO "APPATTACH_CATALOGUES" (
            "ID", "CATALOGUE_NAME", "REFERENCE", "REFERENCE_TYPE", "SEQUENCE_NUMBER",
            "OCR_STATUS", "OCR_TEXT_BLOCK_COUNT", "OCR_AVERAGE_CONFIDENCE", "IS_DELETED", "CREATION_TIME"
        ) VALUES (
            sample_attachment_id, '示例OCR附件', 'sample-ocr-attachment', 1, 1,
            'completed', 3, 0.85, false, CURRENT_TIMESTAMP
        );
        
        RAISE NOTICE '已插入示例附件分类，ID: %', sample_attachment_id;
    ELSE
        SELECT "ID" INTO sample_attachment_id FROM "APPATTACH_CATALOGUES" WHERE "REFERENCE" = 'sample-ocr-attachment';
    END IF;
    
    -- 插入示例OCR文本块
    IF NOT EXISTS (SELECT 1 FROM "OCR_TEXT_BLOCKS" WHERE "ATTACHMENT_ID" = sample_attachment_id) THEN
        -- 插入标题块
        sample_block_id := gen_random_uuid();
        INSERT INTO "OCR_TEXT_BLOCKS" (
            "ID", "ATTACHMENT_ID", "PAGE_NUMBER", "BLOCK_INDEX", "BLOCK_TYPE",
            "CONFIDENCE_SCORE", "BOUNDING_BOX", "TEXT_CONTENT", "IS_HEADER",
            "FONT_SIZE", "IS_DELETED", "CREATION_TIME"
        ) VALUES (
            sample_block_id, sample_attachment_id, 1, 0, 'text',
            0.95, '{"x": 100, "y": 50, "width": 400, "height": 30}'::jsonb, '示例文档标题', true,
            18.0, false, CURRENT_TIMESTAMP
        );
        
        -- 插入正文块
        sample_block_id := gen_random_uuid();
        INSERT INTO "OCR_TEXT_BLOCKS" (
            "ID", "ATTACHMENT_ID", "PAGE_NUMBER", "BLOCK_INDEX", "BLOCK_TYPE",
            "CONFIDENCE_SCORE", "BOUNDING_BOX", "TEXT_CONTENT", "IS_HEADER",
            "FONT_SIZE", "IS_DELETED", "CREATION_TIME"
        ) VALUES (
            sample_block_id, sample_attachment_id, 1, 1, 'text',
            0.88, '{"x": 100, "y": 100, "width": 400, "height": 200}'::jsonb, '这是一个示例文档的正文内容，用于测试OCR文本识别功能。', false,
            12.0, false, CURRENT_TIMESTAMP
        );
        
        -- 插入表格块
        sample_block_id := gen_random_uuid();
        INSERT INTO "OCR_TEXT_BLOCKS" (
            "ID", "ATTACHMENT_ID", "PAGE_NUMBER", "BLOCK_INDEX", "BLOCK_TYPE",
            "CONFIDENCE_SCORE", "BOUNDING_BOX", "TEXT_CONTENT", "IS_TABLE_CELL",
            "TABLE_ROW_INDEX", "TABLE_COLUMN_INDEX", "IS_DELETED", "CREATION_TIME"
        ) VALUES (
            sample_block_id, sample_attachment_id, 1, 2, 'table',
            0.92, '{"x": 100, "y": 350, "width": 400, "height": 100}'::jsonb, '示例表格内容', true,
            0, 0, false, CURRENT_TIMESTAMP
        );
        
        RAISE NOTICE '已插入示例OCR文本块';
    ELSE
        RAISE NOTICE '示例OCR文本块已存在';
    END IF;
END $$;

-- =====================================================
-- 9. 验证迁移结果
-- =====================================================

-- 显示表结构
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name IN ('OCR_TEXT_BLOCKS', 'OCR_PROCESSING_HISTORIES', 'TEXT_BLOCK_INDEXES')
ORDER BY table_name, ordinal_position;

-- 显示索引信息
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('OCR_TEXT_BLOCKS', 'OCR_PROCESSING_HISTORIES', 'TEXT_BLOCK_INDEXES')
ORDER BY tablename, indexname;

-- 显示函数信息
SELECT 
    proname,
    prosrc
FROM pg_proc 
WHERE proname IN ('search_ocr_text', 'get_ocr_statistics');

-- 提交事务
COMMIT;

RAISE NOTICE '=====================================================';
RAISE NOTICE 'OCR文本块迁移完成！';
RAISE NOTICE '已创建以下功能：';
RAISE NOTICE '1. OCR文本块表';
RAISE NOTICE '2. OCR处理历史表';
RAISE NOTICE '3. 文本块索引表';
RAISE NOTICE '4. OCR相关字段';
RAISE NOTICE '5. 完整的索引体系';
RAISE NOTICE '6. 约束和验证规则';
RAISE NOTICE '7. OCR搜索函数';
RAISE NOTICE '8. OCR统计函数';
RAISE NOTICE '9. 示例数据';
RAISE NOTICE '=====================================================';
