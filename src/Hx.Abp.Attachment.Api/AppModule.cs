using Hx.Abp.Attachment.Application;
using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.EntityFrameworkCore;
using Hx.Abp.Attachment.HttpApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
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
    [DependsOn(typeof(HxAbpAttachmentHttpApiModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationModule))]
    [DependsOn(typeof(HxAbpAttachmentApplicationArchAIModule))]
    [DependsOn(typeof(AbpEntityFrameworkCorePostgreSqlModule))]
    [DependsOn(typeof(HxAbpAttachmentEntityFrameworkCoreModule))]
    public class AppModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpSwaggerGen(
                options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "BgApp API", Version = "v1" });
                    options.DocInclusionPredicate((docName, description) => true);
                    options.CustomSchemaIds(type => type.FullName);
                });
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

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
                app.UseExceptionHandler("/Error");
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
