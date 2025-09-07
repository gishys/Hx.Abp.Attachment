-- =====================================================
-- AttachCatalogueTemplate 复合主键迁移脚本
-- 将模板版本管理从基于 TemplateName 改为基于 TemplateId + Version 的复合主键
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 备份现有数据
-- =====================================================

-- 创建备份表
CREATE TABLE IF NOT EXISTS "APPATTACH_CATALOGUE_TEMPLATES_BACKUP" AS 
SELECT * FROM "APPATTACH_CATALOGUE_TEMPLATES";

-- 记录备份信息
DO $$
BEGIN
    RAISE NOTICE '已创建备份表 APPATTACH_CATALOGUE_TEMPLATES_BACKUP，包含 % 条记录', 
        (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES_BACKUP");
END $$;

-- =====================================================
-- 2. 删除现有约束和索引
-- =====================================================

-- 删除现有的唯一约束
DROP INDEX IF EXISTS "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST";

-- 删除现有的主键约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP CONSTRAINT IF EXISTS "PK_ATTACH_CATALOGUE_TEMPLATES";

-- 删除现有的外键约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP CONSTRAINT IF EXISTS "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT";

-- =====================================================
-- 3. 添加新字段
-- =====================================================

-- 添加 TEMPLATE_ID 字段（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND column_name = 'TEMPLATE_ID'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_ID" uuid;
        
        RAISE NOTICE '已添加 TEMPLATE_ID 字段';
    ELSE
        RAISE NOTICE 'TEMPLATE_ID 字段已存在';
    END IF;
END $$;

-- =====================================================
-- 4. 数据迁移：为现有记录生成 TEMPLATE_ID
-- =====================================================

-- 为现有记录生成 TEMPLATE_ID
-- 策略：相同 TemplateName 的记录使用相同的 TEMPLATE_ID
DO $$
DECLARE
    template_record RECORD;
    template_id uuid;
BEGIN
    -- 为每个唯一的 TemplateName 生成一个 TEMPLATE_ID
    FOR template_record IN 
        SELECT DISTINCT "TEMPLATE_NAME" 
        FROM "APPATTACH_CATALOGUE_TEMPLATES" 
        WHERE "TEMPLATE_ID" IS NULL
    LOOP
        -- 生成新的 UUID
        template_id := gen_random_uuid();
        
        -- 更新所有具有相同 TemplateName 的记录
        UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
        SET "TEMPLATE_ID" = template_id
        WHERE "TEMPLATE_NAME" = template_record."TEMPLATE_NAME"
        AND "TEMPLATE_ID" IS NULL;
        
        RAISE NOTICE '为模板名称 "%" 生成了 TEMPLATE_ID: %', 
            template_record."TEMPLATE_NAME", template_id;
    END LOOP;
END $$;

-- 验证数据迁移结果
DO $$
DECLARE
    null_count integer;
    total_count integer;
BEGIN
    SELECT COUNT(*) INTO null_count 
    FROM "APPATTACH_CATALOGUE_TEMPLATES" 
    WHERE "TEMPLATE_ID" IS NULL;
    
    SELECT COUNT(*) INTO total_count 
    FROM "APPATTACH_CATALOGUE_TEMPLATES";
    
    IF null_count > 0 THEN
        RAISE EXCEPTION '数据迁移失败：仍有 % 条记录的 TEMPLATE_ID 为空', null_count;
    ELSE
        RAISE NOTICE '数据迁移成功：共处理 % 条记录', total_count;
    END IF;
END $$;

-- =====================================================
-- 5. 设置字段约束
-- =====================================================

-- 设置 TEMPLATE_ID 为 NOT NULL
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ALTER COLUMN "TEMPLATE_ID" SET NOT NULL;

-- =====================================================
-- 6. 创建新的复合主键
-- =====================================================

-- 创建复合主键 (TEMPLATE_ID, VERSION)
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD CONSTRAINT "PK_ATTACH_CATALOGUE_TEMPLATES" 
PRIMARY KEY ("TEMPLATE_ID", "VERSION");

-- =====================================================
-- 7. 创建新的索引
-- =====================================================

-- 创建模板名称唯一性约束（同一模板名称只能有一个最新版本）
CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "IS_LATEST") 
WHERE "IS_DELETED" = false AND "IS_LATEST" = true;

-- 创建模板ID和最新版本索引
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_ID_LATEST" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_ID", "IS_LATEST") 
WHERE "IS_DELETED" = false AND "IS_LATEST" = true;

-- 创建模板ID索引（用于查询所有版本）
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_ID" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_ID") 
WHERE "IS_DELETED" = false;

-- 创建版本号索引
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_VERSION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("VERSION") 
WHERE "IS_DELETED" = false;

-- =====================================================
-- 8. 重新创建外键约束
-- =====================================================

-- 重新创建自引用外键约束（PARENT_ID 引用 TEMPLATE_ID）
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD CONSTRAINT "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT" 
FOREIGN KEY ("PARENT_ID") 
REFERENCES "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_ID") 
ON DELETE CASCADE;

-- =====================================================
-- 9. 数据完整性验证
-- =====================================================

-- 验证复合主键唯一性
DO $$
DECLARE
    duplicate_count integer;
BEGIN
    SELECT COUNT(*) INTO duplicate_count
    FROM (
        SELECT "TEMPLATE_ID", "VERSION", COUNT(*)
        FROM "APPATTACH_CATALOGUE_TEMPLATES"
        GROUP BY "TEMPLATE_ID", "VERSION"
        HAVING COUNT(*) > 1
    ) duplicates;
    
    IF duplicate_count > 0 THEN
        RAISE EXCEPTION '数据完整性验证失败：发现 % 个重复的复合主键', duplicate_count;
    ELSE
        RAISE NOTICE '复合主键唯一性验证通过';
    END IF;
