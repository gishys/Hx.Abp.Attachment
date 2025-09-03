-- =====================================================
-- FacetType 字段重命名迁移脚本（分步执行版本）
-- 将 TEMPLATE_TYPE 字段重命名为 FACET_TYPE
-- 更新约束、索引和默认值
-- =====================================================

-- 注意：请按顺序执行每个步骤，每执行一步后检查结果
-- 如果某一步出错，请停止并联系管理员

-- =====================================================
-- 步骤 1: 删除旧约束
-- =====================================================
-- 执行此步骤后，检查是否成功
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE";

-- 验证约束是否已删除
SELECT constraint_name 
FROM information_schema.table_constraints 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE';

-- =====================================================
-- 步骤 2: 重命名字段
-- =====================================================
-- 执行此步骤后，检查是否成功
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    RENAME COLUMN "TEMPLATE_TYPE" TO "FACET_TYPE";

-- 验证字段是否已重命名
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND column_name = 'FACET_TYPE';

-- =====================================================
-- 步骤 3: 更新字段注释
-- =====================================================
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."FACET_TYPE" IS '分面类型 - 标识模板的层级和用途';

-- =====================================================
-- 步骤 4: 更新数据值
-- =====================================================
-- 将原有的默认值 99 (General) 更新为 0 (General)
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "FACET_TYPE" = 0 
WHERE "FACET_TYPE" = 99;

-- 验证数据更新是否成功
SELECT "FACET_TYPE", COUNT(*) 
FROM "APPATTACH_CATALOGUE_TEMPLATES" 
GROUP BY "FACET_TYPE";

-- =====================================================
-- 步骤 5: 添加新约束
-- =====================================================
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE"
    CHECK ("FACET_TYPE" IN (0, 1, 2, 3, 4, 5, 6, 99));

-- 验证约束是否已添加
SELECT constraint_name, check_clause
FROM information_schema.check_constraints 
WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE';

-- =====================================================
-- 步骤 6: 删除旧索引
-- =====================================================
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE";

-- 验证旧索引是否已删除
SELECT indexname 
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND indexname IN ('IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE', 'IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE');

-- =====================================================
-- 步骤 7: 创建新索引（在事务外执行）
-- =====================================================
-- 创建单字段索引
CREATE INDEX CONCURRENTLY "IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE")
    WHERE "IS_DELETED" = false;

-- 验证单字段索引是否已创建
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE';

-- 创建复合索引
CREATE INDEX CONCURRENTLY "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE", "TEMPLATE_PURPOSE")
    WHERE "IS_DELETED" = false;

-- 验证复合索引是否已创建
SELECT indexname, indexdef 
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
AND indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE';

-- =====================================================
-- 步骤 8: 更新 AttachCatalogue 表（如果存在）
-- =====================================================
-- 检查表是否存在
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUES') THEN
        RAISE NOTICE 'APPATTACH_CATALOGUES 表存在，开始更新...';
    ELSE
        RAISE NOTICE 'APPATTACH_CATALOGUES 表不存在，跳过更新';
        RETURN;
    END IF;
END $$;

-- 如果表存在，执行以下步骤：

-- 8.1 删除旧约束
ALTER TABLE "APPATTACH_CATALOGUES" 
    DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUES_CATALOGUE_TYPE";

-- 8.2 重命名字段
ALTER TABLE "APPATTACH_CATALOGUES" 
    RENAME COLUMN "CATALOGUE_TYPE" TO "CATALOGUE_FACET_TYPE";

-- 8.3 更新注释
COMMENT ON COLUMN "APPATTACH_CATALOGUES"."CATALOGUE_FACET_TYPE" IS '分类分面类型 - 标识分类的层级和用途';

-- 8.4 更新数据值
UPDATE "APPATTACH_CATALOGUES" 
SET "CATALOGUE_FACET_TYPE" = 0 
WHERE "CATALOGUE_FACET_TYPE" = 99;

-- 8.5 添加新约束
ALTER TABLE "APPATTACH_CATALOGUES" 
    ADD CONSTRAINT "CK_ATTACH_CATALOGUES_CATALOGUE_FACET_TYPE"
    CHECK ("CATALOGUE_FACET_TYPE" IN (0, 1, 2, 3, 4, 5, 6, 99));

-- =====================================================
-- 步骤 9: 最终验证
-- =====================================================
-- 检查主要表字段
SELECT 
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name IN ('APPATTACH_CATALOGUE_TEMPLATES', 'APPATTACH_CATALOGUES')
AND column_name LIKE '%FACET_TYPE%';

-- 检查约束
SELECT 
    table_name,
    constraint_name,
    constraint_type
FROM information_schema.table_constraints 
WHERE table_name IN ('APPATTACH_CATALOGUE_TEMPLATES', 'APPATTACH_CATALOGUES')
AND constraint_name LIKE '%FACET_TYPE%';

-- 检查索引
SELECT 
    tablename,
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename IN ('APPATTACH_CATALOGUE_TEMPLATES', 'APPATTACH_CATALOGUE_TEMPLATES')
AND indexname LIKE '%FACET_TYPE%';

-- =====================================================
-- 迁移完成提示
-- =====================================================
SELECT 'FacetType 字段重命名迁移完成！' AS status;

-- =====================================================
-- 注意事项
-- =====================================================
/*
1. 请按顺序执行每个步骤
2. 每执行一步后检查结果
3. 如果某一步出错，请停止并联系管理员
4. 迁移完成后，请测试相关功能
5. 更新应用程序代码中的字段引用

枚举值映射关系：
- 0: General (通用分面)
- 1: Organization (组织维度)
- 2: ProjectType (项目类型)
- 3: Phase (阶段分面)
- 4: Discipline (专业领域)
- 5: DocumentType (文档类型)
- 6: TimeSlice (时间切片)
- 99: Custom (业务自定义)
*/
