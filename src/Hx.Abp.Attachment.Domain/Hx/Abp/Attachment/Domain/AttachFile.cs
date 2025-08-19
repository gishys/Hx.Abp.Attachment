using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件文件
    /// </summary>
    public class AttachFile : FullAuditedAggregateRoot<Guid>
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
        /// 文件类型
        /// </summary>
        public virtual string FileType { get; protected set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public virtual int FileSize { get; protected set; }
        /// <summary>
        /// 下载次数
        /// </summary>
        public virtual int DownloadTimes { get; protected set; }

        /// <summary>
        /// OCR提取的文本内容
        /// </summary>
        public virtual string? OcrContent { get; protected set; }

        /// <summary>
        /// OCR处理状态
        /// </summary>
        public virtual OcrProcessStatus OcrProcessStatus { get; protected set; }

        /// <summary>
        /// OCR处理时间
        /// </summary>
        public virtual DateTime? OcrProcessedTime { get; protected set; }
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
            [NotNull] string filePath,
            [NotNull] string fileType,
            int fileSize,
            int downloadTimes,
            Guid? attachCatalogueId = null)
        {
            Id = id;
            SequenceNumber = sequenceNumber;
            FileAlias = Check.NotNullOrWhiteSpace(aliasName, nameof(aliasName));
            FileName = Check.NotNullOrWhiteSpace(fileName, nameof(fileName));
            FilePath = Check.NotNullOrWhiteSpace(filePath, nameof(filePath));
            FileType = Check.NotNullOrWhiteSpace(fileType, nameof(fileType));
            FileSize = fileSize;
            DownloadTimes = downloadTimes;
            AttachCatalogueId = attachCatalogueId;
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
            string filePath,
            [NotNull] string fileType,
            int fileSize,
            int downloadTimes)
        {
            Id = id;
            SequenceNumber = sequenceNumber;
            FileAlias = Check.NotNullOrWhiteSpace(aliasName, nameof(aliasName));
            DocumentContent = documentContent;
            FileName = fileName;
            FilePath = filePath;
            FileType = Check.NotNullOrWhiteSpace(fileType, nameof(fileType));
            FileSize = fileSize;
            DownloadTimes = downloadTimes;
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

        /// <summary>
        /// 设置OCR内容
        /// </summary>
        /// <param name="ocrContent">OCR提取的文本内容</param>
        public virtual void SetOcrContent(string? ocrContent)
        {
            OcrContent = ocrContent;
            OcrProcessStatus = string.IsNullOrWhiteSpace(ocrContent) 
                ? OcrProcessStatus.Failed 
                : OcrProcessStatus.Completed;
            OcrProcessedTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 设置OCR处理状态
        /// </summary>
        /// <param name="status">处理状态</param>
        public virtual void SetOcrProcessStatus(OcrProcessStatus status)
        {
            OcrProcessStatus = status;
            if (status == OcrProcessStatus.Completed)
            {
                OcrProcessedTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// 清除OCR内容
        /// </summary>
        public virtual void ClearOcrContent()
        {
            OcrContent = null;
            OcrProcessStatus = OcrProcessStatus.NotProcessed;
            OcrProcessedTime = null;
        }

        public virtual void Download()
        {
            DownloadTimes++;
        }
    }
}
