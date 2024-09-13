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
            builder.HasKey(d => d.Id).HasName("ATTACH_CATALOGUES_PK");
            builder.HasIndex(d => d.CatalogueName)
                .HasDatabaseName("ATTACH_CATALOGUES_IND_CNAME");
            builder.HasIndex(d => d.Reference)
                .HasDatabaseName("ATTACH_CATALOGUES_IND_REFERENCE");

            builder.Property(d => d.AttachReceiveType).HasColumnName("ATTACHRECEIVETYPE");
            builder.Property(d => d.CatalogueName).HasColumnName("CATALOGUENAME").HasMaxLength(50);
            builder.Property(d => d.Reference).HasColumnName("REFERENCE").HasMaxLength(100);
            builder.Property(d => d.ReferenceType).HasColumnName("REFERENCETYPE");
            builder.Property(d => d.AttachCount).HasColumnName("ATTACHCOUNT").HasDefaultValue(0);
            builder.Property(d => d.PageCount).HasColumnName("PAGECOUNT").HasDefaultValue(0);
            builder.Property(d => d.IsVerification).HasColumnName("ISVERIFICATION").HasDefaultValue(false);
            builder.Property(d => d.VerificationPassed).HasColumnName("VERIFICATIONPASSED").HasDefaultValue(false);
            builder.Property(d => d.IsRequired).HasColumnName("ISREQUIRED").HasDefaultValue(false);
            builder.Property(d => d.SequenceNumber).HasColumnName("SEQUENCENUMBER").HasDefaultValue(0);
            builder.Property(d => d.ParentId).HasColumnName("PARENTID");
            builder.Property(d => d.IsStatic).HasColumnName("ISSTATIC").HasDefaultValue(false);

            builder.Property(p => p.ExtraProperties).HasColumnName("EXTRAPROPERTIES");
            builder.Property(p => p.ConcurrencyStamp).HasColumnName("CONCURRENCYSTAMP");
            builder.Property(p => p.CreationTime).HasColumnName("CREATIONTIME").HasColumnType("timestamp without time zone");
            builder.Property(p => p.CreatorId).HasColumnName("CREATORID");
            builder.Property(p => p.LastModificationTime).HasColumnName("LASTMODIFICATIONTIME").HasColumnType("timestamp without time zone");
            builder.Property(p => p.LastModifierId).HasColumnName("LASTMODIFIERID");
            builder.Property(p => p.IsDeleted).HasColumnName("ISDELETED");
            builder.Property(p => p.DeleterId).HasColumnName("DELETERID");
            builder.Property(p => p.DeletionTime).HasColumnName("DELETIONTIME").HasColumnType("timestamp without time zone");

            //relation
            builder.HasMany(d => d.AttachFiles)
                .WithOne()
                .HasForeignKey(d => d.AttachCatalogueId)
                .HasConstraintName("ATTACH_CATALOGUES_ATTFILE_FK")
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasMany(d => d.Children)
                .WithOne()
                .HasForeignKey(d => d.ParentId)
                .HasConstraintName("ATTACH_CATALOGUES_PARENT_FK")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
