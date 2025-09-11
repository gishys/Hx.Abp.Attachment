using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class CreateUpdateAttachCatalogueTemplateDto
    {
        /// <summary>
        /// 模板ID（业务标识，同一模板的所有版本共享相同的ID）
        /// 创建时可以为空，系统会自动生成
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        [Required(ErrorMessage = "模板名称不能为空")]
        [StringLength(200, ErrorMessage = "模板名称长度不能超过200个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        [StringLength(1000, ErrorMessage = "模板描述长度不能超过1000个字符")]
        public string? Description { get; set; }

        /// <summary>
        /// 模板标签
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 附件类型
        /// </summary>
        [Required(ErrorMessage = "附件类型不能为空")]
        public AttachReceiveType AttachReceiveType { get; set; }

        /// <summary>
        /// 工作流配置（JSON格式，存储工作流引擎参数）
        /// 包含：workflowKey、审批节点配置、超时设置、脚本触发、WebHook回调等
        /// </summary>
        [StringLength(2000, ErrorMessage = "工作流配置长度不能超过2000个字符")]
        public string? WorkflowConfig { get; set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 顺序号
        /// </summary>
        [Range(1, 9999, ErrorMessage = "顺序号必须在1-9999之间")]
        public int SequenceNumber { get; set; } = 1;

        /// <summary>
        /// 是否静态
        /// </summary>
        public bool IsStatic { get; set; } = false;

        /// <summary>
        /// 父模板Id
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 父模板版本号（用于复合主键场景下的父节点唯一标识）
        /// </summary>
        public int? ParentVersion { get; set; }

        /// <summary>
        /// 模板路径（用于快速查询层级）
        /// 格式：00001.00002.00003（5位数字，用点分隔）
        /// </summary>
        [StringLength(200, ErrorMessage = "模板路径长度不能超过200个字符")]
        public string? TemplatePath { get; set; }

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType FacetType { get; set; } = FacetType.General;

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 模板角色 - 标识模板在层级结构中的角色
        /// 主要用于前端树状展示和动态分类树创建判断
        /// </summary>
        public TemplateRole TemplateRole { get; set; } = TemplateRole.Normal;

        /// <summary>
        /// 文本向量
        /// </summary>
        public List<double>? TextVector { get; set; }

        /// <summary>
        /// 权限集合
        /// </summary>
        public List<AttachCatalogueTemplatePermissionDto> Permissions { get; set; } = [];

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<CreateUpdateMetaFieldDto> MetaFields { get; set; } = [];
    }
}
