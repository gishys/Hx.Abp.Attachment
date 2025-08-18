namespace Hx.Abp.Attachment.Application.Contracts
{
    public class GetArchveCatalogueListInput
    {
        public required string Reference { get; set; }
        public int ReferenceType { get; set; }
        public string? CatalogueName { get; set; }
    }
}
