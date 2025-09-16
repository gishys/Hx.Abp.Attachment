using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachFileDto
    {
        /// <summary>
        /// 文件别名
        /// </summary>
        public required string FileAlias { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public int SequenceNumber { get; set; }
        public Guid Id { get; set; }
        public required string FilePath { get; set; }
        public required string FileName { get; set; }
        /// <summary>
        /// 文件类型
        /// </summary>
        public required string FileType { get; set; }
        /// <summary>
        /// 文件大小
        /// </summary>
        public int FileSize { get; set; }
        /// <summary>
        /// 下载次数
        /// </summary>
        public int DownloadTimes { get; set; }
        /// <summary>
        /// Attach catalogue id of this attach file.
        /// </summary>
        public Guid? AttachCatalogueId { get; set; }

        /// <summary>
        /// 业务引用（从AttachCatalogue获取）
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// 模板用途（从AttachCatalogue获取）
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }

        /// <summary>
        /// 是否已归类到某个分类
        /// </summary>
        public bool IsCategorized { get; set; } = true;
    }
}
