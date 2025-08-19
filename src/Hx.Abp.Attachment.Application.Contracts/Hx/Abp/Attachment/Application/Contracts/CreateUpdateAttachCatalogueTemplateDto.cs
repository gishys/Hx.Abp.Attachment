using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class CreateUpdateAttachCatalogueTemplateDto
    {
        [Required]
        [StringLength(256)]
        public required string TemplateName { get; set; }

        public AttachReceiveType AttachReceiveType { get; set; }

        [StringLength(512)]
        public string? NamePattern { get; set; }

        public string? RuleExpression { get; set; }

        [StringLength(128)]
        public string? SemanticModel { get; set; }

        public bool IsRequired { get; set; }
        public int SequenceNumber { get; set; }
        public bool IsStatic { get; set; }
        public Guid? ParentId { get; set; }
    }
}
