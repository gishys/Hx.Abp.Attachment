using Volo.Abp.Domain.Services;
using YourNamespace.AttachCatalogues;

namespace Hx.Abp.Attachment.Domain
{
    public interface IAttachCatalogueManager : IDomainService
    {
        Task<AttachCatalogue> GenerateFromTemplateAsync(
            AttachCatalogueTemplate template,
            string? reference,
            int referenceType,
            Dictionary<string, object>? contextData = null);

        Task<AttachCatalogueTemplate> CreateTemplateVersionAsync(
            AttachCatalogueTemplate baseTemplate,
            Guid? newParentId = null);
    }
}
