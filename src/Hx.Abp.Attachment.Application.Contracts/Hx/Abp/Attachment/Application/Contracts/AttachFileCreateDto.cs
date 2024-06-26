﻿namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachFileCreateDto
    {
        /// <summary>
        /// 文件别名
        /// </summary>
        public required virtual string FileAlias { get; set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public required virtual byte[] DocumentContent { get; set; }
    }
}
