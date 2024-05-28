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
        public required string FilePath {  get; set; }
    }
}
