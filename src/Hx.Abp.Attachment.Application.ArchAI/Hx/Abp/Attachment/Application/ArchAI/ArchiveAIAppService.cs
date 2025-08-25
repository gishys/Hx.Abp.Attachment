using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Configuration;
using OcrTextComposer;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    public class ArchiveAIAppService(
        IConfiguration configuration,
        IEfCoreAttachFileRepository efCoreAttachFileRepository,
        TextAnalysisService textAnalysisService) : ApplicationService, IArchiveAIAppService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IEfCoreAttachFileRepository _efCoreAttachFileRepository = efCoreAttachFileRepository;
        private readonly TextAnalysisService _textAnalysisService = textAnalysisService;

        public async Task<List<RecognizeCharacterDto>> OcrFullTextAsync(List<Guid> ids)
        {
            var result = new List<RecognizeCharacterDto>();
            var files = await _efCoreAttachFileRepository.GetListByIdsAsync(ids);
            foreach (var file in files)
            {
                var src = $"{_configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                var apiKey = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_ID") ?? throw new UserFriendlyException(message: "缺少环境变量\"ALIBABA_CLOUD_ACCESS_KEY_ID\"！");
                var secret = Environment.GetEnvironmentVariable("ALIBABA_CLOUD_ACCESS_KEY_SECRET") ?? throw new UserFriendlyException(message: "缺少环境变量\"ALIBABA_CLOUD_ACCESS_KEY_SECRET\"！");
                var fullText = await UniversalTextRecognitionHelper.JpgUniversalTextRecognition(apiKey, secret, src);
                fullText.FileId = file.Id.ToString();
                fullText.Text = OcrComposer.Compose(fullText);
                result.Add(fullText);
            }
            return result;
        }

        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            return await _textAnalysisService.AnalyzeTextAsync(input);
        }
    }
}
