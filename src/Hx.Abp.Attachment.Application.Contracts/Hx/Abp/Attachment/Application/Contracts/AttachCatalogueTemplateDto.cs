using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueTemplateDto : FullAuditedEntityDto<Guid>
    {
        public required string TemplateName { get; set; }
        public int Version { get; set; } // 新增
        public bool IsLatest { get; set; } // 新增
        public AttachReceiveType AttachReceiveType { get; set; }
        public string? NamePattern { get; set; }
        public string? RuleExpression { get; set; }
        public string? SemanticModel { get; set; }
        public bool IsRequired { get; set; }
        public int SequenceNumber { get; set; }
        public bool IsStatic { get; set; }
        public Guid? ParentId { get; set; }
    }
}
