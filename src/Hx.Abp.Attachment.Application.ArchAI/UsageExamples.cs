using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI.Examples
{
    /// <summary>
    /// AI服务使用示例 - 展示如何在实际业务中使用新的接口
    /// </summary>
    public class AIUsageExamples : IScopedDependency
    {
        private readonly AIServiceFactory _aiServiceFactory;
        private readonly ILogger<AIUsageExamples> _logger;

        public AIUsageExamples(AIServiceFactory aiServiceFactory, ILogger<AIUsageExamples> logger)
        {
            _aiServiceFactory = aiServiceFactory;
            _logger = logger;
        }

        #region 场景1: AttachCatalogue智能查询示例

        /// <summary>
        /// 分析附件内容，生成摘要和关键词用于智能查询
        /// </summary>
        public async Task<TextAnalysisDto> AnalyzeAttachmentForQueryAsync(string attachmentContent)
        {
            try
            {
                // 使用文档智能分析服务
                var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
                
                var input = new TextAnalysisInputDto
                {
                    Text = attachmentContent,
                    MaxSummaryLength = 500,
                    KeywordCount = 8,
                    GenerateSemanticVector = true,
                    ExtractEntities = true
                };

                var result = await documentAnalysisService.AnalyzeDocumentAsync(input);
                
                _logger.LogInformation("附件内容分析完成，生成关键词数量: {KeywordCount}", result.Keywords.Count);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "附件内容分析失败");
                throw;
            }
        }

        /// <summary>
        /// 批量分析多个附件内容
        /// </summary>
        public async Task<List<TextAnalysisDto>> BatchAnalyzeAttachmentsAsync(List<string> attachmentContents)
        {
            var results = new List<TextAnalysisDto>();
            var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();

            foreach (var content in attachmentContents)
            {
                try
                {
                    var input = new TextAnalysisInputDto
                    {
                        Text = content,
                        MaxSummaryLength = 300,
                        KeywordCount = 5
                    };

                    var result = await documentAnalysisService.AnalyzeDocumentAsync(input);
                    results.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "附件内容分析失败，跳过此附件");
                    // 添加默认结果，确保列表完整性
                    results.Add(new TextAnalysisDto
                    {
                        Summary = content.Length > 300 ? content[..300] : content,
                        Keywords = new List<string>(),
                        Confidence = 0.0,
                        AnalysisTime = DateTime.Now
                    });
                }
            }

            return results;
        }

        #endregion

        #region 场景2: AttachCatalogueTemplate智能推荐示例

        /// <summary>
        /// 为附件推荐最合适的分类模板
        /// </summary>
        public async Task<ClassificationResult> RecommendTemplateForAttachmentAsync(string attachmentContent, List<string> availableTemplates)
        {
            try
            {
                // 使用智能分类推荐服务
                var classificationService = _aiServiceFactory.GetIntelligentClassificationService();
                
                var result = await classificationService.RecommendDocumentCategoryAsync(attachmentContent, availableTemplates);
                
                _logger.LogInformation("模板推荐完成，推荐模板: {RecommendedTemplate}, 置信度: {Confidence}", 
                    result.RecommendedCategory, result.Confidence);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模板推荐失败");
                throw;
            }
        }

        /// <summary>
        /// 批量推荐多个附件的分类模板
        /// </summary>
        public async Task<List<ClassificationResult>> BatchRecommendTemplatesAsync(List<string> attachmentContents, List<string> availableTemplates)
        {
            try
            {
                var classificationService = _aiServiceFactory.GetIntelligentClassificationService();
                
                var results = await classificationService.BatchRecommendCategoriesAsync(attachmentContents, availableTemplates);
                
                _logger.LogInformation("批量模板推荐完成，处理附件数量: {Count}", results.Count);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量模板推荐失败");
                throw;
            }
        }

        #endregion

        #region 场景3: 综合分析示例

        /// <summary>
        /// 对附件进行全栈分析，同时生成摘要、关键词和分类推荐
        /// </summary>
        public async Task<ComprehensiveAnalysisResult> ComprehensiveAnalysisAsync(string attachmentContent, List<string> availableCategories)
        {
            try
            {
                // 使用全栈智能分析服务
                var fullStackService = _aiServiceFactory.GetFullStackAnalysisService();
                
                var result = await fullStackService.AnalyzeDocumentComprehensivelyAsync(
                    attachmentContent, 
                    availableCategories, 
                    maxSummaryLength: 600, 
                    keywordCount: 10);
                
                _logger.LogInformation("全栈分析完成，摘要长度: {SummaryLength}, 关键词数量: {KeywordCount}, 推荐分类: {Category}", 
                    result.Summary.Length, result.Keywords.Count, result.Classification.RecommendedCategory);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全栈分析失败");
                throw;
            }
        }

        /// <summary>
        /// 批量全栈分析多个附件
        /// </summary>
        public async Task<List<ComprehensiveAnalysisResult>> BatchComprehensiveAnalysisAsync(List<string> attachmentContents, List<string> availableCategories)
        {
            try
            {
                var fullStackService = _aiServiceFactory.GetFullStackAnalysisService();
                
                var results = await fullStackService.BatchAnalyzeComprehensivelyAsync(
                    attachmentContents, 
                    availableCategories, 
                    maxSummaryLength: 500, 
                    keywordCount: 8);
                
                _logger.LogInformation("批量全栈分析完成，处理附件数量: {Count}", results.Count);
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量全栈分析失败");
                throw;
            }
        }

        #endregion

        #region 场景4: 根据业务场景智能选择服务

        /// <summary>
        /// 根据业务场景自动选择最合适的AI服务
        /// </summary>
        public async Task<object> SmartAnalysisByScenarioAsync(string content, List<string> categories, BusinessScenario scenario)
        {
            try
            {
                // 根据业务场景获取合适的服务
                var service = _aiServiceFactory.GetAnalysisServiceByScenario(scenario);
                
                _logger.LogInformation("根据业务场景选择服务: {ServiceName}", service.ServiceName);
                
                return scenario switch
                {
                    BusinessScenario.DocumentAnalysis => await HandleDocumentAnalysisAsync(content, service),
                    BusinessScenario.ClassificationRecommendation => await HandleClassificationAsync(content, categories, service),
                    BusinessScenario.FullStackAnalysis => await HandleFullStackAnalysisAsync(content, categories, service),
                    _ => throw new ArgumentException($"不支持的业务场景: {scenario}")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "智能分析失败，场景: {Scenario}", scenario);
                throw;
            }
        }

        private async Task<TextAnalysisDto> HandleDocumentAnalysisAsync(string content, IAnalysisService service)
        {
            if (service is IDocumentAnalysisService documentService)
            {
                var input = new TextAnalysisInputDto
                {
                    Text = content,
                    MaxSummaryLength = 400,
                    KeywordCount = 6
                };
                return await documentService.AnalyzeDocumentAsync(input);
            }
            throw new InvalidOperationException("服务类型不匹配");
        }

        private async Task<ClassificationResult> HandleClassificationAsync(string content, List<string> categories, IAnalysisService service)
        {
            if (service is IIntelligentClassificationService classificationService)
            {
                return await classificationService.RecommendDocumentCategoryAsync(content, categories);
            }
            throw new InvalidOperationException("服务类型不匹配");
        }

        private async Task<ComprehensiveAnalysisResult> HandleFullStackAnalysisAsync(string content, List<string> categories, IAnalysisService service)
        {
            if (service is IFullStackAnalysisService fullStackService)
            {
                return await fullStackService.AnalyzeDocumentComprehensivelyAsync(content, categories, 500, 8);
            }
            throw new InvalidOperationException("服务类型不匹配");
        }

        #endregion

        #region 场景5: 性能优化示例

        /// <summary>
        /// 并行处理多个附件的分析任务
        /// </summary>
        public async Task<List<TextAnalysisDto>> ParallelAnalyzeAttachmentsAsync(List<string> attachmentContents)
        {
            var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
            var tasks = new List<Task<TextAnalysisDto>>();

            foreach (var content in attachmentContents)
            {
                var input = new TextAnalysisInputDto
                {
                    Text = content,
                    MaxSummaryLength = 300,
                    KeywordCount = 5
                };

                var task = documentAnalysisService.AnalyzeDocumentAsync(input);
                tasks.Add(task);
            }

            try
            {
                // 并行执行所有分析任务
                var results = await Task.WhenAll(tasks);
                _logger.LogInformation("并行分析完成，处理附件数量: {Count}", results.Length);
                return results.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "并行分析过程中发生错误");
                throw;
            }
        }

        #endregion

        #region 场景6: 错误处理和降级策略示例

        /// <summary>
        /// 带降级策略的智能分析
        /// </summary>
        public async Task<TextAnalysisDto> AnalyzeWithFallbackAsync(string content)
        {
            try
            {
                // 尝试使用AI服务进行分析
                var documentAnalysisService = _aiServiceFactory.GetDocumentAnalysisService();
                
                var input = new TextAnalysisInputDto
                {
                    Text = content,
                    MaxSummaryLength = 500,
                    KeywordCount = 5
                };

                return await documentAnalysisService.AnalyzeDocumentAsync(input);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI服务分析失败，使用本地降级策略");
                
                // 降级策略：本地简单分析
                return CreateLocalAnalysisResult(content);
            }
        }

        /// <summary>
        /// 本地降级分析策略
        /// </summary>
        private static TextAnalysisDto CreateLocalAnalysisResult(string content)
        {
            // 简单的本地关键词提取逻辑
            var words = content.Split([' ', ',', '.', ';', ':', '!', '?', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .Select(w => w.Trim('"', '\'', '(', ')', '[', ']', '{', '}'))
                .Where(w => w.Length > 2)
                .GroupBy(w => w.ToLower())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            return new TextAnalysisDto
            {
                Summary = content.Length > 500 ? content[..500] : content,
                Keywords = words,
                Confidence = 0.3, // 本地分析的置信度较低
                AnalysisTime = DateTime.Now
            };
        }

        #endregion
    }
}
