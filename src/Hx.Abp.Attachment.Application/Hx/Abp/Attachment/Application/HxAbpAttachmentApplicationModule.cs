using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application
{
    [DependsOn(
    typeof(AbpAutoMapperModule)
    )]
    public class HxAbpAttachmentApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAutoMapperObjectMapper<HxAbpAttachmentApplicationModule>();

            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddProfile<AttachmentAutoMapperProfile>(validate: true);
            });
        }
    }
}
