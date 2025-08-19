using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// OCR文本块DTO
    /// </summary>
    public class OcrTextBlockDto
    {
        /// <summary>
        /// 文本块ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 关联的文件ID
        /// </summary>
        public Guid AttachFileId { get; set; }

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 置信度
        /// </summary>
        public float Probability { get; set; }

        /// <summary>
        /// 页面索引（PDF多页时使用）
        /// </summary>
        public int PageIndex { get; set; }

        /// <summary>
        /// 位置信息
        /// </summary>
        public TextPositionDto? Position { get; set; }

        /// <summary>
        /// 文本块在文档中的位置（用于排序）
        /// </summary>
        public int BlockOrder { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    }

    /// <summary>
    /// 文本位置信息DTO
    /// </summary>
    public class TextPositionDto
    {
        /// <summary>
        /// 角度
        /// </summary>
        public int Angle { get; set; }

        /// <summary>
        /// 高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 左边距
        /// </summary>
        public int Left { get; set; }

        /// <summary>
        /// 上边距
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 中心点X坐标
        /// </summary>
        public int CenterX => Left + Width / 2;

        /// <summary>
        /// 中心点Y坐标
        /// </summary>
        public int CenterY => Top + Height / 2;
    }

    /// <summary>
    /// 文件OCR内容DTO（包含文本块信息）
    /// </summary>
    public class FileOcrContentWithBlocksDto
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
        /// OCR提取的文本内容（合并后的完整文本）
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

        /// <summary>
        /// 文本块列表
        /// </summary>
        public List<OcrTextBlockDto> TextBlocks { get; set; } = [];

        /// <summary>
        /// 文本块总数
        /// </summary>
        public int TextBlockCount => TextBlocks.Count;
    }

    /// <summary>
    /// 目录OCR内容DTO（包含文本块信息）
    /// </summary>
    public class CatalogueOcrContentWithBlocksDto
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
        /// 全文内容（合并后的完整文本）
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

        /// <summary>
        /// 文件OCR内容列表
        /// </summary>
        public List<FileOcrContentWithBlocksDto> FileOcrContents { get; set; } = [];

        /// <summary>
        /// 总文本块数
        /// </summary>
        public int TotalTextBlockCount => FileOcrContents.Sum(f => f.TextBlockCount);
    }
}
