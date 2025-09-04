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
        /// 分类名称
        /// </summary>
        [MaxLength(500)]
        public required string CatalogueName { get; set; }

        /// <summary>
        /// 分类标签
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 业务Id
        /// </summary>
        [MaxLength(50)]
        public required string Reference { get; set; }
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
        /// <summary>
        /// 父Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }
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
