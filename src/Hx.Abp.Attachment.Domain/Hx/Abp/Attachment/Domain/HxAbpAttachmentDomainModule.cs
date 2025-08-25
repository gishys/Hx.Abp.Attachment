using Microsoft.Extensions.DependencyInjection;
using RulesEngine;
using RulesEngine.Interfaces;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Domain
{
    [DependsOn(
        typeof(AbpDddDomainModule)
        )]
    public class HxAbpAttachmentDomainModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册 RulesEngine 服务，明确指定构造函数
            context.Services.AddScoped<IRulesEngine>(serviceProvider =>
            {
                // 使用无参构造函数创建 RulesEngine 实例
                return new RulesEngine.RulesEngine();
            });
        }
    }
}
