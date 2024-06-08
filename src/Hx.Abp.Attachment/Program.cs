
using Microsoft.AspNetCore.Hosting;

namespace Hx.Abp.Attachment
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(options =>
            {
            });
            builder.Host.UseAutofac();

            builder.Services.ReplaceConfiguration(builder.Configuration);

            builder.Services.AddApplication<AppModule>();

            var app = builder.Build();

            app.InitializeApplication();

            app.Run();
        }
    }
}
