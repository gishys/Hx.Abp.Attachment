-- =====================================================
-- AttachCatalogueTemplate 完整迁移脚本
-- 包含表创建、字段添加、索引创建、约束添加和示例数据
-- =====================================================

-- 设置事务隔离级别
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

-- 开始事务
BEGIN;

-- =====================================================
-- 1. 创建表（如果不存在）
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.tables WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES') THEN
        CREATE TABLE "APPATTACH_CATALOGUE_TEMPLATES" (
            "ID" uuid NOT NULL,
            "TEMPLATE_NAME" character varying(256) NOT NULL,
            "VERSION" integer NOT NULL DEFAULT 1,
            "IS_LATEST" boolean NOT NULL DEFAULT true,
            "ATTACH_RECEIVE_TYPE" integer NOT NULL,
            "NAME_PATTERN" character varying(512),
            "RULE_EXPRESSION" text,
            "SEMANTIC_MODEL" character varying(128),
            "IS_REQUIRED" boolean NOT NULL DEFAULT false,
            "SEQUENCE_NUMBER" integer NOT NULL DEFAULT 0,
            "IS_STATIC" boolean NOT NULL DEFAULT false,
            "PARENT_ID" uuid,
            "TEMPLATE_TYPE" integer NOT NULL DEFAULT 99,
            "TEMPLATE_PURPOSE" integer NOT NULL DEFAULT 1,
            "TEXT_VECTOR" double precision[],
            "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0,
            "EXTRA_PROPERTIES" text,
            "CONCURRENCY_STAMP" character varying(40),
            "CREATION_TIME" timestamp without time zone NOT NULL,
            "CREATOR_ID" uuid,
            "LAST_MODIFICATION_TIME" timestamp without time zone,
            "LAST_MODIFIER_ID" uuid,
            "IS_DELETED" boolean NOT NULL DEFAULT false,
            "DELETER_ID" uuid,
            "DELETION_TIME" timestamp without time zone,
            CONSTRAINT "PK_ATTACH_CATALOGUE_TEMPLATES" PRIMARY KEY ("ID")
        );
        
        RAISE NOTICE '已创建表 APPATTACH_CATALOGUE_TEMPLATES';
    ELSE
        RAISE NOTICE '表 APPATTACH_CATALOGUE_TEMPLATES 已存在';
    END IF;
END $$;

-- =====================================================
-- 2. 添加新字段（如果不存在）
-- =====================================================
DO $$
BEGIN
    -- 添加 TEMPLATE_TYPE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_TYPE" integer NOT NULL DEFAULT 99;
        RAISE NOTICE '已添加 TEMPLATE_TYPE 字段';
    END IF;

    -- 添加 TEMPLATE_PURPOSE 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEMPLATE_PURPOSE" integer NOT NULL DEFAULT 1;
        RAISE NOTICE '已添加 TEMPLATE_PURPOSE 字段';
    END IF;

    -- 添加 TEXT_VECTOR 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'TEXT_VECTOR') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "TEXT_VECTOR" double precision[];
        RAISE NOTICE '已添加 TEXT_VECTOR 字段';
    END IF;

    -- 添加 VECTOR_DIMENSION 字段
    IF NOT EXISTS (SELECT FROM information_schema.columns WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' AND column_name = 'VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD COLUMN "VECTOR_DIMENSION" integer NOT NULL DEFAULT 0;
        RAISE NOTICE '已添加 VECTOR_DIMENSION 字段';
    END IF;
END $$;

-- =====================================================
-- 3. 创建索引
-- =====================================================
DO $$
BEGIN
    -- 基础索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'UK_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION') THEN
        CREATE UNIQUE INDEX "UK_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "VERSION") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建名称版本唯一索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_NAME", "IS_LATEST") 
        WHERE "IS_DELETED" = false AND "IS_LATEST" = true;
        RAISE NOTICE '已创建名称最新版本索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PARENT_ID') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PARENT_ID" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("PARENT_ID");
        RAISE NOTICE '已创建父ID索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_SEQUENCE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_SEQUENCE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("SEQUENCE_NUMBER");
        RAISE NOTICE '已创建顺序号索引';
    END IF;

    -- 新增字段索引
    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_TYPE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建模板类型索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建模板用途索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("TEMPLATE_TYPE", "TEMPLATE_PURPOSE") 
        WHERE "IS_DELETED" = false;
        RAISE NOTICE '已创建复合标识索引';
    END IF;

    IF NOT EXISTS (SELECT FROM pg_indexes WHERE indexname = 'IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM') THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM" 
        ON "APPATTACH_CATALOGUE_TEMPLATES" ("VECTOR_DIMENSION") 
        WHERE "IS_DELETED" = false AND "VECTOR_DIMENSION" > 0;
        RAISE NOTICE '已创建向量维度索引';
    END IF;
END $$;

