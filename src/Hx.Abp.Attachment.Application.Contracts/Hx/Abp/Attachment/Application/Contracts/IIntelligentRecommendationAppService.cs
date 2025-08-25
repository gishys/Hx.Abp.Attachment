using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 智能推荐应用服务接口
    /// 基于语义匹配服务提供智能推荐功能
    /// </summary>
    public interface IIntelligentRecommendationAppService : IApplicationService
    {
        /// <summary>
        /// 智能推荐模板
        /// </summary>
        /// <param name="input">推荐输入参数</param>
        /// <returns>推荐的模板列表</returns>
        Task<IntelligentRecommendationResultDto> RecommendTemplatesAsync(IntelligentRecommendationInputDto input);

        /// <summary>
        /// 基于现有模板生成新模板
        /// </summary>
        /// <param name="input">模板生成输入参数</param>
        /// <returns>生成的新模板</returns>
        Task<AttachCatalogueTemplateDto> GenerateTemplateFromExistingAsync(GenerateTemplateFromExistingInputDto input);

        /// <summary>
        /// 智能分类推荐
        /// </summary>
        /// <param name="input">分类推荐输入参数</param>
        /// <returns>推荐的分类结构</returns>
        Task<IntelligentCatalogueRecommendationDto> RecommendCatalogueStructureAsync(IntelligentCatalogueRecommendationInputDto input);

        /// <summary>
        /// 批量智能推荐
        /// </summary>
        /// <param name="input">批量推荐输入参数</param>
        /// <returns>批量推荐结果</returns>
        Task<BatchIntelligentRecommendationResultDto> BatchRecommendAsync(BatchIntelligentRecommendationInputDto input);

        /// <summary>
        /// 学习用户偏好
        /// </summary>
        /// <param name="input">用户偏好学习输入</param>
        /// <returns>学习结果</returns>
        Task<UserPreferenceLearningResultDto> LearnUserPreferenceAsync(UserPreferenceLearningInputDto input);

        /// <summary>
        /// 获取推荐统计信息
        /// </summary>
        /// <returns>推荐统计信息</returns>
        Task<RecommendationStatisticsDto> GetRecommendationStatisticsAsync();
    }

    /// <summary>
    /// 智能推荐输入DTO
    /// </summary>
    public class IntelligentRecommendationInputDto
    {
        /// <summary>
        /// 查询文本
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 上下文数据
        /// </summary>
        public Dictionary<string, object>? ContextData { get; set; }

        /// <summary>
        /// 业务引用
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// 业务类型
        /// </summary>
        public int? ReferenceType { get; set; }

        /// <summary>
        /// 推荐数量
        /// </summary>
        public int TopN { get; set; } = 5;

        /// <summary>
        /// 相似度阈值
        /// </summary>
        public double Threshold { get; set; } = 0.6;

        /// <summary>
        /// 是否包含历史版本
        /// </summary>
        public bool IncludeHistory { get; set; } = false;

        /// <summary>
        /// 用户ID（用于个性化推荐）
        /// </summary>
        public Guid? UserId { get; set; }
    }

    /// <summary>
    /// 智能推荐结果DTO
    /// </summary>
    public class IntelligentRecommendationResultDto
    {
        /// <summary>
        /// 查询文本
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 推荐类型
        /// </summary>
        public string RecommendationType { get; set; } = "Semantic";

        /// <summary>
        /// 推荐模板列表
        /// </summary>
        public List<RecommendedTemplateDto> RecommendedTemplates { get; set; } = [];

        /// <summary>
        /// 推荐原因
        /// </summary>
        public List<string> RecommendationReasons { get; set; } = [];

        /// <summary>
        /// 推荐置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// 推荐模板DTO
    /// </summary>
    public class RecommendedTemplateDto
    {
        /// <summary>
        /// 模板信息
        /// </summary>
        public required AttachCatalogueTemplateDto Template { get; set; }

        /// <summary>
        /// 推荐分数
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 匹配类型
        /// </summary>
        public string MatchType { get; set; } = string.Empty;

        /// <summary>
        /// 推荐原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// 是否为新模板
        /// </summary>
        public bool IsNewTemplate { get; set; }

        /// <summary>
        /// 使用频率
        /// </summary>
        public int UsageCount { get; set; }
    }

    /// <summary>
    /// 基于现有模板生成新模板输入DTO
    /// </summary>
    public class GenerateTemplateFromExistingInputDto
    {
        /// <summary>
        /// 基础模板ID
        /// </summary>
        public Guid BaseTemplateId { get; set; }

        /// <summary>
        /// 新模板名称
        /// </summary>
        public string NewTemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 修改描述
        /// </summary>
        public string ModificationDescription { get; set; } = string.Empty;

        /// <summary>
        /// 自定义属性
        /// </summary>
        public Dictionary<string, object>? CustomProperties { get; set; }

        /// <summary>
        /// 是否继承父模板
        /// </summary>
        public bool InheritFromParent { get; set; } = true;
    }

    /// <summary>
    /// 智能分类推荐输入DTO
    /// </summary>
    public class IntelligentCatalogueRecommendationInputDto
    {
        /// <summary>
        /// 业务描述
        /// </summary>
        public string BusinessDescription { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型列表
        /// </summary>
        public List<string> FileTypes { get; set; } = [];

        /// <summary>
        /// 业务引用
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// 业务类型
        /// </summary>
        public int ReferenceType { get; set; }

        /// <summary>
        /// 期望的分类层级
        /// </summary>
        public int ExpectedLevels { get; set; } = 3;

        /// <summary>
        /// 是否包含必收项
        /// </summary>
        public bool IncludeRequired { get; set; } = true;
    }

    /// <summary>
    /// 智能分类推荐DTO
    /// </summary>
    public class IntelligentCatalogueRecommendationDto
    {
        /// <summary>
        /// 推荐的分类结构
        /// </summary>
        public List<RecommendedCatalogueDto> RecommendedCatalogues { get; set; } = [];

        /// <summary>
        /// 推荐模板
        /// </summary>
        public AttachCatalogueTemplateDto? RecommendedTemplate { get; set; }

        /// <summary>
        /// 推荐置信度
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// 推荐说明
        /// </summary>
        public string RecommendationExplanation { get; set; } = string.Empty;
    }

    /// <summary>
    /// 推荐分类DTO
    /// </summary>
    public class RecommendedCatalogueDto
    {
        public Guid? Id { get; set; }
        /// <summary>
        /// 分类名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 分类描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 顺序号
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 推荐分数
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 子分类
        /// </summary>
        public List<RecommendedCatalogueDto> Children { get; set; } = [];
    }

    /// <summary>
    /// 批量智能推荐输入DTO
    /// </summary>
    public class BatchIntelligentRecommendationInputDto
    {
        /// <summary>
        /// 批量查询列表
        /// </summary>
        public List<IntelligentRecommendationInputDto> Queries { get; set; } = [];

        /// <summary>
        /// 是否并行处理
        /// </summary>
        public bool ParallelProcessing { get; set; } = true;

        /// <summary>
        /// 批量处理超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// 批量智能推荐结果DTO
    /// </summary>
    public class BatchIntelligentRecommendationResultDto
    {
        /// <summary>
        /// 批量推荐结果
        /// </summary>
        public List<IntelligentRecommendationResultDto> Results { get; set; } = [];

        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 总处理时间（毫秒）
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> ErrorMessages { get; set; } = [];
    }

    /// <summary>
    /// 用户偏好学习输入DTO
    /// </summary>
    public class UserPreferenceLearningInputDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户行为类型
        /// </summary>
        public string BehaviorType { get; set; } = string.Empty; // "Select", "Reject", "Modify"

        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 查询文本
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 上下文数据
        /// </summary>
        public Dictionary<string, object>? ContextData { get; set; }

        /// <summary>
        /// 行为时间
        /// </summary>
        public DateTime BehaviorTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 用户偏好学习结果DTO
    /// </summary>
    public class UserPreferenceLearningResultDto
    {
        /// <summary>
        /// 学习是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 学习到的偏好特征
        /// </summary>
        public List<string> LearnedFeatures { get; set; } = [];

        /// <summary>
        /// 偏好权重更新
        /// </summary>
        public Dictionary<string, double> UpdatedWeights { get; set; } = [];

        /// <summary>
        /// 学习消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 推荐统计信息DTO
    /// </summary>
    public class RecommendationStatisticsDto
    {
        /// <summary>
        /// 总推荐次数
        /// </summary>
        public int TotalRecommendations { get; set; }

        /// <summary>
        /// 成功推荐次数
        /// </summary>
        public int SuccessfulRecommendations { get; set; }

        /// <summary>
        /// 平均推荐分数
        /// </summary>
        public double AverageScore { get; set; }

        /// <summary>
        /// 最受欢迎的模板
        /// </summary>
        public List<TemplateUsageDto> TopTemplates { get; set; } = [];

        /// <summary>
        /// 推荐类型分布
        /// </summary>
        public Dictionary<string, int> RecommendationTypeDistribution { get; set; } = [];

        /// <summary>
        /// 用户偏好统计
        /// </summary>
        public Dictionary<Guid, UserPreferenceDto> UserPreferences { get; set; } = [];
    }

    /// <summary>
    /// 模板使用情况DTO
    /// </summary>
    public class TemplateUsageDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 平均分数
        /// </summary>
        public double AverageScore { get; set; }
    }

    /// <summary>
    /// 用户偏好DTO
    /// </summary>
    public class UserPreferenceDto
    {
        /// <summary>
        /// 用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 偏好特征
        /// </summary>
        public Dictionary<string, double> Preferences { get; set; } = [];

        /// <summary>
        /// 学习次数
        /// </summary>
        public int LearningCount { get; set; }

        /// <summary>
        /// 最后学习时间
        /// </summary>
        public DateTime LastLearningTime { get; set; }
    }

    /// <summary>
    /// 语义匹配测试输入DTO
    /// </summary>
    public class SemanticMatchTestInputDto
    {
        /// <summary>
        /// 测试文本1
        /// </summary>
        public string Text1 { get; set; } = string.Empty;

        /// <summary>
        /// 测试文本2
        /// </summary>
        public string Text2 { get; set; } = string.Empty;

        /// <summary>
        /// 测试类型
        /// </summary>
        public string TestType { get; set; } = "Similarity"; // "Similarity", "FeatureExtraction", "TemplateMatching"
    }

    /// <summary>
    /// 语义匹配测试结果DTO
    /// </summary>
    public class SemanticMatchTestResultDto
    {
        /// <summary>
        /// 测试类型
        /// </summary>
        public string TestType { get; set; } = string.Empty;

        /// <summary>
        /// 相似度分数
        /// </summary>
        public double SimilarityScore { get; set; }

        /// <summary>
        /// 特征向量
        /// </summary>
        public float[]? Features { get; set; }

        /// <summary>
        /// 测试详情
        /// </summary>
        public Dictionary<string, object> TestDetails { get; set; } = [];

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// 测试结果
        /// </summary>
        public string Result { get; set; } = string.Empty;
    }

    /// <summary>
    /// 批量关键字更新结果DTO
    /// </summary>
    public class BatchKeywordUpdateResultDto
    {
        /// <summary>
        /// 成功数量
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败数量
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 总处理时间（毫秒）
        /// </summary>
        public long TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> ErrorMessages { get; set; } = [];

        /// <summary>
        /// 更新详情
        /// </summary>
        public List<KeywordUpdateDetailDto> UpdateDetails { get; set; } = [];
    }

    /// <summary>
    /// 关键字更新详情DTO
    /// </summary>
    public class KeywordUpdateDetailDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 更新是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 更新前的 SemanticModel
        /// </summary>
        public string? OldSemanticModel { get; set; }

        /// <summary>
        /// 更新后的 SemanticModel
        /// </summary>
        public string? NewSemanticModel { get; set; }

        /// <summary>
        /// 更新前的 NamePattern
        /// </summary>
        public string? OldNamePattern { get; set; }

        /// <summary>
        /// 更新后的 NamePattern
        /// </summary>
        public string? NewNamePattern { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 处理时间（毫秒）
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }
}
