namespace Hx.Abp.Attachment.Domain
{
    public class CreateAttachFileCatalogueInfo
    {
        public int SequenceNumber {  get; set; }
        public required string Reference {  get; set; }
        public Guid Id { get; set; }
    }
}
