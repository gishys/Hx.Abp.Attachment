using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 智能文档分析控制器 - 提供OCR识别、文档分析、分类推荐等功能
    /// </summary>
    [ApiController]
    [Route("api/app/intelligent-document-analysis")]
    public class IntelligentDocumentAnalysisController(IArchiveAIAppService archiveAIAppService) : AbpControllerBase
    {
        private readonly IArchiveAIAppService _archiveAIAppService = archiveAIAppService;
        
        /// <summary>
        /// 获取OCR全文识别结果
        /// </summary>
        /// <param name="input">文件ID列表</param>
        /// <returns>OCR识别结果列表</returns>
        [HttpPost]
        [Route("ocr-recognition")]
        public Task<List<RecognizeCharacterDto>> GetOcrRecognitionAsync([FromBody] GetOcrFullTextInput input)
        {
            return _archiveAIAppService.OcrFullTextAsync(input.Ids);
        }

        /// <summary>
        /// 文档智能分析 - 生成摘要和关键词
        /// </summary>
        /// <param name="input">文档分析输入参数</param>
        /// <returns>文档分析结果</returns>
        [HttpPost]
        [Route("document-analysis")]
        public async Task<TextAnalysisDto> AnalyzeDocumentAsync([FromBody] TextAnalysisInputDto input)
        {
            return await _archiveAIAppService.AnalyzeDocumentAsync(input);
        }

        /// <summary>
        /// 提取文档分类特征
        /// </summary>
        /// <param name="input">分类特征提取输入参数</param>
        /// <returns>分类特征分析结果</returns>
        [HttpPost]
        [Route("classification-features")]
        public async Task<TextAnalysisDto> ExtractClassificationFeaturesAsync([FromBody] TextClassificationInputDto input)
        {
            return await _archiveAIAppService.ExtractClassificationFeaturesAsync(input);
        }

        /// <summary>
        /// 智能推荐文档分类
        /// </summary>
        /// <param name="request">分类推荐请求</param>
        /// <returns>分类推荐结果</returns>
        [HttpPost]
        [Route("category-recommendation")]
        public async Task<ClassificationResult> RecommendDocumentCategoryAsync([FromBody] CategoryRecommendationRequest request)
        {
            return await _archiveAIAppService.RecommendDocumentCategoryAsync(request.Content, request.CategoryOptions);
        }

        /// <summary>
        /// 全栈文档分析 - 同时进行摘要、关键词提取和分类推荐
        /// </summary>
        /// <param name="request">全栈分析请求</param>
        /// <returns>全栈分析结果</returns>
        [HttpPost]
        [Route("comprehensive-analysis")]
        public async Task<ComprehensiveAnalysisResult> AnalyzeDocumentComprehensivelyAsync([FromBody] ComprehensiveAnalysisRequest request)
        {
            return await _archiveAIAppService.AnalyzeDocumentComprehensivelyAsync(
                request.Content, 
                request.CategoryOptions, 
                request.MaxSummaryLength, 
                request.KeywordCount);
        }

        /// <summary>
        /// 批量文档分析
        /// </summary>
        /// <param name="request">批量分析请求</param>
        /// <returns>批量分析结果</returns>
        [HttpPost]
        [Route("batch-analysis")]
        public async Task<List<TextAnalysisDto>> BatchAnalyzeDocumentsAsync([FromBody] BatchAnalysisRequest request)
        {
            return await _archiveAIAppService.BatchAnalyzeDocumentsAsync(
                request.Documents, 
                request.MaxSummaryLength, 
                request.KeywordCount);
        }
    }

    #region 请求模型

    /// <summary>
    /// 分类推荐请求模型
    /// </summary>
    public class CategoryRecommendationRequest
    {
        /// <summary>
        /// 文档内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 可选分类列表
        /// </summary>
        public List<string> CategoryOptions { get; set; } = [];
    }

    /// <summary>
    /// 全栈分析请求模型
    /// </summary>
    public class ComprehensiveAnalysisRequest
    {
        /// <summary>
        /// 文档内容
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 可选分类列表
        /// </summary>
        public List<string> CategoryOptions { get; set; } = [];

        /// <summary>
        /// 摘要最大长度
        /// </summary>
        public int MaxSummaryLength { get; set; } = 500;

        /// <summary>
        /// 关键词数量
        /// </summary>
        public int KeywordCount { get; set; } = 5;
    }

    /// <summary>
    /// 批量分析请求模型
    /// </summary>
    public class BatchAnalysisRequest
    {
        /// <summary>
        /// 文档列表
        /// </summary>
        public List<string> Documents { get; set; } = [];

        /// <summary>
        /// 摘要最大长度
        /// </summary>
        public int MaxSummaryLength { get; set; } = 500;

        /// <summary>
        /// 关键词数量
        /// </summary>
        public int KeywordCount { get; set; } = 5;
    }

    #endregion
}
