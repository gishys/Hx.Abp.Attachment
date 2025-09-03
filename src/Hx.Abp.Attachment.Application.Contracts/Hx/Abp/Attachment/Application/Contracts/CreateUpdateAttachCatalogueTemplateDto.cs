using Hx.Abp.Attachment.Domain;
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
        
        /// <summary>
        /// 分面类型 - 标识模板的层级和用途
        /// </summary>
        public FacetType FacetType { get; set; } = FacetType.General;
        
        /// <summary>
        /// 模板用途 - 标识模板的具体用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; } = TemplatePurpose.Classification;
        
        /// <summary>
        /// 文本向量（64-2048维）
        /// </summary>
        [Range(64, 2048, ErrorMessage = "向量维度必须在64到2048之间")]
        public List<double>? TextVector { get; set; }
        
        /// <summary>
        /// 权限集合
        /// </summary>
        public ICollection<AttachCatalogueTemplatePermissionDto>? Permissions { get; set; }
    }
}
