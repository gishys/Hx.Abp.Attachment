using Hx.Abp.Attachment.Domain.Shared;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Domain.Values;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件目录模板权限值对象 - 基于ABP vNext权限系统简化实现
    /// </summary>
    public class AttachCatalogueTemplatePermission : ValueObject
    {
        /// <summary>
        /// 权限类型（Role/User/Policy）
        /// </summary>
        public virtual string PermissionType { get; private set; }

        /// <summary>
        /// 权限目标（角色名/用户ID/策略名）
        /// </summary>
        public virtual string PermissionTarget { get; private set; }

        /// <summary>
        /// 权限操作
        /// </summary>
        public virtual PermissionAction Action { get; private set; }

        /// <summary>
        /// 权限效果
        /// </summary>
        public virtual PermissionEffect Effect { get; private set; }

        /// <summary>
        /// 属性条件（JSONB格式，用于策略权限）
        /// </summary>
        public virtual string? AttributeConditions { get; private set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public virtual bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 生效时间
        /// </summary>
        public virtual DateTime? EffectiveTime { get; private set; }

        /// <summary>
        /// 失效时间
        /// </summary>
        public virtual DateTime? ExpirationTime { get; private set; }

        /// <summary>
        /// 权限描述
        /// </summary>
        public virtual string? Description { get; private set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        protected AttachCatalogueTemplatePermission() { }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

        public AttachCatalogueTemplatePermission(
            string permissionType,
            string permissionTarget,
            PermissionAction action,
            PermissionEffect effect,
            string? attributeConditions = null,
            DateTime? effectiveTime = null,
            DateTime? expirationTime = null,
            string? description = null)
        {
            PermissionType = Check.NotNullOrWhiteSpace(permissionType, nameof(permissionType));
            PermissionTarget = Check.NotNullOrWhiteSpace(permissionTarget, nameof(permissionTarget));
            Action = action;
            Effect = effect;
            AttributeConditions = attributeConditions;
            EffectiveTime = effectiveTime;
            ExpirationTime = expirationTime;
            Description = description;

            ValidatePermissionData();
        }

        /// <summary>
        /// 验证权限数据
        /// </summary>
        private void ValidatePermissionData()
        {
            // 验证权限类型
            if (!sourceArray.Contains(PermissionType))
            {
                throw new ArgumentException("权限类型必须是 Role、User 或 Policy", nameof(PermissionType));
            }

            // 验证时间范围
            if (EffectiveTime.HasValue && ExpirationTime.HasValue && EffectiveTime >= ExpirationTime)
            {
                throw new ArgumentException("生效时间必须早于失效时间");
            }

            // 验证属性条件格式（如果提供）
            if (!string.IsNullOrWhiteSpace(AttributeConditions))
            {
                try
                {
                    JsonDocument.Parse(AttributeConditions);
                }
                catch (JsonException ex)
                {
                    throw new ArgumentException($"属性条件格式错误: {ex.Message}", nameof(AttributeConditions));
                }
            }
        }

        /// <summary>
        /// 检查权限是否在有效期内
        /// </summary>
        public virtual bool IsEffective()
        {
            if (!IsEnabled) return false;

            var now = DateTime.UtcNow;

            if (EffectiveTime.HasValue && now < EffectiveTime.Value)
                return false;

            if (ExpirationTime.HasValue && now > ExpirationTime.Value)
                return false;

            return true;
        }

        /// <summary>
        /// 检查是否为角色权限
        /// </summary>
        public virtual bool IsRolePermission => PermissionType == "Role";

        /// <summary>
        /// 检查是否为用户权限
        /// </summary>
        public virtual bool IsUserPermission => PermissionType == "User";

        /// <summary>
        /// 检查是否为策略权限
        /// </summary>
        public virtual bool IsPolicyPermission => PermissionType == "Policy";
        private static readonly string[] sourceArray = ["Role", "User", "Policy"];

        /// <summary>
        /// 获取权限标识符
        /// </summary>
        public virtual string GetPermissionIdentifier()
        {
            return $"{PermissionType}:{PermissionTarget}:{Action}";
        }

        /// <summary>
        /// 获取权限摘要信息
        /// </summary>
        public virtual string GetPermissionSummary()
        {
            var summary = $"{PermissionType} - {Action} - {Effect} ({PermissionTarget})";
            
            if (!string.IsNullOrWhiteSpace(Description))
                summary += $" - {Description}";

            return summary;
        }

        /// <summary>
        /// 获取值对象原子值
        /// </summary>
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return PermissionType;
            yield return PermissionTarget;
            yield return Action;
            yield return Effect;
            yield return IsEnabled;
        }
    }
}
