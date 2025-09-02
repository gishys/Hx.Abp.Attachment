using Hx.Abp.Attachment.Domain;
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
        
        /// <summary>
        /// 模板类型 - 标识模板的层级和用途
        /// </summary>
        public TemplateType TemplateType { get; set; } = TemplateType.General;
        
        /// <summary>
        /// 模板用途 - 标识模板的具体用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; } = TemplatePurpose.Classification;
        
        /// <summary>
        /// 文本向量
        /// </summary>
        public List<double>? TextVector { get; set; }
        
        /// <summary>
        /// 向量维度
        /// </summary>
        public int VectorDimension { get; set; } = 0;
        
        /// <summary>
        /// 权限集合
        /// </summary>
        public ICollection<AttachCatalogueTemplatePermissionDto>? Permissions { get; set; }
        
        /// <summary>
        /// 模板标识描述
        /// </summary>
        public string TemplateIdentifierDescription => $"{TemplateType} - {TemplatePurpose}";
    }
}
