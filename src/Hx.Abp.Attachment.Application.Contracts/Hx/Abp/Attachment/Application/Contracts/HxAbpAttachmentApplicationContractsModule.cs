using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application;
using Volo.Abp.Authorization;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application.Contracts
{
    [DependsOn(
        typeof(AbpAuthorizationModule),
        typeof(AbpDddApplicationModule),
        typeof(HxAbpAttachmentDomainSharedModule)
        )]
    public class HxAbpAttachmentApplicationContractsModule : AbpModule
    {
    }
}
