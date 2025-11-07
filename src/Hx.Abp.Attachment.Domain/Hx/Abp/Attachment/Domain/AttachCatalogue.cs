using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using System.Collections.ObjectModel;
using Volo.Abp.Domain.Entities.Auditing;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件分类
    /// </summary>
    public class AttachCatalogue : FullAuditedAggregateRoot<Guid>
    {
        /// <summary>
        /// 业务类型Id
        /// </summary>
        public virtual string Reference { get; private set; }
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public virtual int ReferenceType { get; private set; }
        /// <summary>
        /// 父Id
        /// </summary>
        public virtual Guid? ParentId { get; protected set; }
        /// <summary>
        /// 附件类型
        /// </summary>
        public virtual AttachReceiveType AttachReceiveType { get; private set; }
        /// <summary>
        /// 分类名称
        /// </summary>
        public virtual string CatalogueName { get; private set; }

        /// <summary>
        /// 分类标签（JSON数组格式，用于全文检索）
        /// </summary>
        [CanBeNull]
        public virtual List<string>? Tags { get; private set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public virtual int AttachCount { get; private set; }
        /// <summary>
        /// 页数
        /// </summary>
        public virtual int PageCount { get; private set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public virtual bool IsVerification { get; private set; }
        /// <summary>
        /// 核验通过
        /// </summary>
        public virtual bool VerificationPassed { get; private set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public virtual bool IsRequired { get; private set; }
        /// <summary>
        /// 顺序号
        /// </summary>
        public virtual int SequenceNumber { get; private set; }
        /// <summary>
        /// 静态标识
        /// </summary>
        public virtual bool IsStatic { get; private set; }

        /// <summary>
        /// 归档标识 - 标识分类是否已归档
        /// </summary>
        public virtual bool IsArchived { get; private set; } = false;

        /// <summary>
        /// 概要信息 - 分类的描述信息
        /// </summary>
        [CanBeNull]
        public virtual string? Summary { get; private set; }

        /// <summary>
        /// 关联的模板ID
        /// </summary>
        public virtual Guid? TemplateId { get; private set; }

        /// <summary>
        /// 关联的模板版本号
        /// </summary>
        public virtual int? TemplateVersion { get; private set; }

        /// <summary>
        /// 全文内容 - 存储分类下所有文件的OCR提取内容
        /// </summary>
        public virtual string? FullTextContent { get; private set; }

        /// <summary>
        /// 全文内容更新时间
        /// </summary>
        public virtual DateTime? FullTextContentUpdatedTime { get; private set; }


        /// <summary>
        /// 分类分面类型 - 标识分类的层级和用途
        /// </summary>
        public virtual FacetType CatalogueFacetType { get; private set; } = FacetType.General;

        /// <summary>
        /// 分类用途 - 标识分类的具体用途
        /// </summary>
        public virtual TemplatePurpose CataloguePurpose { get; private set; } = TemplatePurpose.Classification;

        /// <summary>
        /// 分类角色 - 标识分类在层级结构中的角色
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
        /// 分类路径（用于快速查询层级）
        /// 格式：0000001.0000002.0000003（7位数字，用点分隔）
        /// </summary>
        [CanBeNull]
        public virtual string? Path { get; private set; }

        /// <summary>
        /// 权限集合（JSONB格式，存储权限值对象数组）
        /// </summary>
        public virtual ICollection<AttachCatalogueTemplatePermission> Permissions { get; private set; } = [];

        /// <summary>
        /// 元数据字段集合（JSONB格式，存储元数据字段信息）
        /// 用于命名实体识别(NER)、前端展示和业务场景配置
        /// </summary>
        public virtual ICollection<MetaField> MetaFields { get; private set; } = [];

        /// <summary>
        /// 子文件夹
        /// </summary>
        public virtual ICollection<AttachCatalogue> Children { get; private set; }
        /// <summary>
        /// 附件文件集合
        /// </summary>
        public virtual Collection<AttachFile> AttachFiles { get; private set; }

        /// <summary>
        /// 提供给ORM用来从数据库中获取实体，
        /// 无需初始化子集合因为它会被来自数据库的值覆盖
        /// </summary>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        protected AttachCatalogue()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        { }
        public AttachCatalogue(
            Guid id,
            AttachReceiveType attachReceiveType,
            string catologueName,
            int sequenceNumber,
            string reference,
            int referenceType,
            Guid? parentId = null,
            bool isRequired = false,
            bool isVerification = false,
            bool verificationPassed = false,
            bool isStatic = false,
            int attachCount = 0,
            int pageCount = 0,
            Guid? templateId = null,
            int? templateVersion = null,
            FacetType catalogueFacetType = FacetType.General,
            TemplatePurpose cataloguePurpose = TemplatePurpose.Classification,
            TemplateRole templateRole = TemplateRole.Branch,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<double>? textVector = null,
            [CanBeNull] List<MetaField>? metaFields = null,
            [CanBeNull] string? path = null,
            bool isArchived = false,
            [CanBeNull] string? summary = null)
        {
            Id = id;
            AttachReceiveType = attachReceiveType;
            CatalogueName = catologueName;
            SequenceNumber = sequenceNumber;
            Reference = reference;
            ReferenceType = referenceType;
            ParentId = parentId;
            IsRequired = isRequired;
            IsVerification = isVerification;
            VerificationPassed = verificationPassed;
            AttachCount = attachCount;
            PageCount = pageCount;
            IsStatic = isStatic;
            IsArchived = isArchived;
            Summary = summary;
            TemplateId = templateId;
            TemplateVersion = templateVersion;
            CatalogueFacetType = catalogueFacetType;
            CataloguePurpose = cataloguePurpose;
            TemplateRole = templateRole;
            Tags = tags ?? [];
            SetTextVector(textVector);
            MetaFields = metaFields ?? [];
            SetPath(path);
            AttachFiles = [];
            Children = [];
            Permissions = [];
        }
        /// <summary>
        /// 验证
        /// </summary>
        public void Verify() => IsVerification = true;
        /// <summary>
        /// 添加附件
        /// </summary>
        /// <param name="attach">文件</param>
        public virtual void AddAttachFile(AttachFile attach, int pageCount)
        {
            AttachFiles.Add(attach);
            AttachCount = AttachFiles.Count;
            PageCount = pageCount;
        }
        public virtual void RemoveFiles(Func<AttachFile, bool> func, int pageCount)
        {
            AttachFiles.RemoveAll(func);
            AttachCount = AttachFiles.Count;
            PageCount = pageCount;
        }
        public virtual void CalculatePageCount(int pageCount)
        {
            PageCount += pageCount;
        }
        public virtual void AddAttachCount(int count = 1)
        {
            AttachCount += count;
        }
        public virtual void AddAttachCatalogue(AttachCatalogue attachCatalogue)
        {
            Children.Add(attachCatalogue);
        }
        public virtual void RemoveAttachCatalogue(Func<AttachCatalogue, bool> func)
        {
            Children.RemoveAll(func);
        }
        public virtual void SetAttachReceiveType([NotNull] AttachReceiveType attachReceiveType) => AttachReceiveType = attachReceiveType;
        public virtual void SetCatalogueName([NotNull] string catalogueName) => CatalogueName = catalogueName;
        public virtual void SetReference([NotNull] string reference, int referenceType)
        {
            Reference = reference;
            ReferenceType = referenceType;
        }
        public virtual void SetSequenceNumber(int sequenceNumber)
        {
            SequenceNumber = sequenceNumber;
        }
        public virtual void RemoveTo(Guid? parentId) => ParentId = parentId;
        public virtual void SetIsVerification(bool isVerification) => IsVerification = isVerification;
        public virtual void SetIsRequired(bool isIsRequired) => IsRequired = isIsRequired;
        public virtual void SetIsStatic(bool isIsStatic) => IsStatic = isIsStatic;

        /// <summary>
        /// 设置归档状态
        /// </summary>
        /// <param name="isArchived">是否归档</param>
        public virtual void SetIsArchived(bool isArchived) => IsArchived = isArchived;

        /// <summary>
        /// 设置概要信息
        /// </summary>
        /// <param name="summary">概要信息</param>
        public virtual void SetSummary([CanBeNull] string? summary) => Summary = summary;

        /// <summary>
        /// 设置关联的模板ID
        /// </summary>
        /// <param name="templateId">模板ID</param>
        public virtual void SetTemplateId(Guid? templateId) => TemplateId = templateId;

        /// <summary>
        /// 设置关联的模板版本号
        /// </summary>
        /// <param name="templateVersion">模板版本号</param>
        public virtual void SetTemplateVersion(int? templateVersion) => TemplateVersion = templateVersion;

        /// <summary>
        /// 设置关联的模板ID和版本号
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号</param>
        public virtual void SetTemplate(Guid? templateId, int? templateVersion)
        {
            TemplateId = templateId;
            TemplateVersion = templateVersion;
        }

        /// <summary>
        /// 设置分类角色
        /// </summary>
        /// <param name="templateRole">分类角色</param>
        public virtual void SetTemplateRole(TemplateRole templateRole) => TemplateRole = templateRole;


        /// <summary>
        /// 设置全文内容
        /// </summary>
        /// <param name="fullTextContent">全文内容</param>
        public virtual void SetFullTextContent(string? fullTextContent)
        {
            FullTextContent = fullTextContent;
            FullTextContentUpdatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新全文内容（追加模式）
        /// </summary>
        /// <param name="additionalContent">追加的内容</param>
        public virtual void AppendFullTextContent(string additionalContent)
        {
            if (string.IsNullOrWhiteSpace(additionalContent))
                return;

            FullTextContent = string.IsNullOrWhiteSpace(FullTextContent) 
                ? additionalContent 
                : $"{FullTextContent}\n{additionalContent}";
            FullTextContentUpdatedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 重新生成全文内容（基于所有附件文件）
        /// </summary>
        public virtual void RegenerateFullTextContent()
        {
            if (AttachFiles == null || AttachFiles.Count == 0)
            {
                FullTextContent = null;
                FullTextContentUpdatedTime = DateTime.UtcNow;
                return;
            }

            // 收集所有文件的OCR内容
            var contentParts = AttachFiles
                .Where(f => !string.IsNullOrWhiteSpace(f.OcrContent))
                .Select(f => f.OcrContent)
                .ToList();

            FullTextContent = contentParts.Count != 0 ? string.Join("\n", contentParts) : null;
            FullTextContentUpdatedTime = DateTime.UtcNow;
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
        /// 设置分类标识
        /// </summary>
        /// <param name="catalogueFacetType">分类分面类型</param>
        /// <param name="cataloguePurpose">分类用途</param>
        public virtual void SetCatalogueIdentifiers(
            FacetType catalogueFacetType,
            TemplatePurpose cataloguePurpose)
        {
            CatalogueFacetType = catalogueFacetType;
            CataloguePurpose = cataloguePurpose;
        }

        /// <summary>
        /// 验证分类配置
        /// </summary>
        public virtual void ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(CatalogueName))
            {
                throw new ArgumentException("分类名称不能为空", nameof(CatalogueName));
            }

            // 验证向量维度（只有当向量存在且不为空时才验证）
            if (TextVector != null && TextVector.Count > 0 && (VectorDimension < 64 || VectorDimension > 2048))
            {
                throw new ArgumentException("向量维度必须在64到2048之间", nameof(TextVector));
            }

            // 验证路径格式
            if (!IsValidPath(Path))
            {
                throw new ArgumentException("分类路径格式不正确", nameof(Path));
            }

            // 验证元数据字段配置
            ValidateMetaFields();
        }

        /// <summary>
        /// 检查是否为根分类
        /// </summary>
        public virtual bool IsRoot => ParentId == null && IsRootPath(Path);

        /// <summary>
        /// 检查是否为叶子分类
        /// </summary>
        public virtual bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 获取分类层级深度
        /// </summary>
        public virtual int GetDepth()
        {
            return GetPathDepth(Path);
        }

        /// <summary>
        /// 获取分类路径
        /// </summary>
        public virtual string GetPath()
        {
            return Path ?? CatalogueName; // 优先使用路径，否则使用分类名称
        }

        /// <summary>
        /// 获取分类标识描述
        /// </summary>
        public virtual string GetCatalogueIdentifierDescription()
        {
            return $"{CatalogueFacetType} - {CataloguePurpose}";
        }

        /// <summary>
        /// 检查是否匹配分类标识
        /// </summary>
        /// <param name="catalogueFacetType">分类分面类型</param>
        /// <param name="cataloguePurpose">分类用途</param>
        /// <returns>是否匹配</returns>
        public virtual bool MatchesCatalogueIdentifier(
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null)
        {
            return (catalogueFacetType == null || CatalogueFacetType == catalogueFacetType) &&
                   (cataloguePurpose == null || CataloguePurpose == cataloguePurpose);
        }

        /// <summary>
        /// 检查是否为项目类型分面分类
        /// </summary>
        public virtual bool IsProjectTypeFacetCatalogue => CatalogueFacetType == FacetType.ProjectType;

        /// <summary>
        /// 检查是否为阶段分面分类
        /// </summary>
        public virtual bool IsPhaseFacetCatalogue => CatalogueFacetType == FacetType.Phase;

        /// <summary>
        /// 检查是否为专业领域分面分类
        /// </summary>
        public virtual bool IsDisciplineFacetCatalogue => CatalogueFacetType == FacetType.Discipline;

        /// <summary>
        /// 检查是否为文档类型分面分类
        /// </summary>
        public virtual bool IsDocumentTypeFacetCatalogue => CatalogueFacetType == FacetType.DocumentType;

        /// <summary>
        /// 检查是否为组织维度分面分类
        /// </summary>
        public virtual bool IsOrganizationFacetCatalogue => CatalogueFacetType == FacetType.Organization;

        /// <summary>
        /// 检查是否为时间切片分面分类
        /// </summary>
        public virtual bool IsTimeSliceFacetCatalogue => CatalogueFacetType == FacetType.TimeSlice;

        /// <summary>
        /// 添加权限
        /// </summary>
        public virtual void AddPermission(AttachCatalogueTemplatePermission permission)
        {
            ArgumentNullException.ThrowIfNull(permission);

            // 验证权限对象格式
            ValidatePermissionObject(permission);
            
            Permissions ??= [];
                
            Permissions.Add(permission);
        }

        /// <summary>
        /// 验证权限对象格式
        /// </summary>
        private static void ValidatePermissionObject(AttachCatalogueTemplatePermission permission)
        {
            ArgumentNullException.ThrowIfNull(permission);

            // 验证必需字段
            if (string.IsNullOrWhiteSpace(permission.PermissionType))
                throw new ArgumentException("权限类型不能为空", nameof(permission));

            if (string.IsNullOrWhiteSpace(permission.PermissionTarget))
                throw new ArgumentException("权限目标不能为空", nameof(permission));

            // 验证操作类型
            if (!Enum.IsDefined(typeof(PermissionAction), permission.Action))
                throw new ArgumentException("无效的权限操作类型", nameof(permission));

            // 验证效果类型
            if (!Enum.IsDefined(typeof(PermissionEffect), permission.Effect))
                throw new ArgumentException("无效的权限效果类型", nameof(permission));

            // 验证时间逻辑
            if (permission.EffectiveTime.HasValue && permission.ExpirationTime.HasValue)
            {
                if (permission.EffectiveTime >= permission.ExpirationTime)
                    throw new ArgumentException("生效时间必须早于过期时间", nameof(permission));
            }
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
        /// 支持用户权限和角色权限检查
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="action">权限操作</param>
        /// <param name="userRoles">用户角色列表（可选，用于角色权限检查）</param>
        /// <returns>是否具有权限</returns>
        public virtual bool HasPermission(Guid userId, PermissionAction action, List<string>? userRoles = null)
        {
            if (Permissions == null || Permissions.Count == 0)
                return false;

            // 检查直接权限
            var relevantPermissions = Permissions
                .Where(p => p.IsEffective() && p.Action == action)
                .ToList();

            if (relevantPermissions.Count == 0)
                return false;

            // 检查用户权限
            var userPermissions = relevantPermissions
                .Where(p => p.PermissionType == "User" && 
                           p.PermissionTarget == userId.ToString())
                .ToList();

            // 检查角色权限
            var rolePermissions = new List<AttachCatalogueTemplatePermission>();
            if (userRoles != null && userRoles.Count > 0)
            {
                rolePermissions = [.. relevantPermissions
                    .Where(p => p.PermissionType == "Role" && 
                               userRoles.Contains(p.PermissionTarget))];
            }

            // 合并所有相关权限
            var allPermissions = userPermissions.Concat(rolePermissions).ToList();

            if (allPermissions.Count == 0)
                return false;

            // 权限优先级：拒绝 > 允许 > 继承
            // 如果有拒绝权限，直接拒绝
            if (allPermissions.Any(p => p.Effect == PermissionEffect.Deny))
                return false;
            
            // 如果有允许权限，返回允许
            if (allPermissions.Any(p => p.Effect == PermissionEffect.Allow))
                return true;

            // 继承权限需要进一步处理（在权限检查器中处理）
            return false;
        }

        /// <summary>
        /// 检查继承权限（从父分类和模板）
        /// 注意：此方法需要外部提供父分类和模板实体，实体类不直接访问仓储
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="action">权限操作</param>
        /// <param name="userRoles">用户角色列表</param>
        /// <param name="parentCatalogue">父分类实体（可选）</param>
        /// <param name="template">模板实体（可选）</param>
        /// <returns>是否具有继承权限</returns>
        public virtual bool HasInheritedPermission(
            Guid userId,
            PermissionAction action,
            List<string>? userRoles = null,
            AttachCatalogue? parentCatalogue = null,
            AttachCatalogueTemplate? template = null)
        {
            // 1. 从父分类继承权限
            if (parentCatalogue != null)
            {
                // 检查父分类的直接权限
                var hasParentPermission = parentCatalogue.HasPermission(userId, action, userRoles);
                if (hasParentPermission)
                {
                    return true;
                }

                // 检查父分类的继承权限（Effect == Inherit）
                if (parentCatalogue.Permissions != null && parentCatalogue.Permissions.Count > 0)
                {
                    var inheritedPermissions = parentCatalogue.Permissions
                        .Where(p => p.IsEffective() && 
                                   p.Action == action && 
                                   p.Effect == PermissionEffect.Inherit)
                        .ToList();

                    if (inheritedPermissions.Count > 0)
                    {
                        var roles = userRoles ?? [];
                        var userInheritedPermissions = inheritedPermissions
                            .Where(p => p.PermissionType == "User" && 
                                       p.PermissionTarget == userId.ToString())
                            .ToList();

                        var roleInheritedPermissions = inheritedPermissions
                            .Where(p => p.PermissionType == "Role" && 
                                       roles.Contains(p.PermissionTarget))
                            .ToList();

                        if (userInheritedPermissions.Count > 0 || roleInheritedPermissions.Count > 0)
                        {
                            // 继续向上查找父分类（需要外部递归调用）
                            return true; // 表示需要继续向上查找
                        }
                    }
                }
            }

            // 2. 从模板继承权限
            if (template != null && template.Permissions != null && template.Permissions.Count > 0)
            {
                var roles = userRoles ?? [];
                var relevantPermissions = template.Permissions
                    .Where(p => p.IsEffective() && p.Action == action)
                    .ToList();

                if (relevantPermissions.Count > 0)
                {
                    var userPermissions = relevantPermissions
                        .Where(p => p.PermissionType == "User" && 
                                   p.PermissionTarget == userId.ToString())
                        .ToList();

                    var rolePermissions = relevantPermissions
                        .Where(p => p.PermissionType == "Role" && 
                                   roles.Contains(p.PermissionTarget))
                        .ToList();

                    var allPermissions = userPermissions.Concat(rolePermissions).ToList();

                    if (allPermissions.Count > 0)
                    {
                        // 权限优先级：拒绝 > 允许 > 继承
                        if (allPermissions.Any(p => p.Effect == PermissionEffect.Deny))
                        {
                            return false;
                        }

                        if (allPermissions.Any(p => p.Effect == PermissionEffect.Allow))
                        {
                            return true;
                        }

                        // 如果有继承权限，需要检查模板的父模板（需要外部递归调用）
                        if (allPermissions.Any(p => p.Effect == PermissionEffect.Inherit))
                        {
                            return true; // 表示需要继续向上查找
                        }
                    }
                }
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

        /// <summary>
        /// 设置分类标签
        /// </summary>
        /// <param name="tags">分类标签列表</param>
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
        /// 设置元数据字段集合
        /// </summary>
        /// <param name="metaFields">元数据字段列表</param>
        public virtual void SetMetaFields([CanBeNull] List<MetaField>? metaFields)
        {
            MetaFields = metaFields ?? [];
        }

        /// <summary>
        /// 设置分类路径
        /// </summary>
        /// <param name="path">分类路径</param>
        public virtual void SetPath([CanBeNull] string? path)
        {
            Path = path;
        }

        /// <summary>
        /// 计算下一个分类路径
        /// </summary>
        /// <param name="currentPath">当前路径</param>
        /// <returns>下一个路径代码</returns>
        public static string CalculateNextPath(string? currentPath)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                return CreatePathCode(1);
            }

            var parentPath = GetParentPath(currentPath);
            var lastUnitCode = GetLastUnitPathCode(currentPath);
            return AppendPathCode(parentPath, CreatePathCode(Convert.ToInt32(lastUnitCode) + 1));
        }

        /// <summary>
        /// 获取路径中的最后一个单元代码
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <returns>最后一个单元代码</returns>
        public static string GetLastUnitPathCode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("路径不能为空", nameof(path));
            }

            var parts = path.Split('.');
            return parts[^1];
        }

        /// <summary>
        /// 获取父级路径
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <returns>父级路径，如果是根节点则返回null</returns>
        public static string? GetParentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var parts = path.Split('.');
            if (parts.Length <= 1)
            {
                return null; // 根节点
            }

            return string.Join(".", parts[..^1]);
        }

        /// <summary>
        /// 创建路径代码
        /// </summary>
        /// <param name="numbers">数字数组</param>
        /// <returns>格式化的路径代码</returns>
        public static string CreatePathCode(params int[] numbers)
        {
            if (numbers == null || numbers.Length == 0)
            {
                throw new ArgumentException("至少需要一个数字", nameof(numbers));
            }

            return string.Join(".", numbers.Select(n => n.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}")));
        }

        /// <summary>
        /// 追加路径代码
        /// </summary>
        /// <param name="parentPath">父路径，如果是根节点可以为null或空</param>
        /// <param name="childPath">子路径代码</param>
        /// <returns>完整的路径代码</returns>
        public static string AppendPathCode(string? parentPath, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
            {
                throw new ArgumentException("子路径不能为空", nameof(childPath));
            }

            if (string.IsNullOrEmpty(parentPath))
            {
                return childPath;
            }

            return $"{parentPath}.{childPath}";
        }

        /// <summary>
        /// 验证路径格式
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <returns>是否为有效格式</returns>
        public static bool IsValidPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return true; // 空路径是有效的（根节点）

            var parts = path.Split('.');
            return parts.All(part => part.Length == AttachmentConstants.PATH_CODE_DIGITS && int.TryParse(part, out _));
        }

        /// <summary>
        /// 获取路径深度
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <returns>层级深度（0表示根节点）</returns>
        public static int GetPathDepth(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return 0;

            return path.Split('.').Length;
        }

        /// <summary>
        /// 检查是否为根路径
        /// </summary>
        /// <param name="path">分类路径</param>
        /// <returns>是否为根路径</returns>
        public static bool IsRootPath(string? path)
        {
            return string.IsNullOrEmpty(path);
        }

        /// <summary>
        /// 检查是否为父子关系
        /// </summary>
        /// <param name="parentPath">父路径</param>
        /// <param name="childPath">子路径</param>
        /// <returns>是否为父子关系</returns>
        public static bool IsParentChildPath(string? parentPath, string childPath)
        {
            if (string.IsNullOrEmpty(childPath))
                return false;

            if (string.IsNullOrEmpty(parentPath))
                return GetPathDepth(childPath) == 1;

            return childPath.StartsWith(parentPath + ".", StringComparison.Ordinal) &&
                   GetPathDepth(childPath) == GetPathDepth(parentPath) + 1;
        }

        /// <summary>
        /// 获取分类路径的显示名称（用于UI展示）
        /// </summary>
        /// <returns>格式化的路径显示名称</returns>
        public virtual string GetPathDisplayName()
        {
            if (string.IsNullOrEmpty(Path))
                return "根节点";

            var parts = Path.Split('.');
            return string.Join(" → ", parts.Select(part => int.Parse(part).ToString()));
        }

        /// <summary>
        /// 更改父级分类
        /// </summary>
        /// <param name="parentId">父级ID</param>
        /// <param name="parentPath">父级路径</param>
        public virtual void ChangeParent(Guid? parentId, [CanBeNull] string? parentPath = null)
        {
            ParentId = parentId;
            
            // 如果提供了父路径，自动计算新的路径
            if (parentPath != null)
            {
                var newPath = CalculateNextPath(parentPath);
                SetPath(newPath);
            }
        }


        /// <summary>
        /// 检查是否为分支节点（可以有子节点，但不能直接上传文件）
        /// </summary>
        public virtual bool IsBranchNode => TemplateRole == TemplateRole.Branch;

        /// <summary>
        /// 检查是否为叶子节点（不能有子节点，但可以直接上传文件）
        /// </summary>
        public virtual bool IsLeafNode => TemplateRole == TemplateRole.Leaf;

        /// <summary>
        /// 检查是否可以包含子分类
        /// 根分类和分支节点都可以包含子分类
        /// </summary>
        /// <returns>是否可以包含子分类</returns>
        public virtual bool CanContainChildCatalogues()
        {
            return TemplateRole == TemplateRole.Root || 
                   TemplateRole == TemplateRole.Branch;
        }

        /// <summary>
        /// 检查是否支持文件上传
        /// 只有叶子节点才支持直接上传文件
        /// </summary>
        /// <returns>是否支持文件上传</returns>
        public virtual bool SupportsFileUpload()
        {
            return TemplateRole == TemplateRole.Leaf;
        }
    }
}
