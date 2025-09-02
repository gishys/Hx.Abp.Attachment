using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using System.Text.Json;

namespace Hx.Abp.Attachment.Domain
{
    public class AttachCatalogueTemplate : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        [NotNull]
        public virtual string TemplateName { get; private set; }

        /// <summary>
        /// 模板版本号
        /// </summary>
        public virtual int Version { get; private set; } = 1;

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        public virtual bool IsLatest { get; private set; } = true;

        /// <summary>
        /// 附件类型
        /// </summary>
        public virtual AttachReceiveType AttachReceiveType { get; private set; }

        /// <summary>
        /// 分类名称规则
        /// </summary>
        [CanBeNull]
        public virtual string? NamePattern { get; private set; }

        /// <summary>
        /// 规则引擎表达式
        /// </summary>
        [CanBeNull]
        public virtual string? RuleExpression { get; private set; }

        /// <summary>
        /// AI语义匹配模型名称
        /// </summary>
        [CanBeNull]
        public virtual string? SemanticModel { get; private set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public virtual bool IsRequired { get; private set; }

        /// <summary>
        /// 顺序号
        /// </summary>
        public virtual int SequenceNumber { get; private set; }

        /// <summary>
        /// 是否静态
        /// </summary>
        public virtual bool IsStatic { get; private set; }

        /// <summary>
        /// 父模板Id
        /// </summary>
        public virtual Guid? ParentId { get; protected set; }

        /// <summary>
        /// 子模板集合
        /// </summary>
        public virtual ICollection<AttachCatalogueTemplate> Children { get; private set; }

        /// <summary>
        /// 模板类型 - 标识模板的层级和用途
        /// </summary>
        public virtual TemplateType TemplateType { get; private set; } = TemplateType.General;

        /// <summary>
        /// 模板用途 - 标识模板的具体用途
        /// </summary>
        public virtual TemplatePurpose TemplatePurpose { get; private set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 文本向量（64-2048维）
        /// </summary>
        [CanBeNull]
        public virtual List<double>? TextVector { get; private set; }

        /// <summary>
        /// 向量维度
        /// </summary>
        public virtual int VectorDimension { get; private set; } = 0;

        /// <summary>
        /// 权限集合（JSONB格式，存储权限值对象数组）
        /// </summary>
        public virtual ICollection<AttachCatalogueTemplatePermission> Permissions { get; private set; } = [];

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        protected AttachCatalogueTemplate() { }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。

        public AttachCatalogueTemplate(
            Guid id,
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired = false,
            bool isStatic = false,
            Guid? parentId = null,
            [CanBeNull] string? namePattern = null,
            [CanBeNull] string? ruleExpression = null,
            [CanBeNull] string? semanticModel = null,
            int version = 1,
            bool isLatest = true,
            TemplateType templateType = TemplateType.General,
            TemplatePurpose templatePurpose = TemplatePurpose.Classification,
            [CanBeNull] List<double>? textVector = null)
        {
            Id = id;
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            IsStatic = isStatic;
            ParentId = parentId;
            NamePattern = namePattern;
            RuleExpression = ruleExpression;
            SemanticModel = semanticModel;
            Version = version;
            IsLatest = isLatest;
            TemplateType = templateType;
            TemplatePurpose = templatePurpose;
            SetTextVector(textVector);
            Children = [];
            Permissions = [];
        }

        public virtual void Update(
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired,
            bool isStatic,
            [CanBeNull] string namePattern,
            [CanBeNull] string ruleExpression,
            [CanBeNull] string semanticModel,
            TemplateType templateType,
            TemplatePurpose templatePurpose)
        {
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            IsStatic = isStatic;
            NamePattern = namePattern;
            RuleExpression = ruleExpression;
            SemanticModel = semanticModel;
            TemplateType = templateType;
            TemplatePurpose = templatePurpose;
        }

        public virtual void SetVersion(int version, bool isLatest)
        {
            Version = version;
            IsLatest = isLatest;
        }

        public virtual void ChangeParent(Guid? parentId)
        {
            ParentId = parentId;
        }

        public virtual void AddChildTemplate(AttachCatalogueTemplate child)
        {
            Children.Add(child);
        }

        public virtual void RemoveChildTemplate(AttachCatalogueTemplate child)
        {
            Children.Remove(child);
        }

        /// <summary>
        /// 设置文本向量
        /// </summary>
        /// <param name="textVector">文本向量</param>
        public virtual void SetTextVector([CanBeNull] List<double>? textVector)
        {
            if (textVector != null)
            {
                if (textVector.Count < 64 || textVector.Count > 2048)
                {
                    throw new ArgumentException("向量维度必须在64到2048之间", nameof(textVector));
                }
                TextVector = textVector;
                VectorDimension = textVector.Count;
            }
            else
            {
                TextVector = null;
                VectorDimension = 0;
            }
        }

        /// <summary>
        /// 设置模板标识
        /// </summary>
        /// <param name="templateType">模板类型</param>
        /// <param name="templatePurpose">模板用途</param>
        public virtual void SetTemplateIdentifiers(
            TemplateType templateType,
            TemplatePurpose templatePurpose)
        {
            TemplateType = templateType;
            TemplatePurpose = templatePurpose;
        }

        /// <summary>
        /// 验证模板配置
        /// </summary>
        public virtual void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(TemplateName))
            {
                throw new ArgumentException("模板名称不能为空", nameof(TemplateName));
            }

            if (Version <= 0)
            {
                throw new ArgumentException("版本号必须大于0", nameof(Version));
            }

            // 验证向量维度
            if (TextVector != null && (VectorDimension < 64 || VectorDimension > 2048))
            {
                throw new ArgumentException("向量维度必须在64到2048之间", nameof(TextVector));
            }

            // 验证规则表达式格式（如果提供）
            if (!string.IsNullOrWhiteSpace(RuleExpression))
            {
                try
                {
                    // 这里可以添加更详细的规则表达式验证逻辑
                    if (!RuleExpression.Contains("WorkflowName"))
                    {
                        throw new ArgumentException("规则表达式格式不正确，必须包含 WorkflowName", nameof(RuleExpression));
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"规则表达式格式错误: {ex.Message}", nameof(RuleExpression));
                }
            }
        }

