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

        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">文本分析输入参数</param>
        /// <returns>文本分析结果</returns>
        [HttpPost]
        [Route("analyze-text")]
        public async Task<TextAnalysisDto> AnalyzeTextAsync([FromBody] TextAnalysisInputDto input)
        {
            return await _archiveAIAppService.AnalyzeTextAsync(input);
        }

        /// <summary>
        /// 提取文本分类特征
        /// </summary>
        /// <param name="input">文本分类输入参数</param>
        /// <returns>文本分类特征结果</returns>
        [HttpPost]
        [Route("extract-classification-features")]
        public async Task<TextAnalysisDto> ExtractClassificationFeaturesAsync([FromBody] TextClassificationInputDto input)
        {
            return await _archiveAIAppService.ExtractClassificationFeaturesAsync(input);
        }
    }
}
