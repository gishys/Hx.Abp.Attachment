-- =============================================
-- MetaFieldPreset 表创建脚本
-- 描述: 预设元数据内容表，用于存储和管理预设的元数据字段集合
-- 创建时间: 2024
-- =============================================

-- 删除表（如果存在，谨慎使用）
-- DROP TABLE IF EXISTS "APPMETA_FIELD_PRESETS" CASCADE;

-- 创建表
CREATE TABLE IF NOT EXISTS "APPMETA_FIELD_PRESETS" (
    -- 主键
    "ID" UUID NOT NULL,
    
    -- 基础字段
    "PRESET_NAME" VARCHAR(256) COLLATE "und-x-icu" NOT NULL,
    "DESCRIPTION" TEXT COLLATE "und-x-icu",
    "TAGS" JSONB DEFAULT '[]'::jsonb,
    "META_FIELDS" JSONB,
    "BUSINESS_SCENARIOS" JSONB DEFAULT '[]'::jsonb,
    "APPLICABLE_FACET_TYPES" JSONB DEFAULT '[]'::jsonb,
    "APPLICABLE_TEMPLATE_PURPOSES" JSONB DEFAULT '[]'::jsonb,
    
    -- 统计和推荐字段
    "USAGE_COUNT" INTEGER NOT NULL DEFAULT 0,
    "RECOMMENDATION_WEIGHT" DOUBLE PRECISION NOT NULL DEFAULT 0.5,
    "LAST_USED_TIME" TIMESTAMP,
    
    -- 状态字段
    "IS_ENABLED" BOOLEAN NOT NULL DEFAULT true,
    "IS_SYSTEM_PRESET" BOOLEAN NOT NULL DEFAULT false,
    "SORT_ORDER" INTEGER NOT NULL DEFAULT 0,
    
    -- 主键约束
    CONSTRAINT "PK_META_FIELD_PRESETS" PRIMARY KEY ("ID")
);

-- 添加表注释
COMMENT ON TABLE "APPMETA_FIELD_PRESETS" IS '预设元数据内容表，用于存储和管理预设的元数据字段集合，支持快速创建分类模板';

-- 添加字段注释
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."ID" IS '预设ID（主键）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."PRESET_NAME" IS '预设名称（唯一）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."DESCRIPTION" IS '预设描述';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."TAGS" IS '预设标签（JSONB数组格式）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."META_FIELDS" IS '元数据字段集合（JSONB数组格式）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."BUSINESS_SCENARIOS" IS '适用业务场景（JSONB数组格式）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."APPLICABLE_FACET_TYPES" IS '适用的分面类型（JSONB数组格式，存储枚举值）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."APPLICABLE_TEMPLATE_PURPOSES" IS '适用的模板用途（JSONB数组格式，存储枚举值）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."USAGE_COUNT" IS '使用次数（用于推荐算法）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."RECOMMENDATION_WEIGHT" IS '推荐权重（0.0-1.0，用于智能推荐）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."LAST_USED_TIME" IS '最后使用时间（用于推荐算法）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."IS_ENABLED" IS '是否启用';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."IS_SYSTEM_PRESET" IS '是否系统预设（系统预设不可删除）';
COMMENT ON COLUMN "APPMETA_FIELD_PRESETS"."SORT_ORDER" IS '排序号（用于展示顺序）';

-- =============================================
-- 索引创建
-- =============================================

-- 唯一索引：预设名称
CREATE UNIQUE INDEX IF NOT EXISTS "UK_META_FIELD_PRESETS_NAME"
    ON "APPMETA_FIELD_PRESETS" ("PRESET_NAME");

-- 普通索引：是否启用
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_ENABLED"
    ON "APPMETA_FIELD_PRESETS" ("IS_ENABLED");

-- 普通索引：使用次数（用于热门预设查询）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_USAGE_COUNT"
    ON "APPMETA_FIELD_PRESETS" ("USAGE_COUNT");

-- 普通索引：推荐权重（用于推荐查询）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_RECOMMENDATION_WEIGHT"
    ON "APPMETA_FIELD_PRESETS" ("RECOMMENDATION_WEIGHT");

-- 普通索引：最后使用时间（用于推荐算法）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_LAST_USED_TIME"
    ON "APPMETA_FIELD_PRESETS" ("LAST_USED_TIME")
    WHERE "LAST_USED_TIME" IS NOT NULL;

