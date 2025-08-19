using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.Api.Controllers
{
    [ApiController]
    [Route("api/app/attachmentai")]
    public class ArchiveAIController(IArchiveAIAppService archiveAIAppService) : AbpControllerBase
    {
        private readonly IArchiveAIAppService _archiveAIAppService = archiveAIAppService;
        [HttpGet]
        [Route("ocrfulltext")]
        public Task<List<RecognizeCharacterDto>> GetOcrFullTextAsync([FromBody] GetOcrFullTextInput input)
        {
            return _archiveAIAppService.OcrFullTextAsync(input.Ids);
        }
    }
}
