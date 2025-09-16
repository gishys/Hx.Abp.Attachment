-- =====================================================
-- 为附件文件表添加Reference和TemplatePurpose字段
-- 文件: add-ref-purpose-to-files.sql
-- 描述: 为 AttachFile 实体添加 Reference 和 TemplatePurpose 字段
-- 作者: 系统自动生成
-- 创建时间: 2025-09-16
-- =====================================================

-- 1. 添加Reference字段
DO $$
BEGIN
    -- 添加Reference字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'REFERENCE'
    ) THEN
        ALTER TABLE "APPATTACHFILE" 
        ADD COLUMN "REFERENCE" VARCHAR(28);
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACHFILE"."REFERENCE" IS '业务引用（从AttachCatalogue获取）';
        
        RAISE NOTICE '已添加Reference字段';
    ELSE
        RAISE NOTICE 'Reference字段已存在，跳过';
    END IF;
END $$;

-- 2. 添加TemplatePurpose字段
DO $$
BEGIN
    -- 添加TemplatePurpose字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'TEMPLATE_PURPOSE'
    ) THEN
        ALTER TABLE "APPATTACHFILE" 
        ADD COLUMN "TEMPLATE_PURPOSE" INTEGER;
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACHFILE"."TEMPLATE_PURPOSE" IS '模板用途（从AttachCatalogue获取）';
        
        RAISE NOTICE '已添加TemplatePurpose字段';
    ELSE
        RAISE NOTICE 'TemplatePurpose字段已存在，跳过';
    END IF;
END $$;

-- 3. 添加IsCategorized字段
DO $$
BEGIN
    -- 添加IsCategorized字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'IS_CATEGORIZED'
    ) THEN
        ALTER TABLE "APPATTACHFILE" 
        ADD COLUMN "IS_CATEGORIZED" BOOLEAN DEFAULT true;
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACHFILE"."IS_CATEGORIZED" IS '是否已归类到某个分类';
        
        RAISE NOTICE '已添加IsCategorized字段';
    ELSE
        RAISE NOTICE 'IsCategorized字段已存在，跳过';
    END IF;
END $$;

-- 4. 创建Reference字段索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_REFERENCE') THEN
        CREATE INDEX "IDX_APPATTACHFILE_REFERENCE" 
        ON "APPATTACHFILE" ("REFERENCE") 
        WHERE "ISDELETED" = false AND "REFERENCE" IS NOT NULL;
        RAISE NOTICE '已创建Reference字段索引';
    ELSE
        RAISE NOTICE 'Reference字段索引已存在，跳过';
    END IF;
END $$;

-- 5. 创建TemplatePurpose字段索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_TEMPLATE_PURPOSE') THEN
        CREATE INDEX "IDX_APPATTACHFILE_TEMPLATE_PURPOSE" 
        ON "APPATTACHFILE" ("TEMPLATE_PURPOSE") 
        WHERE "ISDELETED" = false AND "TEMPLATE_PURPOSE" IS NOT NULL;
        RAISE NOTICE '已创建TemplatePurpose字段索引';
    ELSE
        RAISE NOTICE 'TemplatePurpose字段索引已存在，跳过';
    END IF;
END $$;

-- 6. 创建IsCategorized字段索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_APPATTACHFILE_IS_CATEGORIZED') THEN
        CREATE INDEX "IDX_APPATTACHFILE_IS_CATEGORIZED" 
        ON "APPATTACHFILE" ("IS_CATEGORIZED") 
        WHERE "ISDELETED" = false;
        RAISE NOTICE '已创建IsCategorized字段索引';
    ELSE
        RAISE NOTICE 'IsCategorized字段索引已存在，跳过';
    END IF;
END $$;

-- 7. 更新现有数据：从关联的AttachCatalogue获取Reference和TemplatePurpose
DO $$
BEGIN
    -- 更新Reference字段
    UPDATE "APPATTACHFILE" 
    SET "REFERENCE" = ac."REFERENCE"
    FROM "APPATTACH_CATALOGUES" ac
    WHERE "APPATTACHFILE"."ATTACHCATALOGUEID" = ac."Id"
    AND "APPATTACHFILE"."REFERENCE" IS NULL;
    
    -- 更新TemplatePurpose字段
    UPDATE "APPATTACHFILE" 
    SET "TEMPLATE_PURPOSE" = ac."CATALOGUE_PURPOSE"
    FROM "APPATTACH_CATALOGUES" ac
    WHERE "APPATTACHFILE"."ATTACHCATALOGUEID" = ac."Id"
    AND "APPATTACHFILE"."TEMPLATE_PURPOSE" IS NULL;
    
    RAISE NOTICE '已更新现有文件的Reference和TemplatePurpose字段';
END $$;

-- 8. 更新现有数据的IsCategorized字段
DO $$
BEGIN
    -- 更新IsCategorized字段：有AttachCatalogueId的文件标记为已归类
    UPDATE "APPATTACHFILE" 
    SET "IS_CATEGORIZED" = CASE 
        WHEN "ATTACHCATALOGUEID" IS NOT NULL THEN true
        ELSE false
    END
    WHERE "IS_CATEGORIZED" IS NULL;
    
    RAISE NOTICE '已更新现有文件的IsCategorized字段';
END $$;

-- 9. 验证字段添加成功
DO $$
DECLARE
    ref_count INTEGER;
    purpose_count INTEGER;
    categorized_count INTEGER;
    total_count INTEGER;
BEGIN
    -- 检查字段是否存在
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'REFERENCE'
    ) AND EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'TEMPLATE_PURPOSE'
    ) AND EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACHFILE' 
        AND column_name = 'IS_CATEGORIZED'
    ) THEN
        -- 统计数据更新情况
        SELECT 
            COUNT(*),
            COUNT("REFERENCE"),
            COUNT("TEMPLATE_PURPOSE"),
            COUNT(CASE WHEN "IS_CATEGORIZED" = true THEN 1 END)
        INTO total_count, ref_count, purpose_count, categorized_count
        FROM "APPATTACHFILE";
        
        RAISE NOTICE '字段添加成功 - 总文件数: %, 有Reference的文件数: %, 有TemplatePurpose的文件数: %, 已归类文件数: %', 
            total_count, ref_count, purpose_count, categorized_count;
    ELSE
        RAISE EXCEPTION '字段添加失败';
    END IF;
END $$;
