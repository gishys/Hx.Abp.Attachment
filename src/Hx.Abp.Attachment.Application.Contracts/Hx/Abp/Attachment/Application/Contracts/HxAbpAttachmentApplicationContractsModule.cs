using Volo.Abp.Application;
using Volo.Abp.Authorization;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application.Contracts
{
    [DependsOn(
        typeof(AbpAuthorizationModule),
        typeof(AbpDddApplicationModule)
        )]
    public class HxAbpAttachmentApplicationContractsModule:AbpModule
    {
    }
}
