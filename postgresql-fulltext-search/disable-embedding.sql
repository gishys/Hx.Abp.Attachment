-- 临时禁用 Embedding 字段的 SQL 脚本
-- 如果 pgvector 配置有问题，可以使用这个脚本临时解决

-- 1. 删除 EMBEDDING 字段（如果存在）
DO $$
BEGIN
    -- 如果 EMBEDDING 字段存在，则删除
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'EMBEDDING'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        DROP COLUMN "EMBEDDING";
        
        RAISE NOTICE 'EMBEDDING 字段已从 APPATTACH_CATALOGUES 表删除';
    ELSE
        RAISE NOTICE 'EMBEDDING 字段不存在，无需删除';
    END IF;
END $$;

-- 2. 删除相关的向量索引（如果存在）
DO $$
BEGIN
    -- 如果向量索引存在，则删除
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_EMBEDDING'
    ) THEN
        DROP INDEX "IDX_ATTACH_CATALOGUES_EMBEDDING";
        
        RAISE NOTICE '向量索引已删除';
    ELSE
        RAISE NOTICE '向量索引不存在，无需删除';
    END IF;
END $$;

-- 3. 验证表结构
SELECT 
    column_name, 
    data_type, 
    is_nullable
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
AND column_name IN ('REFERENCE', 'CATALOGUE_NAME')
ORDER BY column_name;

-- 4. 验证索引
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUES' 
AND indexname LIKE '%fts%';
