using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.HttpApi
{
    [RemoteService]
    [Route("api/attach-catalogue-template")]
    public class AttachCatalogueTemplateController(IAttachCatalogueTemplateAppService attachCatalogueTemplateAppService)
    {
        private readonly IAttachCatalogueTemplateAppService _attachCatalogueTemplateAppService = attachCatalogueTemplateAppService;

        public IAttachCatalogueTemplateAppService AttachCatalogueTemplateAppService => _attachCatalogueTemplateAppService;

        /// <summary>
        /// 获取模板（最新版本）
        /// </summary>
        [HttpGet("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> GetAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.GetAsync(id);
        }

        /// <summary>
        /// 更新模板（最新版本）
        /// </summary>
        [HttpPut("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> UpdateAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.UpdateAsync(id, input);
        }

        /// <summary>
        /// 删除模板（所有版本）
        /// </summary>
        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.DeleteAsync(id);
        }

        /// <summary>
        /// 获取分类模板列表
        /// </summary>
        [HttpGet]
        public virtual Task<PagedResultDto<AttachCatalogueTemplateDto>> GetListAsync(GetAttachCatalogueTemplateListDto input)
        {
            return AttachCatalogueTemplateAppService.GetListAsync(input);
        }

        /// <summary>
        /// 根据ID获取分类模板
        /// </summary>
        [HttpGet("{id}/{version}")]
        public virtual Task<AttachCatalogueTemplateDto> GetByVersionAsync(Guid id, int version)
        {
            return AttachCatalogueTemplateAppService.GetByVersionAsync(id, version);
        }

        /// <summary>
        /// 创建分类模板
        /// </summary>
        [HttpPost]
        public virtual Task<AttachCatalogueTemplateDto> CreateAsync(CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.CreateAsync(input);
        }

        /// <summary>
        /// 更新分类模板
        /// </summary>
        [HttpPut("{id}/{version}")]
        public virtual Task<AttachCatalogueTemplateDto> UpdateVersionAsync(Guid id, int version, CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.UpdateVersionAsync(id, version, input);
        }

        /// <summary>
        /// 删除分类模板
        /// </summary>
        [HttpDelete("{id}/{version}")]
        public virtual Task DeleteVersionAsync(Guid id, int version)
        {
            return AttachCatalogueTemplateAppService.DeleteVersionAsync(id, version);
        }

        /// <summary>
        /// 查找匹配的模板
        /// </summary>
        [HttpPost("find-matching")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> FindMatchingTemplatesAsync(TemplateMatchInput input)
        {
            return AttachCatalogueTemplateAppService.FindMatchingTemplatesAsync(input);
        }

        /// <summary>
        /// 获取模板结构（优化版本）
        /// 返回包含当前版本、历史版本和子模板树形结构的完整信息
        /// </summary>
        [HttpGet("{id}/structure")]
        public virtual Task<TemplateStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false)
        {
            return AttachCatalogueTemplateAppService.GetTemplateStructureAsync(id, includeHistory);
        }

        /// <summary>
        /// 从模板生成分类
        /// </summary>
        [HttpPost("generate-catalogue")]
        public virtual Task GenerateCatalogueFromTemplateAsync(GenerateCatalogueInput input)
        {
            return AttachCatalogueTemplateAppService.GenerateCatalogueFromTemplateAsync(input);
        }

        /// <summary>
        /// 创建新版本
        /// </summary>
        [HttpPost("{id}/new-version")]
        public virtual Task<AttachCatalogueTemplateDto> CreateNewVersionAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.CreateNewVersionAsync(id, input);
        }

        /// <summary>
        /// 设为最新版本
        /// </summary>
        [HttpPut("{id}/{version}/set-latest")]
        public virtual Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid id, int version)
        {
            return AttachCatalogueTemplateAppService.SetAsLatestVersionAsync(id, version);
        }

        /// <summary>
        /// 获取模板历史
        /// </summary>
        [HttpGet("{id}/history")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplateHistoryAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.GetTemplateHistoryAsync(id);
        }

        /// <summary>
        /// 回滚到指定版本
        /// </summary>
        [HttpPost("{id}/{version}/rollback")]
        public virtual Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid id, int version)
        {
            return AttachCatalogueTemplateAppService.RollbackToVersionAsync(id, version);
        }

        // ============= 新增模板标识查询接口 =============

        /// <summary>
        /// 按模板标识查询模板
        /// </summary>
        [HttpGet("by-identifier")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByIdentifierAsync(
            [FromQuery] FacetType? facetType = null,
            [FromQuery] TemplatePurpose? templatePurpose = null,
            [FromQuery] bool onlyLatest = true)
        {
            return AttachCatalogueTemplateAppService.GetTemplatesByIdentifierAsync(
                facetType, templatePurpose, onlyLatest);
        }

        /// <summary>
        /// 查找相似模板（基于语义向量）
        /// </summary>
        [HttpPost("find-similar")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> FindSimilarTemplatesAsync(
            [FromQuery] string semanticQuery,
            [FromQuery] double similarityThreshold = 0.7,
            [FromQuery] int maxResults = 10)
        {
            return AttachCatalogueTemplateAppService.FindSimilarTemplatesAsync(
                semanticQuery, similarityThreshold, maxResults);
        }

        /// <summary>
        /// 按向量维度查询模板
        /// </summary>
        [HttpGet("by-vector-dimension")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByVectorDimensionAsync(
            [FromQuery] int minDimension,
            [FromQuery] int maxDimension,
            [FromQuery] bool onlyLatest = true)
        {
            return AttachCatalogueTemplateAppService.GetTemplatesByVectorDimensionAsync(
                minDimension, maxDimension, onlyLatest);
        }

        /// <summary>
        /// 获取模板统计信息
        /// </summary>
        [HttpGet("statistics")]
        public virtual Task<AttachCatalogueTemplateStatisticsDto> GetTemplateStatisticsAsync()
        {
            return AttachCatalogueTemplateAppService.GetTemplateStatisticsAsync();
        }

        // ============= 混合检索接口 =============

        /// <summary>
        /// 混合检索模板（字面 + 语义）
        /// </summary>
        [HttpPost("search/hybrid")]
        public virtual Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesHybridAsync(TemplateSearchInputDto input)
        {
            return AttachCatalogueTemplateAppService.SearchTemplatesHybridAsync(input);
        }

        /// <summary>
        /// 文本检索模板
        /// </summary>
        [HttpGet("search/text")]
        public virtual Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTextAsync(
            [FromQuery] string keyword,
            [FromQuery] FacetType? facetType = null,
            [FromQuery] TemplatePurpose? templatePurpose = null,
            [FromQuery] List<string>? tags = null,
            [FromQuery] int maxResults = 20)
        {
            return AttachCatalogueTemplateAppService.SearchTemplatesByTextAsync(keyword, facetType, templatePurpose, tags, maxResults);
        }

        /// <summary>
        /// 标签检索模板
        /// </summary>
        [HttpGet("search/tags")]
        public virtual Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesByTagsAsync(
            [FromQuery] List<string> tags,
            [FromQuery] FacetType? facetType = null,
            [FromQuery] TemplatePurpose? templatePurpose = null,
            [FromQuery] int maxResults = 20)
        {
            return AttachCatalogueTemplateAppService.SearchTemplatesByTagsAsync(tags, facetType, templatePurpose, maxResults);
        }

        /// <summary>
        /// 语义检索模板
        /// </summary>
        [HttpGet("search/semantic")]
        public virtual Task<ListResultDto<TemplateSearchResultDto>> SearchTemplatesBySemanticAsync(
            [FromQuery] string semanticQuery,
            [FromQuery] FacetType? facetType = null,
            [FromQuery] TemplatePurpose? templatePurpose = null,
            [FromQuery] double similarityThreshold = 0.7,
            [FromQuery] int maxResults = 20)
        {
            return AttachCatalogueTemplateAppService.SearchTemplatesBySemanticAsync(semanticQuery, facetType, templatePurpose, similarityThreshold, maxResults);
        }

        /// <summary>
        /// 获取热门标签
        /// </summary>
        [HttpGet("tags/popular")]
        public virtual Task<ListResultDto<string>> GetPopularTagsAsync([FromQuery] int topN = 20)
        {
            return AttachCatalogueTemplateAppService.GetPopularTagsAsync(topN);
        }

        /// <summary>
        /// 获取标签统计
        /// </summary>
        [HttpGet("tags/statistics")]
        public virtual Task<Dictionary<string, int>> GetTagStatisticsAsync()
        {
            return AttachCatalogueTemplateAppService.GetTagStatisticsAsync();
        }

        // ============= 树状结构查询接口 =============

        /// <summary>
        /// 获取根节点模板（用于树状展示）
        /// </summary>
        [HttpGet("tree/roots")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateTreeDto>> GetRootTemplatesAsync(
            [FromQuery] FacetType? facetType = null,
            [FromQuery] TemplatePurpose? templatePurpose = null,
            [FromQuery] bool includeChildren = true,
            [FromQuery] bool onlyLatest = true,
            [FromQuery] string? fulltextQuery = null)
        {
            return AttachCatalogueTemplateAppService.GetRootTemplatesAsync(
                facetType, templatePurpose, includeChildren, onlyLatest, fulltextQuery);
        }

        // ============= 元数据字段管理接口 =============

        /// <summary>
        /// 获取模板的元数据字段列表
        /// </summary>
        [HttpGet("{id}/meta-fields")]
        public virtual Task<ListResultDto<MetaFieldDto>> GetTemplateMetaFieldsAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.GetTemplateMetaFieldsAsync(id);
        }

        /// <summary>
        /// 添加元数据字段到模板
        /// </summary>
        [HttpPost("{id}/meta-fields")]
        public virtual Task<MetaFieldDto> AddMetaFieldToTemplateAsync(Guid id, [FromBody] CreateUpdateMetaFieldDto input)
        {
            return AttachCatalogueTemplateAppService.AddMetaFieldToTemplateAsync(id, input);
        }

        /// <summary>
        /// 更新模板的元数据字段
        /// </summary>
        [HttpPut("{id}/meta-fields/{fieldKey}")]
        public virtual Task<MetaFieldDto> UpdateTemplateMetaFieldAsync(Guid id, string fieldKey, [FromBody] CreateUpdateMetaFieldDto input)
        {
            return AttachCatalogueTemplateAppService.UpdateTemplateMetaFieldAsync(id, fieldKey, input);
        }

        /// <summary>
        /// 从模板移除元数据字段
        /// </summary>
        [HttpDelete("{id}/meta-fields/{fieldKey}")]
        public virtual Task RemoveMetaFieldFromTemplateAsync(Guid id, string fieldKey)
        {
            return AttachCatalogueTemplateAppService.RemoveMetaFieldFromTemplateAsync(id, fieldKey);
        }

        /// <summary>
        /// 获取模板的元数据字段
        /// </summary>
        [HttpGet("{id}/meta-fields/{fieldKey}")]
        public virtual Task<MetaFieldDto?> GetTemplateMetaFieldAsync(Guid id, string fieldKey)
        {
            return AttachCatalogueTemplateAppService.GetTemplateMetaFieldAsync(id, fieldKey);
        }

        /// <summary>
        /// 根据条件查询元数据字段
        /// </summary>
        [HttpPost("{id}/meta-fields/query")]
        public virtual Task<ListResultDto<MetaFieldDto>> QueryTemplateMetaFieldsAsync(Guid id, [FromBody] MetaFieldQueryDto input)
        {
            return AttachCatalogueTemplateAppService.QueryTemplateMetaFieldsAsync(id, input);
        }

        /// <summary>
        /// 批量更新元数据字段顺序
        /// </summary>
        [HttpPut("{id}/meta-fields/order")]
        public virtual Task UpdateMetaFieldsOrderAsync(Guid id, [FromBody] List<string> fieldKeys)
        {
            return AttachCatalogueTemplateAppService.UpdateMetaFieldsOrderAsync(id, fieldKeys);
        }

        // ============= 模板路径相关接口 =============

        /// <summary>
        /// 根据路径获取模板
        /// </summary>
        [HttpGet("by-path")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathAsync(
            [FromQuery] string? templatePath = null,
            [FromQuery] bool includeChildren = false)
        {
            return AttachCatalogueTemplateAppService.GetTemplatesByPathAsync(templatePath, includeChildren);
        }

        /// <summary>
        /// 根据路径深度获取模板
        /// </summary>
        [HttpGet("by-path-depth")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathDepthAsync(
            [FromQuery] int depth,
            [FromQuery] bool onlyLatest = true)
        {
            return AttachCatalogueTemplateAppService.GetTemplatesByPathDepthAsync(depth, onlyLatest);
        }

        /// <summary>
        /// 计算下一个模板路径
        /// </summary>
        [HttpGet("calculate-next-path")]
        public virtual Task<string> CalculateNextTemplatePathAsync([FromQuery] string? parentPath = null)
        {
            return AttachCatalogueTemplateAppService.CalculateNextTemplatePathAsync(parentPath);
        }

        /// <summary>
        /// 验证模板路径格式
        /// </summary>
        [HttpGet("validate-path")]
        public virtual Task<bool> ValidateTemplatePathAsync([FromQuery] string? templatePath = null)
        {
            return AttachCatalogueTemplateAppService.ValidateTemplatePathAsync(templatePath);
        }

        /// <summary>
        /// 根据路径范围获取模板
        /// </summary>
        [HttpGet("by-path-range")]
        public virtual Task<ListResultDto<AttachCatalogueTemplateDto>> GetTemplatesByPathRangeAsync(
            [FromQuery] string? startPath = null,
            [FromQuery] string? endPath = null,
            [FromQuery] bool onlyLatest = true)
        {
            return AttachCatalogueTemplateAppService.GetTemplatesByPathRangeAsync(startPath, endPath, onlyLatest);
        }
    }
}
