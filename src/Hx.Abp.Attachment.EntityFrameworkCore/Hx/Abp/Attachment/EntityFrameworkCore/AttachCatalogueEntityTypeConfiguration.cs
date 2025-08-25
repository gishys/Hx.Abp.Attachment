using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachCatalogueEntityTypeConfiguration
        : IEntityTypeConfiguration<AttachCatalogue>
    {
        public void Configure(EntityTypeBuilder<AttachCatalogue> builder)
        {
            builder.ConfigureFullAuditedAggregateRoot();
            builder.ToTable(
                BgAppConsts.DbTablePrefix + "ATTACH_CATALOGUES",
                BgAppConsts.DbSchema);
            
            // 主键配置
            builder.HasKey(d => d.Id).HasName("PK_ATTACH_CATALOGUES");

            // 基础字段配置
            builder.Property(d => d.AttachReceiveType).HasColumnName("ATTACH_RECEIVE_TYPE");
            builder.Property(d => d.CatalogueName).HasColumnName("CATALOGUE_NAME")
                .HasMaxLength(128) // 增加长度以支持中文
                .UseCollation("und-x-icu") // 使用ICU提供更好的中文排序支持
                .IsRequired();
            builder.Property(d => d.Reference).HasColumnName("REFERENCE").HasMaxLength(100).IsRequired();
            builder.Property(d => d.ReferenceType).HasColumnName("REFERENCE_TYPE");
            builder.Property(d => d.AttachCount).HasColumnName("ATTACH_COUNT").HasDefaultValue(0);
            builder.Property(d => d.PageCount).HasColumnName("PAGE_COUNT").HasDefaultValue(0);
            builder.Property(d => d.IsVerification).HasColumnName("IS_VERIFICATION").HasDefaultValue(false);
            builder.Property(d => d.VerificationPassed).HasColumnName("VERIFICATION_PASSED").HasDefaultValue(false);
            builder.Property(d => d.IsRequired).HasColumnName("IS_REQUIRED").HasDefaultValue(false);
            builder.Property(d => d.SequenceNumber).HasColumnName("SEQUENCE_NUMBER").HasDefaultValue(0);
            builder.Property(d => d.ParentId).HasColumnName("PARENT_ID");
            builder.Property(d => d.IsStatic).HasColumnName("IS_STATIC").HasDefaultValue(false);
            builder.Property(d => d.TemplateId).HasColumnName("TEMPLATE_ID").IsRequired(false);

            // 全文内容字段配置
            builder.Property(d => d.FullTextContent).HasColumnName("FULL_TEXT_CONTENT").HasColumnType("text");
            builder.Property(d => d.FullTextContentUpdatedTime).HasColumnName("FULL_TEXT_CONTENT_UPDATED_TIME");

            // 审计字段配置
            builder.Property(p => p.ExtraProperties).HasColumnName("EXTRA_PROPERTIES");
            builder.Property(p => p.ConcurrencyStamp).HasColumnName("CONCURRENCY_STAMP")
                .IsConcurrencyToken(); // 添加并发标记
            builder.Property(p => p.CreationTime).HasColumnName("CREATION_TIME");
            builder.Property(p => p.CreatorId).HasColumnName("CREATOR_ID");
            builder.Property(p => p.LastModificationTime).HasColumnName("LAST_MODIFICATION_TIME");
            builder.Property(p => p.LastModifierId).HasColumnName("LAST_MODIFIER_ID");
            builder.Property(p => p.IsDeleted).HasColumnName("IS_DELETED");
            builder.Property(p => p.DeleterId).HasColumnName("DELETER_ID");
            builder.Property(p => p.DeletionTime).HasColumnName("DELETION_TIME");

            // 全文检索配置 - 不进行数据库映射，使用原生SQL查询
            // builder.Property(d => d.SearchVector)
            //     .HasColumnName("SEARCH_VECTOR")
            //     .HasComputedColumnSql(
            //         "to_tsvector('chinese', " +
            //         "coalesce(\"CATALOGUE_NAME\",'') || ' ' || " +
            //         "coalesce(\"REFERENCE\",'')", true);

            // 语义检索配置 - 暂时忽略 Embedding 字段以避免 pgvector 配置问题
            // builder.Property(d => d.Embedding)
            //     .HasColumnName("EMBEDDING")
            //     .HasColumnType("vector(384)")
            //     .HasVectorDimensions(384);

            // 索引配置 - 全文搜索索引通过原生SQL创建
            // builder.HasIndex(d => d.SearchVector)
            //     .HasDatabaseName("IDX_ATTACH_CATALOGUES_SEARCH_VECTOR")
            //     .HasMethod("GIN");

            // 向量索引 - 暂时注释掉
            // builder.HasIndex(d => d.Embedding)
            //     .HasDatabaseName("IDX_ATTACH_CATALOGUES_EMBEDDING")
            //     .HasMethod("ivfflat")
            //     .HasOperators("vector_cosine_ops");

            // 业务唯一索引 - 使用包含筛选条件的唯一索引
            builder.HasIndex(e => new { e.Reference, e.ReferenceType, e.CatalogueName })
                .HasDatabaseName("UK_ATTACH_CATALOGUES_REF_TYPE_NAME")
                .HasFilter("\"IS_DELETED\" = false") // 软删除过滤
                .IsUnique()
                .HasAnnotation("ConcurrencyCheck", true);

            // 全文搜索索引 - 通过原生SQL创建
            // CREATE INDEX CONCURRENTLY IF NOT EXISTS IDX_ATTACH_CATALOGUES_FULLTEXT 
            // ON "APPATTACH_CATALOGUES" USING GIN (
            //     to_tsvector('chinese_fts', 
            //         COALESCE("CATALOGUE_NAME", '') || ' ' || 
            //         COALESCE("FULL_TEXT_CONTENT", '')
            //     )
            // );

            // 关系配置
            builder.HasMany(d => d.AttachFiles)
                .WithOne()
                .HasForeignKey(d => d.AttachCatalogueId)
                .HasConstraintName("FK_ATTACH_CATALOGUES_FILES")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Children)
                .WithOne()
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_ATTACH_CATALOGUES_PARENT")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

//--需要在数据库中执行
//CREATE EXTENSION IF NOT EXISTS zhparser;
//CREATE TEXT SEARCH CONFIGURATION chinese (PARSER = zhparser);
//ALTER TEXT SEARCH CONFIGURATION chinese ADD MAPPING FOR n, v, a, i, e, l WITH simple;
