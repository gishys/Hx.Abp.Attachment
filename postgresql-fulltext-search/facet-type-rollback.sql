-- =====================================================
-- FacetType 字段重命名回滚脚本
-- 将 FACET_TYPE 字段重命名回 TEMPLATE_TYPE
-- 恢复约束、索引和默认值
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 删除新约束
-- =====================================================
-- 删除新的检查约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE";

-- =====================================================
-- 2. 重命名字段
-- =====================================================
-- 重命名 FACET_TYPE 字段为 TEMPLATE_TYPE
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    RENAME COLUMN "FACET_TYPE" TO "TEMPLATE_TYPE";

-- =====================================================
-- 3. 恢复字段注释
-- =====================================================
COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_TYPE" IS '模板类型 - 标识模板的层级和用途';

-- =====================================================
-- 4. 恢复默认值
-- =====================================================
-- 将 0 (General) 恢复为 99 (General)
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "TEMPLATE_TYPE" = 99 
WHERE "TEMPLATE_TYPE" = 0;

-- =====================================================
-- 5. 恢复旧约束
-- =====================================================
-- 添加旧的检查约束
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
    ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE"
    CHECK ("TEMPLATE_TYPE" IN (1, 2, 3, 4, 99));

-- =====================================================
-- 6. 恢复索引
-- =====================================================
-- 删除新索引
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE";
DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE";

-- 恢复旧索引
CREATE INDEX CONCURRENTLY "IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE")
    WHERE "IS_DELETED" = false;

CREATE INDEX CONCURRENTLY "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE", "TEMPLATE_PURPOSE")
    WHERE "IS_DELETED" = false;

-- =====================================================
-- 7. 恢复 AttachCatalogue 表相关字段
-- =====================================================
-- 检查表是否存在
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUES') THEN
        -- 删除新约束
        ALTER TABLE "APPATTACH_CATALOGUES" 
            DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUES_CATALOGUE_FACET_TYPE";
        
        -- 重命名字段
        ALTER TABLE "APPATTACH_CATALOGUES" 
            RENAME COLUMN "CATALOGUE_FACET_TYPE" TO "CATALOGUE_TYPE";
        
        -- 恢复注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."CATALOGUE_TYPE" IS '分类类型 - 标识分类的层级和用途';
        
        -- 恢复默认值
        UPDATE "APPATTACH_CATALOGUES" 
        SET "CATALOGUE_TYPE" = 99 
        WHERE "CATALOGUE_TYPE" = 0;
        
        -- 恢复旧约束
        ALTER TABLE "APPATTACH_CATALOGUES" 
            ADD CONSTRAINT "CK_ATTACH_CATALOGUES_CATALOGUE_TYPE"
            CHECK ("CATALOGUE_TYPE" IN (1, 2, 3, 4, 99));
    END IF;
END $$;

-- =====================================================
-- 8. 验证回滚结果
-- =====================================================
-- 检查字段是否存在
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.columns 
                   WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
                   AND column_name = 'TEMPLATE_TYPE') THEN
        RAISE EXCEPTION '字段回滚失败：TEMPLATE_TYPE 字段不存在';
    END IF;
    
    RAISE NOTICE '字段回滚成功：FACET_TYPE -> TEMPLATE_TYPE';
END $$;

-- =====================================================
-- 9. 提交事务
-- =====================================================
COMMIT;

-- =====================================================
-- 回滚完成后的注意事项
-- =====================================================
/*
1. 恢复应用程序代码中的字段引用
2. 恢复相关的 DTO 和映射配置
3. 恢复业务逻辑中的枚举值判断
4. 测试所有相关功能
5. 更新文档和注释

枚举值映射关系（恢复为原值）：
- 1: Organization (组织维度)
- 2: ProjectType (项目类型)
- 3: Phase (阶段分面)
- 4: Discipline (专业领域)
- 99: General (通用分面)
*/
