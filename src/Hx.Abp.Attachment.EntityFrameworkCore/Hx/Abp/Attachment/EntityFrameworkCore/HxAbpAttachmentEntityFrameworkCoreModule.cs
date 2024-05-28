using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.Users.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    [DependsOn(
        typeof(HxAbpAttachmentDomainModule),
    typeof(AbpUsersEntityFrameworkCoreModule))]
    public class HxAbpAttachmentEntityFrameworkCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpDbContext<AttachmentDbContext>(options =>
            {
                options.AddRepository<AttachCatalogue, EfCoreAttachCatalogueRepository>();

                options.AddDefaultRepositories(includeAllEntities: true);
            });
        }
    }
}
