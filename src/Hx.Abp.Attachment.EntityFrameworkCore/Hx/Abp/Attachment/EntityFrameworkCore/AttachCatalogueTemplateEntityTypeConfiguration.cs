using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
                BgAppConsts.DbSchema,
                tableBuilder =>
                {
                    // 约束配置
                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIMENSION",
                        "\"VECTOR_DIMENSION\" >= 0 AND \"VECTOR_DIMENSION\" <= 2048");

                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE",
                        "\"FACET_TYPE\" IN (0, 1, 2, 3, 4, 5, 6, 99)");

                    tableBuilder.HasCheckConstraint("CK_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PURPOSE",
                        "\"TEMPLATE_PURPOSE\" IN (1, 2, 3, 4, 99)");
                });
            
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
            builder.Property(d => d.WorkflowConfig).HasColumnName("WORKFLOW_CONFIG").HasColumnType("text");
            builder.Property(d => d.IsRequired).HasColumnName("IS_REQUIRED").HasDefaultValue(false);
            builder.Property(d => d.SequenceNumber).HasColumnName("SEQUENCE_NUMBER").HasDefaultValue(0);
            builder.Property(d => d.IsStatic).HasColumnName("IS_STATIC").HasDefaultValue(false);
            builder.Property(d => d.ParentId).HasColumnName("PARENT_ID");

            // 模板路径字段配置
            builder.Property(d => d.TemplatePath).HasColumnName("TEMPLATE_PATH")
                .HasMaxLength(200)
                .IsRequired(false);

            // 新增字段配置
            builder.Property(d => d.FacetType).HasColumnName("FACET_TYPE")
                .HasConversion<int>();

            builder.Property(d => d.TemplatePurpose).HasColumnName("TEMPLATE_PURPOSE")
                .HasConversion<int>();

            builder.Property(d => d.TextVector).HasColumnName("TEXT_VECTOR")
                .HasColumnType("double precision[]");

            builder.Property(d => d.VectorDimension).HasColumnName("VECTOR_DIMENSION")
                .HasDefaultValue(0);

            // 新增字段配置
            builder.Property(d => d.Description).HasColumnName("DESCRIPTION")
                .HasColumnType("text")
                .HasMaxLength(1000)
                .UseCollation("und-x-icu")
                .IsRequired(false);

            builder.Property(d => d.Tags).HasColumnName("TAGS")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'[]'::jsonb");

            // Tags字段配置（JSONB格式）
#pragma warning disable CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。
            var tagsConverter = new ValueConverter<List<string>, string>(
                // 转换为数据库值 - 空集合转换为空数组字符串，而不是null
                v => v == null || v.Count == 0 ? "[]" : 
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                // 从数据库值转换
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<string>() : 
                     System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
            );
#pragma warning restore CS8600 // 将 null 字面量或可能为 null 的值转换为非 null 类型。

#pragma warning disable CS8620 // 由于引用类型的可为 null 性差异，实参不能用于形参。
            builder.Property(d => d.Tags)
                .HasColumnName("TAGS")
                .HasColumnType("jsonb")
                .HasConversion(tagsConverter)
                .HasDefaultValueSql("'[]'::jsonb");
#pragma warning restore CS8620 // 由于引用类型的可为 null 性差异，实参不能用于形参。

            // 权限集合字段配置（JSONB格式）
#pragma warning disable CS8600 // 将 null 文本或可能的 null 值转换为不可为 null 类型
            var permissionsConverter = new ValueConverter<ICollection<AttachCatalogueTemplatePermission>, string>(
                // 转换为数据库值 - 空集合转换为空数组字符串，而不是null
                v => v == null || v.Count == 0 ? "[]" : 
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                // 从数据库值转换
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<AttachCatalogueTemplatePermission>() : 
                     System.Text.Json.JsonSerializer.Deserialize<List<AttachCatalogueTemplatePermission>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<AttachCatalogueTemplatePermission>()
            );
#pragma warning restore CS8600

            builder.Property(d => d.Permissions)
                .HasColumnName("PERMISSIONS")
                .HasColumnType("jsonb")
                .HasConversion(permissionsConverter)
                .IsRequired(false);

            // 元数据字段集合字段配置（JSONB格式）
#pragma warning disable CS8600 // 将 null 文本或可能的 null 值转换为不可为 null 类型
            var metaFieldsConverter = new ValueConverter<ICollection<MetaField>, string>(
                // 转换为数据库值 - 空集合转换为空数组字符串，而不是null
                v => v == null || v.Count == 0 ? "[]" : 
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                // 从数据库值转换
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<MetaField>() : 
                     System.Text.Json.JsonSerializer.Deserialize<List<MetaField>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<MetaField>()
            );
#pragma warning restore CS8600

            builder.Property(d => d.MetaFields)
                .HasColumnName("META_FIELDS")
                .HasColumnType("jsonb")
                .HasConversion(metaFieldsConverter)
                .IsRequired(false);

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

            // 新增索引配置
            builder.HasIndex(e => e.FacetType)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_FACET_TYPE")
                .HasFilter("\"IS_DELETED\" = false");

            builder.HasIndex(e => e.TemplatePurpose)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_PURPOSE")
                .HasFilter("\"IS_DELETED\" = false");

            builder.HasIndex(e => new { e.FacetType, e.TemplatePurpose })
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_IDENTIFIER_COMPOSITE")
                .HasFilter("\"IS_DELETED\" = false");

            builder.HasIndex(e => e.VectorDimension)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_VECTOR_DIM")
                .HasFilter("\"IS_DELETED\" = false AND \"VECTOR_DIMENSION\" > 0");

            // 模板路径相关索引
            builder.HasIndex(e => e.TemplatePath)
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_TEMPLATE_PATH")
                .HasFilter("\"IS_DELETED\" = false AND \"TEMPLATE_PATH\" IS NOT NULL");

            builder.HasIndex(e => new { e.TemplatePath, e.IsLatest })
                .HasDatabaseName("IDX_ATTACH_CATALOGUE_TEMPLATES_PATH_LATEST")
                .HasFilter("\"IS_DELETED\" = false");

            // 关系配置
            builder.HasMany(d => d.Children)
                .WithOne()
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("FK_ATTACH_CATALOGUE_TEMPLATES_PARENT")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
