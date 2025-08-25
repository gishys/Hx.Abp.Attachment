using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 文本分析控制器
    /// </summary>
    [Route("api/text-analysis")]
    public class TextAnalysisController(IArchiveAIAppService archiveAIAppService) : AbpController
    {
        private readonly IArchiveAIAppService _archiveAIAppService = archiveAIAppService;

        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">文本分析输入参数</param>
        /// <returns>文本分析结果</returns>
        [HttpPost("analyze")]
        public async Task<ActionResult<TextAnalysisDto>> AnalyzeText([FromBody] TextAnalysisInputDto input)
        {
            try
            {
                var result = await _archiveAIAppService.AnalyzeTextAsync(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
