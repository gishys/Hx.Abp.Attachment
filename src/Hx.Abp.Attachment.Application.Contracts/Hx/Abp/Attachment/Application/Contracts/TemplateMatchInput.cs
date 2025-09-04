namespace Hx.Abp.Attachment.Application.Contracts
{
    public class TemplateMatchInput
    {
        public string? SemanticQuery { get; set; }
        public Dictionary<string, object>? ContextData { get; set; }
        public bool OnlyLatest { get; set; } = true;
        
        /// <summary>
        /// 相似度阈值 (0-1)
        /// </summary>
        public double Threshold { get; set; } = 0.3;
        
        /// <summary>
        /// 返回最匹配的前N个结果
        /// </summary>
        public int TopN { get; set; } = 10;
        
        /// <summary>
        /// 是否包含历史版本
        /// </summary>
        public bool IncludeHistory { get; set; } = false;
    }
}
