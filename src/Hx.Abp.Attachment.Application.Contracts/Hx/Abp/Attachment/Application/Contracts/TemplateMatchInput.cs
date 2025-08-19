namespace Hx.Abp.Attachment.Application.Contracts
{
    public class TemplateMatchInput
    {
        public string? SemanticQuery { get; set; }
        public Dictionary<string, object>? ContextData { get; set; }
        public bool OnlyLatest { get; set; } = true; // 新增
    }
}
