using Hx.Abp.Attachment.Domain.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class CreateUpdateAttachCatalogueTemplateDto : EntityDto<Guid>
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        [Required(ErrorMessage = "模板名称不能为空")]
        [StringLength(200, ErrorMessage = "模板名称长度不能超过200个字符")]
        public string TemplateName { get; set; } = string.Empty;

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
        /// 规则引擎表达式
        /// </summary>
        [StringLength(1000, ErrorMessage = "规则表达式长度不能超过1000个字符")]
        public string? RuleExpression { get; set; }

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
        /// 分面类型
        /// </summary>
        public FacetType FacetType { get; set; } = FacetType.General;

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 文本向量
        /// </summary>
        public List<double>? TextVector { get; set; }

        /// <summary>
        /// 权限集合
        /// </summary>
        public List<AttachCatalogueTemplatePermissionDto> Permissions { get; set; } = [];
    }
}
