-- =====================================================
-- 为附件文件表添加OCR相关字段 - 表结构修改部分
-- 文件: add-ocr-fields-to-attach-files.sql
-- 描述: 为 AttachFile 实体添加 OCR_CONTENT、OCR_PROCESS_STATUS、OCR_PROCESSED_TIME 字段
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- 注意: 此脚本只包含表结构修改，索引创建请单独执行
-- =====================================================

-- 检查并安装必要的扩展
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- 检查并创建中文全文搜索配置
DO $$
BEGIN
    -- 检查 chinese_fts 配置是否存在
    IF NOT EXISTS (
        SELECT 1 FROM pg_ts_config 
        WHERE cfgname = 'chinese_fts'
    ) THEN
        -- 创建中文全文搜索配置
        CREATE TEXT SEARCH CONFIGURATION chinese_fts (COPY = simple);
        
        -- 配置中文分词器（使用pg_trgm扩展）
        ALTER TEXT SEARCH CONFIGURATION chinese_fts 
        ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part 
        WITH simple;
        
        RAISE NOTICE '已创建中文全文搜索配置 chinese_fts';
    ELSE
        RAISE NOTICE '中文全文搜索配置 chinese_fts 已存在';
    END IF;
END $$;

-- 添加OCR内容字段（如果不存在）
ALTER TABLE "APPATTACHFILE" 
ADD COLUMN IF NOT EXISTS "OCR_CONTENT" TEXT;

-- 添加OCR处理状态字段（如果不存在）
ALTER TABLE "APPATTACHFILE" 
ADD COLUMN IF NOT EXISTS "OCR_PROCESS_STATUS" INTEGER NOT NULL DEFAULT 0;

-- 添加OCR处理时间字段（如果不存在）
ALTER TABLE "APPATTACHFILE" 
ADD COLUMN IF NOT EXISTS "OCR_PROCESSED_TIME" TIMESTAMP;

-- 添加字段注释
COMMENT ON COLUMN "APPATTACHFILE"."OCR_CONTENT" IS 'OCR提取的文本内容（用于全文检索）';
COMMENT ON COLUMN "APPATTACHFILE"."OCR_PROCESS_STATUS" IS 'OCR处理状态：0=未处理，1=处理中，2=处理完成，3=处理失败，4=跳过';
COMMENT ON COLUMN "APPATTACHFILE"."OCR_PROCESSED_TIME" IS 'OCR处理时间';

-- 验证字段添加成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'OCR_CONTENT'
    ) THEN
        RAISE NOTICE 'OCR内容字段添加成功';
    ELSE
        RAISE EXCEPTION 'OCR内容字段添加失败';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'OCR_PROCESS_STATUS'
    ) THEN
        RAISE NOTICE 'OCR处理状态字段添加成功';
    ELSE
        RAISE EXCEPTION 'OCR处理状态字段添加失败';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'OCR_PROCESSED_TIME'
    ) THEN
        RAISE NOTICE 'OCR处理时间字段添加成功';
    ELSE
        RAISE EXCEPTION 'OCR处理时间字段添加失败';
    END IF;
END $$;

-- =====================================================
-- 为附件文件表创建OCR相关索引
-- 文件: add-ocr-fields-to-attach-files.sql
-- 描述: 为 AttachFile 表的OCR字段创建索引
-- 作者: 系统自动生成
-- 创建时间: 2024-12-19
-- 注意: 此脚本必须在表结构修改完成后单独执行
-- =====================================================

