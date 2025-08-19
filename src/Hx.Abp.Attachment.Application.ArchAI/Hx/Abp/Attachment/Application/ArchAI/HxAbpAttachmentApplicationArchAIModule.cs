using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    [DependsOn(typeof(AbpDddApplicationModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationArchAIContractsModule))]
    public class HxAbpAttachmentApplicationArchAIModule : AbpModule
    {
    }
}
