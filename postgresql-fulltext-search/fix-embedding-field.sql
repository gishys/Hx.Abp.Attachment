-- 修复 Embedding 字段的 SQL 脚本
-- 这个脚本解决 float[] 属性无法映射到 vector(384) 类型的问题

-- 1. 确保 pgvector 扩展已安装
CREATE EXTENSION IF NOT EXISTS vector;

-- 2. 检查 EMBEDDING 字段是否存在
DO $$
BEGIN
    -- 如果 EMBEDDING 字段不存在，则添加
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'APPATTACH_CATALOGUES' 
        AND column_name = 'EMBEDDING'
    ) THEN
        ALTER TABLE "APPATTACH_CATALOGUES" 
        ADD COLUMN "EMBEDDING" vector(384);
        
        RAISE NOTICE 'EMBEDDING 字段已添加到 APPATTACH_CATALOGUES 表';
    ELSE
        RAISE NOTICE 'EMBEDDING 字段已存在';
    END IF;
END $$;

-- 3. 检查并创建向量索引
DO $$
BEGIN
    -- 如果向量索引不存在，则创建
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_EMBEDDING'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_EMBEDDING" 
        ON "APPATTACH_CATALOGUES" 
        USING ivfflat ("EMBEDDING" vector_cosine_ops);
        
        RAISE NOTICE '向量索引已创建';
    ELSE
        RAISE NOTICE '向量索引已存在';
    END IF;
END $$;

-- 4. 检查并创建全文搜索索引
DO $$
BEGIN
    -- 如果全文搜索索引不存在，则创建
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUES' 
        AND indexname = 'IDX_ATTACH_CATALOGUES_SEARCH_VECTOR'
    ) THEN
        CREATE INDEX "IDX_ATTACH_CATALOGUES_SEARCH_VECTOR" 
        ON "APPATTACH_CATALOGUES" 
        USING GIN ("SEARCH_VECTOR");
        
        RAISE NOTICE '全文搜索索引已创建';
    ELSE
        RAISE NOTICE '全文搜索索引已存在';
    END IF;
END $$;

-- 5. 验证表结构
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_name = 'APPATTACH_CATALOGUES' 
AND column_name IN ('EMBEDDING', 'SEARCH_VECTOR', 'REFERENCE', 'CATALOGUE_NAME')
ORDER BY column_name;

-- 6. 验证索引
SELECT 
    indexname, 
    indexdef
FROM pg_indexes 
WHERE tablename = 'APPATTACH_CATALOGUES' 
AND indexname LIKE '%EMBEDDING%' OR indexname LIKE '%SEARCH_VECTOR%';

-- 7. 测试向量功能
DO $$
BEGIN
    -- 测试向量类型是否正常工作
    DECLARE
        test_vector vector(384);
    BEGIN
        -- 创建一个测试向量（384个0）
        test_vector := array_fill(0.0::real, ARRAY[384]);
        
        -- 如果执行到这里没有错误，说明向量功能正常
        RAISE NOTICE '向量功能测试通过';
    EXCEPTION
        WHEN OTHERS THEN
            RAISE NOTICE '向量功能测试失败: %', SQLERRM;
    END;
END $$;
