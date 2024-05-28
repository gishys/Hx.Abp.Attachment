namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachFileUpdateDto
    {
        /// <summary>
        /// 主键
        /// </summary>
        public Guid Id { get; set; }
        /// <summary>
        /// 文件别名
        /// </summary>
        public virtual required string FileAlias { get; set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public virtual byte[]? DocumentContent { get; set; }
    }
}