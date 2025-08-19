using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public static class AttachmentDbContextModelBuilderExtensions
    {
        public static void ConfigureAttachment([NotNull] this ModelBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));
            builder.ApplyConfigurationsFromAssembly(typeof(AttachCatalogueEntityTypeConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(AttachFileEntityTypeConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(OcrTextBlockEntityTypeConfiguration).Assembly);
        }
    }
}
