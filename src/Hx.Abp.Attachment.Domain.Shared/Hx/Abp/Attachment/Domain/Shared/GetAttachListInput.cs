namespace Hx.Abp.Attachment.Domain.Shared
{
    public class GetAttachListInput
    {
        public required string Reference {  get; set; }
        public int ReferenceType {  get; set; }
        public string? CatalogueName {  get; set; }
    }
}
