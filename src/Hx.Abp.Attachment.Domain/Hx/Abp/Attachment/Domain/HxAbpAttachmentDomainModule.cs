using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Domain
{
    [DependsOn(
        typeof(AbpDddDomainModule)
        )]
    public class HxAbpAttachmentDomainModule : AbpModule
    {

    }
}
