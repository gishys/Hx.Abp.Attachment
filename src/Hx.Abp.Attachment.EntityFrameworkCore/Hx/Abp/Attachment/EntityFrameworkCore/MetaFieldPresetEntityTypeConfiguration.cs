using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class MetaFieldPresetEntityTypeConfiguration
        : IEntityTypeConfiguration<MetaFieldPreset>
    {
        public void Configure(EntityTypeBuilder<MetaFieldPreset> builder)
        {
            builder.ToTable(
                BgAppConsts.DbTablePrefix + "META_FIELD_PRESETS",
                BgAppConsts.DbSchema,
                tableBuilder =>
                {
                    // 约束配置
                    tableBuilder.HasCheckConstraint("CK_META_FIELD_PRESETS_RECOMMENDATION_WEIGHT",
                        "\"RECOMMENDATION_WEIGHT\" >= 0.0 AND \"RECOMMENDATION_WEIGHT\" <= 1.0");
                });

            // 主键配置
            builder.HasKey(d => d.Id).HasName("PK_META_FIELD_PRESETS");

            // 基础字段配置
            builder.Property(d => d.Id).HasColumnName("ID");
            builder.Property(d => d.PresetName).HasColumnName("PRESET_NAME")
                .HasMaxLength(256)
                .UseCollation("und-x-icu")
                .IsRequired();

            builder.Property(d => d.Description).HasColumnName("DESCRIPTION")
                .HasColumnType("text")
                .HasMaxLength(1000)
                .UseCollation("und-x-icu")
                .IsRequired(false);

            builder.Property(d => d.UsageCount).HasColumnName("USAGE_COUNT").HasDefaultValue(0);
            builder.Property(d => d.RecommendationWeight).HasColumnName("RECOMMENDATION_WEIGHT")
                .HasDefaultValue(0.5)
                .HasColumnType("double precision");
            builder.Property(d => d.IsEnabled).HasColumnName("IS_ENABLED").HasDefaultValue(true);
            builder.Property(d => d.IsSystemPreset).HasColumnName("IS_SYSTEM_PRESET").HasDefaultValue(false);
            builder.Property(d => d.SortOrder).HasColumnName("SORT_ORDER").HasDefaultValue(0);
            builder.Property(d => d.LastUsedTime).HasColumnName("LAST_USED_TIME");

            // Tags字段配置（JSONB格式）
#pragma warning disable CS8600
            var tagsConverter = new ValueConverter<List<string>, string>(
                v => v == null || v.Count == 0 ? "[]" :
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<string>() :
                     System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
            );
#pragma warning restore CS8600

#pragma warning disable CS8620
            builder.Property(d => d.Tags)
                .HasColumnName("TAGS")
                .HasColumnType("jsonb")
                .HasConversion(tagsConverter)
                .HasDefaultValueSql("'[]'::jsonb");
#pragma warning restore CS8620

            // MetaFields字段配置（JSONB格式）
#pragma warning disable CS8600
            var metaFieldsConverter = new ValueConverter<ICollection<MetaField>, string>(
                v => v == null || v.Count == 0 ? "[]" :
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<MetaField>() :
                     System.Text.Json.JsonSerializer.Deserialize<List<MetaField>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<MetaField>()
            );
#pragma warning restore CS8600

            builder.Property(d => d.MetaFields)
                .HasColumnName("META_FIELDS")
                .HasColumnType("jsonb")
                .HasConversion(metaFieldsConverter)
                .IsRequired(false);

            // BusinessScenarios字段配置（JSONB格式）
#pragma warning disable CS8600
            var businessScenariosConverter = new ValueConverter<List<string>, string>(
                v => v == null || v.Count == 0 ? "[]" :
                     System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) || v == "[]" ? new List<string>() :
                     System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
            );
#pragma warning restore CS8600

#pragma warning disable CS8620
            builder.Property(d => d.BusinessScenarios)
                .HasColumnName("BUSINESS_SCENARIOS")
                .HasColumnType("jsonb")
                .HasConversion(businessScenariosConverter)
                .HasDefaultValueSql("'[]'::jsonb");
#pragma warning restore CS8620

            // ApplicableFacetTypes字段配置（JSONB格式）
#pragma warning disable CS8600
            var facetTypesConverter = new ValueConverter<List<FacetType>, string>(
                v => v == null || v.Count == 0 ? "[]" :
                     System.Text.Json.JsonSerializer.Serialize(v.Select(x => (int)x), (System.Text.Json.JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) || v == "[]" 
                    ? new List<FacetType>() 
                    : (System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<int>())
                        .Select(x => (FacetType)x)
                        .ToList()
            );
#pragma warning restore CS8600

#pragma warning disable CS8620
            builder.Property(d => d.ApplicableFacetTypes)
                .HasColumnName("APPLICABLE_FACET_TYPES")
                .HasColumnType("jsonb")
                .HasConversion(facetTypesConverter)
                .HasDefaultValueSql("'[]'::jsonb");
#pragma warning restore CS8620

            // ApplicableTemplatePurposes字段配置（JSONB格式）
#pragma warning disable CS8600
            var templatePurposesConverter = new ValueConverter<List<TemplatePurpose>, string>(
                v => v == null || v.Count == 0 ? "[]" :
                     System.Text.Json.JsonSerializer.Serialize(v.Select(x => (int)x), (System.Text.Json.JsonSerializerOptions)null),
                v => string.IsNullOrEmpty(v) || v == "[]" 
                    ? new List<TemplatePurpose>() 
                    : (System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<int>())
                        .Select(x => (TemplatePurpose)x)
                        .ToList()
            );
#pragma warning restore CS8600

#pragma warning disable CS8620
            builder.Property(d => d.ApplicableTemplatePurposes)
                .HasColumnName("APPLICABLE_TEMPLATE_PURPOSES")
                .HasColumnType("jsonb")
                .HasConversion(templatePurposesConverter)
                .HasDefaultValueSql("'[]'::jsonb");
#pragma warning restore CS8620

            // 索引配置
            builder.HasIndex(e => e.PresetName)
                .HasDatabaseName("UK_META_FIELD_PRESETS_NAME")
                .IsUnique();

            builder.HasIndex(e => e.IsEnabled)
                .HasDatabaseName("IDX_META_FIELD_PRESETS_ENABLED");

            builder.HasIndex(e => e.UsageCount)
                .HasDatabaseName("IDX_META_FIELD_PRESETS_USAGE_COUNT");

            builder.HasIndex(e => e.RecommendationWeight)
                .HasDatabaseName("IDX_META_FIELD_PRESETS_RECOMMENDATION_WEIGHT");

            builder.HasIndex(e => e.LastUsedTime)
                .HasDatabaseName("IDX_META_FIELD_PRESETS_LAST_USED_TIME")
                .HasFilter("\"LAST_USED_TIME\" IS NOT NULL");
        }
    }
}

