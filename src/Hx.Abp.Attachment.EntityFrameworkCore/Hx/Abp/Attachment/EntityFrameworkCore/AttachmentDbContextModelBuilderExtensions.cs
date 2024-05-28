using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
