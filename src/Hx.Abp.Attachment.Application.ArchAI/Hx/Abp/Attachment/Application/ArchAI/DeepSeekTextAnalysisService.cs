using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    /// <summary>
    /// DeepSeek文本分析服务
    /// </summary>
    public class DeepSeekTextAnalysisService(
        ILogger<DeepSeekTextAnalysisService> logger,
        HttpClient httpClient,
        SemanticVectorService semanticVectorService) : BaseTextAnalysisService(logger, httpClient, semanticVectorService), IScopedDependency
    {

        /// <summary>
        /// 调用AI API（公开方法）
        /// </summary>
        /// <param name="prompt">提示词</param>
        /// <param name="userContent">用户内容</param>
        /// <param name="maxTokens">最大token数</param>
        /// <returns>API响应</returns>
        public new async Task<DeepSeekResponse> CallAIApiAsync(string prompt, string userContent, int maxTokens = 50000)
        {
            return await base.CallAIApiAsync(prompt, userContent, maxTokens);
        }
    }
}
