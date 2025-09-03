using Hx.Abp.Attachment.Domain.Shared;
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 模板层级结构 DTO（用于树形显示，避免循环引用）
    /// </summary>
    public class AttachCatalogueTemplateTreeDto : AuditedEntityDto<Guid>
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

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
        /// 规则引擎表达式
        /// </summary>
        public string? RuleExpression { get; set; }

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
        /// 子模板集合（用于树形结构）
        /// </summary>
        public List<AttachCatalogueTemplateTreeDto> Children { get; set; } = [];

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
        /// 获取模板层级深度
        /// </summary>
        public int GetDepth()
        {
            if (IsRoot) return 0;
            return 1; // 简化实现，实际应该递归计算
        }

        /// <summary>
        /// 获取模板路径
        /// </summary>
        public string GetPath()
        {
            return TemplateName; // 简化实现，实际应该构建完整路径
        }
    }
}
