namespace Hx.Abp.Attachment.Domain.Shared
{
    public class GetCatalogueInput
    {
        /// <summary>
        /// 业务类型Id
        /// </summary>
        public required string Reference { get; set; }
        /// <summary>
        /// 业务类型标识
        /// </summary>
        public int ReferenceType { get; set; }
        /// <summary>
        /// 分类名称
        /// </summary>
        public required string CatalogueName { get; set; }
        /// <summary>
        /// 父Id
        /// </summary>
        public Guid? ParentId { get; set; }
    }
}
