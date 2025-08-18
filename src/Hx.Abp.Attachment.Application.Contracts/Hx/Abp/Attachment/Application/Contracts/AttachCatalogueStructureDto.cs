namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueStructureDto
    {
        public AttachCatalogueTemplateDto? Root { get; set; }
        public List<AttachCatalogueStructureDto>? Children { get; set; }
        public List<AttachCatalogueTemplateDto>? History { get; set; } // 新增版本历史
    }
}
