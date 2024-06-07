using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachFileEntityTypeConfiguration
        : IEntityTypeConfiguration<AttachFile>
    {
        public void Configure(EntityTypeBuilder<AttachFile> builder)
        {
            builder.ConfigureFullAuditedAggregateRoot();
            builder.ToTable(BgAppConsts.DbTablePrefix + "ATTACHFILE", BgAppConsts.DbSchema);
            builder.HasKey(d => d.Id).HasName("ATTACH_FILES_PK");
            builder.HasIndex(d => d.FileName)
                .HasDatabaseName("ATTACH_FILES_IND_FILENAME");
            builder.HasIndex(d => d.FileAlias)
                .HasDatabaseName("ATTACH_FILES_IND_FILEALIAS");

            builder.Property(d => d.Id).HasColumnName("ID");
            builder.Property(d => d.FileName).HasColumnName("FILENAME").HasMaxLength(50);
            builder.Property(d => d.FileAlias).HasColumnName("FILEALIAS").HasMaxLength(50);
            builder.Property(d => d.FilePath).HasColumnName("FILEPATH").HasMaxLength(100);
            builder.Property(d => d.SequenceNumber).HasColumnName("SEQUENCENUMBER");
            builder.Property(d => d.DocumentContent).HasColumnName("DOCUMENTCONTENT");
            builder.Property(d => d.AttachCatalogueId).HasColumnName("ATTACHCATALOGUEID");
            builder.Property(d => d.FileType).HasColumnName("FILETYPE").HasMaxLength(10);
            builder.Property(d => d.FileSize).HasColumnName("FILESIZE").HasDefaultValue(0);
            builder.Property(d => d.DownloadTimes).HasColumnName("DOWNLOADTIMES").HasDefaultValue(0);

            builder.Property(p => p.ExtraProperties).HasColumnName("EXTRAPROPERTIES");
            builder.Property(p => p.ConcurrencyStamp).HasColumnName("CONCURRENCYSTAMP");
            builder.Property(p => p.CreationTime).HasColumnName("CREATIONTIME").HasColumnType("timestamp without time zone");
            builder.Property(p => p.CreatorId).HasColumnName("CREATORID");
            builder.Property(p => p.LastModificationTime).HasColumnName("LASTMODIFICATIONTIME").HasColumnType("timestamp without time zone");
            builder.Property(p => p.LastModifierId).HasColumnName("LASTMODIFIERID");
            builder.Property(p => p.IsDeleted).HasColumnName("ISDELETED");
            builder.Property(p => p.DeleterId).HasColumnName("DELETERID");
            builder.Property(p => p.DeletionTime).HasColumnName("DELETIONTIME").HasColumnType("timestamp without time zone");
        }
    }
}