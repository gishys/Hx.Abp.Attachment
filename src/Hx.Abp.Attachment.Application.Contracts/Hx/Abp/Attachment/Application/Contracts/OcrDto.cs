using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 文件OCR处理DTO
    /// </summary>
    public class FileOcrDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 目录ID
        /// </summary>
        public Guid? CatalogueId { get; set; }
    }

    /// <summary>
    /// 批量文件OCR处理DTO
    /// </summary>
    public class BatchFileOcrDto
    {
        /// <summary>
        /// 文件ID列表
        /// </summary>
        public List<Guid> FileIds { get; set; } = new();
    }

    /// <summary>
    /// 目录OCR处理DTO
    /// </summary>
    public class CatalogueOcrDto
    {
        /// <summary>
        /// 目录ID
        /// </summary>
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// 目录名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 是否包含子目录
        /// </summary>
        public bool IncludeSubCatalogues { get; set; } = false;
    }

    /// <summary>
    /// 文件OCR状态DTO
    /// </summary>
    public class FileOcrStatusDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 是否支持OCR
        /// </summary>
        public bool IsSupported { get; set; }

        /// <summary>
        /// OCR处理状态
        /// </summary>
        public OcrProcessStatus OcrProcessStatus { get; set; }

        /// <summary>
        /// OCR处理时间
        /// </summary>
        public DateTime? OcrProcessedTime { get; set; }
    }

    /// <summary>
    /// 文件OCR内容DTO
    /// </summary>
    public class FileOcrContentDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// OCR提取的文本内容
        /// </summary>
        public string? OcrContent { get; set; }

        /// <summary>
        /// OCR处理状态
        /// </summary>
        public OcrProcessStatus OcrProcessStatus { get; set; }

        /// <summary>
        /// OCR处理时间
        /// </summary>
        public DateTime? OcrProcessedTime { get; set; }
    }

    /// <summary>
    /// 目录全文内容DTO
    /// </summary>
    public class CatalogueFullTextDto
    {
        /// <summary>
        /// 目录ID
        /// </summary>
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// 目录名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 全文内容
        /// </summary>
        public string? FullTextContent { get; set; }

        /// <summary>
        /// 全文内容更新时间
        /// </summary>
        public DateTime? FullTextContentUpdatedTime { get; set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachCount { get; set; }
    }
}
