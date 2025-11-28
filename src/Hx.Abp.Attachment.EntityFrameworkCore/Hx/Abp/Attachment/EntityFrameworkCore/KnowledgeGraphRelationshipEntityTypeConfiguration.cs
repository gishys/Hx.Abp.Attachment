using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.KnowledgeGraph;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    /// <summary>
    /// 知识图谱关系实体配置
    /// </summary>
    public class KnowledgeGraphRelationshipEntityTypeConfiguration : IEntityTypeConfiguration<KnowledgeGraphRelationship>
    {
        public void Configure(EntityTypeBuilder<KnowledgeGraphRelationship> builder)
        {
            // 配置表名和架构
            builder.ToTable(
                BgAppConsts.DbTablePrefix + "KG_RELATIONSHIPS",
                BgAppConsts.DbSchema);
            
            // 配置 ABP 约定（包括 CreationAuditedAggregateRoot 的字段）
            builder.ConfigureCreationAuditedAggregateRoot();
            
            // 主键配置
            builder.HasKey(r => r.Id).HasName("PK_KG_RELATIONSHIPS");

            // 字段配置
            builder.Property(r => r.SourceEntityId)
                .HasColumnName("SOURCE_ENTITY_ID")
                .IsRequired();

            builder.Property(r => r.SourceEntityType)
                .HasColumnName("SOURCE_ENTITY_TYPE")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.TargetEntityId)
                .HasColumnName("TARGET_ENTITY_ID")
                .IsRequired();

            builder.Property(r => r.TargetEntityType)
                .HasColumnName("TARGET_ENTITY_TYPE")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(r => r.Type)
                .HasColumnName("RELATIONSHIP_TYPE")
                .HasMaxLength(50)
                .HasConversion<string>() // 将枚举转换为字符串存储
                .IsRequired();

            builder.Property(r => r.Role)
                .HasColumnName("ROLE")
                .HasMaxLength(50);

            builder.Property(r => r.SemanticType)
                .HasColumnName("SEMANTIC_TYPE")
                .HasMaxLength(50);

            builder.Property(r => r.Description)
                .HasColumnName("DESCRIPTION")
                .HasColumnType("TEXT");

            builder.Property(r => r.Weight)
                .HasColumnName("WEIGHT")
                .HasDefaultValue(1.0);

            // 配置扩展属性（ExtraProperties）
            builder.Property(r => r.ExtraProperties)
                .HasColumnName("EXTRA_PROPERTIES")
                .HasColumnType("jsonb");

            // 配置并发控制标记（ConcurrencyStamp）
            builder.Property(r => r.ConcurrencyStamp)
                .HasColumnName("CONCURRENCY_STAMP")
                .IsConcurrencyToken();

            // 配置创建审计字段（CreationTime 和 CreatorId）
            builder.Property(r => r.CreationTime)
                .HasColumnName("CREATION_TIME")
                .IsRequired();
            
            builder.Property(r => r.CreatorId)
                .HasColumnName("CREATOR_ID");

            // 注意：唯一约束在 SQL 脚本中创建（create-kg-relationships-table.sql）
            // 因为需要使用 COALESCE 处理 NULL 值，EF Core 的 HasIndex 不支持表达式
            // 唯一约束名称：UK_KG_RELATIONSHIPS_SOURCE_TARGET_TYPE_ROLE_SEMANTIC
            // 约束字段：SOURCE_ENTITY_ID, TARGET_ENTITY_ID, RELATIONSHIP_TYPE, COALESCE(ROLE, ''), COALESCE(SEMANTIC_TYPE, '')

            // 创建索引
            builder.HasIndex(r => new { r.SourceEntityId, r.SourceEntityType })
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_SOURCE");

            builder.HasIndex(r => new { r.TargetEntityId, r.TargetEntityType })
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_TARGET");

            builder.HasIndex(r => r.Type)
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_TYPE");

            builder.HasIndex(r => new { r.SourceEntityType, r.Type })
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_SOURCE_TYPE");

            builder.HasIndex(r => r.Role)
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_ROLE")
                .HasFilter("\"ROLE\" IS NOT NULL");

            builder.HasIndex(r => r.SemanticType)
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_SEMANTIC_TYPE")
                .HasFilter("\"SEMANTIC_TYPE\" IS NOT NULL");

            builder.HasIndex(r => new { r.Type, r.Role })
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_TYPE_ROLE")
                .HasFilter("\"ROLE\" IS NOT NULL");

            builder.HasIndex(r => new { r.Type, r.SemanticType })
                .HasDatabaseName("IDX_KG_RELATIONSHIPS_TYPE_SEMANTIC")
                .HasFilter("\"SEMANTIC_TYPE\" IS NOT NULL");
        }
    }
}

