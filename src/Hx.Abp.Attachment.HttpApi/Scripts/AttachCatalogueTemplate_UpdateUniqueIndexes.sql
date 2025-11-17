-- =============================================
-- AttachCatalogueTemplate 表唯一索引更新脚本
-- 描述: 更新模板名称唯一性约束，从全局唯一改为按父节点分组唯一
-- 修改时间: 2024
-- =============================================

-- 说明：
-- 1. 删除旧的全局唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST
-- 2. 创建根节点唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST_ROOT
-- 3. 创建子节点唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_PARENT_LATEST
--
-- 业务规则：
-- - 根节点（ParentId IS NULL）：模板名称在根节点下唯一
-- - 子节点（ParentId IS NOT NULL）：模板名称在同一父节点下唯一
-- - 不同父节点下可以有相同的模板名称

-- =============================================
-- 注意：请根据实际情况修改表名
-- 表名 = DbTablePrefix + "ATTACH_CATALOGUE_TEMPLATES"
-- 如果 DbTablePrefix = "APP"，则表名为 "APPATTACH_CATALOGUE_TEMPLATES"
-- =============================================

DO $$
DECLARE
    table_name_var TEXT := 'APPATTACH_CATALOGUE_TEMPLATES'; -- 请根据实际情况修改表名
BEGIN
    -- 检查表是否存在
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.tables 
        WHERE table_schema = 'public' 
        AND table_name = table_name_var
    ) THEN
        RAISE EXCEPTION '表 % 不存在，请检查表名是否正确', table_name_var;
    END IF;

    -- =============================================
    -- 步骤1：删除旧的唯一性约束（如果存在）
    -- =============================================
    IF EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = table_name_var
        AND indexname = 'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST'
    ) THEN
        EXECUTE format('DROP INDEX IF EXISTS "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST"');
        RAISE NOTICE '已删除旧的唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST';
    ELSE
        RAISE NOTICE '旧的唯一性约束不存在，跳过删除步骤';
    END IF;

    -- =============================================
    -- 步骤2：创建根节点唯一性约束
    -- =============================================
    -- 根节点（ParentId IS NULL）：TemplateName + IsLatest 唯一
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = table_name_var
        AND indexname = 'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST_ROOT'
    ) THEN
        EXECUTE format('CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST_ROOT"
            ON %I ("TEMPLATE_NAME", "IS_LATEST")
            WHERE "IS_DELETED" = false 
            AND "IS_LATEST" = true 
            AND "PARENT_ID" IS NULL', table_name_var);
        
        RAISE NOTICE '已创建根节点唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST_ROOT';
    ELSE
        RAISE NOTICE '根节点唯一性约束已存在，跳过创建步骤';
    END IF;

    -- =============================================
    -- 步骤3：创建子节点唯一性约束
    -- =============================================
    -- 子节点（ParentId IS NOT NULL）：TemplateName + ParentId + IsLatest 唯一
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'public' 
        AND tablename = table_name_var
        AND indexname = 'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_PARENT_LATEST'
    ) THEN
        EXECUTE format('CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_PARENT_LATEST"
            ON %I ("TEMPLATE_NAME", "PARENT_ID", "IS_LATEST")
            WHERE "IS_DELETED" = false 
            AND "IS_LATEST" = true 
            AND "PARENT_ID" IS NOT NULL', table_name_var);
        
        RAISE NOTICE '已创建子节点唯一性约束：UK_ATTACH_CATALOGUE_TEMPLATES_NAME_PARENT_LATEST';
    ELSE
        RAISE NOTICE '子节点唯一性约束已存在，跳过创建步骤';
    END IF;

    RAISE NOTICE '索引更新完成！';
END $$;

-- =============================================
-- 验证索引是否创建成功
-- =============================================
-- 注意：请将 'APPATTACH_CATALOGUE_TEMPLATES' 替换为实际的表名
SELECT 
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'public' 
AND tablename = 'APPATTACH_CATALOGUE_TEMPLATES' -- 请根据实际情况修改表名
AND indexname IN (
    'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST_ROOT',
    'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_PARENT_LATEST'
)
ORDER BY indexname;

-- =============================================
-- 注意事项：
-- =============================================
-- 1. 执行此脚本前，请确保数据库中没有违反新唯一性约束的数据
-- 2. 如果存在重复数据，需要先清理数据后再执行此脚本
-- 3. 建议在非生产环境先测试执行
-- 4. 执行前建议备份数据库
--
-- 检查是否存在违反新约束的数据：
-- 
-- -- 检查根节点是否有重复的模板名称
-- SELECT "TEMPLATE_NAME", COUNT(*) as count
-- FROM "APPATTACH_CATALOGUE_TEMPLATES"
-- WHERE "IS_DELETED" = false 
-- AND "IS_LATEST" = true 
-- AND "PARENT_ID" IS NULL
-- GROUP BY "TEMPLATE_NAME"
-- HAVING COUNT(*) > 1;
--
-- -- 检查同一父节点下是否有重复的模板名称
-- SELECT "PARENT_ID", "TEMPLATE_NAME", COUNT(*) as count
-- FROM "APPATTACH_CATALOGUE_TEMPLATES"
-- WHERE "IS_DELETED" = false 
-- AND "IS_LATEST" = true 
-- AND "PARENT_ID" IS NOT NULL
-- GROUP BY "PARENT_ID", "TEMPLATE_NAME"
-- HAVING COUNT(*) > 1;

