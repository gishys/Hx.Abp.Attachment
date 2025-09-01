using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 附件目录模板权限DTO
    /// </summary>
    public class AttachCatalogueTemplatePermissionDto : EntityDto
    {
        /// <summary>
        /// 权限类型（Role/User/Policy）
        /// </summary>
        [Required(ErrorMessage = "权限类型不能为空")]
        [StringLength(20, ErrorMessage = "权限类型长度不能超过20个字符")]
        public string PermissionType { get; set; } = string.Empty;

        /// <summary>
        /// 权限目标（角色名/用户ID/策略名）
        /// </summary>
        [Required(ErrorMessage = "权限目标不能为空")]
        [StringLength(100, ErrorMessage = "权限目标长度不能超过100个字符")]
        public string PermissionTarget { get; set; } = string.Empty;

        /// <summary>
        /// 权限操作
        /// </summary>
        [Required(ErrorMessage = "权限操作不能为空")]
        public PermissionAction Action { get; set; }

        /// <summary>
        /// 权限效果
        /// </summary>
        [Required(ErrorMessage = "权限效果不能为空")]
        public PermissionEffect Effect { get; set; }

        /// <summary>
        /// 属性条件（JSON格式，用于策略权限）
        /// </summary>
        [StringLength(1000, ErrorMessage = "属性条件长度不能超过1000个字符")]
        public string? AttributeConditions { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime? EffectiveTime { get; set; }

        /// <summary>
        /// 失效时间
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        [StringLength(500, ErrorMessage = "权限描述长度不能超过500个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 创建权限DTO
    /// </summary>
    public class CreateAttachCatalogueTemplatePermissionDto
    {
        /// <summary>
        /// 权限类型（Role/User/Policy）
        /// </summary>
        [Required(ErrorMessage = "权限类型不能为空")]
        [StringLength(20, ErrorMessage = "权限类型长度不能超过20个字符")]
        public string PermissionType { get; set; } = string.Empty;

        /// <summary>
        /// 权限目标（角色名/用户ID/策略名）
        /// </summary>
        [Required(ErrorMessage = "权限目标不能为空")]
        [StringLength(100, ErrorMessage = "权限目标长度不能超过100个字符")]
        public string PermissionTarget { get; set; } = string.Empty;

        /// <summary>
        /// 权限操作
        /// </summary>
        [Required(ErrorMessage = "权限操作不能为空")]
        public PermissionAction Action { get; set; }

        /// <summary>
        /// 权限效果
        /// </summary>
        [Required(ErrorMessage = "权限效果不能为空")]
        public PermissionEffect Effect { get; set; }

        /// <summary>
        /// 属性条件（JSON格式，用于策略权限）
        /// </summary>
        [StringLength(1000, ErrorMessage = "属性条件长度不能超过1000个字符")]
        public string? AttributeConditions { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime? EffectiveTime { get; set; }

        /// <summary>
        /// 失效时间
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        [StringLength(500, ErrorMessage = "权限描述长度不能超过500个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 更新权限DTO
    /// </summary>
    public class UpdateAttachCatalogueTemplatePermissionDto
    {
        /// <summary>
        /// 权限类型（Role/User/Policy）
        /// </summary>
        [Required(ErrorMessage = "权限类型不能为空")]
        [StringLength(20, ErrorMessage = "权限类型长度不能超过20个字符")]
        public string PermissionType { get; set; } = string.Empty;

        /// <summary>
        /// 权限目标（角色名/用户ID/策略名）
        /// </summary>
        [Required(ErrorMessage = "权限目标不能为空")]
        [StringLength(100, ErrorMessage = "权限目标长度不能超过100个字符")]
        public string PermissionTarget { get; set; } = string.Empty;

        /// <summary>
        /// 权限操作
        /// </summary>
        [Required(ErrorMessage = "权限操作不能为空")]
        public PermissionAction Action { get; set; }

        /// <summary>
        /// 权限效果
        /// </summary>
        [Required(ErrorMessage = "权限效果不能为空")]
        public PermissionEffect Effect { get; set; }

        /// <summary>
        /// 属性条件（JSON格式，用于策略权限）
        /// </summary>
        [StringLength(1000, ErrorMessage = "属性条件长度不能超过1000个字符")]
        public string? AttributeConditions { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime? EffectiveTime { get; set; }

        /// <summary>
        /// 失效时间
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        [StringLength(500, ErrorMessage = "权限描述长度不能超过500个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 权限查询结果DTO
    /// </summary>
    public class AttachCatalogueTemplatePermissionResultDto : EntityDto
    {
        /// <summary>
        /// 权限类型（Role/User/Policy）
        /// </summary>
        public string PermissionType { get; set; } = string.Empty;

        /// <summary>
        /// 权限目标（角色名/用户ID/策略名）
        /// </summary>
        public string PermissionTarget { get; set; } = string.Empty;

        /// <summary>
        /// 权限操作
        /// </summary>
        public PermissionAction Action { get; set; }

        /// <summary>
        /// 权限效果
        /// </summary>
        public PermissionEffect Effect { get; set; }

        /// <summary>
        /// 属性条件（JSON格式，用于策略权限）
        /// </summary>
        public string? AttributeConditions { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime? EffectiveTime { get; set; }

        /// <summary>
        /// 失效时间
        /// </summary>
        public DateTime? ExpirationTime { get; set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否在有效期内
        /// </summary>
        public bool IsEffective { get; set; }

        /// <summary>
        /// 权限标识符
        /// </summary>
        public string PermissionIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// 权限摘要
        /// </summary>
        public string PermissionSummary { get; set; } = string.Empty;
    }
}
