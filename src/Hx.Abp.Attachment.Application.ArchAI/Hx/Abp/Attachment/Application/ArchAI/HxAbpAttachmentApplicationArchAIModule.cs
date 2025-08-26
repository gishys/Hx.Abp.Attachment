using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    [DependsOn(typeof(AbpDddApplicationModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationArchAIContractsModule))]
    public class HxAbpAttachmentApplicationArchAIModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            // 注册 HttpClient
            context.Services.AddHttpClient();
            
            // 注册 TextAnalysisService
            context.Services.AddScoped<TextAnalysisService>();
            
            // 注册 TextClassificationService
            context.Services.AddScoped<TextClassificationService>();
            
            // 注册 SemanticVectorService
            context.Services.AddScoped<SemanticVectorService>();
        }
    }
}
