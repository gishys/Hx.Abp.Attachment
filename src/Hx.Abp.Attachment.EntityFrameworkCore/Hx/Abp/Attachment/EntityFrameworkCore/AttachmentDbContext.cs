﻿using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class AttachmentDbContext : AbpDbContext<AttachmentDbContext>
    {
        public DbSet<AttachCatalogue> Users { get; set; }
        public AttachmentDbContext(DbContextOptions<AttachmentDbContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ConfigureAttachment();
        }
    }
}
