using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.HttpApi
{
    [RemoteService]
    [Route("api/attach-catalogue-template")]
    public class AttachCatalogueTemplateController(IAttachCatalogueTemplateAppService attachCatalogueTemplateAppService) : IAttachCatalogueTemplateAppService
    {
        private readonly IAttachCatalogueTemplateAppService _attachCatalogueTemplateAppService = attachCatalogueTemplateAppService;

        public IAttachCatalogueTemplateAppService AttachCatalogueTemplateAppService => _attachCatalogueTemplateAppService;

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
        [HttpGet("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> GetAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.GetAsync(id);
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
        [HttpPut("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> UpdateAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.UpdateAsync(id, input);
        }

        /// <summary>
        /// 删除分类模板
        /// </summary>
        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.DeleteAsync(id);
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
        /// 获取模板结构
        /// </summary>
        [HttpGet("{id}/structure")]
        public virtual Task<AttachCatalogueStructureDto> GetTemplateStructureAsync(Guid id, bool includeHistory = false)
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
        [HttpPut("{id}/set-latest")]
        public virtual Task<AttachCatalogueTemplateDto> SetAsLatestVersionAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.SetAsLatestVersionAsync(id);
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
        [HttpPost("{id}/rollback")]
        public virtual Task<AttachCatalogueTemplateDto> RollbackToVersionAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.RollbackToVersionAsync(id);
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
            [FromQuery] bool onlyLatest = true)
        {
            return AttachCatalogueTemplateAppService.GetRootTemplatesAsync(
                facetType, templatePurpose, includeChildren, onlyLatest);
        }
    }
}
