using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueTemplateAppService :
        ICrudAppService<
            AttachCatalogueTemplateDto,
            Guid,
            GetAttachCatalogueTemplateListDto,
            CreateUpdateAttachCatalogueTemplateDto>
    {
        Task<ListResultDto<AttachCatalogueTemplateDto>> FindMatchingTemplatesAsync(TemplateMatchInput input);
        Task<AttachCatalogueStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false);
        Task GenerateCatalogueFromTemplateAsync(GenerateCatalogueInput input);

        // 新增版本管理方法
        Task<AttachCatalogueTemplateDto> CreateNewVersionAsync(Guid baseTemplateId, CreateUpdateAttachCatalogueTemplateDto input);
        Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid templateId);
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplateHistoryAsync(Guid templateId);
        Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid templateId);

        // 新增模板标识查询方法
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByIdentifierAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool onlyLatest = true);

        // 新增向量相关方法
        Task<ListResultDto<AttachCatalogueTemplateDto>> FindSimilarTemplatesAsync(
            string semanticQuery, 
            double similarityThreshold = 0.7,
            int maxResults = 10);

        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByVectorDimensionAsync(
            int minDimension, 
            int maxDimension, 
            bool onlyLatest = true);

        // 新增统计方法
        Task<AttachCatalogueTemplateStatisticsDto> GetTemplateStatisticsAsync();

        // 新增混合检索方法
        Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesHybridAsync(TemplateSearchInputDto input);
        Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTextAsync(string keyword, FacetType? facetType = null, TemplatePurpose? templatePurpose = null, List<string>? tags = null, int maxResults = 20);
        Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTagsAsync(List<string> tags, FacetType? facetType = null, TemplatePurpose? templatePurpose = null, int maxResults = 20);
        Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesBySemanticAsync(string semanticQuery, FacetType? facetType = null, TemplatePurpose? templatePurpose = null, double similarityThreshold = 0.7, int maxResults = 20);
        Task<ListResultDto<string>> GetPopularTagsAsync(int topN = 20);
        Task<Dictionary<string, int>> GetTagStatisticsAsync();

        // 新增获取根节点方法（用于树状展示）
        Task<ListResultDto<AttachCatalogueTemplateTreeDto>> GetRootTemplatesAsync(
            FacetType? facetType = null,
            TemplatePurpose? templatePurpose = null,
            bool includeChildren = true,
            bool onlyLatest = true);
    }
}
