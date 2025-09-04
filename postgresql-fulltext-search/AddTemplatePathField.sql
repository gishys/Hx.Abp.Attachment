-- =============================================
-- 添加模板路径字段的数据库迁移脚本
-- 文件名: AddTemplatePathField.sql
-- 描述: 为 AttachCatalogueTemplate 表添加 TemplatePath 字段及相关索引
-- 创建时间: 2024-12-19
-- =============================================

-- 开始事务
BEGIN;

-- 1. 添加 TEMPLATE_PATH 字段
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD COLUMN IF NOT EXISTS "TEMPLATE_PATH" VARCHAR(200) NULL;

-- 2. 添加字段注释
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_PATH" IS '模板路径（用于快速查询层级），格式：00001.00002.00003（5位数字，用点分隔）';

-- 3. 添加模板路径格式验证约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH_FORMAT" 
CHECK (
    "TEMPLATE_PATH" IS NULL 
    OR "TEMPLATE_PATH" ~ '^[0-9]{5}(\.[0-9]{5})*$'
);

-- 4. 创建触发器函数：自动维护模板路径
CREATE OR REPLACE FUNCTION maintain_template_path()
RETURNS TRIGGER AS $$
BEGIN
    -- 验证模板路径格式
    IF NEW."TEMPLATE_PATH" IS NOT NULL AND NEW."TEMPLATE_PATH" != '' THEN
        IF NOT (NEW."TEMPLATE_PATH" ~ '^[0-9]{5}(\.[0-9]{5})*$') THEN
            RAISE EXCEPTION 'Invalid template path format: %', NEW."TEMPLATE_PATH";
        END IF;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 5. 创建触发器
DROP TRIGGER IF EXISTS "TRG_MAINTAIN_TEMPLATE_PATH" ON "APPATTACH_CATALOGUE_TEMPLATES";
CREATE TRIGGER "TRG_MAINTAIN_TEMPLATE_PATH"
    BEFORE INSERT OR UPDATE ON "APPATTACH_CATALOGUE_TEMPLATES"
    FOR EACH ROW
    EXECUTE FUNCTION maintain_template_path();

-- 提交事务
COMMIT;

-- =============================================
-- 以下索引创建需要在事务外执行
-- =============================================

-- 6. 创建模板路径索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PATH") 
WHERE "IS_DELETED" = false AND "TEMPLATE_PATH" IS NOT NULL;

-- 7. 创建模板路径和最新版本的复合索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_LATEST" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PATH", "IS_LATEST") 
WHERE "IS_DELETED" = false;

-- 8. 创建模板路径前缀索引（用于子路径查询）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_PREFIX" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING btree ("TEMPLATE_PATH" text_pattern_ops) 
WHERE "IS_DELETED" = false AND "TEMPLATE_PATH" IS NOT NULL;

-- =============================================
-- 迁移完成
-- =============================================

-- 验证迁移结果
SELECT 
    'Migration completed successfully' as status,
    COUNT(*) as total_templates,
    COUNT(CASE WHEN "TEMPLATE_PATH" IS NOT NULL THEN 1 END) as templates_with_path
FROM "APPATTACH_CATALOGUE_TEMPLATES" 
WHERE "IS_DELETED" = false;