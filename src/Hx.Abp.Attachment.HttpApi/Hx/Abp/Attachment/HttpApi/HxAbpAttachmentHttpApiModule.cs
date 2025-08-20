using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.HttpApi
{
    [DependsOn(typeof(AbpAspNetCoreMvcModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationContractsModule))]
    public class HxAbpAttachmentHttpApiModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            PreConfigure(delegate (IMvcBuilder mvcBuilder)
            {
                mvcBuilder.AddApplicationPartIfNotExists(typeof(HxAbpAttachmentHttpApiModule).Assembly);
            });
        }
    }
}
