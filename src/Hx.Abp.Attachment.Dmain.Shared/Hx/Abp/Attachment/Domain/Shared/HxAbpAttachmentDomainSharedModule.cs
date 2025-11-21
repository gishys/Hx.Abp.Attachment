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
                // 注册本地化资源（只注册一次）
                options.Resources
                    .Add<AttachmentLocalizationResource>()
                    .AddVirtualJson("/Hx/Abp/Attachment/Domain/Shared/Localization");

                // 设置默认语言
                options.DefaultResourceType = typeof(AttachmentLocalizationResource);
            });
        }
    }
}

