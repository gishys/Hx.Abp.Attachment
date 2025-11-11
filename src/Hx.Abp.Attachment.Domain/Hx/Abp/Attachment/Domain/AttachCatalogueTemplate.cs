using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;

namespace Hx.Abp.Attachment.Domain
{
    public class AttachCatalogueTemplate : FullAuditedAggregateRoot
    {
        /// <summary>
        /// 模板ID（业务标识，同一模板的所有版本共享相同的ID）
        /// </summary>
        public virtual Guid Id { get; private set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public virtual int Version { get; private set; }

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
        /// 是否为最新版本
        /// </summary>
        public virtual bool IsLatest { get; private set; } = true;

        /// <summary>
        /// 附件类型
        /// </summary>
        public virtual AttachReceiveType AttachReceiveType { get; private set; }

        /// <summary>
        /// 工作流配置（JSON格式，存储工作流引擎参数）
        /// 包含：workflowKey、审批节点配置、超时设置、脚本触发、WebHook回调等
        /// </summary>
        [CanBeNull]
        public virtual string? WorkflowConfig { get; private set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public virtual bool IsRequired { get; private set; }

        /// <summary>
        /// 顺序号
        /// </summary>
        public virtual int SequenceNumber { get; private set; }

        /// <summary>
        /// 是否静态（是否为静态分面类型）
        /// </summary>
        public virtual bool IsStatic { get; private set; }

        /// <summary>
        /// 父模板Id
        /// </summary>
        public virtual Guid? ParentId { get; protected set; }

        /// <summary>
        /// 父模板版本号（用于复合主键场景下的父节点唯一标识）
        /// </summary>
        public virtual int? ParentVersion { get; protected set; }

        /// <summary>
        /// 模板路径（用于快速查询层级）
        /// 格式：00001.00002.00003（5位数字，用点分隔）
        /// </summary>
        [CanBeNull]
        public virtual string? TemplatePath { get; private set; }

        /// <summary>
        /// 子模板集合
        /// </summary>
        public virtual ICollection<AttachCatalogueTemplate> Children { get; private set; }

        /// <summary>
        /// 分面类型
        /// </summary>
        public virtual FacetType FacetType { get; private set; } = FacetType.General;

        /// <summary>
        /// 模板用途 - 标识模板的具体用途
        /// </summary>
        public virtual TemplatePurpose TemplatePurpose { get; private set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 模板角色 - 标识模板在层级结构中的角色
        /// 主要用于前端树状展示和动态分类树创建判断
        /// </summary>
        public virtual TemplateRole TemplateRole { get; private set; } = TemplateRole.Branch;

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
            Guid templateId,
            int version,
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired = false,
            bool isStatic = false,
            Guid? parentId = null,
            int? parentVersion = null,
            [CanBeNull] string? workflowConfig = null,
            bool isLatest = true,
            FacetType facetType = FacetType.General,
            TemplatePurpose templatePurpose = TemplatePurpose.Classification,
            TemplateRole templateRole = TemplateRole.Branch,
            [CanBeNull] List<double>? textVector = null,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null,
            [CanBeNull] string? templatePath = null)
        {
            Id = templateId;
            Version = version;
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            // 根据分面类型计算静态标识，前端传入值不生效
            IsStatic = Domain.Shared.FacetTypePolicies.IsStaticFacet(facetType);
            ParentId = parentId;
            ParentVersion = parentVersion;
            WorkflowConfig = workflowConfig;
            IsLatest = isLatest;
            FacetType = facetType;
            TemplatePurpose = templatePurpose;
            TemplateRole = templateRole;
            SetTextVector(textVector);
            Description = description;
            Tags = tags ?? [];
            MetaFields = metaFields ?? [];
            Children = [];
            Permissions = [];
            
            // 设置模板路径
            SetTemplatePath(templatePath);
        }

        public virtual void Update(
            [NotNull] string templateName,
            AttachReceiveType attachReceiveType,
            int sequenceNumber,
            bool isRequired,
            bool isStatic,
            [CanBeNull] string? workflowConfig,
            FacetType facetType,
            TemplatePurpose templatePurpose,
            TemplateRole templateRole,
            [CanBeNull] string? description = null,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<MetaField>? metaFields = null,
            [CanBeNull] string? templatePath = null)
        {
            TemplateName = Check.NotNullOrWhiteSpace(templateName, nameof(templateName));
            AttachReceiveType = attachReceiveType;
            SequenceNumber = sequenceNumber;
            IsRequired = isRequired;
            WorkflowConfig = workflowConfig;
            FacetType = facetType;
            TemplatePurpose = templatePurpose;
            TemplateRole = templateRole;
            // 分面类型变化后同步计算静态标识（由分面计算）
            IsStatic = Domain.Shared.FacetTypePolicies.IsStaticFacet(facetType);
            
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
            
            if (templatePath != null)
            {
                SetTemplatePath(templatePath);
            }
        }

        public virtual void SetIsLatest(bool isLatest)
        {
            IsLatest = isLatest;
        }

        /// <summary>
        /// 创建新版本的模板
        /// </summary>
        /// <param name="newVersion">新版本号</param>
        /// <param name="isLatest">是否是最新版本</param>
        /// <returns>新版本的模板实例</returns>
        public virtual AttachCatalogueTemplate CreateNewVersion(int newVersion, bool isLatest = true)
        {
            // 构造函数会根据 FacetType 自动计算 IsStatic，传入的 IsStatic 参数会被忽略
            return new AttachCatalogueTemplate(
                Id, // 使用 Id 获取模板ID
                newVersion,
                TemplateName,
                AttachReceiveType,
                SequenceNumber,
                IsRequired,
                false, // 此参数会被构造函数根据 FacetType 重新计算，传入任意值即可
                ParentId,
                ParentVersion,
                WorkflowConfig,
                isLatest,
                FacetType,
                TemplatePurpose,
                TemplateRole,
                TextVector,
                Description,
                Tags,
                MetaFields?.ToList(),
                TemplatePath);
        }

        public virtual void ChangeParent(Guid? parentId, int? parentVersion = null, [CanBeNull] string? parentTemplatePath = null)
        {
            ParentId = parentId;
            ParentVersion = parentVersion;
            
            // 如果提供了父模板路径，自动计算新的模板路径
            if (parentTemplatePath != null)
            {
                var newPath = CalculateNextTemplatePath(parentTemplatePath);
                SetTemplatePath(newPath);
            }
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
        /// 设置模板分面标识
        /// </summary>
        /// <param name="facetType">分面类型</param>
        /// <param name="templatePurpose">模板用途</param>
        public virtual void SetFacetIdentifiers(
            FacetType facetType,
            TemplatePurpose templatePurpose)
        {
            FacetType = facetType;
            TemplatePurpose = templatePurpose;
            // 分面改变时同步计算静态标识
            IsStatic = Domain.Shared.FacetTypePolicies.IsStaticFacet(facetType);
        }

        /// <summary>
        /// 设置静态标识（统一由分面类型推导，不接受外部直接设置）
        /// </summary>
        /// <param name="_">忽略的参数，保持方法签名兼容性</param>
        public virtual void SetIsStatic(bool _)
        {
            // 统一由分面类型推导，不接受外部直接设置
            IsStatic = Domain.Shared.FacetTypePolicies.IsStaticFacet(FacetType);
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
        /// 设置元数据字段集合
        /// </summary>
        /// <param name="metaFields">元数据字段列表</param>
        public virtual void SetMetaFields([CanBeNull] List<MetaField>? metaFields)
        {
            MetaFields = metaFields ?? [];
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

            // 验证工作流配置格式（如果提供）
            if (!string.IsNullOrWhiteSpace(WorkflowConfig))
            {
                try
                {
                    // 验证JSON格式
                    if (WorkflowConfig.TrimStart().StartsWith('{') && WorkflowConfig.TrimEnd().EndsWith('}'))
                    {
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(WorkflowConfig);
                    }
                    else
                    {
                        throw new ArgumentException("工作流配置必须是有效的JSON格式", nameof(WorkflowConfig));
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    throw new ArgumentException($"工作流配置JSON格式不正确: {ex.Message}", nameof(WorkflowConfig));
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"工作流配置验证失败: {ex.Message}", nameof(WorkflowConfig));
                }
            }

            // 验证模板路径格式
            if (!IsValidTemplatePath(TemplatePath))
            {
                throw new ArgumentException("模板路径格式不正确", nameof(TemplatePath));
            }

            // 验证元数据字段配置
            ValidateMetaFields();
        }

        /// <summary>
        /// 复制模板配置（不复制主键信息）
        /// </summary>
        /// <param name="source">源模板</param>
        public virtual void CopyFrom(AttachCatalogueTemplate source)
        {
            ArgumentNullException.ThrowIfNull(source);

            TemplateName = source.TemplateName;
            AttachReceiveType = source.AttachReceiveType;
            WorkflowConfig = source.WorkflowConfig;
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
            
            // 复制模板路径
            SetTemplatePath(source.TemplatePath);
        }

        /// <summary>
        /// 检查是否为根模板
        /// </summary>
        public virtual bool IsRoot => ParentId == null && IsRootTemplatePath(TemplatePath);

        /// <summary>
        /// 检查是否为叶子模板
        /// </summary>
        public virtual bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 获取模板层级深度
        /// </summary>
        public virtual int GetDepth()
        {
            return GetTemplatePathDepth(TemplatePath);
        }

        /// <summary>
        /// 获取模板路径
        /// </summary>
        public virtual string GetPath()
        {
            return TemplatePath ?? TemplateName; // 优先使用模板路径，否则使用模板名称
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
            
            // 添加工作流配置（如果包含有意义的关键词）
            if (!string.IsNullOrWhiteSpace(WorkflowConfig))
            {
                contentParts.Add(WorkflowConfig);
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

        #region 模板路径管理

        /// <summary>
        /// 设置模板路径
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        public virtual void SetTemplatePath([CanBeNull] string? templatePath)
        {
            TemplatePath = templatePath;
        }

        /// <summary>
        /// 生成下一个模板路径代码
        /// 示例：如果当前路径为 "00019.00055.00001"，返回 "00019.00055.00002"
        /// </summary>
        /// <param name="currentPath">当前路径</param>
        /// <returns>下一个路径代码</returns>
        public static string CalculateNextTemplatePath(string? currentPath)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                return CreateTemplatePathCode(1);
            }

            var parentPath = GetParentTemplatePath(currentPath);
            var lastUnitCode = GetLastUnitTemplatePathCode(currentPath);
            return AppendTemplatePathCode(parentPath, CreateTemplatePathCode(Convert.ToInt32(lastUnitCode) + 1));
        }

        /// <summary>
        /// 获取路径中的最后一个单元代码
        /// 示例：如果路径为 "00019.00055.00001"，返回 "00001"
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>最后一个单元代码</returns>
        public static string GetLastUnitTemplatePathCode(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new ArgumentNullException(nameof(templatePath), "模板路径不能为空");
            }

            var splittedCode = templatePath.Split('.');
            return splittedCode[^1];
        }

        /// <summary>
        /// 获取父级模板路径
        /// 示例：如果路径为 "00019.00055.00001"，返回 "00019.00055"
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>父级路径，如果是根节点则返回null</returns>
        public static string? GetParentTemplatePath(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
            {
                throw new ArgumentNullException(nameof(templatePath), "模板路径不能为空");
            }

            var splittedCode = templatePath.Split('.');
            if (splittedCode.Length == 1)
            {
                return null;
            }

            return string.Join(".", splittedCode.Take(splittedCode.Length - 1));
        }

        /// <summary>
        /// 创建模板路径代码
        /// 示例：如果数字为 4,2，则返回 "00004.00002"
        /// </summary>
        /// <param name="numbers">数字数组</param>
        /// <returns>格式化的路径代码</returns>
        public static string CreateTemplatePathCode(params int[] numbers)
        {
            if (numbers == null || numbers.Length == 0)
            {
                throw new ArgumentNullException(nameof(numbers), "数字数组不能为空");
            }

            return string.Join(".", numbers.Select(number => number.ToString($"D{AttachmentConstants.TEMPLATE_PATH_CODE_DIGITS}")));
        }

        /// <summary>
        /// 将子路径代码追加到父路径代码
        /// 示例：如果父路径为 "00001"，子路径为 "00042"，则返回 "00001.00042"
        /// </summary>
        /// <param name="parentPath">父路径，如果是根节点可以为null或空</param>
        /// <param name="childPath">子路径代码</param>
        /// <returns>完整的路径代码</returns>
        public static string AppendTemplatePathCode(string? parentPath, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
            {
                throw new ArgumentNullException(nameof(childPath), "子路径代码不能为空");
            }

            if (string.IsNullOrEmpty(parentPath))
            {
                return childPath;
            }

            return parentPath + "." + childPath;
        }

        /// <summary>
        /// 验证模板路径格式
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>是否为有效格式</returns>
        public static bool IsValidTemplatePath(string? templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return true; // 空路径是有效的（根节点）

            var parts = templatePath.Split('.');
            return parts.All(part => 
                part.Length == 5 && 
                part.All(char.IsDigit) && 
                int.TryParse(part, out _));
        }

        /// <summary>
        /// 获取模板路径的层级深度
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>层级深度（0表示根节点）</returns>
        public static int GetTemplatePathDepth(string? templatePath)
        {
            if (string.IsNullOrEmpty(templatePath))
                return 0;

            return templatePath.Split('.').Length;
        }

        /// <summary>
        /// 检查是否为根模板路径
        /// </summary>
        /// <param name="templatePath">模板路径</param>
        /// <returns>是否为根路径</returns>
        public static bool IsRootTemplatePath(string? templatePath)
        {
            return string.IsNullOrEmpty(templatePath);
        }

        /// <summary>
        /// 检查两个模板路径是否为父子关系
        /// </summary>
        /// <param name="parentPath">父路径</param>
        /// <param name="childPath">子路径</param>
        /// <returns>是否为父子关系</returns>
        public static bool IsParentChildPath(string? parentPath, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
                return false;

            if (string.IsNullOrEmpty(parentPath))
                return GetTemplatePathDepth(childPath) == 1;

            return childPath.StartsWith(parentPath + ".", StringComparison.Ordinal) &&
                   GetTemplatePathDepth(childPath) == GetTemplatePathDepth(parentPath) + 1;
        }

        /// <summary>
        /// 获取模板路径的显示名称（用于UI展示）
        /// </summary>
        /// <returns>格式化的路径显示名称</returns>
        public virtual string GetTemplatePathDisplayName()
        {
            if (string.IsNullOrEmpty(TemplatePath))
                return "根节点";

            var parts = TemplatePath.Split('.');
            return string.Join(" → ", parts.Select(part => int.Parse(part).ToString()));
        }

        #endregion

        #region 复合主键相关方法

        /// <summary>
        /// 获取复合主键
        /// </summary>
        /// <returns>复合主键对象</returns>
        public override object[] GetKeys()
        {
            return [Id, Version];
        }

        /// <summary>
        /// 重写Equals方法，支持复合主键比较
        /// </summary>
        /// <param name="obj">要比较的对象</param>
        /// <returns>是否相等</returns>
        public override bool Equals(object? obj)
        {
            if (obj is AttachCatalogueTemplate other)
            {
                return Id == other.Id && Version == other.Version;
            }
            return false;
        }

        /// <summary>
        /// 重写GetHashCode方法，支持复合主键
        /// </summary>
        /// <returns>哈希码</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Version);
        }

        /// <summary>
        /// 重写ToString方法
        /// </summary>
        /// <returns>字符串表示</returns>
        public override string ToString()
        {
            return $"{Id}_{Version}";
        }

        #endregion

        #region 模板角色相关方法

        /// <summary>
        /// 获取模板完整分类描述
        /// </summary>
        /// <returns>完整的分类描述</returns>
        public virtual string GetFullClassificationDescription()
        {
            return $"{TemplateRole} - {FacetType} - {TemplatePurpose}";
        }

        /// <summary>
        /// 检查是否为根模板（可以作为根节点创建动态分类树）
        /// </summary>
        public virtual bool IsRootTemplate => TemplateRole == TemplateRole.Root;

        /// <summary>
        /// 检查是否为导航模板（仅用于导航，不参与动态分类树创建）
        /// </summary>
        public virtual bool IsNavigationTemplate => TemplateRole == TemplateRole.Navigation;


        /// <summary>
        /// 检查是否为分支节点（可以有子节点，但不能直接上传文件）
        /// </summary>
        public virtual bool IsBranchNode => TemplateRole == TemplateRole.Branch;

        /// <summary>
        /// 检查是否为叶子节点（不能有子节点，但可以直接上传文件）
        /// </summary>
        public virtual bool IsLeafNode => TemplateRole == TemplateRole.Leaf;

        /// <summary>
        /// 检查是否可以参与动态分类树创建
        /// </summary>
        /// <returns>是否可以参与动态分类树创建</returns>
        public virtual bool CanParticipateInDynamicTree()
        {
            return TemplateRole == TemplateRole.Root;
        }

        /// <summary>
        /// 检查是否可以包含子模板
        /// 根模板和分支节点都可以包含子模板，子模板的层级关系由父子关系决定
        /// </summary>
        /// <returns>是否可以包含子模板</returns>
        public virtual bool CanContainChildTemplates()
        {
            return TemplateRole == TemplateRole.Root || 
                   TemplateRole == TemplateRole.Branch;
        }

        /// <summary>
        /// 检查是否支持文件上传
        /// 只有叶子节点才支持直接上传文件，分支节点用于组织分类结构
        /// </summary>
        /// <returns>是否支持文件上传</returns>
        public virtual bool SupportsFileUpload()
        {
            return TemplateRole == TemplateRole.Leaf;
        }

        /// <summary>
        /// 检查是否支持文件存储
        /// 根据模板用途判断是否支持文件存储
        /// </summary>
        /// <returns>是否支持文件存储</returns>
        public virtual bool SupportsFileStorage()
        {
            return TemplatePurpose == TemplatePurpose.Document;
        }

        #endregion
    }
}