-- =====================================================
-- 4. 添加约束
-- =====================================================
DO $$
BEGIN
    -- 向量维度约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION" 
        CHECK ("VECTOR_DIMENSION" >= 0 AND "VECTOR_DIMENSION" <= 2048);
        RAISE NOTICE '已添加向量维度约束';
    END IF;

    -- 模板类型约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_TYPE" 
        CHECK ("TEMPLATE_TYPE" IN (1, 2, 3, 4, 99));
        RAISE NOTICE '已添加模板类型约束';
    END IF;

    -- 模板用途约束
    IF NOT EXISTS (SELECT FROM information_schema.check_constraints WHERE constraint_name = 'CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE" 
        CHECK ("TEMPLATE_PURPOSE" IN (1, 2, 3, 4, 99));
        RAISE NOTICE '已添加模板用途约束';
    END IF;
END $$;

-- =====================================================
-- 5. 添加外键约束
-- =====================================================
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM information_schema.table_constraints WHERE constraint_name = 'FK_ATTACH_CATALOGUE_TEMPLATES_PARENT') THEN
        ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
        ADD CONSTRAINT "FK_ATTACH_CATALOGUE_TEMPLATES_PARENT" 
        FOREIGN KEY ("PARENT_ID") REFERENCES "APPATTACH_CATALOGUE_TEMPLATES" ("ID") 
        ON DELETE CASCADE;
        RAISE NOTICE '已添加父模板外键约束';
    END IF;
END $$;

-- =====================================================
-- 6. 添加字段注释
-- =====================================================
DO $$
BEGIN
    -- 表注释
    COMMENT ON TABLE "APPATTACH_CATALOGUE_TEMPLATES" IS '附件分类模板表';
    
    -- 字段注释
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_NAME" IS '模板名称';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."VERSION" IS '模板版本号';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."IS_LATEST" IS '是否为最新版本';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."ATTACH_RECEIVE_TYPE" IS '附件类型';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."NAME_PATTERN" IS '分类名称规则';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."RULE_EXPRESSION" IS '规则引擎表达式';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."SEMANTIC_MODEL" IS 'AI语义匹配模型名称';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."IS_REQUIRED" IS '是否必收';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."SEQUENCE_NUMBER" IS '顺序号';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."IS_STATIC" IS '是否静态';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."PARENT_ID" IS '父模板Id';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_TYPE" IS '模板类型：1=项目级,2=阶段级,3=业务分类,4=专业领域,99=通用';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEMPLATE_PURPOSE" IS '模板用途：1=分类管理,2=文档管理,3=流程管理,4=权限管理,99=其他';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."TEXT_VECTOR" IS '文本向量（64-2048维）';
    COMMENT ON COLUMN "APPATTACH_CATALOGUE_TEMPLATES"."VECTOR_DIMENSION" IS '向量维度';
    
    RAISE NOTICE '已添加字段注释';
END $$;

-- =====================================================
-- 7. 插入示例数据（如果表为空）
-- =====================================================
DO $$
DECLARE
    record_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO record_count FROM "APPATTACH_CATALOGUE_TEMPLATES";
    
    IF record_count = 0 THEN
        -- 插入项目级模板示例
        INSERT INTO "APPATTACH_CATALOGUE_TEMPLATES" (
            "ID", "TEMPLATE_NAME", "VERSION", "IS_LATEST", "ATTACH_RECEIVE_TYPE", 
            "NAME_PATTERN", "RULE_EXPRESSION", "SEMANTIC_MODEL", "IS_REQUIRED", 
            "SEQUENCE_NUMBER", "IS_STATIC", "PARENT_ID", "TEMPLATE_TYPE", 
            "TEMPLATE_PURPOSE", "TEXT_VECTOR", "VECTOR_DIMENSION", "CREATION_TIME"
        ) VALUES (
            gen_random_uuid(), '项目级模板-建筑工程', 1, true, 0, 
            '建筑工程_{DateTime:yyyyMMdd}', 
            '{"WorkflowName": "ProjectWorkflow", "Rules": [{"RuleName": "ProjectRule", "Expression": "TemplateType == 1"}]}', 
            'project_construction_model', false, 1, true, null, 1, 1, '{}', 0, NOW()
        );

        -- 插入阶段级模板示例
        INSERT INTO "APPATTACH_CATALOGUE_TEMPLATES" (
            "ID", "TEMPLATE_NAME", "VERSION", "IS_LATEST", "ATTACH_RECEIVE_TYPE", 
            "NAME_PATTERN", "RULE_EXPRESSION", "SEMANTIC_MODEL", "IS_REQUIRED", 
            "SEQUENCE_NUMBER", "IS_STATIC", "PARENT_ID", "TEMPLATE_TYPE", 
            "TEMPLATE_PURPOSE", "TEXT_VECTOR", "VECTOR_DIMENSION", "CREATION_TIME"
        ) VALUES (
            gen_random_uuid(), '阶段级模板-施工图设计', 1, true, 0, 
            '施工图设计_{DateTime:yyyyMMdd}', 
            '{"WorkflowName": "PhaseWorkflow", "Rules": [{"RuleName": "PhaseRule", "Expression": "TemplateType == 2"}]}', 
            'phase_design_model', false, 2, true, null, 2, 1, '{}', 0, NOW()
        );

        -- 插入业务分类模板示例
        INSERT INTO "APPATTACH_CATALOGUE_TEMPLATES" (
            "ID", "TEMPLATE_NAME", "VERSION", "IS_LATEST", "ATTACH_RECEIVE_TYPE", 
            "NAME_PATTERN", "RULE_EXPRESSION", "SEMANTIC_MODEL", "IS_REQUIRED", 
            "SEQUENCE_NUMBER", "IS_STATIC", "PARENT_ID", "TEMPLATE_TYPE", 
            "TEMPLATE_PURPOSE", "TEXT_VECTOR", "VECTOR_DIMENSION", "CREATION_TIME"
        ) VALUES (
            gen_random_uuid(), '业务分类模板-文档管理', 1, true, 0, 
            '文档管理_{DateTime:yyyyMMdd}', 
            '{"WorkflowName": "BusinessWorkflow", "Rules": [{"RuleName": "BusinessRule", "Expression": "TemplateType == 3"}]}', 
            'business_document_model', false, 3, true, null, 3, 2, '{}', 0, NOW()
        );

        RAISE NOTICE '已插入示例数据，共插入 % 条记录', 3;
    ELSE
        RAISE NOTICE '表中已有数据，跳过示例数据插入';
    END IF;
