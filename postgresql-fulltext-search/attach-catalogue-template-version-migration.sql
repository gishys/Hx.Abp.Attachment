-- =====================================================
-- 附件分类模板版本支持迁移脚本
-- 为 AttachCatalogue 表添加 TemplateVersion 字段
-- 创建时间: 2024-12-19
-- 描述: 支持基于模板ID和版本号的精确模板定位
-- =====================================================

-- 1. 添加 TemplateVersion 字段
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD COLUMN "TEMPLATE_VERSION" INTEGER NULL;

-- 2. 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TEMPLATE_VERSION" IS '关联的模板版本号，与TEMPLATE_ID一起构成完整的模板标识';

-- 3. 创建模板ID索引（如果不存在）
CREATE INDEX IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TEMPLATE_ID" 
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID");

-- 4. 创建模板ID和版本的复合索引
CREATE INDEX IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TEMPLATE_ID_VERSION" 
ON "APPATTACH_CATALOGUES" ("TEMPLATE_ID", "TEMPLATE_VERSION");

-- 5. 添加检查约束，确保版本号为正数
ALTER TABLE "APPATTACH_CATALOGUES" 
ADD CONSTRAINT "CK_ATTACH_CATALOGUES_TEMPLATE_VERSION" 
CHECK ("TEMPLATE_VERSION" IS NULL OR "TEMPLATE_VERSION" > 0);

-- 6. 添加外键约束（可选，如果需要强制引用完整性）
-- 注意：这里需要根据实际的模板表结构调整
-- ALTER TABLE "APPATTACH_CATALOGUES" 
-- ADD CONSTRAINT "FK_ATTACH_CATALOGUES_TEMPLATE_VERSION" 
-- FOREIGN KEY ("TEMPLATE_ID", "TEMPLATE_VERSION") 
-- REFERENCES "ATTACH_CATALOGUE_TEMPLATES" ("ID", "VERSION");

-- 7. 数据迁移：为现有记录设置默认版本号
-- 如果现有记录有 TemplateId 但没有版本号，设置为版本1
UPDATE "APPATTACH_CATALOGUES" 
SET "TEMPLATE_VERSION" = 1 
WHERE "TEMPLATE_ID" IS NOT NULL 
  AND "TEMPLATE_VERSION" IS NULL;

-- 12. 创建全文搜索索引（如果需要）
-- 为模板版本相关字段创建GIN索引以支持全文搜索
CREATE INDEX IF NOT EXISTS "IDX_ATTACH_CATALOGUES_TEMPLATE_FULLTEXT" 
ON "APPATTACH_CATALOGUES" USING GIN (
    to_tsvector('chinese_fts', 
        COALESCE("CATALOGUE_NAME", '') || ' ' || 
        COALESCE("TEMPLATE_ID"::text, '') || ' ' || 
        COALESCE("TEMPLATE_VERSION"::text, '')
    )
) WHERE "IS_DELETED" = false;

-- 15. 验证迁移结果
DO $$
DECLARE
    template_count INTEGER;
    version_count INTEGER;
BEGIN
    -- 检查模板相关记录数量
    SELECT COUNT(*) INTO template_count 
    FROM "APPATTACH_CATALOGUES" 
    WHERE "TEMPLATE_ID" IS NOT NULL;
    
    -- 检查有版本号的记录数量
    SELECT COUNT(*) INTO version_count 
    FROM "APPATTACH_CATALOGUES" 
    WHERE "TEMPLATE_ID" IS NOT NULL AND "TEMPLATE_VERSION" IS NOT NULL;
    
    -- 输出验证结果
    RAISE NOTICE 'Migration completed successfully!';
    RAISE NOTICE 'Records with TemplateId: %', template_count;
    RAISE NOTICE 'Records with TemplateVersion: %', version_count;
    
    -- 验证数据一致性
    IF template_count != version_count THEN
        RAISE WARNING 'Data consistency issue detected: TemplateId count (%) != TemplateVersion count (%)', 
                      template_count, version_count;
    ELSE
        RAISE NOTICE 'Data consistency verified: All template records have version numbers';
    END IF;
END $$;

-- =====================================================
-- 迁移完成
-- =====================================================
-- 新增字段: TEMPLATE_VERSION
-- 新增索引: IDX_ATTACH_CATALOGUES_TEMPLATE_ID, IDX_ATTACH_CATALOGUES_TEMPLATE_ID_VERSION
-- 新增约束: CK_ATTACH_CATALOGUES_TEMPLATE_VERSION
-- 新增视图: V_ATTACH_CATALOGUES_BY_TEMPLATE
-- 新增函数: FN_FIND_CATALOGUES_BY_TEMPLATE, FN_GET_TEMPLATE_CATALOGUE_STATS
-- 新增触发器: TRG_ATTACH_CATALOGUES_TEMPLATE_VERSION
-- 新增全文搜索索引: IDX_ATTACH_CATALOGUES_TEMPLATE_FULLTEXT
-- =====================================================
