using Hx.Abp.Attachment.Domain.Shared;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachCatalogueDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        /// <summary>
        /// 业务类型Id
        /// </summary>
        public required string Reference { get; set; }
        /// <summary>
        /// 附件收取类型
        /// </summary>
        public AttachReceiveType AttachReceiveType { get; set; }
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
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
        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachCount { get; set; }
        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount { get; set; }
        /// <summary>
        /// 静态标识
        /// </summary>
        public bool IsStatic { get; set; }
        /// <summary>
        /// 子文件夹
        /// </summary>
        public ICollection<AttachCatalogueDto>? Children { get; set; }
        /// <summary>
        /// 附件文件集合
        /// </summary>
        public Collection<AttachFileDto>? AttachFiles { get; set; }
    }
}