END $$;

-- 验证模板名称唯一性（最新版本）
DO $$
DECLARE
    duplicate_latest_count integer;
BEGIN
    SELECT COUNT(*) INTO duplicate_latest_count
    FROM (
        SELECT "TEMPLATE_NAME", COUNT(*)
        FROM "APPATTACH_CATALOGUE_TEMPLATES"
        WHERE "IS_LATEST" = true AND "IS_DELETED" = false
        GROUP BY "TEMPLATE_NAME"
        HAVING COUNT(*) > 1
    ) duplicates;
    
    IF duplicate_latest_count > 0 THEN
        RAISE EXCEPTION '数据完整性验证失败：发现 % 个重复的最新版本模板名称', duplicate_latest_count;
    ELSE
        RAISE NOTICE '模板名称唯一性验证通过';
    END IF;
END $$;

-- 验证外键约束
DO $$
DECLARE
    orphan_count integer;
BEGIN
    SELECT COUNT(*) INTO orphan_count
    FROM "APPATTACH_CATALOGUE_TEMPLATES" t1
    WHERE t1."PARENT_ID" IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM "APPATTACH_CATALOGUE_TEMPLATES" t2
        WHERE t2."TEMPLATE_ID" = t1."PARENT_ID"
    );
    
    IF orphan_count > 0 THEN
        RAISE EXCEPTION '外键约束验证失败：发现 % 个孤立的父模板引用', orphan_count;
    ELSE
        RAISE NOTICE '外键约束验证通过';
    END IF;
END $$;

-- =====================================================
-- 10. 性能优化索引
-- =====================================================

-- 创建复合索引以优化常见查询
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_ID_VERSION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_ID", "VERSION") 
WHERE "IS_DELETED" = false;

-- 创建模板名称和版本复合索引
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "VERSION") 
WHERE "IS_DELETED" = false;

-- 创建创建时间索引（用于排序）
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_CREATION_TIME" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("CREATION_TIME" DESC) 
WHERE "IS_DELETED" = false;

-- 创建修改时间索引（用于排序）
CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_LAST_MODIFICATION_TIME" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("LAST_MODIFICATION_TIME" DESC) 
WHERE "IS_DELETED" = false;

-- =====================================================
-- 11. 统计信息更新
-- =====================================================

-- 更新表统计信息
ANALYZE "APPATTACH_CATALOGUE_TEMPLATES";

-- =====================================================
-- 12. 迁移完成报告
-- =====================================================

DO $$
DECLARE
    total_templates integer;
    unique_template_names integer;
    total_versions integer;
    latest_versions integer;
BEGIN
    -- 统计信息
    SELECT COUNT(*) INTO total_templates FROM "APPATTACH_CATALOGUE_TEMPLATES";
    SELECT COUNT(DISTINCT "TEMPLATE_NAME") INTO unique_template_names FROM "APPATTACH_CATALOGUE_TEMPLATES";
    SELECT COUNT(DISTINCT "TEMPLATE_ID") INTO total_versions FROM "APPATTACH_CATALOGUE_TEMPLATES";
    SELECT COUNT(*) INTO latest_versions FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "IS_LATEST" = true;
    
    RAISE NOTICE '=====================================================';
    RAISE NOTICE 'AttachCatalogueTemplate 复合主键迁移完成';
    RAISE NOTICE '=====================================================';
    RAISE NOTICE '总记录数: %', total_templates;
    RAISE NOTICE '唯一模板名称数: %', unique_template_names;
    RAISE NOTICE '唯一模板ID数: %', total_versions;
    RAISE NOTICE '最新版本数: %', latest_versions;
    RAISE NOTICE '=====================================================';
    RAISE NOTICE '迁移成功！现在可以使用复合主键 (TEMPLATE_ID, VERSION)';
    RAISE NOTICE '同一模板的不同版本共享相同的 TEMPLATE_ID';
    RAISE NOTICE '可以通过修改模板名称而不影响版本管理';
    RAISE NOTICE '=====================================================';
END $$;

-- 提交事务
COMMIT;

-- =====================================================
-- 13. 清理脚本（可选）
-- =====================================================

-- 如果需要回滚，可以执行以下脚本：
/*
-- 回滚脚本（谨慎使用）
BEGIN;

-- 删除新创建的索引
DROP INDEX IF EXISTS "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_ID_LATEST";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_ID";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_VERSION";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_ID_VERSION";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_CREATION_TIME";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_LAST_MODIFICATION_TIME";

-- 删除外键约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP CONSTRAINT IF EXISTS "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT";

-- 删除复合主键
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP CONSTRAINT IF EXISTS "PK_ATTACH_CATALOGUE_TEMPLATES";

-- 删除 TEMPLATE_ID 字段
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" DROP COLUMN IF EXISTS "TEMPLATE_ID";

-- 恢复原始主键
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" ADD CONSTRAINT "PK_ATTACH_CATALOGUE_TEMPLATES" PRIMARY KEY ("ID");

-- 恢复原始索引
CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "VERSION") 
WHERE "IS_DELETED" = false;

CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "IS_LATEST") 
WHERE "IS_DELETED" = false AND "IS_LATEST" = true;

-- 恢复外键约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD CONSTRAINT "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT" 
FOREIGN KEY ("PARENT_ID") 
REFERENCES "APPATTACH_CATALOGUE_TEMPLATES" ("ID") 
ON DELETE CASCADE;

COMMIT;
*/
