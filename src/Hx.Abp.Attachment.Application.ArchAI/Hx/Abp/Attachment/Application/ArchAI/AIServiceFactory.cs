using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// AI智能分析服务工厂 - 提供不同场景的AI分析能力
    /// </summary>
    public class AIServiceFactory(ILogger<AIServiceFactory> logger, AliyunAIService aliyunAIService) : IScopedDependency
    {
        private readonly ILogger<AIServiceFactory> _logger = logger;
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        #region 核心服务提供者

        /// <summary>
        /// 获取文档智能分析服务 - 适用于AttachCatalogue文档内容分析
        /// </summary>
        /// <returns>文档智能分析服务</returns>
        public IDocumentAnalysisService GetDocumentAnalysisService()
        {
            return new AliyunDocumentAnalysisService(_aliyunAIService);
        }

        /// <summary>
        /// 获取智能分类推荐服务 - 适用于AttachCatalogueTemplate分类推荐
        /// </summary>
        /// <returns>智能分类推荐服务</returns>
        public IIntelligentClassificationService GetIntelligentClassificationService()
        {
            return new AliyunIntelligentClassificationService(_aliyunAIService);
        }

        /// <summary>
        /// 获取全栈智能分析服务 - 同时支持文档分析和分类推荐
        /// </summary>
        /// <returns>全栈智能分析服务</returns>
        public IFullStackAnalysisService GetFullStackAnalysisService()
        {
            return new AliyunFullStackAnalysisService(_aliyunAIService);
        }

        #endregion

        #region 便捷访问方法

        /// <summary>
        /// 获取默认的文档分析服务（推荐使用）
        /// </summary>
        /// <returns>默认文档分析服务</returns>
        public IDocumentAnalysisService GetDefaultDocumentAnalysisService()
        {
            _logger.LogDebug("使用默认文档分析服务");
            return GetDocumentAnalysisService();
        }

        /// <summary>
        /// 根据业务场景获取合适的分析服务
        /// </summary>
        /// <param name="businessScenario">业务场景</param>
        /// <returns>对应的分析服务</returns>
        public IAnalysisService GetAnalysisServiceByScenario(BusinessScenario businessScenario)
        {
            return businessScenario switch
            {
                BusinessScenario.DocumentAnalysis => GetDocumentAnalysisService(),
                BusinessScenario.ClassificationRecommendation => GetIntelligentClassificationService(),
                BusinessScenario.FullStackAnalysis => GetFullStackAnalysisService(),
                _ => GetDefaultDocumentAnalysisService()
            };
        }

        #endregion
    }

    #region 业务场景枚举

    /// <summary>
    /// 业务场景枚举
    /// </summary>
    public enum BusinessScenario
    {
        /// <summary>
        /// 文档分析场景 - 适用于AttachCatalogue
        /// </summary>
        DocumentAnalysis,

        /// <summary>
        /// 分类推荐场景 - 适用于AttachCatalogueTemplate
        /// </summary>
        ClassificationRecommendation,

        /// <summary>
        /// 全栈分析场景 - 同时支持文档分析和分类推荐
        /// </summary>
        FullStackAnalysis
    }

    #endregion

    #region 核心服务接口

    /// <summary>
    /// 分析服务基础接口
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// 服务描述
        /// </summary>
        string ServiceDescription { get; }
    }

    /// <summary>
    /// 文档智能分析服务接口 - 适用于AttachCatalogue智能查询
    /// </summary>
    public interface IDocumentAnalysisService : IAnalysisService
    {
        /// <summary>
        /// 分析文档内容并生成摘要和关键词
        /// </summary>
        /// <param name="input">文档分析输入参数</param>
        /// <returns>文档分析结果</returns>
        Task<TextAnalysisDto> AnalyzeDocumentAsync(TextAnalysisInputDto input);

        /// <summary>
        /// 生成文档摘要
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="maxLength">摘要最大长度</param>
        /// <returns>文档摘要</returns>
        Task<string> GenerateDocumentSummaryAsync(string content, int maxLength = 500);

        /// <summary>
        /// 提取文档关键词
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>关键词列表</returns>
        Task<List<string>> ExtractDocumentKeywordsAsync(string content, int keywordCount = 5);
    }

    /// <summary>
    /// 智能分类推荐服务接口 - 适用于AttachCatalogueTemplate智能推荐
    /// </summary>
    public interface IIntelligentClassificationService : IAnalysisService
    {
        /// <summary>
        /// 智能推荐文档分类
        /// </summary>
        /// <param name="content">文档内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>分类推荐结果</returns>
        Task<ClassificationResult> RecommendDocumentCategoryAsync(string content, List<string> categoryOptions);

        /// <summary>
        /// 批量分类推荐
        /// </summary>
        /// <param name="documents">文档列表</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>批量分类推荐结果</returns>
        Task<List<ClassificationResult>> BatchRecommendCategoriesAsync(List<string> documents, List<string> categoryOptions);
    }

    /// <summary>
    /// 全栈智能分析服务接口 - 同时支持文档分析和分类推荐
    /// </summary>
    public interface IFullStackAnalysisService : IAnalysisService
    {
        /// <summary>
        /// 全栈文档分析
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
        /// 批量全栈分析
        /// </summary>
        /// <param name="documents">文档列表</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <param name="maxSummaryLength">摘要最大长度</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>批量全栈分析结果</returns>
        Task<List<ComprehensiveAnalysisResult>> BatchAnalyzeComprehensivelyAsync(
            List<string> documents,
            List<string> categoryOptions,
            int maxSummaryLength = 500,
            int keywordCount = 5);
    }

    #endregion

    #region 阿里云服务实现

    /// <summary>
    /// 阿里云文档智能分析服务实现
    /// </summary>
    public class AliyunDocumentAnalysisService(AliyunAIService aliyunAIService) : IDocumentAnalysisService
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public string ServiceName => "阿里云文档智能分析服务";
        public string ServiceDescription => "基于OpenNLU的文档内容分析，支持摘要生成和关键词提取";

        public async Task<TextAnalysisDto> AnalyzeDocumentAsync(TextAnalysisInputDto input)
        {
            try
            {
                // 并行调用阿里云AI服务生成摘要和关键词
                var summaryTask = _aliyunAIService.GenerateSummaryAsync(input.Text, input.MaxSummaryLength);
                var keywordsTask = _aliyunAIService.ExtractKeywordsAsync(input.Text, input.KeywordCount);

                await Task.WhenAll(summaryTask, keywordsTask);

                return new TextAnalysisDto
                {
                    Summary = summaryTask.Result,
                    Keywords = keywordsTask.Result,
                    Confidence = 0.9,
                    AnalysisTime = DateTime.Now
                };
            }
            catch (Exception)
            {
                // 如果AI服务调用失败，返回默认结果
                return new TextAnalysisDto
                {
                    Summary = input.Text.Length > input.MaxSummaryLength ? input.Text[..input.MaxSummaryLength] : input.Text,
                    Keywords = [],
                    Confidence = 0.0,
                    AnalysisTime = DateTime.Now
                };
            }
        }

        public async Task<string> GenerateDocumentSummaryAsync(string content, int maxLength = 500)
        {
            try
            {
                return await _aliyunAIService.GenerateSummaryAsync(content, maxLength);
            }
            catch (Exception)
            {
                return content.Length > maxLength ? content[..maxLength] : content;
            }
        }

        public async Task<List<string>> ExtractDocumentKeywordsAsync(string content, int keywordCount = 5)
        {
            try
            {
                return await _aliyunAIService.ExtractKeywordsAsync(content, keywordCount);
            }
            catch (Exception)
            {
                return [];
            }
        }
    }

    /// <summary>
    /// 阿里云智能分类推荐服务实现
    /// </summary>
    public class AliyunIntelligentClassificationService(AliyunAIService aliyunAIService) : IIntelligentClassificationService
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public string ServiceName => "阿里云智能分类推荐服务";
        public string ServiceDescription => "基于OpenNLU的智能分类推荐，支持多分类选项的智能匹配";

        public async Task<ClassificationResult> RecommendDocumentCategoryAsync(string content, List<string> categoryOptions)
        {
            try
            {
                return await _aliyunAIService.ClassifyTextAsync(content, categoryOptions);
            }
            catch (Exception)
            {
                return new ClassificationResult
                {
                    RecommendedCategory = categoryOptions.FirstOrDefault(),
                    Confidence = 0.0
                };
            }
        }

        public async Task<List<ClassificationResult>> BatchRecommendCategoriesAsync(List<string> documents, List<string> categoryOptions)
        {
            var results = new List<ClassificationResult>();
            
            foreach (var document in documents)
            {
                try
                {
                    var result = await RecommendDocumentCategoryAsync(document, categoryOptions);
                    results.Add(result);
                }
                catch (Exception)
                {
                    results.Add(new ClassificationResult
                    {
                        RecommendedCategory = categoryOptions.FirstOrDefault(),
                        Confidence = 0.0
                    });
                }
            }

            return results;
        }
    }

    /// <summary>
    /// 阿里云全栈智能分析服务实现
    /// </summary>
    public class AliyunFullStackAnalysisService(AliyunAIService aliyunAIService) : IFullStackAnalysisService
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public string ServiceName => "阿里云全栈智能分析服务";
        public string ServiceDescription => "基于OpenNLU的全栈分析，同时支持文档分析和分类推荐";

        public async Task<ComprehensiveAnalysisResult> AnalyzeDocumentComprehensivelyAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5)
        {
            try
            {
                return await _aliyunAIService.AnalyzeComprehensivelyAsync(
                    content, categoryOptions, maxSummaryLength, keywordCount);
            }
            catch (Exception)
            {
                return new ComprehensiveAnalysisResult
                {
                    Summary = content.Length > maxSummaryLength ? content[..maxSummaryLength] : content,
                    Keywords = [],
                    Classification = new ClassificationResult
                    {
                        RecommendedCategory = categoryOptions.FirstOrDefault(),
                        Confidence = 0.0
                    },
                    AnalysisTime = DateTime.Now,
                    Confidence = 0.0
                };
            }
        }

        public async Task<List<ComprehensiveAnalysisResult>> BatchAnalyzeComprehensivelyAsync(
            List<string> documents,
            List<string> categoryOptions,
            int maxSummaryLength = 500,
            int keywordCount = 5)
        {
            var results = new List<ComprehensiveAnalysisResult>();
            
            foreach (var document in documents)
            {
                try
                {
                    var result = await AnalyzeDocumentComprehensivelyAsync(document, categoryOptions, maxSummaryLength, keywordCount);
                    results.Add(result);
                }
                catch (Exception)
                {
                    results.Add(new ComprehensiveAnalysisResult
                    {
                        Summary = document.Length > maxSummaryLength ? document[..maxSummaryLength] : document,
                        Keywords = [],
                        Classification = new ClassificationResult
                        {
                            RecommendedCategory = categoryOptions.FirstOrDefault(),
                            Confidence = 0.0
                        },
                        AnalysisTime = DateTime.Now,
                        Confidence = 0.0
                    });
                }
            }

            return results;
        }
    }

    #endregion

    #region 兼容性接口（保持向后兼容）

    /// <summary>
    /// 文本分析提供者接口（兼容性接口）
    /// </summary>
    [Obsolete("请使用 IDocumentAnalysisService 替代")]
    public interface ITextAnalysisProvider
    {
        /// <summary>
        /// 分析文本并生成摘要和关键词
        /// </summary>
        /// <param name="input">分析输入参数</param>
        /// <returns>分析结果</returns>
        Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input);
    }

    /// <summary>
    /// 智能分类提供者接口（兼容性接口）
    /// </summary>
    [Obsolete("请使用 IIntelligentClassificationService 替代")]
    public interface IClassificationProvider
    {
        /// <summary>
        /// 智能分类推荐
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <returns>推荐分类结果</returns>
        Task<ClassificationResult> ClassifyAsync(string content, List<string> categoryOptions);
    }

    /// <summary>
    /// 综合分析提供者接口（兼容性接口）
    /// </summary>
    [Obsolete("请使用 IFullStackAnalysisService 替代")]
    public interface IComprehensiveAnalysisProvider
    {
        /// <summary>
        /// 综合分析
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="categoryOptions">可选分类列表</param>
        /// <param name="maxSummaryLength">摘要最大长度</param>
        /// <param name="keywordCount">关键词数量</param>
        /// <returns>综合分析结果</returns>
        Task<ComprehensiveAnalysisResult> AnalyzeAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5);
    }

    /// <summary>
    /// 阿里云文本分析提供者（兼容性实现）
    /// </summary>
    [Obsolete("请使用 AliyunDocumentAnalysisService 替代")]
    public class AliyunTextAnalysisProvider(AliyunAIService aliyunAIService) : ITextAnalysisProvider
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public async Task<TextAnalysisDto> AnalyzeTextAsync(TextAnalysisInputDto input)
        {
            var service = new AliyunDocumentAnalysisService(_aliyunAIService);
            return await service.AnalyzeDocumentAsync(input);
        }
    }

    /// <summary>
    /// 阿里云智能分类提供者（兼容性实现）
    /// </summary>
    [Obsolete("请使用 AliyunIntelligentClassificationService 替代")]
    public class AliyunClassificationProvider(AliyunAIService aliyunAIService) : IClassificationProvider
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public async Task<ClassificationResult> ClassifyAsync(string content, List<string> categoryOptions)
        {
            var service = new AliyunIntelligentClassificationService(_aliyunAIService);
            return await service.RecommendDocumentCategoryAsync(content, categoryOptions);
        }
    }

    /// <summary>
    /// 阿里云综合分析提供者（兼容性实现）
    /// </summary>
    [Obsolete("请使用 AliyunFullStackAnalysisService 替代")]
    public class AliyunComprehensiveAnalysisProvider(AliyunAIService aliyunAIService) : IComprehensiveAnalysisProvider
    {
        private readonly AliyunAIService _aliyunAIService = aliyunAIService;

        public async Task<ComprehensiveAnalysisResult> AnalyzeAsync(
            string content, 
            List<string> categoryOptions, 
            int maxSummaryLength = 500, 
            int keywordCount = 5)
        {
            var service = new AliyunFullStackAnalysisService(_aliyunAIService);
            return await service.AnalyzeDocumentComprehensivelyAsync(content, categoryOptions, maxSummaryLength, keywordCount);
        }
    }

    #endregion
}
