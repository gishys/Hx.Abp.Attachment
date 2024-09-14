using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class DetailedFileVerifyResultDto
    {
        public Guid Id { get; set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public required bool IsRequired { get; set; }
        /// <summary>
        /// 业务类型Id
        /// </summary>
        public required string Reference { get; set; }
        /// <summary>
        /// 目录名称
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// 已上传
        /// </summary>
        public required bool Uploaded {  get; set; }
        /// <summary>
        /// 子文件夹
        /// </summary>
        public List<DetailedFileVerifyResultDto>? Children { get; set; }
    }
}
