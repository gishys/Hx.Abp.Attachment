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
    }
}
