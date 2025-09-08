using Hx.Abp.Attachment.Domain.Shared;
using System.Text.Json.Serialization;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueTemplateDto : AuditedEntityDto
    {
        public Guid Id { get; set; }
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 模板标签
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 模板版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// 附件类型
        /// </summary>
        public AttachReceiveType AttachReceiveType { get; set; }

        /// <summary>
        /// 工作流配置（JSON格式，存储工作流引擎参数）
        /// 包含：workflowKey、审批节点配置、超时设置、脚本触发、WebHook回调等
        /// </summary>
        public string? WorkflowConfig { get; set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 顺序号
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 是否静态
        /// </summary>
        public bool IsStatic { get; set; }

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
        public string? TemplatePath { get; set; }

        /// <summary>
        /// 子模板集合
        /// </summary>
        [JsonIgnore]
        public List<AttachCatalogueTemplateDto> Children { get; set; } = [];

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType FacetType { get; set; }

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; }

        /// <summary>
        /// 文本向量
        /// </summary>
        public List<double>? TextVector { get; set; }

        /// <summary>
        /// 向量维度
        /// </summary>
        public int VectorDimension { get; set; }

        /// <summary>
        /// 权限集合
        /// </summary>
        public List<AttachCatalogueTemplatePermissionDto> Permissions { get; set; } = [];

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<MetaFieldDto> MetaFields { get; set; } = [];

        /// <summary>
        /// 模板标识描述
        /// </summary>
        public string TemplateIdentifierDescription => $"{FacetType} - {TemplatePurpose}";

        /// <summary>
        /// 是否为根模板
        /// </summary>
        public bool IsRoot => ParentId == null;

        /// <summary>
        /// 是否为叶子模板
        /// </summary>
        public bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 模板层级深度
        /// </summary>
        public int Depth => IsRoot ? 0 : 1;

        /// <summary>
        /// 模板路径
        /// </summary>
        public string? Path => TemplatePath;
    }
}
