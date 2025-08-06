using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Configuration;
using Volo.Abp;

namespace Hx.Abp.Attachment.Application
{
    public class ArchiveAIAppService(IConfiguration configuration, IEfCoreAttachFileRepository efCoreAttachFileRepository) : AttachmentService, IArchiveAIAppService
    {
        private readonly IConfiguration Configuration = configuration;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        public async Task OcrFullTextAsync(List<Guid> ids)
        {
            var files = await EfCoreAttachFileRepository.GetListByIdsAsync(ids);
            foreach (var file in files)
            {
                var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                var apiKey = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID") ?? throw new UserFriendlyException(message: "没有添加环境变量“ALIBABA_CLOUD_ACCESS_KEY_ID”！");
                var secret = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET") ?? throw new UserFriendlyException(message: "没有添加环境变量“ALIBABA_CLOUD_ACCESS_KEY_SECRET”！");
                UniversalTextRecognitionHelper.JpgUniversalTextRecognition(apiKey, secret, src);
            }
        }
    }
}
