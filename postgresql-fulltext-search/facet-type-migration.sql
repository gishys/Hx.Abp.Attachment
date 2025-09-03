-- =====================================================
-- FacetType 字段重命名迁移脚本
-- 将 TEMPLATE_TYPE 字段重命名为 FACET_TYPE
-- 更新约束、索引和默认值
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- 设置错误处理
DO $$
BEGIN
    -- =====================================================
    -- 1. 备份现有数据（可选）
    -- =====================================================
    -- 如果需要备份，可以在这里添加备份逻辑
    RAISE NOTICE '开始执行 FacetType 字段重命名迁移...';

    -- =====================================================
    -- 2. 先删除旧约束（重要：必须在重命名字段前删除）
    -- =====================================================
    -- 删除旧的检查约束
    ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE";
    
    RAISE NOTICE '已删除旧约束 CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE';

    -- =====================================================
    -- 3. 重命名字段
    -- =====================================================
    -- 重命名 TEMPLATE_TYPE 字段为 FACET_TYPE
    ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        RENAME COLUMN "TEMPLATE_TYPE" TO "FACET_TYPE";
    
    RAISE NOTICE '已重命名字段 TEMPLATE_TYPE -> FACET_TYPE';

    -- =====================================================
    -- 4. 更新字段注释
    -- =====================================================
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."FACET_TYPE" IS '分面类型 - 标识模板的层级和用途';
    
    RAISE NOTICE '已更新字段注释';

    -- =====================================================
    -- 5. 更新默认值
    -- =====================================================
    -- 将原有的默认值 99 (General) 更新为 0 (General)
    UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
    SET "FACET_TYPE" = 0 
    WHERE "FACET_TYPE" = 99;
    
    RAISE NOTICE '已更新枚举值映射：99 -> 0';

    -- 更新其他枚举值映射
    -- 1 -> 1 (Organization)
    -- 2 -> 2 (ProjectType) 
    -- 3 -> 3 (Phase)
    -- 4 -> 4 (Discipline)
    -- 99 -> 0 (General)

    -- =====================================================
    -- 6. 添加新约束
    -- =====================================================
    -- 添加新的检查约束
    ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE"
        CHECK ("FACET_TYPE" IN (0, 1, 2, 3, 4, 5, 6, 99));
    
    RAISE NOTICE '已添加新约束 CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE';

    -- =====================================================
    -- 7. 删除旧索引（在事务中删除）
    -- =====================================================
    -- 删除旧索引
    DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE";
    DROP INDEX IF EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE";
    
    RAISE NOTICE '已删除旧索引';

    -- =====================================================
    -- 8. 更新 AttachCatalogue 表相关字段
    -- =====================================================
    -- 检查表是否存在
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUES') THEN
        RAISE NOTICE '开始更新 APPATTACH_CATALOGUE_TEMPLATES 表...';
        
        -- 先删除旧约束
        ALTER TABLE "APPATTACH_CATALOGUES" 
            DROP CONSTRAINT IF EXISTS "CK_ATTACH_CATALOGUES_CATALOGUE_TYPE";
        
        -- 重命名字段
        ALTER TABLE "APPATTACH_CATALOGUES" 
            RENAME COLUMN "CATALOGUE_TYPE" TO "CATALOGUE_FACET_TYPE";
        
        -- 更新注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."CATALOGUE_FACET_TYPE" IS '分类分面类型 - 标识分类的层级和用途';
        
        -- 更新默认值
        UPDATE "APPATTACH_CATALOGUES" 
        SET "CATALOGUE_FACET_TYPE" = 0 
        WHERE "CATALOGUE_FACET_TYPE" = 99;
        
        -- 添加新约束
        ALTER TABLE "APPATTACH_CATALOGUES" 
            ADD CONSTRAINT "CK_ATTACH_CATALOGUES_CATALOGUE_FACET_TYPE"
            CHECK ("CATALOGUE_FACET_TYPE" IN (0, 1, 2, 3, 4, 5, 6, 99));
        
        RAISE NOTICE '已成功更新 APPATTACH_CATALOGUES 表';
    ELSE
        RAISE NOTICE 'APPATTACH_CATALOGUES 表不存在，跳过更新';
    END IF;

    -- =====================================================
    -- 9. 验证迁移结果
    -- =====================================================
    -- 检查字段是否存在
    IF NOT EXISTS (SELECT FROM information_schema.columns 
                   WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
                   AND column_name = 'FACET_TYPE') THEN
        RAISE EXCEPTION '字段重命名失败：FACET_TYPE 字段不存在';
    END IF;
    
    RAISE NOTICE '字段重命名成功：TEMPLATE_TYPE -> FACET_TYPE';
    RAISE NOTICE '迁移执行完成，准备提交事务...';

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION '迁移过程中发生错误：%', SQLERRM;
END $$;

-- =====================================================
-- 10. 提交事务
-- =====================================================
COMMIT;

-- =====================================================
-- 11. 在事务外创建新索引（CREATE INDEX CONCURRENTLY 不能在事务中运行）
-- =====================================================
-- 使用 DO 块来输出信息
DO $$
BEGIN
    RAISE NOTICE '开始创建新索引...';
END $$;

-- 创建单字段索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE")
    WHERE "IS_DELETED" = false;

DO $$
BEGIN
    RAISE NOTICE '已创建单字段索引 IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE';
END $$;

-- 创建复合索引
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE"
    ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE", "TEMPLATE_PURPOSE")
    WHERE "IS_DELETED" = false;

DO $$
BEGIN
    RAISE NOTICE '已创建复合索引 IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE';
END $$;

-- =====================================================
-- 12. 最终验证
-- =====================================================
-- 检查索引是否已创建
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE'
    ) THEN
        RAISE NOTICE '索引创建验证成功';
    ELSE
        RAISE WARNING '索引可能未成功创建';
    END IF;
END $$;

-- =====================================================
-- 迁移完成后的注意事项
-- =====================================================
/*
1. 更新应用程序代码中的字段引用
2. 更新相关的 DTO 和映射配置
3. 更新业务逻辑中的枚举值判断
4. 测试所有相关功能
5. 更新文档和注释

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