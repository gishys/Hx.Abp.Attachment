using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueUpdateDto
    {
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public required AttachReceiveType AttachReceiveType { get; set; }
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
        /// 业务Id
        /// </summary>
        [MaxLength(50)]
        public required string Reference { get; set; }
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
        /// <summary>
        /// 父Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 静态标识
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// 关联的模板ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 关联的模板版本号
        /// </summary>
        public int? TemplateVersion { get; set; }

        /// <summary>
        /// 分类分面类型 - 标识分类的层级和用途
        /// </summary>
        public FacetType CatalogueFacetType { get; set; } = FacetType.General;

        /// <summary>
        /// 分类用途 - 标识分类的具体用途
        /// </summary>
        public TemplatePurpose CataloguePurpose { get; set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 分类角色 - 标识分类在层级结构中的角色
        /// 主要用于前端树状展示和动态分类树创建判断
        /// </summary>
        public TemplateRole TemplateRole { get; set; } = TemplateRole.Branch;

        /// <summary>
        /// 文本向量（可为空，如果提供则维度必须在64-2048之间）
        /// </summary>
        public List<double>? TextVector { get; set; }

        /// <summary>
        /// 分类路径（用于快速查询层级）
        /// 格式：0000001.0000002.0000003（7位数字，用点分隔）
        /// 如果为空，系统将自动生成
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<CreateUpdateMetaFieldDto>? MetaFields { get; set; }

        /// <summary>
        /// 附件文件
        /// </summary>
        public required ICollection<AttachFileUpdateDto> AttachFiles
        {
            get;
            set;
        }
    }
}
