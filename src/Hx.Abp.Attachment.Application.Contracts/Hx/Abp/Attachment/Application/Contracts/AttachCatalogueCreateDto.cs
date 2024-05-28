using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueCreateDto
    {
        /// <summary>
        /// 顺序号
        /// </summary>
        public required int SequenceNumber { get; set; }
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
        [MaxLength(20)]
        public required string BusinessId { get; set; }
        /// <summary>
        /// 父节点Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否核验
        /// </summary>
        public bool IsVerification { get; set; }
        /// <summary>
        /// 核验通过
        /// </summary>
        public bool VerificationPassed { get; set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
