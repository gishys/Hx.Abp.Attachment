namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachFileBase64Dto
    {
        public Guid? Id { get; set; }
        /// <summary>
        /// 文件别名
        /// </summary>
        public required string FileAlias { get; set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public required string DocumentContent { get; set; }
        /// <summary>
        /// 序号
        /// </summary>
        public int SequenceNumber { get; set; }
    }
}
