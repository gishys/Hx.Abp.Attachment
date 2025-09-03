-- =====================================================
-- 创建元数据字段索引脚本
-- 在迁移脚本执行完成后运行
-- 注意：已移除CONCURRENTLY关键字以支持事务执行
-- 并缩短了超过63字符的索引名称
-- 
-- 索引类型说明：
-- - GIN索引：用于JSONB数组和全文搜索向量
-- - BTREE索引：用于提取的文本值（->>操作符）
-- =====================================================

-- 为元数据字段创建GIN索引（用于JSON查询）
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CATALOGUE_TEMPLATES_META_FIELDS" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("META_FIELDS");

-- 为元数据字段的常用查询路径创建索引（使用BTREE，因为提取的文本值更适合BTREE索引）
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_ENTITY_TYPE" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'EntityType'));

CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_DATA_TYPE" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'DataType'));

CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_IS_REQUIRED" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'IsRequired'));

CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_IS_ENABLED" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'IsEnabled'));

CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_GROUP" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'Group'));

-- 为元数据字段的标签创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_TAGS" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (("META_FIELDS"->'Tags'));

-- 为元数据字段的字段键名创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_FIELD_KEY" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'FieldKey'));

-- 为元数据字段的字段名称创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_FIELD_NAME" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'FieldName'));

-- 为元数据字段的顺序创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_ORDER" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'Order'));

-- 复合索引：分面类型 + 模板用途 + 元数据字段
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_FACET_PURPOSE_META" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE", "TEMPLATE_PURPOSE") 
WHERE "META_FIELDS" IS NOT NULL AND "META_FIELDS" != '[]'::jsonb;

-- 复合索引：是否最新 + 元数据字段
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_IS_LATEST_META" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("IS_LATEST") 
WHERE "META_FIELDS" IS NOT NULL AND "META_FIELDS" != '[]'::jsonb;

-- 为元数据字段的验证规则创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_VALIDATION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'ValidationRules'));

-- 为元数据字段的默认值创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_DEFAULT_VAL" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'DefaultValue'));

-- 为元数据字段的正则表达式创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_REGEX" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'RegexPattern'));

-- 为元数据字段的枚举选项创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_OPTIONS" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'Options'));

-- 为元数据字段的单位创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_UNIT" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'Unit'));

-- 为元数据字段的描述创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_DESC" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'Description'));

-- 为元数据字段的创建时间创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_CREATE_TIME" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'CreationTime'));

-- 为元数据字段的最后修改时间创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_LAST_MOD_TIME" 
ON "APPATTACH_CATALOGUE_TEMPLATES" (("META_FIELDS"->>'LastModificationTime'));

-- 创建全文检索向量字段（用于混合检索）
ALTER TABLE "APPATTACH_CATALOGUE_TEMPLATES" 
ADD COLUMN IF NOT EXISTS "META_FIELDS_FULL_TEXT_VECTOR" tsvector;

-- 更新全文检索向量
UPDATE "APPATTACH_CATALOGUE_TEMPLATES" 
SET "META_FIELDS_FULL_TEXT_VECTOR" = (
    SELECT to_tsvector('chinese_fts', 
        COALESCE(string_agg(
            COALESCE(field->>'FieldName', '') || ' ' || 
            COALESCE(field->>'FieldKey', '') || ' ' || 
            COALESCE(field->>'Description', '') || ' ' || 
            COALESCE(array_to_string(ARRAY(SELECT jsonb_array_elements_text(field->'Tags')), ' '), ''),
            ' '
        ), '')
    )
    FROM jsonb_array_elements("META_FIELDS") AS field
    WHERE "META_FIELDS" IS NOT NULL AND "META_FIELDS" != '[]'::jsonb
);

-- 为全文检索向量创建索引
CREATE INDEX IF NOT EXISTS "IX_ATTACH_CTLG_TMPL_META_FULL_TEXT" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("META_FIELDS_FULL_TEXT_VECTOR");

-- 创建触发器函数，自动更新全文检索向量
CREATE OR REPLACE FUNCTION update_meta_fields_full_text_vector()
RETURNS TRIGGER AS $$
BEGIN
    NEW."META_FIELDS_FULL_TEXT_VECTOR" = (
        SELECT to_tsvector('chinese_fts', 
            COALESCE(string_agg(
                COALESCE(field->>'FieldName', '') || ' ' || 
                COALESCE(field->>'FieldKey', '') || ' ' || 
                COALESCE(field->>'Description', '') || ' ' || 
                COALESCE(array_to_string(ARRAY(SELECT jsonb_array_elements_text(field->'Tags')), ' '), ''),
                ' '
            ), '')
        )
        FROM jsonb_array_elements(NEW."META_FIELDS") AS field
        WHERE NEW."META_FIELDS" IS NOT NULL AND NEW."META_FIELDS" != '[]'::jsonb
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 创建触发器
DROP TRIGGER IF EXISTS trigger_update_meta_fields_full_text_vector ON "APPATTACH_CATALOGUE_TEMPLATES";
CREATE TRIGGER trigger_update_meta_fields_full_text_vector
    BEFORE INSERT OR UPDATE ON "APPATTACH_CATALOGUE_TEMPLATES"
    FOR EACH ROW
    EXECUTE FUNCTION update_meta_fields_full_text_vector();

-- 验证索引创建结果
SELECT 
    indexname,
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
    AND indexname LIKE '%META_FIELDS%' OR indexname LIKE '%META%'
ORDER BY indexname;