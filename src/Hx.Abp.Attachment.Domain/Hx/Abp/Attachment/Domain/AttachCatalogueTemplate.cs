using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

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
        /// 模板版本号 (新增)
        /// </summary>
        public virtual int Version { get; private set; } = 1;

        /// <summary>
        /// 是否为最新版本 (新增)
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
            bool isLatest = true)
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
            Children = [];
        }

        public virtual void Update(
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired,
            bool isStatic,
            [CanBeNull] string namePattern,
            [CanBeNull] string ruleExpression,
            [CanBeNull] string semanticModel)
        {
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            IsStatic = isStatic;
            NamePattern = namePattern;
            RuleExpression = ruleExpression;
            SemanticModel = semanticModel;
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
    }
}
