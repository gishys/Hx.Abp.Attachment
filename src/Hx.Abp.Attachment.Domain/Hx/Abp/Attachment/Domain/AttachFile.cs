using JetBrains.Annotations;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Hx.Abp.Attachment.Domain.Shared;
using System.Collections.ObjectModel;

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
        /// 业务引用（从AttachCatalogue获取）
        /// </summary>
        public virtual string? Reference { get; protected set; }

        /// <summary>
        /// 模板用途（从AttachCatalogue获取）
        /// </summary>
        public virtual TemplatePurpose? TemplatePurpose { get; protected set; }

        /// <summary>
        /// 是否已归类到某个分类
        /// </summary>
        public virtual bool IsCategorized { get; protected set; } = true;

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
        /// OCR文本块集合
        /// </summary>
        public virtual Collection<OcrTextBlock> OcrTextBlocks { get; private set; }
        /// <summary>
        /// 提供给ORM用来从数据库中获取实体，
        /// 无需初始化子集合因为它会被来自数据库的值覆盖
        /// </summary>
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        protected AttachFile()
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
        {
            OcrTextBlocks = [];
        }
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
            OcrTextBlocks = [];
            IsCategorized = true;
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
            OcrTextBlocks = [];
            IsCategorized = true;
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
            OcrTextBlocks.Clear();
        }

        /// <summary>
        /// 添加OCR文本块
        /// </summary>
        /// <param name="textBlock">文本块</param>
        public virtual void AddOcrTextBlock(OcrTextBlock textBlock)
        {
            if (textBlock != null)
            {
                OcrTextBlocks.Add(textBlock);
            }
        }

        /// <summary>
        /// 批量添加OCR文本块
        /// </summary>
        /// <param name="textBlocks">文本块集合</param>
        public virtual void AddOcrTextBlocks(IEnumerable<OcrTextBlock> textBlocks)
        {
            if (textBlocks != null)
            {
                foreach (var block in textBlocks)
                {
                    OcrTextBlocks.Add(block);
                }
            }
        }

        /// <summary>
        /// 清除所有OCR文本块
        /// </summary>
        public virtual void ClearOcrTextBlocks()
        {
            OcrTextBlocks.Clear();
        }

        /// <summary>
        /// 检查是否支持OCR处理
        /// </summary>
        /// <returns>是否支持OCR</returns>
        public virtual bool IsSupportedForOcr()
        {
            if (string.IsNullOrWhiteSpace(FileType))
                return false;

            var lowerFileType = FileType.ToLowerInvariant();
            var supportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".tiff", ".tif", ".bmp", ".gif", ".pdf"
            };
            
            return supportedTypes.Contains(lowerFileType);
        }

        /// <summary>
        /// 检查是否已完成OCR处理
        /// </summary>
        /// <returns>是否已完成OCR处理</returns>
        public virtual bool IsOcrCompleted()
        {
            return !string.IsNullOrWhiteSpace(OcrContent) && 
                   OcrProcessStatus == OcrProcessStatus.Completed;
        }

        /// <summary>
        /// 检查是否正在处理OCR
        /// </summary>
        /// <returns>是否正在处理OCR</returns>
        public virtual bool IsOcrProcessing()
        {
            return OcrProcessStatus == OcrProcessStatus.Processing;
        }

        /// <summary>
        /// 设置业务引用
        /// </summary>
        /// <param name="reference">业务引用</param>
        public virtual void SetReference(string? reference)
        {
            Reference = reference;
        }

        /// <summary>
        /// 设置模板用途
        /// </summary>
        /// <param name="templatePurpose">模板用途</param>
        public virtual void SetTemplatePurpose(TemplatePurpose? templatePurpose)
        {
            TemplatePurpose = templatePurpose;
        }

        /// <summary>
        /// 设置是否已归类
        /// </summary>
        /// <param name="isCategorized">是否已归类</param>
        public virtual void SetIsCategorized(bool isCategorized)
        {
            IsCategorized = isCategorized;
        }

        /// <summary>
        /// 设置关联的分类ID
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        public virtual void SetAttachCatalogueId(Guid? catalogueId)
        {
            AttachCatalogueId = catalogueId;
        }

        /// <summary>
        /// 从AttachCatalogue设置相关属性
        /// </summary>
        /// <param name="catalogue">附件分类</param>
        public virtual void SetFromAttachCatalogue(AttachCatalogue? catalogue)
        {
            if (catalogue != null)
            {
                Reference = catalogue.Reference;
                TemplatePurpose = catalogue.CataloguePurpose;
            }
            else
            {
                Reference = null;
                TemplatePurpose = null;
            }
        }

        public virtual void Download()
        {
            DownloadTimes++;
        }
    }
}
