using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueUpdateDto
    {
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public required AttachReceiveType AttachReceiveType { get; set; }
        /// <summary>
        /// 目录名称
        /// </summary>
        [MaxLength(500)]
        public required string CatalogueName { get; set; }
        /// <summary>
        /// 业务Id
        /// </summary>
        [MaxLength(50)]
        public required string Reference { get; set; }
        /// <summary>
        /// 附件文件
        /// </summary>
        public required ICollection<AttachFileUpdateDto> AttachFiles
        {
            get;
            set;
        }
    }
}
