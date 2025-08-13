using Hx.Abp.Attachment.Application;
using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Modularity;

namespace Hx.Abp.Attachment.Api
{
    [DependsOn(typeof(AbpAutofacModule))]
    [DependsOn(typeof(AbpAspNetCoreMvcModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationArchAIModule))]
    [DependsOn(typeof(AbpEntityFrameworkCorePostgreSqlModule))]
    [DependsOn(typeof(HxAbpAttachmentEntityFrameworkCoreModule))]
    public class AppModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpBlobStoringOptions>(options =>
            {
                options.Containers.Configure<AttachmentContainer>(container =>
                {
                    container.UseFileSystem(fileSystem =>
                    {
                        fileSystem.BasePath = "C:\\my-files";
                    });
                });
                options.Containers.ConfigureDefault(container =>
                {
                    container.UseFileSystem(fileSystem =>
                    {
                        fileSystem.BasePath = "C:\\my-files";
                    });
                });
            });
            Configure<AbpDbContextOptions>(options =>
            {
                /* The main point to change your DBMS.
                 * See also BgAppMigrationsDbContextFactory for EF Core tooling. */
                options.UseNpgsql(options =>
                {
                    //options.UseNetTopologySuite();
                });
            });
        }
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            var env = context.GetEnvironment();

            // Configure the HTTP request pipeline.
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider("C:\\my-files"),
                RequestPath = "",
                OnPrepareResponse = (c) =>
                {
                    c.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                }
            });
            app.UseRouting();
            app.UseConfiguredEndpoints();
        }
    }
}
