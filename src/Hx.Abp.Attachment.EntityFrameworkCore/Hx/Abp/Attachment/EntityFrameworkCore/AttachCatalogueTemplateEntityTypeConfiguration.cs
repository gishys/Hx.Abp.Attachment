using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachCatalogueTemplateEntityTypeConfiguration
        : IEntityTypeConfiguration<AttachCatalogueTemplate>
    {
        public void Configure(EntityTypeBuilder<AttachCatalogueTemplate> builder)
        {
            builder.ConfigureFullAuditedAggregateRoot();
            builder.ToTable(
                BgAppConsts.DbTablePrefix + "ATTACH_CATALOGUE_TEMPLATES",
                BgAppConsts.DbSchema);
            
            // 主键配置
            builder.HasKey(d => d.Id).HasName("PK_ATTACH_CATALOGUE_TEMPLATES");

            // 基础字段配置
            builder.Property(d => d.TemplateName).HasColumnName("TEMPLATE_NAME")
                .HasMaxLength(256)
                .UseCollation("und-x-icu")
                .IsRequired();

            builder.Property(d => d.Id).HasColumnName("ID");
            builder.Property(d => d.Version).HasColumnName("VERSION").HasDefaultValue(1);
            builder.Property(d => d.IsLatest).HasColumnName("IS_LATEST").HasDefaultValue(true);
            builder.Property(d => d.AttachReceiveType).HasColumnName("ATTACH_RECEIVE_TYPE");
            builder.Property(d => d.NamePattern).HasColumnName("NAME_PATTERN").HasMaxLength(512);
            builder.Property(d => d.RuleExpression).HasColumnName("RULE_EXPRESSION").HasColumnType("text");
            builder.Property(d => d.SemanticModel).HasColumnName("SEMANTIC_MODEL").HasMaxLength(128);
            builder.Property(d => d.IsRequired).HasColumnName("IS_REQUIRED").HasDefaultValue(false);
            builder.Property(d => d.SequenceNumber).HasColumnName("SEQUENCE_NUMBER").HasDefaultValue(0);
            builder.Property(d => d.IsStatic).HasColumnName("IS_STATIC").HasDefaultValue(false);
            builder.Property(d => d.ParentId).HasColumnName("PARENT_ID");

            // 审计字段配置
            builder.Property(p => p.ExtraProperties).HasColumnName("EXTRA_PROPERTIES");
            builder.Property(p => p.ConcurrencyStamp).HasColumnName("CONCURRENCY_STAMP")
                .IsConcurrencyToken();
            builder.Property(p => p.CreationTime).HasColumnName("CREATION_TIME");
            builder.Property(p => p.CreatorId).HasColumnName("CREATOR_ID");
            builder.Property(p => p.LastModificationTime).HasColumnName("LAST_MODIFICATION_TIME");
            builder.Property(p => p.LastModifierId).HasColumnName("LAST_MODIFIER_ID");
            builder.Property(p => p.IsDeleted).HasColumnName("IS_DELETED");
            builder.Property(p => p.DeleterId).HasColumnName("DELETER_ID");
            builder.Property(p => p.DeletionTime).HasColumnName("DELETION_TIME");

            // 索引配置
            builder.HasIndex(e => new { e.TemplateName, e.Version })
                .HasDatabaseName("UK_ATTACH_CATALOGUE_TEMPLATES_NAME_VERSION")
                .HasFilter("\"IS_DELETED\" = false")
                .IsUnique();

            builder.HasIndex(e => new { e.TemplateName, e.IsLatest })
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_NAME_LATEST")
                .HasFilter("\"IS_DELETED\" = false AND \"IS_LATEST\" = true");

            builder.HasIndex(e => e.ParentId)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_PARENT_ID");

            builder.HasIndex(e => e.SequenceNumber)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_SEQUENCE");

            // 全文搜索索引 - 通过原生SQL创建
            // CREATE INDEX CONCURRENTLY IF NOT EXISTS IDX_ATTACH_CATALOGUE_TEMPLATES_FULLTEXT 
            // ON "APPATTACH_CATALOGUE_TEMPLATES" USING GIN (
            //     to_tsvector('chinese_fts', 
            //         COALESCE("TEMPLATE_NAME", '') || ' ' || 
            //         COALESCE("NAME_PATTERN", '') || ' ' ||
            //         COALESCE("RULE_EXPRESSION", '')
            //     )
            // );

            // 关系配置
            builder.HasMany(d => d.Children)
                .WithOne()
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_ATTACH_CATALOGUE_TEMPLATES_PARENT")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
