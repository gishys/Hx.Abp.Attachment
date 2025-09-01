using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OcrTextComposer;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    public class IntelligentDocumentAnalysisAppService(
        IConfiguration configuration,
        IEfCoreAttachFileRepository efCoreAttachFileRepository,
        AIServiceFactory aiServiceFactory,
        ILogger<IntelligentDocumentAnalysisAppService> logger) : ApplicationService, IIntelligentDocumentAnalysisAppService
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IEfCoreAttachFileRepository _efCoreAttachFileRepository = efCoreAttachFileRepository;
        private readonly AIServiceFactory _aiServiceFactory = aiServiceFactory;
        private readonly ILogger<IntelligentDocumentAnalysisAppService> _logger = logger;

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

        public async Task<TextAnalysisDto> AnalyzeDocumentAsync(TextAnalysisInputDto input)
        {
            try
            {
                // 使用新的文档智能分析服务
                var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
                return await documentAnalysisService.AnalyzeDocumentAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档分析失败");
                throw new UserFriendlyException("文档分析服务暂时不可用，请稍后再试");
            }
        }

        public async Task<TextAnalysisDto> ExtractClassificationFeaturesAsync(TextClassificationInputDto input)
        {
            try
            {
                // 使用新的文档智能分析服务处理分类特征提取
                var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
                
                var analysisInput = new TextAnalysisInputDto
                {
                    Text = input.TextSamples.Count > 0 ? string.Join("\n", input.TextSamples) : "",
                    MaxSummaryLength = input.MaxSummaryLength,
                    KeywordCount = input.KeywordCount,
                    GenerateSemanticVector = input.GenerateSemanticVector,
                    ExtractEntities = false
                };

                return await documentAnalysisService.AnalyzeDocumentAsync(analysisInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分类特征提取失败");
                throw new UserFriendlyException("分类特征提取服务暂时不可用，请稍后再试");
            }
        }

        public async Task<ClassificationResult> RecommendDocumentCategoryAsync(string content, List<string> categoryOptions)
        {
            try
            {
                var classificationService = _aiServiceFactory.GetIntelligentClassificationService();
                return await classificationService.RecommendDocumentCategoryAsync(content, categoryOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分类推荐失败");
                throw new UserFriendlyException("分类推荐服务暂时不可用，请稍后再试");
            }
        }

        public async Task<ComprehensiveAnalysisResult> AnalyzeDocumentComprehensivelyAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5)
        {
            try
            {
                var fullStackService = _aiServiceFactory.GetFullStackAnalysisService();
                return await fullStackService.AnalyzeDocumentComprehensivelyAsync(
                    content, categoryOptions, maxSummaryLength, keywordCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全栈分析失败");
                throw new UserFriendlyException("全栈分析服务暂时不可用，请稍后再试");
            }
        }

        public async Task<List<TextAnalysisDto>> BatchAnalyzeDocumentsAsync(
            List<string> documents, 
            int maxSummaryLength = 500, 
            int keywordCount = 5)
        {
            try
            {
                var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
                var results = new List<TextAnalysisDto>();

                foreach (var document in documents)
                {
                    var input = new TextAnalysisInputDto
                    {
                        Text = document,
                        MaxSummaryLength = maxSummaryLength,
                        KeywordCount = keywordCount
                    };

                    var result = await documentAnalysisService.AnalyzeDocumentAsync(input);
                    results.Add(result);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量文档分析失败");
                throw new UserFriendlyException("批量文档分析服务暂时不可用，请稍后再试");
            }
        }

        public async Task<EntityRecognitionResultDto> RecognizeEntitiesAsync(EntityRecognitionInputDto input)
        {
            try
            {
                var entityRecognitionService = _aiServiceFactory.GetEntityRecognitionService();
                return await entityRecognitionService.RecognizeEntitiesAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实体识别失败");
                throw new UserFriendlyException("实体识别服务暂时不可用，请稍后再试");
            }
        }

        public async Task<CategoryNameRecommendationResultDto> RecommendCategoryNamesAsync(CategoryNameRecommendationInputDto input)
        {
            try
            {
                var categoryRecommendationService = _aiServiceFactory.GetCategoryNameRecommendationService();
                return await categoryRecommendationService.RecommendCategoryNamesAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分类名称推荐失败");
                throw new UserFriendlyException("分类名称推荐服务暂时不可用，请稍后再试");
            }
        }
    }
}
