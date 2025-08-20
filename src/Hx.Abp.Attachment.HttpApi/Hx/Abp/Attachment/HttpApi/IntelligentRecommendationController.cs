using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 智能推荐控制器
    /// 提供基于语义匹配的智能推荐功能
    /// </summary>
    [ApiController]
    [Route("api/app/intelligent-recommendation")]
    public class IntelligentRecommendationController(IIntelligentRecommendationAppService intelligentRecommendationAppService) : AbpControllerBase
    {
        private readonly IIntelligentRecommendationAppService _intelligentRecommendationAppService = intelligentRecommendationAppService;

        /// <summary>
        /// 智能推荐模板
        /// </summary>
        /// <param name="input">推荐输入参数</param>
        /// <returns>推荐的模板列表</returns>
        [HttpPost("recommend-templates")]
        public async Task<IntelligentRecommendationResultDto> RecommendTemplatesAsync([FromBody] IntelligentRecommendationInputDto input)
        {
            return await _intelligentRecommendationAppService.RecommendTemplatesAsync(input);
        }

        /// <summary>
        /// 基于现有模板生成新模板
        /// </summary>
        /// <param name="input">模板生成输入参数</param>
        /// <returns>生成的新模板</returns>
        [HttpPost("generate-template-from-existing")]
        public async Task<AttachCatalogueTemplateDto> GenerateTemplateFromExistingAsync([FromBody] GenerateTemplateFromExistingInputDto input)
        {
            return await _intelligentRecommendationAppService.GenerateTemplateFromExistingAsync(input);
        }

        /// <summary>
        /// 智能分类推荐
        /// </summary>
        /// <param name="input">分类推荐输入参数</param>
        /// <returns>推荐的分类结构</returns>
        [HttpPost("recommend-catalogue-structure")]
        public async Task<IntelligentCatalogueRecommendationDto> RecommendCatalogueStructureAsync([FromBody] IntelligentCatalogueRecommendationInputDto input)
        {
            return await _intelligentRecommendationAppService.RecommendCatalogueStructureAsync(input);
        }

        /// <summary>
        /// 批量智能推荐
        /// </summary>
        /// <param name="input">批量推荐输入参数</param>
        /// <returns>批量推荐结果</returns>
        [HttpPost("batch-recommend")]
        public async Task<BatchIntelligentRecommendationResultDto> BatchRecommendAsync([FromBody] BatchIntelligentRecommendationInputDto input)
        {
            return await _intelligentRecommendationAppService.BatchRecommendAsync(input);
        }

        /// <summary>
        /// 学习用户偏好
        /// </summary>
        /// <param name="input">用户偏好学习输入</param>
        /// <returns>学习结果</returns>
        [HttpPost("learn-user-preference")]
        public async Task<UserPreferenceLearningResultDto> LearnUserPreferenceAsync([FromBody] UserPreferenceLearningInputDto input)
        {
            return await _intelligentRecommendationAppService.LearnUserPreferenceAsync(input);
        }

        /// <summary>
        /// 获取推荐统计信息
        /// </summary>
        /// <returns>推荐统计信息</returns>
        [HttpGet("statistics")]
        public async Task<RecommendationStatisticsDto> GetRecommendationStatisticsAsync()
        {
            return await _intelligentRecommendationAppService.GetRecommendationStatisticsAsync();
        }

        /// <summary>
        /// 快速推荐模板（简化版）
        /// </summary>
        /// <param name="query">查询文本</param>
        /// <param name="topN">返回数量</param>
        /// <returns>推荐的模板列表</returns>
        [HttpGet("quick-recommend")]
        public async Task<IntelligentRecommendationResultDto> QuickRecommendAsync([FromQuery] string query, [FromQuery] int topN = 5)
        {
            var input = new IntelligentRecommendationInputDto
            {
                Query = query,
                TopN = topN,
                Threshold = 0.3
            };

            return await _intelligentRecommendationAppService.RecommendTemplatesAsync(input);
        }

        /// <summary>
        /// 基于业务场景推荐
        /// </summary>
        /// <param name="businessDescription">业务描述</param>
        /// <param name="fileTypes">文件类型列表</param>
        /// <returns>推荐的分类结构</returns>
        [HttpGet("recommend-by-business")]
        public async Task<IntelligentCatalogueRecommendationDto> RecommendByBusinessAsync(
            [FromQuery] string businessDescription, 
            [FromQuery] string fileTypes = "")
        {
            var input = new IntelligentCatalogueRecommendationInputDto
            {
                BusinessDescription = businessDescription,
                FileTypes = string.IsNullOrWhiteSpace(fileTypes) ? [] : [.. fileTypes.Split(',')],
                Reference = "BusinessRecommendation",
                ReferenceType = 1,
                ExpectedLevels = 3,
                IncludeRequired = true
            };

            return await _intelligentRecommendationAppService.RecommendCatalogueStructureAsync(input);
        }
    }
}