        /// <summary>
        /// 复制模板配置
        /// </summary>
        /// <param name="source">源模板</param>
        public virtual void CopyFrom(AttachCatalogueTemplate source)
        {
            ArgumentNullException.ThrowIfNull(source);

            TemplateName = source.TemplateName;
            AttachReceiveType = source.AttachReceiveType;
            NamePattern = source.NamePattern;
            RuleExpression = source.RuleExpression;
            SemanticModel = source.SemanticModel;
            IsRequired = source.IsRequired;
            SequenceNumber = source.SequenceNumber;
            IsStatic = source.IsStatic;
            ParentId = source.ParentId;
            TemplateType = source.TemplateType;
            TemplatePurpose = source.TemplatePurpose;
            SetTextVector(source.TextVector);
            Permissions = [.. source.Permissions.Select(p => new AttachCatalogueTemplatePermission(p.PermissionType, p.PermissionTarget, p.Action, p.Effect, p.AttributeConditions, p.EffectiveTime, p.ExpirationTime, p.Description))];
        }

        /// <summary>
        /// 检查是否为根模板
        /// </summary>
        public virtual bool IsRoot => ParentId == null;

        /// <summary>
        /// 检查是否为叶子模板
        /// </summary>
        public virtual bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 获取模板层级深度
        /// </summary>
        public virtual int GetDepth()
        {
            if (IsRoot) return 0;
            return 1; // 简化实现，实际应该递归计算
        }

        /// <summary>
        /// 获取模板路径
        /// </summary>
        public virtual string GetPath()
        {
            return TemplateName; // 简化实现，实际应该构建完整路径
        }

