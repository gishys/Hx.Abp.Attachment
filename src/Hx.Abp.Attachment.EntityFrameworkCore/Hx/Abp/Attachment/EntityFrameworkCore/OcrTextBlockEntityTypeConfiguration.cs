using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class OcrTextBlockEntityTypeConfiguration : IEntityTypeConfiguration<OcrTextBlock>
    {
        public void Configure(EntityTypeBuilder<OcrTextBlock> builder)
        {
            builder.ConfigureByConvention();
            builder.ToTable(BgAppConsts.DbTablePrefix + "ATTACH_OCR_TEXT_BLOCKS", BgAppConsts.DbSchema);
            
            // 主键配置
            builder.HasKey(d => d.Id).HasName("PK_APPATTACH_OCR_TEXT_BLOCKS");

            // 基础字段配置
            builder.Property(d => d.Id).HasColumnName("ID");
            builder.Property(d => d.AttachFileId).HasColumnName("ATTACH_FILE_ID").IsRequired();
            builder.Property(d => d.Text).HasColumnName("TEXT").HasMaxLength(4000).IsRequired();
            builder.Property(d => d.Probability).HasColumnName("PROBABILITY").HasPrecision(5, 4).IsRequired();
            builder.Property(d => d.PageIndex).HasColumnName("PAGE_INDEX").IsRequired();
            builder.Property(d => d.PositionData).HasColumnName("POSITION_DATA").HasMaxLength(1000);
            builder.Property(d => d.BlockOrder).HasColumnName("BLOCK_ORDER").IsRequired();
            builder.Property(p => p.CreationTime).HasColumnName("CREATION_TIME");

            builder.Property(x => x.Probability)
                .HasPrecision(5, 4)
                .IsRequired();

            builder.Property(x => x.PageIndex)
                .IsRequired();

            builder.Property(x => x.PositionData)
                .HasMaxLength(1000);

            builder.Property(x => x.BlockOrder)
                .IsRequired();

            // 索引配置
            builder.HasIndex(x => x.AttachFileId)
                .HasDatabaseName("IX_APPATTACH_OCR_TEXT_BLOCKS_ATTACH_FILE_ID");
            builder.HasIndex(x => x.Text)
                .HasDatabaseName("IX_APPATTACH_OCR_TEXT_BLOCKS_TEXT");
            builder.HasIndex(x => x.Probability)
                .HasDatabaseName("IX_APPATTACH_OCR_TEXT_BLOCKS_PROBABILITY");
            builder.HasIndex(x => new { x.AttachFileId, x.PageIndex, x.BlockOrder })
                .HasDatabaseName("IX_APPATTACH_OCR_TEXT_BLOCKS_FILE_PAGE_ORDER");

            // 外键关系
            builder.HasOne<AttachFile>()
                .WithMany(x => x.OcrTextBlocks)
                .HasForeignKey(x => x.AttachFileId)
                .HasConstraintName("FK_APPATTACH_OCR_TEXT_BLOCKS_ATTACH_FILE_ID")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
