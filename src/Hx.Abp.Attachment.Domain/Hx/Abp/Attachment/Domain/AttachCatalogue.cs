using Hx.Abp.Attachment.Domain.Shared;
using JetBrains.Annotations;
using NpgsqlTypes;
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
        /// 关联的模板ID
        /// </summary>
        public virtual Guid? TemplateId { get; private set; }

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
            FacetType catalogueFacetType = FacetType.General,
            TemplatePurpose cataloguePurpose = TemplatePurpose.Classification,
            [CanBeNull] List<string>? tags = null,
            [CanBeNull] List<double>? textVector = null)
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
            TemplateId = templateId;
            CatalogueFacetType = catalogueFacetType;
            CataloguePurpose = cataloguePurpose;
            Tags = tags ?? [];
            SetTextVector(textVector);
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
        /// 设置关联的模板ID
        /// </summary>
        /// <param name="templateId">模板ID</param>
        public virtual void SetTemplateId(Guid? templateId) => TemplateId = templateId;


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
        }

        /// <summary>
        /// 检查是否为根分类
        /// </summary>
        public virtual bool IsRoot => ParentId == null;

        /// <summary>
        /// 检查是否为叶子分类
        /// </summary>
        public virtual bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>
        /// 获取分类层级深度
        /// </summary>
        public virtual int GetDepth()
        {
            if (IsRoot) return 0;
            return 1; // 简化实现，实际应该递归计算
        }

        /// <summary>
        /// 获取分类路径
        /// </summary>
        public virtual string GetPath()
        {
            return CatalogueName; // 简化实现，实际应该构建完整路径
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
    }
}
