using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    public interface IAttachCatalogueTemplateRepository : IRepository<AttachCatalogueTemplate, Guid>
    {
        Task<List<AttachCatalogueTemplate>> FindBySemanticMatchAsync(string query, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> FindByRuleMatchAsync(Dictionary<string, object> context, bool onlyLatest = true);
        Task<List<AttachCatalogueTemplate>> GetChildrenAsync(Guid parentId, bool onlyLatest = true);

        // 新增版本相关方法
        Task<AttachCatalogueTemplate?> GetLatestVersionAsync(string templateName);
        Task<List<AttachCatalogueTemplate>> GetAllVersionsAsync(string templateName);
        Task<List<AttachCatalogueTemplate>> GetTemplateHistoryAsync(Guid templateId);
        Task SetAsLatestVersionAsync(Guid templateId);
        Task SetAllOtherVersionsAsNotLatestAsync(string templateName, Guid excludeId);
    }
}
