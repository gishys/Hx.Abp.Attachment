using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueCreateDto
    {
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public required AttachReceiveType AttachReceiveType { get; set; } = AttachReceiveType.Copy;
        /// <summary>
        /// 分类名称
        /// </summary>
        [MaxLength(500)]
        public required string CatalogueName { get; set; }

        /// <summary>
        /// 分类标签
        /// </summary>
        public List<string> Tags { get; set; } = [];
        
        /// <summary>
        /// 序号
        /// </summary>
        public int? SequenceNumber { get; set; }
        
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
        /// <summary>
        /// 业务Id
        /// </summary>
        [MaxLength(28)]
        public required string Reference { get; set; }
        /// <summary>
        /// 父节点Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; } = false;
        /// <summary>
        /// 核验通过
        /// </summary>
        public bool VerificationPassed { get; set; } = false;
        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }
        /// <summary>
        /// 静态标识
        /// </summary>
        public bool IsStatic { get; set; } = false;
        /// <summary>
        /// 子文件夹
        /// </summary>
        [JsonIgnore]
        public ICollection<AttachCatalogueCreateDto>? Children { get; set; }
        /// <summary>
        /// 子文件
        /// </summary>
        public List<AttachFileCreateDto>? AttachFiles {  get; set; }

        /// <summary>
        /// 关联的模板ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 分类分面类型 - 标识分类的层级和用途
        /// </summary>
        public FacetType CatalogueFacetType { get; set; } = FacetType.General;

        /// <summary>
        /// 分类用途 - 标识分类的具体用途
        /// </summary>
        public TemplatePurpose CataloguePurpose { get; set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 文本向量（可为空，如果提供则维度必须在64-2048之间）
        /// </summary>
        public List<double>? TextVector { get; set; }
    }
}
