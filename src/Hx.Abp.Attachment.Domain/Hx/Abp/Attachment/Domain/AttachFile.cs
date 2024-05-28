using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件文件
    /// </summary>
    public class AttachFile : Entity<Guid>
    {
        /// <summary>
        /// 文件名称
        /// </summary>
        public virtual string FileName { get; private set; }
        /// <summary>
        /// 文件别名
        /// </summary>
        public virtual string FileAlias { get; private set; }
        /// <summary>
        /// 文件路径
        /// </summary>
        public virtual string FilePath { get; private set; }
        /// <summary>
        /// 顺序号
        /// </summary>
        public virtual int SequenceNumber { get; private set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public virtual byte[]? DocumentContent { get; private set; }
        /// <summary>
        /// Attach catalogue id of this attach file.
        /// </summary>
        public virtual Guid? AttachCatalogueId { get; protected set; }
        /// <summary>
        /// 提供给ORM用来从数据库中获取实体，
        /// 无需初始化子集合因为它会被来自数据库的值覆盖
        /// </summary>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        protected AttachFile()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        { }
        /// <summary>
        /// 创建附件文件，通过文件管理系统持久化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="aliasName"></param>
        /// <param name="sequenceNumber"></param>
        public AttachFile(
            Guid id,
            [NotNull] string aliasName,
            int sequenceNumber,
            [NotNull] string fileName,
            [NotNull] string filePath)
        {
            Id = id;
            SequenceNumber = sequenceNumber;
            FileAlias = Check.NotNullOrWhiteSpace(aliasName, nameof(aliasName));
            FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName));
            FilePath = Check.NotNullOrWhiteSpace(filePath, nameof(filePath));
        }
        /// <summary>
        /// 创建附件文件，通过数据库持久化
        /// </summary>
        /// <param name="id"></param>
        /// <param name="aliasName"></param>
        /// <param name="documentContent"></param>
        /// <param name="sequenceNumber"></param>
        public AttachFile(
            Guid id,
            [NotNull] string aliasName,
            byte[] documentContent,
            int sequenceNumber,
            string fileName,
            string filePath)
        {
            Id = id;
            SequenceNumber = sequenceNumber;
            FileAlias = Check.NotNullOrWhiteSpace(aliasName, nameof(aliasName));
            DocumentContent = documentContent;
            FileName = fileName;
            FilePath = filePath;
        }
        public virtual void SetFileAlias(string fileAlias)
        {
            FileAlias = fileAlias;
        }
        public virtual void SetFileName(string fileName)
        {
            FileName = fileName;
        }
        public virtual void SetDocumentContent(byte[] documentContent)
        {
            DocumentContent = documentContent;
        }
    }
}