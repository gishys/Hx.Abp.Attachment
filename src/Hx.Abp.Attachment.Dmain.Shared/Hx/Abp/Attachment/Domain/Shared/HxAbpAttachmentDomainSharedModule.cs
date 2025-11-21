using Volo.Abp.Localization;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 附件模块Domain.Shared模块
    /// </summary>
    [DependsOn(typeof(AbpLocalizationModule))]
    public class HxAbpAttachmentDomainSharedModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<AttachmentLocalizationResource>("zh-CN")
                    .AddVirtualJson("/Hx/Abp/Attachment/Domain/Shared/Localization");

                options.Resources
                    .Add<AttachmentLocalizationResource>("en")
                    .AddVirtualJson("/Hx/Abp/Attachment/Domain/Shared/Localization");

                // 设置默认语言
                options.DefaultResourceType = typeof(AttachmentLocalizationResource);
            });
        }
    }
}

