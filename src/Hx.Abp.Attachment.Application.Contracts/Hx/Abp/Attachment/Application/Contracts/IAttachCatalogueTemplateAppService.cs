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
            TemplateType? templateType = null,
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
    }
}
