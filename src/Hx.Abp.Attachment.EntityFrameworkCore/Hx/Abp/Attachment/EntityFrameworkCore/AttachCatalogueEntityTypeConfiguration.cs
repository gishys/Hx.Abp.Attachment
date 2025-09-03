using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Hx.Abp.Attachment.Domain;
using System.Text.Json;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachCatalogueEntityTypeConfiguration : IEntityTypeConfiguration<AttachCatalogue>
    {
        public void Configure(EntityTypeBuilder<AttachCatalogue> builder)
        {
            builder.ConfigureFullAuditedAggregateRoot();
            builder.ToTable(
                BgAppConsts.DbTablePrefix + "ATTACH_CATALOGUES",
                BgAppConsts.DbSchema,
                tableBuilder =>
                {
                    // 约束配置
                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUES_VECTOR_DIMENSION",
                        "\"VECTOR_DIMENSION\" >= 0 AND \"VECTOR_DIMENSION\" <= 2048");

                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUES_CATALOGUE_TYPE",
                        "\"CATALOGUE_FACET_TYPE\" IN (1, 2, 3, 4, 99)");

                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUES_CATALOGUE_PURPOSE",
                        "\"CATALOGUE_PURPOSE\" IN (1, 2, 3, 4, 99)");
                });
            
            // 主键配置
            builder.HasKey(d => d.Id).HasName("PK_ATTACH_CATALOGUES");

            // 创建权限集合的值转换器
#pragma warning disable CS8600 // 将 null 文本或可能的 null 值转换为不可为 null 类型
            var permissionsConverter = new ValueConverter<ICollection<AttachCatalogueTemplatePermission>, string>(
                // 转换为数据库值 - 空集合转换为空数组字符串，而不是null
                v => v == null || v.Count == 0 ? "[]" : 
                     JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                // 从数据库值转换
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<AttachCatalogueTemplatePermission>() : 
                     JsonSerializer.Deserialize<List<AttachCatalogueTemplatePermission>>(v, (JsonSerializerOptions)null) ?? new List<AttachCatalogueTemplatePermission>()
            );
#pragma warning restore CS8600

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

            // 新增字段配置
            builder.Property(d => d.CatalogueFacetType).HasColumnName("CATALOGUE_FACET_TYPE")
                .HasConversion<int>();

            builder.Property(d => d.CataloguePurpose).HasColumnName("CATALOGUE_PURPOSE")
                .HasConversion<int>();

            builder.Property(d => d.TextVector).HasColumnName("TEXT_VECTOR")
                .HasColumnType("double precision[]");

            builder.Property(d => d.VectorDimension).HasColumnName("VECTOR_DIMENSION")
                .HasDefaultValue(0);

            // 权限集合字段配置（JSONB格式）
            builder.Property(d => d.Permissions)
                .HasColumnName("PERMISSIONS")
                .HasColumnType("jsonb")
                .HasConversion(permissionsConverter)
                .IsRequired(false);

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

            // 业务唯一索引 - 使用包含筛选条件的唯一索引
            builder.HasIndex(e => new { e.Reference, e.ReferenceType, e.CatalogueName })
                .HasDatabaseName("UK_ATTACH_CATALOGUES_REF_TYPE_NAME")
                .HasFilter("\"IS_DELETED\" = false") // 软删除过滤
                .IsUnique()
                .HasAnnotation("ConcurrencyCheck", true);

            // 新增字段索引
            builder.HasIndex(e => e.CatalogueFacetType)
                .HasDatabaseName("IDX_ATTACH_CATALOGUES_CATALOGUE_TYPE");

            builder.HasIndex(e => e.CataloguePurpose)
                .HasDatabaseName("IDX_ATTACH_CATALOGUES_CATALOGUE_PURPOSE");

            builder.HasIndex(e => e.VectorDimension)
                .HasDatabaseName("IDX_ATTACH_CATALOGUES_VECTOR_DIMENSION");

            // 复合索引
            builder.HasIndex(e => new { e.CatalogueFacetType, e.CataloguePurpose })
                .HasDatabaseName("IDX_ATTACH_CATALOGUES_TYPE_PURPOSE");

            builder.HasIndex(e => new { e.ParentId, e.CatalogueFacetType })
                .HasDatabaseName("IDX_ATTACH_CATALOGUES_PARENT_TYPE");

            // 关系配置
            builder.HasMany(d => d.AttachFiles)
                .WithOne()
                .HasForeignKey(d => d.AttachCatalogueId)
                .HasConstraintName("FK_ATTACH_CATALOGUES_FILES")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Children)
                .WithOne()
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_ATTACH_CATALOGUES_CHILDREN")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
