using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public AttachReceiveType AttachReceiveType { get; set; }
        /// <summary>
        /// 目录名称
        /// </summary>
        public required string CatalogueName { get; set; }
        /// <summary>
        /// 顺序号
        /// </summary>
        public int SequenceNumber { get; set; }
        /// <summary>
        /// Parent Id
        /// </summary>
        public Guid? ParentId { get; set; }
        /// <summary>
        /// 是否必收
        /// </summary>
        public required bool IsRequired { get; set; }
    }
}