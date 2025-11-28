namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 分类创建方式
    /// </summary>
    public enum CatalogueCreateMode
    {
        /// <summary>
        /// 重建
        /// </summary>
        Rebuild = 1,
        /// <summary>
        /// 追加
        /// </summary>
        Append = 2,
        /// <summary>
        /// 覆盖
        /// </summary>
        Overlap = 3,
        /// <summary>
        /// 跳过已存在追加
        /// </summary>
        SkipExistAppend = 5,
    }
}
