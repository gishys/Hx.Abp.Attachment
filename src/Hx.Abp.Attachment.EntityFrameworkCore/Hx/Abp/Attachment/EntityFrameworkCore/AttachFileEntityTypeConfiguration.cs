using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachFileEntityTypeConfiguration
        : IEntityTypeConfiguration<AttachFile>
    {
        public void Configure(EntityTypeBuilder<AttachFile> builder)
        {
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
        }
    }
}