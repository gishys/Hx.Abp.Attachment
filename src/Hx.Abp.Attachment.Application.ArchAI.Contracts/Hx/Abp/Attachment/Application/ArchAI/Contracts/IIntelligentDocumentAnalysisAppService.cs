using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    public interface IIntelligentDocumentAnalysisAppService : IApplicationService
    {
        /// <summary>
        /// 获取OCR全文识别结果
        /// </summary>
        /// <param name="ids">文件ID列表</param>
        /// <returns>OCR识别结果列表</returns>
        Task<List<RecognizeCharacterDto>> OcrFullTextAsync(List<Guid> ids);
        
        /// <summary>
        /// 文档智能分析 - 生成摘要和关键词
        /// </summary>
        /// <param name="input">文档分析输入参数</param>
        /// <returns>文档分析结果</returns>
        Task<TextAnalysisDto> AnalyzeDocumentAsync(TextAnalysisInputDto input);

        /// <summary>
        /// 提取文档分类特征
        /// </summary>
        /// <param name="input">分类特征提取输入参数</param>
        /// <returns>分类特征分析结果</returns>
        Task<TextAnalysisDto> ExtractClassificationFeaturesAsync(TextClassificationInputDto input);

        /// <summary>
        /// 智能推荐文档分类
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>分类推荐结果</returns>
        Task<ClassificationResult> RecommendDocumentCategoryAsync(string content, List<string> categoryOptions);

        /// <summary>
        /// 全栈文档分析 - 同时进行摘要、关键词提取和分类推荐
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <param name="maxSummaryLength">摘要最大长度</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>全栈分析结果</returns>
        Task<ComprehensiveAnalysisResult> AnalyzeDocumentComprehensivelyAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5);

        /// <summary>
        /// 批量文档分析
        /// </summary>
        /// <param name="documents">文档列表</param>
        /// <param name="maxSummaryLength">摘要最大长度</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>批量分析结果</returns>
        Task<List<TextAnalysisDto>> BatchAnalyzeDocumentsAsync(
            List<string> documents, 
            int maxSummaryLength = 500, 
            int keywordCount = 5);
    }
}
