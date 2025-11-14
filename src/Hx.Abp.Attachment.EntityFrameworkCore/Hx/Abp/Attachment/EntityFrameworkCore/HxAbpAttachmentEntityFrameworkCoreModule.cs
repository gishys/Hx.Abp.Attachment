using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Volo.Abp.Users.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    [DependsOn(
        typeof(HxAbpAttachmentDomainModule),
    typeof(AbpUsersEntityFrameworkCoreModule))]
    public class HxAbpAttachmentEntityFrameworkCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpDbContext<AttachmentDbContext>(options =>
            {
                options.AddRepository<AttachCatalogue, EfCoreAttachCatalogueRepository>();
                options.AddRepository<AttachFile, EfCoreAttachFileRepository>();
                options.AddRepository<MetaFieldPreset, EfCoreMetaFieldPresetRepository>();

                options.AddDefaultRepositories(includeAllEntities: true);

                // 注册自定义仓储
                options.AddRepository<OcrTextBlock, OcrTextBlockRepository>();
            });

            // 注册全文搜索仓储
            context.Services.AddScoped<IFullTextSearchRepository, FullTextSearchRepository>();

            // 配置 Npgsql 时区处理
            ConfigureNpgsqlDateTimeHandling();
        }

        private static void ConfigureNpgsqlDateTimeHandling()
        {
            // 启用传统时间戳行为，这样可以避免时区问题
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", false);
        }
    }
}
