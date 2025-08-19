using Hx.Abp.Attachment.Api;
using Hx.Abp.Attachment.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();
builder.Services.ReplaceConfiguration(builder.Configuration);

builder.Services.AddApplication<AppModule>();

var app = builder.Build();

// 初始化全文搜索配置
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AttachmentDbContext>();
    try
    {
        await dbContext.InitializeFullTextSearchAsync();
        await dbContext.CreateFullTextSearchIndexesAsync();
        Console.WriteLine("全文搜索配置初始化成功");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"全文搜索配置初始化失败: {ex.Message}");
    }
}

app.InitializeApplication();

app.Run();
