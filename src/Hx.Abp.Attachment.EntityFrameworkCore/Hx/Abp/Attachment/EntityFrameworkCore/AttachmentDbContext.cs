using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachmentDbContext(DbContextOptions<AttachmentDbContext> options) : AbpDbContext<AttachmentDbContext>(options)
    {
        public DbSet<AttachCatalogue> AttachCatalogues { get; set; }
        public DbSet<AttachFile> AttachFiles { get; set; }
        public DbSet<OcrTextBlock> OcrTextBlocks { get; set; }
        public DbSet<AttachCatalogueTemplate> AttachCatalogueTemplates { get; set; }
        public DbSet<MetaFieldPreset> MetaFieldPresets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 忽略值对象类型，避免EF Core将其作为实体处理
            builder.Ignore<AttachCatalogueTemplatePermission>();
            builder.Ignore<MetaField>();

            // 配置Npgsql JSON处理
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            // 启用动态JSON序列化支持
            builder.HasAnnotation("Npgsql:EnableDynamicJson", true);

            // 配置JSON字段处理
            builder.HasAnnotation("Npgsql:JsonDocumentSupport", true);

            builder.ConfigureAttachment();
            
            // 启用 pgvector 扩展
            builder.HasPostgresExtension("vector");
            
            // 启用 pg_trgm 扩展（用于模糊搜索）
            builder.HasPostgresExtension("pg_trgm");

            // 配置全文搜索
            ConfigureFullTextSearch(builder);
        }

        private static void ConfigureFullTextSearch(ModelBuilder builder)
        {
            // 配置AttachCatalogue实体的全文搜索索引
            builder.Entity<AttachCatalogue>(entity =>
            {
                entity.HasIndex(e => e.CatalogueName)
                    .HasMethod("gin")
                    .HasOperators("gin_trgm_ops");
            });
        }

        /// <summary>
        /// 初始化中文全文搜索配置
        /// </summary>
        public async Task InitializeFullTextSearchAsync()
        {
            // 创建中文全文搜索配置
            await Database.ExecuteSqlRawAsync(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_ts_config
                        WHERE cfgname = 'chinese_fts'
                    ) THEN
                        CREATE TEXT SEARCH CONFIGURATION chinese_fts (PARSER = pg_catalog.default);
                        ALTER TEXT SEARCH CONFIGURATION chinese_fts
                            ALTER MAPPING FOR
                                asciiword, asciihword, hword_asciipart,
                                word, hword, hword_part
                            WITH simple;
                    END IF;
                END $$;
            ");
        }

        /// <summary>
        /// 创建全文搜索索引
        /// </summary>
        public async Task CreateFullTextSearchIndexesAsync()
        {
            var sql = @"
                -- 创建全文搜索索引
                CREATE INDEX IF NOT EXISTS idx_attach_catalogue_name_fts 
                ON ""APPATTACH_CATALOGUES"" USING gin(to_tsvector('chinese_fts', ""CATALOGUE_NAME""));
                
                CREATE INDEX IF NOT EXISTS idx_attach_file_name_fts 
                ON ""APPATTACHFILE"" USING gin(to_tsvector('chinese_fts', ""FILENAME""));
                
                -- 创建模糊搜索索引
                CREATE INDEX IF NOT EXISTS idx_attach_catalogue_name_trgm 
                ON ""APPATTACH_CATALOGUES"" USING gin(""CATALOGUE_NAME"" gin_trgm_ops);
                
                CREATE INDEX IF NOT EXISTS idx_attach_file_name_trgm 
                ON ""APPATTACHFILE"" USING gin(""FILENAME"" gin_trgm_ops);
            ";
            await Database.ExecuteSqlRawAsync(sql);
        }

        /// <summary>
        /// 为指定表创建全文搜索索引
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="columnName">列名</param>
        /// <param name="indexName">索引名</param>
        public async Task CreateFullTextSearchIndexAsync(string tableName, string columnName, string indexName)
        {
            var sql = $@"
                CREATE INDEX IF NOT EXISTS {indexName} 
                ON {tableName} 
                USING gin(to_tsvector('chinese_fts', {columnName}));
            ";
            await Database.ExecuteSqlRawAsync(sql);
        }
    }
}
