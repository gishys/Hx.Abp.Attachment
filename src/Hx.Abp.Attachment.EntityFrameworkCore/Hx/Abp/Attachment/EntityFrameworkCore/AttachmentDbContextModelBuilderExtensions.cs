using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public static class AttachmentDbContextModelBuilderExtensions
    {
        public static void ConfigureAttachment([NotNull] this ModelBuilder builder)
        {
            Check.NotNull(builder, nameof(builder));
            builder.ApplyConfigurationsFromAssembly(typeof(AttachCatalogueEntityTypeConfiguration).Assembly);
            builder.ApplyConfigurationsFromAssembly(typeof(AttachFileEntityTypeConfiguration).Assembly);
        }
    }
}
