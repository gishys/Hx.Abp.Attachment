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
        Task<TemplateStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false);
        Task GenerateCatalogueFromTemplateAsync(GenerateCatalogueInput input);

        // 新增版本管理方法
        Task<AttachCatalogueTemplateDto> CreateNewVersionAsync(Guid baseTemplateId, CreateUpdateAttachCatalogueTemplateDto input);
        Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid templateId, int version);
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplateHistoryAsync(Guid templateId);
        Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid templateId, int version);
        
        // 新增基于版本的操作方法
        Task<AttachCatalogueTemplateDto> GetByVersionAsync(Guid templateId, int version);
        Task<AttachCatalogueTemplateDto> UpdateVersionAsync(Guid templateId, int version, CreateUpdateAttachCatalogueTemplateDto input);
        Task DeleteVersionAsync(Guid templateId, int version);

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

        // 新增模板路径相关方法
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathAsync(string? templatePath, bool includeChildren = false);
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathDepthAsync(int depth, bool onlyLatest = true);
        Task<string> CalculateNextTemplatePathAsync(string? parentPath);
        Task<bool> ValidateTemplatePathAsync(string? templatePath);
        Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathRangeAsync(string? startPath, string? endPath, bool onlyLatest = true);
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

        #region 元数据字段管理

        /// <summary>
        /// 获取模板的元数据字段列表
        /// </summary>
        Task<ListResultDto<MetaFieldDto>> GetTemplateMetaFieldsAsync(Guid templateId);

        /// <summary>
        /// 添加元数据字段到模板
        /// </summary>
        Task<MetaFieldDto> AddMetaFieldToTemplateAsync(Guid templateId, CreateUpdateMetaFieldDto input);

        /// <summary>
        /// 更新模板的元数据字段
        /// </summary>
        Task<MetaFieldDto> UpdateTemplateMetaFieldAsync(Guid templateId, string fieldKey, CreateUpdateMetaFieldDto input);

        /// <summary>
        /// 从模板移除元数据字段
        /// </summary>
        Task RemoveMetaFieldFromTemplateAsync(Guid templateId, string fieldKey);

        /// <summary>
        /// 获取模板的元数据字段
        /// </summary>
        Task<MetaFieldDto?> GetTemplateMetaFieldAsync(Guid templateId, string fieldKey);

        /// <summary>
        /// 根据条件查询元数据字段
        /// </summary>
        Task<ListResultDto<MetaFieldDto>> QueryTemplateMetaFieldsAsync(Guid templateId, MetaFieldQueryDto input);

        /// <summary>
        /// 批量更新元数据字段顺序
        /// </summary>
        Task UpdateMetaFieldsOrderAsync(Guid templateId, List<string> fieldKeys);

        #endregion
    }
}
