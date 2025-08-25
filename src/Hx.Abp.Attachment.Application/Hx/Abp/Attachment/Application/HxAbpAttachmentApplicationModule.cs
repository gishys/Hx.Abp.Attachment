﻿using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Application.Utils;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Application
{
    [DependsOn(typeof(AbpAutoMapperModule))]
    [DependsOn(typeof(HxAbpAttachmentDomainModule))]
    [DependsOn(typeof(AbpBlobStoringModule))]
    [DependsOn(typeof(AbpBlobStoringFileSystemModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationContractsModule))]
    public class HxAbpAttachmentApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAutoMapperObjectMapper<HxAbpAttachmentApplicationModule>();

            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddProfile<AttachmentAutoMapperProfile>(validate: true);
            });
            Configure<AbpBlobStoringOptions>(options =>
            {
                options.Containers.Configure<AttachmentContainer>(container =>
                {
                    container.IsMultiTenant = false;
                });
            });

            // 注册OCR服务
            context.Services.AddScoped<IOcrService, OcrService>();
            
            // 注册全文搜索应用服务
            context.Services.AddScoped<IFullTextSearchAppService, FullTextSearchAppService>();
            
            // 注册跨平台PDF转图片工具
            context.Services.AddScoped<CrossPlatformPdfToImageConverter>();
            
            // 注册模板使用统计服务
            context.Services.AddScoped<ITemplateUsageStatsAppService, TemplateUsageStatsAppService>();
        }
    }
}