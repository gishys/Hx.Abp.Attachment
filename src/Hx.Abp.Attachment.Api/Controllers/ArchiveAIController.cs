using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.Api.Controllers
{
    [ApiController]
    [Route("api/app/attachment")]
    public class ArchiveAIController(IArchiveAIAppService archiveAIAppService) : AbpControllerBase
    {
        private readonly IArchiveAIAppService _archiveAIAppService = archiveAIAppService;
        public Task GetOcrFullTextAsync([FromBody] GetOcrFullTextInput input)
        {
            return _archiveAIAppService.OcrFullTextAsync(input.Ids);
        }
    }
}