END $$;

-- =====================================================
-- 8. 更新现有数据的默认值
-- =====================================================
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "TEMPLATE_TYPE" = 99, "TEMPLATE_PURPOSE" = 1, "VECTOR_DIMENSION" = 0, "TEXT_VECTOR" = '{}'
WHERE "TEMPLATE_TYPE" IS NULL OR "TEMPLATE_PURPOSE" IS NULL OR "VECTOR_DIMENSION" IS NULL OR "TEXT_VECTOR" IS NULL;

-- =====================================================
-- 9. 验证迁移结果
-- =====================================================
DO $$
DECLARE
    column_count INTEGER;
    index_count INTEGER;
    constraint_count INTEGER;
    record_count INTEGER;
BEGIN
    -- 验证字段
    SELECT COUNT(*) INTO column_count
    FROM information_schema.columns 
    WHERE table_name = 'APPATTACH_CATALOGUE_TEMPLATES' 
      AND column_name IN ('TEMPLATE_TYPE', 'TEMPLATE_PURPOSE', 'TEXT_VECTOR', 'VECTOR_DIMENSION');
    
    -- 验证索引
    SELECT COUNT(*) INTO index_count
    FROM pg_indexes 
    WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
      AND (indexname LIKE '%TEMPLATE_TYPE%' 
           OR indexname LIKE '%TEMPLATE_PURPOSE%' 
           OR indexname LIKE '%VECTOR_DIM%'
           OR indexname LIKE '%IDENTIFIER%');
    
    -- 验证约束
    SELECT COUNT(*) INTO constraint_count
    FROM information_schema.check_constraints 
    WHERE constraint_name LIKE '%TEMPLATE_TYPE%' 
       OR constraint_name LIKE '%TEMPLATE_PURPOSE%' 
       OR constraint_name LIKE '%VECTOR_DIMENSION%';
    
    -- 验证数据
    SELECT COUNT(*) INTO record_count FROM "APPATTACH_CATALOGUE_TEMPLATES";
    
    RAISE NOTICE '迁移验证结果:';
    RAISE NOTICE '- 新增字段数量: %', column_count;
    RAISE NOTICE '- 新增索引数量: %', index_count;
    RAISE NOTICE '- 新增约束数量: %', constraint_count;
    RAISE NOTICE '- 总记录数量: %', record_count;
    
    IF column_count = 4 AND index_count >= 4 AND constraint_count = 3 THEN
        RAISE NOTICE '✅ 迁移成功完成！';
    ELSE
        RAISE NOTICE '⚠️  迁移可能存在问题，请检查上述结果';
    END IF;
END $$;

-- 提交事务
COMMIT;

-- 显示最终结果
SELECT 
    '迁移完成' as status,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES") as total_records,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "TEMPLATE_TYPE" = 1) as project_templates,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "TEMPLATE_TYPE" = 2) as phase_templates,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "TEMPLATE_TYPE" = 3) as business_templates,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "TEMPLATE_TYPE" = 4) as professional_templates,
    (SELECT COUNT(*) FROM "APPATTACH_CATALOGUE_TEMPLATES" WHERE "TEMPLATE_TYPE" = 99) as general_templates;
