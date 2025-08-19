using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Configuration;
using OcrTextComposer;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    public class ArchiveAIAppService(IConfiguration configuration, IEfCoreAttachFileRepository efCoreAttachFileRepository) : ApplicationService, IArchiveAIAppService
    {
        private readonly IConfiguration Configuration = configuration;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        public async Task<List<RecognizeCharacterDto>> OcrFullTextAsync(List<Guid> ids)
        {
            var result = new List<RecognizeCharacterDto>();
            var files = await EfCoreAttachFileRepository.GetListByIdsAsync(ids);
            foreach (var file in files)
            {
                var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                var apiKey = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID") ?? throw new UserFriendlyException(message: "缺少环境变量“ALIBABA_CLOUD_ACCESS_KEY_ID”！");
                var secret = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET") ?? throw new UserFriendlyException(message: "缺少环境变量“ALIBABA_CLOUD_ACCESS_KEY_SECRET”！");
                var fullText = await UniversalTextRecognitionHelper.JpgUniversalTextRecognition(apiKey, secret, src);
                fullText.FileId = file.Id.ToString();
                fullText.Text = OcrComposer.Compose(fullText);
                result.Add(fullText);
            }
            return result;
        }
    }
}
