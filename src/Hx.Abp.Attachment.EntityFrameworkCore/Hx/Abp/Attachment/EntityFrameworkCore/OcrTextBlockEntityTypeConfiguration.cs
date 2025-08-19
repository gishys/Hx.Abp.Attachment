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
            builder.ToTable(BgAppConsts.DbTablePrefix + "OcrTextBlocks", BgAppConsts.DbSchema);
            builder.ConfigureByConvention();

            builder.Property(x => x.AttachFileId)
                .IsRequired();

            builder.Property(x => x.Text)
                .HasMaxLength(4000)
                .IsRequired();

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
            builder.HasIndex(x => x.AttachFileId);
            builder.HasIndex(x => x.Text);
            builder.HasIndex(x => x.Probability);
            builder.HasIndex(x => new { x.AttachFileId, x.PageIndex, x.BlockOrder });

            // 外键关系
            builder.HasOne<AttachFile>()
                .WithMany(x => x.OcrTextBlocks)
                .HasForeignKey(x => x.AttachFileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
