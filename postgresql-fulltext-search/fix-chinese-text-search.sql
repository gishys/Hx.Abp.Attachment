-- 修复中文文本搜索配置
-- 解决 "文本搜寻配置 'chinese' 不存在" 错误

-- 1. 创建 chinese 文本搜索配置（如果不存在）
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'chinese'
    ) THEN
        -- 创建 chinese 配置，基于现有的 chinese_fts 配置
        CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION chinese
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
        
        RAISE NOTICE 'Created chinese text search configuration';
    ELSE
        RAISE NOTICE 'chinese text search configuration already exists';
    END IF;
END $$;

-- 2. 验证配置是否存在
SELECT cfgname, cfgparser 
FROM pg_ts_config 
WHERE cfgname IN ('chinese', 'chinese_fts');

-- 3. 测试文本搜索功能
SELECT to_tsvector('chinese', '工程文档管理系统') as test_vector;

-- 4. 如果需要，也可以创建其他语言的配置
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_ts_config
        WHERE cfgname = 'simple'
    ) THEN
        CREATE TEXT SEARCH CONFIGURATION simple (PARSER = pg_catalog.default);
        ALTER TEXT SEARCH CONFIGURATION simple
            ALTER MAPPING FOR
                asciiword, asciihword, hword_asciipart,
                word, hword, hword_part
            WITH simple;
    END IF;
END $$;

-- 5. 显示所有可用的文本搜索配置
SELECT cfgname, cfgparser, cfgnamespace::regnamespace as schema_name
FROM pg_ts_config
ORDER BY cfgname;
