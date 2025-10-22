using Volo.Abp.Domain.Services;

namespace Hx.Abp.Attachment.Domain
{
    public interface IAttachCatalogueManager : IDomainService
    {
        Task<AttachCatalogue> GenerateFromTemplateAsync(
            AttachCatalogueTemplate template,
            string reference,
            int referenceType,
            Dictionary<string, object>? contextData,
            string? customTemplateName = null,
            List<MetaField>? customMetaFields = null);

        Task<AttachCatalogueTemplate> CreateTemplateVersionAsync(
            AttachCatalogueTemplate baseTemplate,
            Guid? newParentId = null);

        /// <summary>
        /// 软删除分类及其所有子分类
        /// </summary>
        /// <param name="catalogue">要删除的分类</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task SoftDeleteCatalogueWithChildrenAsync(
            AttachCatalogue catalogue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 硬删除分类及其所有子分类
        /// </summary>
        /// <param name="catalogue">要删除的分类</param>
        /// <param name="cancellationToken">取消令牌</param>
        Task HardDeleteCatalogueWithChildrenAsync(
            AttachCatalogue catalogue,
            CancellationToken cancellationToken = default);
    }
}
