namespace Hx.Abp.Attachment.Application.Contracts
{
    public class GenerateCatalogueInput
    {
        public Guid TemplateId { get; set; }
        public required string Reference { get; set; }
        public int ReferenceType { get; set; }
        public Dictionary<string, object>? ContextData { get; set; }
    }
}
