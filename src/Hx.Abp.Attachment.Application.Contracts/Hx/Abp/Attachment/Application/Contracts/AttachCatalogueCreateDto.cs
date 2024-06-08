using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueCreateDto
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
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
        /// <summary>
        /// 业务Id
        /// </summary>
        [MaxLength(20)]
        public required string Reference { get; set; }
        /// <summary>
        /// 父节点Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; } = false;
        /// <summary>
        /// 核验通过
        /// </summary>
        public bool VerificationPassed { get; set; } = false;
        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
