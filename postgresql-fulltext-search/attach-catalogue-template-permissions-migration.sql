-- =====================================================
-- AttachCatalogueTemplate 权限字段迁移脚本
-- 为 APPATTACH_CATALOGUE_TEMPLATES 表添加 PERMISSIONS 字段
-- =====================================================

-- 检查表是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES') THEN
        RAISE EXCEPTION '表 APPATTACH_CATALOGUE_TEMPLATES 不存在';
    END IF;
END $$;

-- =====================================================
-- 第一步：添加 PERMISSIONS 字段
-- =====================================================

-- 检查字段是否已存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns 
                   WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
                   AND column_name = 'PERMISSIONS') THEN
        
        -- 添加 PERMISSIONS 字段
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "PERMISSIONS" jsonb;
        
        RAISE NOTICE '已添加 PERMISSIONS 字段';
    ELSE
        RAISE NOTICE 'PERMISSIONS 字段已存在';
    END IF;
END $$;

-- =====================================================
-- 第二步：数据清理和标准化
-- =====================================================

-- 更新 NULL 的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "PERMISSIONS" = '[]'::jsonb 
WHERE "PERMISSIONS" IS NULL;

-- 更新空字符串的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "PERMISSIONS" = '[]'::jsonb 
WHERE "PERMISSIONS" = '""'::jsonb;

-- 更新无效 JSON 的 PERMISSIONS 字段为空数组
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "PERMISSIONS" = '[]'::jsonb 
WHERE "PERMISSIONS" = 'null'::jsonb;

-- 显示更新结果
SELECT 
    COUNT(*) as total_records,
    COUNT("PERMISSIONS") as non_null_permissions,
    COUNT(CASE WHEN "PERMISSIONS" = '[]'::jsonb THEN 1 END) as empty_array_permissions,
    COUNT(CASE WHEN "PERMISSIONS" IS NULL THEN 1 END) as null_permissions
FROM "APPATTACH_CATALOGUE_TEMPLATES";

-- =====================================================
-- 第三步：设置字段约束和默认值
-- =====================================================

-- 设置 PERMISSIONS 字段的默认值
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ALTER COLUMN "PERMISSIONS" SET DEFAULT '[]'::jsonb;

-- 添加 NOT NULL 约束（确保数据一致性）
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ALTER COLUMN "PERMISSIONS" SET NOT NULL;

-- 添加 JSONB 格式验证约束（确保是数组格式）
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_FORMAT" 
CHECK (jsonb_typeof("PERMISSIONS") = 'array');

-- =====================================================
-- 第四步：创建索引
-- =====================================================

-- 创建权限集合GIN索引
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IX_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_GIN') THEN
        CREATE INDEX "IX_ATTACH_CATALOGUE_TEMPLATES_PERMISSIONS_GIN" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("PERMISSIONS" jsonb_path_ops);
        
        RAISE NOTICE '已创建权限集合GIN索引';
    ELSE
        RAISE NOTICE '权限集合GIN索引已存在';
    END IF;
END $$;

-- =====================================================
-- 第五步：验证结果
-- =====================================================

-- 显示最终结果
RAISE NOTICE '=====================================================';
RAISE NOTICE 'AttachCatalogueTemplate 权限字段迁移完成！';
RAISE NOTICE '已创建以下功能：';
RAISE NOTICE '1. 权限字段数据清理和标准化';
RAISE NOTICE '2. 权限字段格式验证约束（数组格式）';
RAISE NOTICE '3. 默认值设置为空数组';
RAISE NOTICE '4. NOT NULL 约束确保数据一致性';
RAISE NOTICE '5. GIN 索引支持高效查询';
RAISE NOTICE '=====================================================';

-- 显示表结构
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default,
    character_maximum_length
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND column_name = 'PERMISSIONS';
