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
        /// 模板描述
        /// </summary>
        [CanBeNull]
        public virtual string? Description { get; private set; }

        /// <summary>
        /// 模板标签（JSON数组格式，用于全文检索）
        /// </summary>
        [CanBeNull]
        public virtual List<string>? Tags { get; private set; }

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
        /// 规则引擎表达式
        /// </summary>
        [CanBeNull]
        public virtual string? RuleExpression { get; private set; }

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
        /// 分面类型 - 标识模板的层级和用途
        /// </summary>
        public virtual FacetType FacetType { get; private set; } = FacetType.General;

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

        /// <summary>
        /// 元数据字段集合（JSONB格式，存储元数据字段信息）
        /// 用于命名实体识别(NER)、前端展示和业务场景配置
        /// </summary>
        public virtual ICollection<MetaField> MetaFields { get; private set; } = [];

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
            [CanBeNull] string? ruleExpression = null,
            int version = 1,
            bool isLatest = true,
            FacetType facetType = FacetType.General,
            TemplatePurpose templatePurpose = TemplatePurpose.Classification,
            [CanBeNull] List<double>? textVector = null,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null)
        {
            Id = id;
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            IsStatic = isStatic;
            ParentId = parentId;
            RuleExpression = ruleExpression;
            Version = version;
            IsLatest = isLatest;
            FacetType = facetType;
            TemplatePurpose = templatePurpose;
            SetTextVector(textVector);
            Description = description;
            Tags = tags ?? [];
            MetaFields = metaFields ?? [];
            Children = [];
            Permissions = [];
        }

        public virtual void Update(
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired,
            bool isStatic,
            [CanBeNull] string ruleExpression,
            FacetType facetType,
            TemplatePurpose templatePurpose,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null)
        {
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            IsStatic = isStatic;
            RuleExpression = ruleExpression;
            FacetType = facetType;
            TemplatePurpose = templatePurpose;
            
            if (description != null)
            {
                Description = description;
            }
            
            if (tags != null)
            {
                Tags = tags;
            }
            
            if (metaFields != null)
            {
                MetaFields = metaFields;
            }
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
        /// 设置模板描述
        /// </summary>
        /// <param name="description">模板描述</param>
        public virtual void SetDescription([CanBeNull] string? description)
        {
            Description = description;
        }

        /// <summary>
        /// 设置模板标签
        /// </summary>
        /// <param name="tags">模板标签列表</param>
        public virtual void SetTags([CanBeNull] List<string>? tags)
        {
            Tags = tags ?? [];
        }

        /// <summary>
        /// 添加单个标签
        /// </summary>
        /// <param name="tag">标签</param>
        public virtual void AddTag(string tag)
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                Tags ??= [];
                if (!Tags.Contains(tag))
                {
                    Tags.Add(tag);
                }
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        /// <param name="tag">要移除的标签</param>
        public virtual void RemoveTag(string tag)
        {
            Tags?.Remove(tag);
        }

        /// <summary>
        /// 添加元数据字段
        /// </summary>
        /// <param name="metaField">元数据字段</param>
        public virtual void AddMetaField(MetaField metaField)
        {
            ArgumentNullException.ThrowIfNull(metaField);
            
            MetaFields ??= [];
            
            // 检查字段键名是否已存在
            if (MetaFields.Any(f => f.FieldKey == metaField.FieldKey))
            {
                throw new ArgumentException($"字段键名 '{metaField.FieldKey}' 已存在", nameof(metaField));
            }
            
            MetaFields.Add(metaField);
        }

        /// <summary>
        /// 移除元数据字段
        /// </summary>
        /// <param name="fieldKey">字段键名</param>
        public virtual void RemoveMetaField(string fieldKey)
        {
            if (!string.IsNullOrWhiteSpace(fieldKey))
            {
                var field = MetaFields?.FirstOrDefault(f => f.FieldKey == fieldKey);
                if (field != null)
                {
                    MetaFields?.Remove(field);
                }
            }
        }

        /// <summary>
        /// 更新元数据字段
        /// </summary>
        /// <param name="fieldKey">字段键名</param>
        /// <param name="metaField">更新后的元数据字段</param>
        public virtual void UpdateMetaField(string fieldKey, MetaField metaField)
        {
            ArgumentNullException.ThrowIfNull(metaField);
            
            if (string.IsNullOrWhiteSpace(fieldKey))
                throw new ArgumentException("字段键名不能为空", nameof(fieldKey));
            
            var existingField = (MetaFields?.FirstOrDefault(f => f.FieldKey == fieldKey)) ?? throw new ArgumentException($"字段键名 '{fieldKey}' 不存在", nameof(fieldKey));

            // 检查新字段键名是否与其他字段冲突
            if (fieldKey != metaField.FieldKey && 
                MetaFields?.Any(f => f.FieldKey == metaField.FieldKey) == true)
            {
                throw new ArgumentException($"字段键名 '{metaField.FieldKey}' 已存在", nameof(metaField));
            }
            
            // 移除旧字段，添加新字段
            MetaFields?.Remove(existingField);
            MetaFields?.Add(metaField);
        }

        /// <summary>
        /// 获取元数据字段
        /// </summary>
        /// <param name="fieldKey">字段键名</param>
        /// <returns>元数据字段，如果不存在则返回null</returns>
        public virtual MetaField? GetMetaField(string fieldKey)
        {
            if (string.IsNullOrWhiteSpace(fieldKey))
                return null;
                
            return MetaFields?.FirstOrDefault(f => f.FieldKey == fieldKey);
        }

        /// <summary>
        /// 获取所有启用的元数据字段
        /// </summary>
        /// <returns>启用的元数据字段列表</returns>
        public virtual IEnumerable<MetaField> GetEnabledMetaFields()
        {
            return MetaFields?.Where(f => f.IsEnabled) ?? [];
        }

        /// <summary>
        /// 根据实体类型获取元数据字段
        /// </summary>
        /// <param name="entityType">实体类型</param>
        /// <returns>匹配的元数据字段列表</returns>
        public virtual IEnumerable<MetaField> GetMetaFieldsByEntityType(string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return [];
                
            return MetaFields?.Where(f => f.EntityType == entityType && f.IsEnabled) ?? [];
        }

        /// <summary>
        /// 根据数据类型获取元数据字段
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <returns>匹配的元数据字段列表</returns>
        public virtual IEnumerable<MetaField> GetMetaFieldsByDataType(string dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType))
                return [];
                
            return MetaFields?.Where(f => f.DataType == dataType && f.IsEnabled) ?? [];
        }

        /// <summary>
        /// 获取必填的元数据字段
        /// </summary>
        /// <returns>必填的元数据字段列表</returns>
        public virtual IEnumerable<MetaField> GetRequiredMetaFields()
        {
            return MetaFields?.Where(f => f.IsRequired && f.IsEnabled) ?? [];
        }

        /// <summary>
        /// 验证元数据字段配置
        /// </summary>
        public virtual void ValidateMetaFields()
        {
            if (MetaFields == null || MetaFields.Count == 0)
                return;
                
            foreach (var field in MetaFields)
            {
                try
                {
                    field.Validate();
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"元数据字段 '{field.FieldKey}' 验证失败: {ex.Message}", nameof(MetaFields));
                }
            }
            
            // 检查字段键名唯一性
            var duplicateKeys = MetaFields
                .GroupBy(f => f.FieldKey)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
                
            if (duplicateKeys.Count > 0)
            {
                throw new ArgumentException($"存在重复的字段键名: {string.Join(", ", duplicateKeys)}", nameof(MetaFields));
            }
        }

        /// <summary>
        /// 设置文本向量
        /// </summary>
        /// <param name="textVector">文本向量</param>
        public virtual void SetTextVector([CanBeNull] List<double>? textVector)
        {
            if (textVector != null && textVector.Count > 0)
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
        /// <param name="facetType">分面类型</param>
        /// <param name="templatePurpose">模板用途</param>
        public virtual void SetTemplateIdentifiers(
            FacetType facetType,
            TemplatePurpose templatePurpose)
        {
            FacetType = facetType;
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

            // 验证向量维度（只有当向量存在且不为空时才验证）
            if (TextVector != null && TextVector.Count > 0 && (VectorDimension < 64 || VectorDimension > 2048))
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

            // 验证元数据字段配置
            ValidateMetaFields();
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
            RuleExpression = source.RuleExpression;
            IsRequired = source.IsRequired;
            SequenceNumber = source.SequenceNumber;
            IsStatic = source.IsStatic;
            ParentId = source.ParentId;
            FacetType = source.FacetType;
            TemplatePurpose = source.TemplatePurpose;
            SetTextVector(source.TextVector);
            Description = source.Description;
            Tags = source.Tags != null ? [.. source.Tags] : [];
            MetaFields = source.MetaFields != null ? [.. source.MetaFields.Select(f => new MetaField(f.EntityType, f.FieldKey, f.FieldName, f.DataType, f.IsRequired, f.Unit, f.RegexPattern, f.Options, f.Description, f.DefaultValue, f.Order, f.IsEnabled, f.Group, f.ValidationRules, f.Tags != null ? [.. f.Tags] : []))] : [];
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
            return $"{FacetType} - {TemplatePurpose}";
        }

        /// <summary>
        /// 检查是否匹配模板标识
        /// </summary>
        /// <param name="facetType">分面类型</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>是否匹配</returns>
        public virtual bool MatchesTemplateIdentifier(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null)
        {
            return (facetType == null || FacetType == facetType) &&
                   (templatePurpose == null || TemplatePurpose == templatePurpose);
        }

        /// <summary>
        /// 检查是否为项目类型分面
        /// </summary>
        public virtual bool IsProjectTypeFacet => FacetType == FacetType.ProjectType;

        /// <summary>
        /// 检查是否为阶段分面
        /// </summary>
        public virtual bool IsPhaseFacet => FacetType == FacetType.Phase;

        /// <summary>
        /// 检查是否为专业领域分面
        /// </summary>
        public virtual bool IsDisciplineFacet => FacetType == FacetType.Discipline;

        /// <summary>
        /// 检查是否为文档类型分面
        /// </summary>
        public virtual bool IsDocumentTypeFacet => FacetType == FacetType.DocumentType;

        /// <summary>
        /// 检查是否为组织维度分面
        /// </summary>
        public virtual bool IsOrganizationFacet => FacetType == FacetType.Organization;

        /// <summary>
        /// 检查是否为时间切片分面
        /// </summary>
        public virtual bool IsTimeSliceFacet => FacetType == FacetType.TimeSlice;

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
        /// 获取全文检索内容（用于倒排索引）
        /// </summary>
        public virtual string GetFullTextContent()
        {
            var contentParts = new List<string>();
            
            // 添加模板名称
            if (!string.IsNullOrWhiteSpace(TemplateName))
            {
                contentParts.Add(TemplateName);
            }
            
            // 添加描述
            if (!string.IsNullOrWhiteSpace(Description))
            {
                contentParts.Add(Description);
            }
            
            // 添加标签
            if (Tags != null && Tags.Count > 0)
            {
                contentParts.AddRange(Tags);
            }
            
            // 添加规则表达式（如果包含有意义的关键词）
            if (!string.IsNullOrWhiteSpace(RuleExpression))
            {
                contentParts.Add(RuleExpression);
            }
            
            // 添加元数据字段信息
            if (MetaFields != null && MetaFields.Count > 0)
            {
                foreach (var field in MetaFields.Where(f => f.IsEnabled))
                {
                    contentParts.Add(field.FieldName);
                    contentParts.Add(field.FieldKey);
                    if (!string.IsNullOrWhiteSpace(field.Description))
                    {
                        contentParts.Add(field.Description);
                    }
                    if (field.Tags != null && field.Tags.Count > 0)
                    {
                        contentParts.AddRange(field.Tags);
                    }
                }
            }
            
            return string.Join(" ", contentParts);
        }

        /// <summary>
        /// 检查是否包含关键词（用于全文检索）
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="searchInTags">是否在标签中搜索</param>
        /// <returns>是否包含关键词</returns>
        public virtual bool ContainsKeyword(string keyword, bool searchInTags = true)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return false;
                
            var lowerKeyword = keyword.ToLowerInvariant();
            
            // 在模板名称中搜索
            if (TemplateName.Contains(lowerKeyword, StringComparison.InvariantCultureIgnoreCase))
                return true;
                
            // 在描述中搜索
            if (!string.IsNullOrWhiteSpace(Description) && 
                Description.Contains(lowerKeyword, StringComparison.InvariantCultureIgnoreCase))
                return true;
                
            // 在标签中搜索
            if (searchInTags && Tags != null)
            {
                if (Tags.Any(tag => !string.IsNullOrWhiteSpace(tag) && 
                                   tag.Contains(lowerKeyword, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
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
