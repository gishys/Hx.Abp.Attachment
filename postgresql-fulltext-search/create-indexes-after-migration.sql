-- =====================================================
-- 索引创建脚本（在迁移完成后执行）
-- 基于行业最佳实践：倒排索引 + 向量索引的混合检索
-- =====================================================

-- 注意：此脚本应在主迁移脚本执行完成后运行
-- CREATE INDEX CONCURRENTLY 不能在事务块中运行

-- 1. 全文检索倒排索引（GIN索引，支持高效全文搜索）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FULL_TEXT" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("FULL_TEXT_VECTOR");

-- 2. 标签索引（GIN索引，支持JSONB查询）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TAGS" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("TAGS");

-- 3. 描述字段索引（用于前缀搜索和模糊匹配）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_DESCRIPTION" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("DESCRIPTION");

-- 4. 复合索引（分面类型 + 用途 + 最新版本）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_PURPOSE_LATEST" 
ON "APPATTACH_CATALOGUE_TEMPLATES" ("FACET_TYPE", "TEMPLATE_PURPOSE", "IS_LATEST");

-- 5. 向量相似度索引（用于语义检索）
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IDX_ATTACH_CATALOGUE_TEMPLATES_TEXT_VECTOR" 
ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN ("TEXT_VECTOR" vector_cosine_ops);

-- 6. 验证索引创建结果
DO $$ 
BEGIN
    RAISE NOTICE '=== 索引创建验证 ===';
    
    -- 检查索引是否存在
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE tablename = 'APPATTACH_CATALOGUE_TEMPLATES' 
        AND indexname LIKE 'IDX_ATTACH_CATALOGUE_TEMPLATES_%'
    ) THEN
        RAISE NOTICE '✓ 索引创建成功';
    ELSE
        RAISE EXCEPTION '✗ 索引创建失败';
    END IF;
    
    RAISE NOTICE '=== 索引创建完成 ===';
EXCEPTION WHEN OTHERS THEN
    RAISE EXCEPTION '验证索引创建结果时发生错误: %', SQLERRM;
END $$;

-- 7. 性能优化建议
DO $$ 
BEGIN
    RAISE NOTICE '=== 性能优化建议 ===';
    RAISE NOTICE '1. 定期运行 ANALYZE 更新统计信息';
    RAISE NOTICE '2. 监控索引使用情况，根据查询模式调整索引';
    RAISE NOTICE '3. 考虑使用分区表处理大量数据';
    RAISE NOTICE '4. 定期清理无效的全文检索向量';
    RAISE NOTICE '5. 使用连接池优化数据库连接';
    RAISE NOTICE '6. 如需中文全文检索，可安装 zhparser 扩展';
END $$;
