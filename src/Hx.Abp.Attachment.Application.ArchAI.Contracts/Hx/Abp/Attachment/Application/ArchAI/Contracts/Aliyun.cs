using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    /// <summary>
    /// 阿里云NLU API请求模型
    /// </summary>
    public class AliyunNLURequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "opennlu-v1";
        [JsonPropertyName("input")]
        public AliyunNLUInput Input { get; set; } = new();
        [JsonPropertyName("parameters")]
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API输入参数
    /// </summary>
    public class AliyunNLUInput
    {
        [JsonPropertyName("sentence")]
        public string Sentence { get; set; } = string.Empty;
        [JsonPropertyName("task")]
        public string Task { get; set; } = "extraction";
        [JsonPropertyName("labels")]
        public string Labels { get; set; } = string.Empty;
    }

    /// <summary>
    /// 阿里云NLU API响应模型
    /// </summary>
    public class AliyunNLUResponse
    {
        [JsonPropertyName("output")]
        public AliyunNLUOutput? Output { get; set; }
        [JsonPropertyName("usage")]
        public AliyunNLUUsage? Usage { get; set; }
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API输出
    /// </summary>
    public class AliyunNLUOutput
    {
        [JsonPropertyName("rt")]
        public decimal? Rt { get; set; }
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    /// <summary>
    /// 阿里云NLU API使用量统计
    /// </summary>
    public class AliyunNLUUsage
    {
        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// 阿里云错误响应模型
    /// </summary>
    public class AliyunErrorResponse
    {
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }
    }

    /// <summary>
    /// 智能分类结果
    /// </summary>
    public class ClassificationResult
    {
        [JsonPropertyName("recommended_category")]
        public string? RecommendedCategory { get; set; }
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }

    /// <summary>
    /// 综合分析结果
    /// </summary>
    public class ComprehensiveAnalysisResult
    {
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = [];
        [JsonPropertyName("classification")]
        public ClassificationResult Classification { get; set; } = new();
        [JsonPropertyName("analysis_time")]
        public DateTime AnalysisTime { get; set; }
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }
    }
}
