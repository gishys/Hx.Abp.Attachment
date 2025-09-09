using Hx.Abp.Attachment.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 分类层级结构 DTO（用于树形显示，避免循环引用）
    /// </summary>
    public class AttachCatalogueTreeDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 业务类型Id
        /// </summary>
        public required string Reference { get; set; }

        /// <summary>
        /// 附件收取类型
        /// </summary>
        public AttachReceiveType AttachReceiveType { get; set; }

        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public required string CatalogueName { get; set; }

        /// <summary>
        /// 分类标签
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 顺序号
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Parent Id
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public required bool IsRequired { get; set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachCount { get; set; }

        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// 静态标识
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; }

        /// <summary>
        /// 核验通过
        /// </summary>
        public bool VerificationPassed { get; set; }

        /// <summary>
        /// 子文件夹（用于树形结构）
        /// </summary>
        public List<AttachCatalogueTreeDto> Children { get; set; } = [];

        /// <summary>
        /// 附件文件集合
        /// </summary>
        public Collection<AttachFileDto>? AttachFiles { get; set; }

        /// <summary>
        /// 关联的模板ID
        /// </summary>
        public Guid? TemplateId { get; set; }

        /// <summary>
        /// 全文内容
        /// </summary>
        public string? FullTextContent { get; set; }

        /// <summary>
        /// 全文内容更新时间
        /// </summary>
        public DateTime? FullTextContentUpdatedTime { get; set; }

        /// <summary>
        /// 分类分面类型 - 标识分类的层级和用途
        /// </summary>
        public FacetType CatalogueFacetType { get; set; } = FacetType.General;

        /// <summary>
        /// 分类用途 - 标识分类的具体用途
        /// </summary>
        public TemplatePurpose CataloguePurpose { get; set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 文本向量
        /// </summary>
        public List<double>? TextVector { get; set; }

        /// <summary>
        /// 向量维度
        /// </summary>
        public int VectorDimension { get; set; }

        /// <summary>
        /// 分类路径（用于快速查询层级）
        /// 格式：00001.00002.00003（5位数字，用点分隔）
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// 权限集合
        /// </summary>
        public List<AttachCatalogueTemplatePermissionDto> Permissions { get; set; } = [];

        /// <summary>
        /// 元数据字段集合
        /// </summary>
        public List<MetaFieldDto> MetaFields { get; set; } = [];

        /// <summary>
        /// 分类标识描述
        /// </summary>
        public string CatalogueIdentifierDescription => $"{CatalogueFacetType} - {CataloguePurpose}";

        /// <summary>
        /// 检查是否为根分类
        /// </summary>
        public bool IsRoot => ParentId == null;

        /// <summary>
        /// 检查是否为叶子分类
        /// </summary>
        public bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 获取分类层级深度
        /// </summary>
        public int GetDepth()
        {
            if (string.IsNullOrEmpty(Path))
                return 0;
            return Path.Split('.').Length;
        }

        /// <summary>
        /// 获取分类路径
        /// </summary>
        public string GetPath()
        {
            return Path ?? CatalogueName; // 优先使用路径，否则使用分类名称
        }
    }
}