-- GIN 索引：标签（用于标签搜索）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_TAGS_GIN"
    ON "APPMETA_FIELD_PRESETS" USING GIN ("TAGS");

-- GIN 索引：业务场景（用于业务场景搜索）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_BUSINESS_SCENARIOS_GIN"
    ON "APPMETA_FIELD_PRESETS" USING GIN ("BUSINESS_SCENARIOS");

-- GIN 索引：适用的分面类型（用于分面类型搜索）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_FACET_TYPES_GIN"
    ON "APPMETA_FIELD_PRESETS" USING GIN ("APPLICABLE_FACET_TYPES");

-- GIN 索引：适用的模板用途（用于模板用途搜索）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_TEMPLATE_PURPOSES_GIN"
    ON "APPMETA_FIELD_PRESETS" USING GIN ("APPLICABLE_TEMPLATE_PURPOSES");

-- GIN 索引：元数据字段（用于元数据字段搜索）
CREATE INDEX IF NOT EXISTS "IDX_META_FIELD_PRESETS_META_FIELDS_GIN"
    ON "APPMETA_FIELD_PRESETS" USING GIN ("META_FIELDS");

-- =============================================
-- 约束创建
-- =============================================

-- 检查约束：推荐权重范围（0.0-1.0）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'CK_META_FIELD_PRESETS_RECOMMENDATION_WEIGHT'
    ) THEN
        ALTER TABLE "APPMETA_FIELD_PRESETS"
            ADD CONSTRAINT "CK_META_FIELD_PRESETS_RECOMMENDATION_WEIGHT"
            CHECK ("RECOMMENDATION_WEIGHT" >= 0.0 AND "RECOMMENDATION_WEIGHT" <= 1.0);
    END IF;
END $$;

-- 检查约束：使用次数非负
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'CK_META_FIELD_PRESETS_USAGE_COUNT'
    ) THEN
        ALTER TABLE "APPMETA_FIELD_PRESETS"
            ADD CONSTRAINT "CK_META_FIELD_PRESETS_USAGE_COUNT"
            CHECK ("USAGE_COUNT" >= 0);
    END IF;
END $$;

-- =============================================
-- 性能优化建议
-- =============================================

-- 1. 定期执行 VACUUM ANALYZE 以优化查询性能
-- VACUUM ANALYZE "APPMETA_FIELD_PRESETS";

-- 2. 对于大量数据的表，考虑创建部分索引以优化特定查询
-- 例如：只对启用的预设创建索引
-- CREATE INDEX "IDX_META_FIELD_PRESETS_ENABLED_POPULAR"
--     ON "APPMETA_FIELD_PRESETS" ("USAGE_COUNT" DESC, "RECOMMENDATION_WEIGHT" DESC)
--     WHERE "IS_ENABLED" = true;

-- 3. 对于 JSONB 字段的查询，可以使用表达式索引优化特定查询
-- 例如：为标签数组长度创建索引
-- CREATE INDEX "IDX_META_FIELD_PRESETS_TAGS_COUNT"
--     ON "APPMETA_FIELD_PRESETS" ((jsonb_array_length("TAGS")));

-- =============================================
-- 示例数据插入（可选）
-- =============================================

-- INSERT INTO "APPMETA_FIELD_PRESETS" (
--     "ID", "PRESET_NAME", "DESCRIPTION", "TAGS", "META_FIELDS",
--     "BUSINESS_SCENARIOS", "APPLICABLE_FACET_TYPES", "APPLICABLE_TEMPLATE_PURPOSES",
--     "USAGE_COUNT", "RECOMMENDATION_WEIGHT", "IS_ENABLED", "IS_SYSTEM_PRESET",
--     "SORT_ORDER"
-- ) VALUES (
--     gen_random_uuid(),
--     '项目文档预设',
--     '适用于项目文档的元数据预设',
--     '["项目", "文档", "常用"]'::jsonb,
--     '[{"entityType":"Project","fieldKey":"project_name","fieldName":"项目名称","dataType":"string","isRequired":true,"order":1}]'::jsonb,
--     '["Project"]'::jsonb,
--     '[1,2]'::jsonb,
--     '[1,2]'::jsonb,
--     0,
--     0.5,
--     true,
--     false,
--     0
-- );

