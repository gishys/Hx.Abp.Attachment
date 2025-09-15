-- =====================================================
-- 为附件分类表添加模板角色字段
-- 文件: add-template-role-to-attach-catalogues.sql
-- 描述: 为 AttachCatalogue 实体添加 TemplateRole 字段
-- 作者: 系统自动生成
-- 创建时间: 2025-09-15
-- =====================================================

-- 1. 添加模板角色字段
DO $$
BEGIN
    -- 添加模板角色字段（如果不存在）
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'TEMPLATE_ROLE'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "TEMPLATE_ROLE" integer NOT NULL DEFAULT 3; -- 默认为 Branch
        
        -- 添加字段注释
        COMMENT ON COLUMN "APPATTACH_CATALOGUES"."TEMPLATE_ROLE" IS '分类角色（1=Root, 2=Navigation, 3=Branch, 4=Leaf）';
        
        RAISE NOTICE '已添加模板角色字段';
    ELSE
        RAISE NOTICE '模板角色字段已存在，跳过';
    END IF;
END $$;

-- 2. 添加模板角色约束
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.check_constraints 
        WHERE constraint_name = 'CK_ATTACH_CATALOGUES_TEMPLATE_ROLE'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES"
        ADD CONSTRAINT "CK_ATTACH_CATALOGUES_TEMPLATE_ROLE"
        CHECK ("TEMPLATE_ROLE" IN (1, 2, 3, 4));
        
        RAISE NOTICE '已添加模板角色约束';
    ELSE
        RAISE NOTICE '模板角色约束已存在，跳过';
    END IF;
END $$;

-- 3. 创建模板角色字段的btree索引（用于查询优化）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_TEMPLATE_ROLE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_TEMPLATE_ROLE" 
        ON "APPATTACH_CATALOGUES" ("TEMPLATE_ROLE") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建模板角色索引';
    ELSE
        RAISE NOTICE '模板角色索引已存在，跳过';
    END IF;
END $$;

-- 4. 创建复合索引（模板角色 + 其他常用字段）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_ROLE_REF_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_ROLE_REF_TYPE" 
        ON "APPATTACH_CATALOGUES" ("TEMPLATE_ROLE", "REFERENCE", "REFERENCE_TYPE") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建模板角色复合索引';
    ELSE
        RAISE NOTICE '模板角色复合索引已存在，跳过';
    END IF;
END $$;

-- 5. 创建模板角色 + 父级ID的复合索引（用于树状查询）
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUES_ROLE_PARENT') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_ROLE_PARENT" 
        ON "APPATTACH_CATALOGUES" ("TEMPLATE_ROLE", "PARENT_ID") 
        WHERE "IS_DELETED" = false;
        
        RAISE NOTICE '已创建模板角色父级索引';
    ELSE
        RAISE NOTICE '模板角色父级索引已存在，跳过';
    END IF;
END $$;

-- 6. 更新现有数据的模板角色（基于业务逻辑）
DO $$
DECLARE
    updated_count INTEGER := 0;
BEGIN
    -- 更新根分类（没有父级的分类）为Root角色
    UPDATE "APPATTACH_CATALOGUES" 
    SET "TEMPLATE_ROLE" = 1 -- Root
    WHERE "PARENT_ID" IS NULL 
      AND "IS_DELETED" = false
      AND "TEMPLATE_ROLE" = 3; -- 只更新默认的Branch值
    
    GET DIAGNOSTICS updated_count = ROW_COUNT;
    RAISE NOTICE '已将 % 个根分类更新为Root角色', updated_count;
    
    -- 更新有子分类的分类为Branch角色（保持默认值）
    -- 这里不需要额外操作，因为默认值已经是Branch(3)
    
    -- 更新没有子分类且没有附件的分类为Leaf角色
    UPDATE "APPATTACH_CATALOGUES" 
    SET "TEMPLATE_ROLE" = 4 -- Leaf
    WHERE "IS_DELETED" = false
      AND "TEMPLATE_ROLE" = 3 -- 只更新默认的Branch值
      AND "PARENT_ID" IS NOT NULL -- 不是根分类
      AND NOT EXISTS (
          SELECT 1 FROM "APPATTACH_CATALOGUES" child 
          WHERE child."PARENT_ID" = "APPATTACH_CATALOGUES"."Id" 
            AND child."IS_DELETED" = false
      )
      AND "ATTACH_COUNT" = 0; -- 没有附件
    
    GET DIAGNOSTICS updated_count = ROW_COUNT;
    RAISE NOTICE '已将 % 个叶子分类更新为Leaf角色', updated_count;
    
    RAISE NOTICE '模板角色字段迁移完成';
END $$;