-- 创建索引
DO $$
BEGIN
    -- 创建OCR内容字段的GIN索引（用于全文搜索优化）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_OCR_CONTENT_GIN') THEN
        CREATE INDEX "IDX_APPATTACHFILE_OCR_CONTENT_GIN" 
        ON "APPATTACHFILE" USING GIN (to_tsvector('chinese_fts', "OCR_CONTENT")) 
        WHERE "ISDELETED" = false AND "OCR_CONTENT" IS NOT NULL;
        
        RAISE NOTICE '已创建OCR内容GIN索引';
    ELSE
        RAISE NOTICE 'OCR内容GIN索引已存在';
    END IF;

    -- 创建OCR处理状态字段的btree索引（用于状态查询）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_OCR_PROCESS_STATUS') THEN
        CREATE INDEX "IDX_APPATTACHFILE_OCR_PROCESS_STATUS" 
        ON "APPATTACHFILE" ("OCR_PROCESS_STATUS") 
        WHERE "ISDELETED" = false;
        
        RAISE NOTICE '已创建OCR处理状态索引';
    ELSE
        RAISE NOTICE 'OCR处理状态索引已存在';
    END IF;

    -- 创建OCR处理时间字段的btree索引（用于时间范围查询）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_OCR_PROCESSED_TIME') THEN
        CREATE INDEX "IDX_APPATTACHFILE_OCR_PROCESSED_TIME" 
        ON "APPATTACHFILE" ("OCR_PROCESSED_TIME") 
        WHERE "ISDELETED" = false AND "OCR_PROCESSED_TIME" IS NOT NULL;
        
        RAISE NOTICE '已创建OCR处理时间索引';
    ELSE
        RAISE NOTICE 'OCR处理时间索引已存在';
    END IF;

    -- 创建复合索引（OCR处理状态 + 处理时间）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_OCR_STATUS_TIME') THEN
        CREATE INDEX "IDX_APPATTACHFILE_OCR_STATUS_TIME" 
        ON "APPATTACHFILE" ("OCR_PROCESS_STATUS", "OCR_PROCESSED_TIME") 
        WHERE "ISDELETED" = false;
        
        RAISE NOTICE '已创建OCR状态时间复合索引';
    ELSE
        RAISE NOTICE 'OCR状态时间复合索引已存在';
    END IF;

    -- 创建复合索引（文件类型 + OCR处理状态）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_FILETYPE_OCR_STATUS') THEN
        CREATE INDEX "IDX_APPATTACHFILE_FILETYPE_OCR_STATUS" 
        ON "APPATTACHFILE" ("FILETYPE", "OCR_PROCESS_STATUS") 
        WHERE "ISDELETED" = false;
        
        RAISE NOTICE '已创建文件类型OCR状态复合索引';
    ELSE
        RAISE NOTICE '文件类型OCR状态复合索引已存在';
    END IF;

    -- 创建综合全文搜索索引，包含文件名、OCR内容等
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_COMPREHENSIVE_FULLTEXT') THEN
        -- 先删除旧的全文搜索索引（如果存在）
        DROP INDEX IF EXISTS "IDX_APPATTACHFILE_FULLTEXT";
        DROP INDEX IF EXISTS "IDX_APPATTACHFILE_FULLTEXT_WITH_OCR";
        
        -- 创建新的综合全文搜索索引，包含所有文本内容
        CREATE INDEX "IDX_APPATTACHFILE_COMPREHENSIVE_FULLTEXT" 
        ON "APPATTACHFILE" USING GIN (
            to_tsvector('chinese_fts', 
                COALESCE("FILENAME", '') || ' ' || 
                COALESCE("FILEALIAS", '') || ' ' ||
                COALESCE("REFERENCE", '') || ' ' ||
                COALESCE("OCR_CONTENT", '')
            )
        ) 
        WHERE "ISDELETED" = false;
        
        RAISE NOTICE '已创建综合全文搜索索引（包含文件名、别名、业务引用、OCR内容）';
    ELSE
        RAISE NOTICE '综合全文搜索索引已存在';
    END IF;

    -- 创建支持OCR的文件类型索引（用于快速查找需要OCR处理的文件）
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_OCR_SUPPORTED_FILES') THEN
        CREATE INDEX "IDX_APPATTACHFILE_OCR_SUPPORTED_FILES" 
        ON "APPATTACHFILE" ("FILETYPE", "OCR_PROCESS_STATUS") 
        WHERE "ISDELETED" = false 
        AND "FILETYPE" IN ('.pdf', '.jpg', '.jpeg', '.png', '.tiff', '.tif', '.bmp', '.gif')
        AND "OCR_PROCESS_STATUS" = 0;
        
        RAISE NOTICE '已创建支持OCR的文件类型索引';
    ELSE
        RAISE NOTICE '支持OCR的文件类型索引已存在';
    END IF;
END $$;

-- 验证索引创建成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACHFILE' 
        AND indexname = 'IDX_APPATTACHFILE_COMPREHENSIVE_FULLTEXT'
    ) THEN
        RAISE NOTICE '综合全文搜索索引创建成功';
    ELSE
        RAISE WARNING '综合全文搜索索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACHFILE' 
        AND indexname = 'IDX_APPATTACHFILE_OCR_CONTENT_GIN'
    ) THEN
        RAISE NOTICE 'OCR内容GIN索引创建成功';
    ELSE
        RAISE WARNING 'OCR内容GIN索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACHFILE' 
        AND indexname = 'IDX_APPATTACHFILE_OCR_PROCESS_STATUS'
    ) THEN
        RAISE NOTICE 'OCR处理状态索引创建成功';
    ELSE
        RAISE WARNING 'OCR处理状态索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACHFILE' 
        AND indexname = 'IDX_APPATTACHFILE_OCR_PROCESSED_TIME'
    ) THEN
        RAISE NOTICE 'OCR处理时间索引创建成功';
    ELSE
        RAISE WARNING 'OCR处理时间索引创建可能失败，请检查';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = 'APPATTACHFILE' 
        AND indexname = 'IDX_APPATTACHFILE_OCR_SUPPORTED_FILES'
    ) THEN
        RAISE NOTICE '支持OCR的文件类型索引创建成功';
    ELSE
        RAISE WARNING '支持OCR的文件类型索引创建可能失败，请检查';
    END IF;
END $$;

-- 添加约束检查OCR处理状态的有效值
DO $$
BEGIN
    -- 检查约束是否已存在
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.check_constraints 
        WHERE constraint_name = 'CK_APPATTACHFILE_OCR_PROCESS_STATUS'
    ) THEN
        -- 添加OCR处理状态约束
        ALTER TABLE "APPATTACHFILE" 
        ADD CONSTRAINT "CK_APPATTACHFILE_OCR_PROCESS_STATUS" 
        CHECK ("OCR_PROCESS_STATUS" IN (0, 1, 2, 3, 4));
        
        RAISE NOTICE '已添加OCR处理状态约束';
    ELSE
        RAISE NOTICE 'OCR处理状态约束已存在';
    END IF;
END $$;

-- 验证约束添加成功
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.check_constraints 
        WHERE constraint_name = 'CK_APPATTACHFILE_OCR_PROCESS_STATUS'
    ) THEN
        RAISE NOTICE 'OCR处理状态约束添加成功';
    ELSE
        RAISE WARNING 'OCR处理状态约束添加可能失败，请检查';
    END IF;
END $$;

-- 显示表结构验证信息
DO $$
DECLARE
    column_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO column_count
    FROM information_schema.columns 
    WHERE table_schema = 'public' 
    AND table_name = 'APPATTACHFILE'
    AND column_name IN ('OCR_CONTENT', 'OCR_PROCESS_STATUS', 'OCR_PROCESSED_TIME');
    
    IF column_count = 3 THEN
        RAISE NOTICE '所有OCR相关字段已成功添加到APPATTACHFILE表';
    ELSE
        RAISE WARNING 'OCR相关字段添加不完整，请检查表结构';
    END IF;
END $$;
