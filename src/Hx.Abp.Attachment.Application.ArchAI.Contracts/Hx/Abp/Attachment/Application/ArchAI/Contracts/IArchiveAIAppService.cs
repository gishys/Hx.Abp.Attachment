using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    public interface IArchiveAIAppService : IApplicationService
    {
        Task<List<RecognizeCharacterDto>> OcrFullTextAsync(List<Guid> ids);
        
        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">文本分析输入参数</param>
        /// <returns>文本分析结果</returns>
        Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input);

        /// <summary>
        /// 提取文本分类特征
        /// </summary>
        /// <param name="input">文本分类输入参数</param>
        /// <returns>文本分类特征结果</returns>
        Task<TextAnalysisDto> ExtractClassificationFeaturesAsync(TextClassificationInputDto input);
    }
}