        /// <summary>
        /// 获取模板标识描述
        /// </summary>
        public virtual string GetTemplateIdentifierDescription()
        {
            return $"{TemplateType} - {TemplatePurpose}";
        }

        /// <summary>
        /// 检查是否匹配模板标识
        /// </summary>
        /// <param name="templateType">模板类型</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>是否匹配</returns>
        public virtual bool MatchesTemplateIdentifier(
            TemplateType? templateType = null,
            TemplatePurpose? templatePurpose = null)
        {
            return (templateType == null || TemplateType == templateType) &&
                   (templatePurpose == null || TemplatePurpose == templatePurpose);
        }

        /// <summary>
        /// 检查是否为项目级模板
        /// </summary>
        public virtual bool IsProjectTemplate => TemplateType == TemplateType.Project;

        /// <summary>
        /// 检查是否为阶段级模板
        /// </summary>
        public virtual bool IsPhaseTemplate => TemplateType == TemplateType.Phase;

        /// <summary>
        /// 检查是否为业务分类模板
        /// </summary>
        public virtual bool IsBusinessCategoryTemplate => TemplateType == TemplateType.BusinessCategory;

        /// <summary>
        /// 检查是否为专业领域模板
        /// </summary>
        public virtual bool IsProfessionalTemplate => TemplateType == TemplateType.Professional;

        /// <summary>
        /// 添加权限
        /// </summary>
        public virtual void AddPermission(AttachCatalogueTemplatePermission permission)
        {
            ArgumentNullException.ThrowIfNull(permission);
            
            Permissions ??= [];
                
            Permissions.Add(permission);
        }

        /// <summary>
        /// 移除权限
        /// </summary>
        public virtual void RemovePermission(AttachCatalogueTemplatePermission permission)
        {
            ArgumentNullException.ThrowIfNull(permission);
            
            Permissions?.Remove(permission);
        }

        /// <summary>
        /// 根据类型和操作获取权限
        /// </summary>
        public virtual AttachCatalogueTemplatePermission? GetPermission(PermissionAction action, string permissionType, string permissionTarget)
        {
            return Permissions?.FirstOrDefault(p => 
                p.Action == action && 
                p.PermissionType == permissionType && 
                p.PermissionTarget == permissionTarget);
        }

        /// <summary>
        /// 根据操作获取所有权限
        /// </summary>
        public virtual IEnumerable<AttachCatalogueTemplatePermission> GetPermissionsByAction(PermissionAction action)
        {
            return Permissions?.Where(p => p.Action == action) ?? [];
        }

        /// <summary>
        /// 根据类型获取所有权限
        /// </summary>
        public virtual IEnumerable<AttachCatalogueTemplatePermission> GetPermissionsByType(string permissionType)
        {
            return Permissions?.Where(p => p.PermissionType == permissionType) ?? [];
        }

        /// <summary>
        /// 检查用户是否具有指定权限
        /// </summary>
        public virtual bool HasPermission(Guid userId, PermissionAction action)
        {
            if (Permissions == null || Permissions.Count == 0)
                return false;

            // 检查直接权限
            var directPermissions = Permissions
                .Where(p => p.IsEffective() && p.Action == action)
                .ToList();

            if (directPermissions.Count != 0)
            {
                // 如果有拒绝权限，直接拒绝
                if (directPermissions.Any(p => p.Effect == PermissionEffect.Deny))
                    return false;
                
                // 如果有允许权限，返回允许
                return directPermissions.Any(p => p.Effect == PermissionEffect.Allow);
            }

            return false;
        }

        /// <summary>
        /// 获取权限摘要
        /// </summary>
        public virtual string GetPermissionSummary()
        {
            if (Permissions == null || Permissions.Count == 0)
                return "无权限配置";

            var summary = $"权限数量: {Permissions.Count}";
            var enabledCount = Permissions.Count(p => p.IsEnabled);
            var effectiveCount = Permissions.Count(p => p.IsEffective());
            
            summary += $", 启用: {enabledCount}, 有效: {effectiveCount}";
            
            return summary;
        }
    }
}
