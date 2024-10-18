using Hx.Abp.Attachment.Domain.Shared;
using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueCreateDto
    {
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public required AttachReceiveType AttachReceiveType { get; set; } = AttachReceiveType.Copy;
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
        [MaxLength(28)]
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
        /// <summary>
        /// 静态标识
        /// </summary>
        public bool IsStatic { get; set; } = false;
        /// <summary>
        /// 子文件夹
        /// </summary>
        public ICollection<AttachCatalogueCreateDto>? Children { get; set; }
        /// <summary>
        /// 子文件
        /// </summary>
        public List<AttachFileCreateDto>? AttachFiles {  get; set; }
    }
}
