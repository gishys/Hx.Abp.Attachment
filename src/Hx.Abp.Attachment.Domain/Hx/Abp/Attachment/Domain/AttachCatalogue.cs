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

        // 全文检索向量 - 不进行数据库映射，仅用于类型定义
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public virtual NpgsqlTsVector? SearchVector { get; private set; }

        /// <summary>
        /// 语义检索向量
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public virtual float[]? Embedding { get; private set; }

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
            Guid? templateId = null
            )
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
            AttachFiles = [];
            Children = [];
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
        /// 设置语义检索向量
        /// </summary>
        /// <param name="embedding">语义向量</param>
        public virtual void SetEmbedding(float[]? embedding) => Embedding = embedding;

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
    }
}
