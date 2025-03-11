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
        public string FilePath { get; set; }
        public string FileName { get; set; }
        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; }
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
    }
}
