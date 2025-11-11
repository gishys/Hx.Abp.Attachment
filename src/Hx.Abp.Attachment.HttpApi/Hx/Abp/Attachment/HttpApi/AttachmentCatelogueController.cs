using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    [ApiController]
    [Route("api/app/attachment")]
    public class AttachmentCatalogueController(IAttachCatalogueAppService attachCatalogueAppService) : AbpControllerBase
    {
        protected IAttachCatalogueAppService AttachCatalogueAppService { get; } = attachCatalogueAppService;

        [HttpPost]
        public virtual Task<AttachCatalogueDto?> CreateAsync(AttachCatalogueCreateDto input, CatalogueCreateMode? mode)
        {
            return AttachCatalogueAppService.CreateAsync(input, mode);
        }

        [Route("createmany")]
        [HttpPost]
        public virtual Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode mode)
        {
            return AttachCatalogueAppService.CreateManyAsync(inputs, mode);
        }

        [Route("queryfiles")]
        [HttpGet]
        public virtual Task<List<AttachFileDto>> QueryFilesAsync(Guid catalogueId)
        {
            return AttachCatalogueAppService.QueryFilesAsync(catalogueId);
        }

        [Route("query")]
        [HttpGet]
        public virtual Task<AttachFileDto> QueryFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.QueryFileAsync(attachFileId);
        }

        [Route("uploadfiles")]
        [HttpPost]
        public virtual async Task<List<AttachFileDto>> CreateFilesAsync(Guid? id, string? prefix)
        {
            var files = Request.Form.Files;
            var inputs = new List<AttachFileCreateDto>();
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var input = new AttachFileCreateDto
                {
                    FileAlias = file.FileName,
                    DocumentContent = memoryStream.ToArray()
                };
                inputs.Add(input);
            }
            return await AttachCatalogueAppService.CreateFilesAsync(id, inputs, prefix);
        }

        [Route("update")]
        [HttpPut]
        public virtual Task<AttachCatalogueDto?> UpdateAsync(Guid id, AttachCatalogueCreateDto input)
        {
            return AttachCatalogueAppService.UpdateAsync(id, input);
        }

        [Route("delete")]
        [HttpDelete]
        public virtual Task DeleteAsync(Guid id)
        {
            return AttachCatalogueAppService.DeleteAsync(id);
        }

        [Route("deletefile")]
        [HttpDelete]
        public virtual Task DeleteSingleFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.DeleteSingleFileAsync(attachFileId);
        }

        [Route("updatefile")]
        [HttpPut]
        public virtual Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input)
        {
            return AttachCatalogueAppService.UpdateSingleFileAsync(catalogueId, attachFileId, input);
        }

        [Route("findbyreference")]
        [HttpPost]
        public virtual Task<List<AttachCatalogueDto>> FindByReferenceAsync(List<GetAttachListInput> inputs)
        {
            return AttachCatalogueAppService.FindByReferenceAsync(inputs);
        }

        [Route("verifyupload")]
        [HttpPost]
        public virtual Task<FileVerifyResultDto> VerifyUploadAsync(List<GetAttachListInput> inputs, bool details = false)
        {
            return AttachCatalogueAppService.VerifyUploadAsync(inputs, details);
        }

        [Route("deletebyreference")]
        [HttpPost]
        public virtual Task DeleteByReferenceAsync(List<GetAttachListInput> inputs)
        {
            return AttachCatalogueAppService.DeleteByReferenceAsync(inputs);
        }

        [Route("getbyfileid")]
        [HttpGet]
        public virtual Task<AttachCatalogueDto?> GetAttachCatalogueByFileIdAsync(Guid fileId)
        {
            return AttachCatalogueAppService.GetAttachCatalogueByFileIdAsync(fileId);
        }

        [Route("search/fulltext")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> SearchByFullTextAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10)
        {
            return AttachCatalogueAppService.SearchByFullTextAsync(searchText, reference, referenceType, limit);
        }

        [Route("search/hybrid")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> SearchByHybridAsync(string? searchText = null, string? reference = null, int? referenceType = null, int limit = 10, string? queryTextVector = null, float similarityThreshold = 0.7f)
        {
            return AttachCatalogueAppService.SearchByHybridAsync(searchText, reference, referenceType, limit, queryTextVector, similarityThreshold);
        }


        // 新增的API端点

        [Route("permissions/set")]
        [HttpPut]
        public virtual Task SetPermissionsAsync(Guid id, [FromBody] List<CreateAttachCatalogueTemplatePermissionDto> permissions)
        {
            return AttachCatalogueAppService.SetPermissionsAsync(id, permissions);
        }

        [Route("permissions/get")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueTemplatePermissionDto>> GetPermissionsAsync(Guid id)
        {
            return AttachCatalogueAppService.GetPermissionsAsync(id);
        }

        [Route("permissions/check")]
        [HttpGet]
        public virtual Task<bool> HasPermissionAsync(Guid id, Guid userId, PermissionAction action)
        {
            return AttachCatalogueAppService.HasPermissionAsync(id, userId, action);
        }

        [Route("identifier/description")]
        [HttpGet]
        public virtual Task<string> GetCatalogueIdentifierDescriptionAsync(Guid id)
        {
            return AttachCatalogueAppService.GetCatalogueIdentifierDescriptionAsync(id);
        }

        [Route("search/by-identifier")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> GetByCatalogueIdentifierAsync(FacetType? catalogueFacetType = null, TemplatePurpose? cataloguePurpose = null)
        {
            return AttachCatalogueAppService.GetByCatalogueIdentifierAsync(catalogueFacetType, cataloguePurpose);
        }

        [Route("search/by-vector-dimension")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> GetByVectorDimensionAsync(int? minDimension = null, int? maxDimension = null)
        {
            return AttachCatalogueAppService.GetByVectorDimensionAsync(minDimension, maxDimension);
        }

        // 元数据字段管理接口

        [Route("metafields/set")]
        [HttpPut]
        public virtual Task SetMetaFieldsAsync(Guid id, [FromBody] List<CreateUpdateMetaFieldDto> metaFields)
        {
            return AttachCatalogueAppService.SetMetaFieldsAsync(id, metaFields);
        }

        [Route("metafields/get")]
        [HttpGet]
        public virtual Task<MetaFieldDto?> GetMetaFieldAsync(Guid id, string fieldKey)
        {
            return AttachCatalogueAppService.GetMetaFieldAsync(id, fieldKey);
        }

        [Route("metafields/enabled")]
        [HttpGet]
        public virtual Task<List<MetaFieldDto>> GetEnabledMetaFieldsAsync(Guid id)
        {
            return AttachCatalogueAppService.GetEnabledMetaFieldsAsync(id);
        }

        // 模板相关查询接口

        [Route("search/by-template")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> FindByTemplateAsync(Guid templateId, int? templateVersion = null)
        {
            return AttachCatalogueAppService.FindByTemplateAsync(templateId, templateVersion);
        }

        [Route("search/by-template-id")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> FindByTemplateIdAsync(Guid templateId)
        {
            return AttachCatalogueAppService.FindByTemplateIdAsync(templateId);
        }

        // ============= 树形结构查询接口 =============

        /// <summary>
        /// 获取分类树形结构（用于树状展示）
        /// 基于行业最佳实践，支持多种查询条件和性能优化
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践，使用路径优化
        /// 注意：分页是对根节点进行分页，返回的每个根节点包含完整的子树结构
        /// </summary>
        /// <param name="reference">业务引用，null表示查询所有业务</param>
        /// <param name="referenceType">业务类型，null表示查询所有类型</param>
        /// <param name="catalogueFacetType">分类分面类型，null表示查询所有类型</param>
        /// <param name="cataloguePurpose">分类用途，null表示查询所有用途</param>
        /// <param name="includeChildren">是否包含子节点，默认true</param>
        /// <param name="includeFiles">是否包含附件文件，默认false</param>
        /// <param name="fulltextQuery">全文搜索查询，支持中文分词</param>
        /// <param name="templateId">模板ID过滤，null表示查询所有模板</param>
        /// <param name="templateVersion">模板版本过滤，null表示查询所有版本</param>
        /// <param name="skipCount">跳过数量，用于分页，默认0</param>
        /// <param name="maxResultCount">最大返回数量，用于分页，默认不限制</param>
        /// <returns>分类树形结构分页结果</returns>
        [Route("tree")]
        [HttpGet]
        public virtual Task<PagedResultDto<AttachCatalogueTreeDto>> GetCataloguesTreeAsync(
            [FromQuery] string? reference = null,
            [FromQuery] int? referenceType = null,
            [FromQuery] FacetType? catalogueFacetType = null,
            [FromQuery] TemplatePurpose? cataloguePurpose = null,
            [FromQuery] bool includeChildren = true,
            [FromQuery] bool includeFiles = false,
            [FromQuery] string? fulltextQuery = null,
            [FromQuery] Guid? templateId = null,
            [FromQuery] int? templateVersion = null,
            [FromQuery] int skipCount = 0,
            [FromQuery] int maxResultCount = int.MaxValue)
        {
            return AttachCatalogueAppService.GetCataloguesTreeAsync(
                reference, referenceType, catalogueFacetType, cataloguePurpose,
                includeChildren, includeFiles, fulltextQuery, templateId, templateVersion,
                skipCount, maxResultCount);
        }

        // ============= 智能分类接口 =============

        /// <summary>
        /// 智能分类文件上传和推荐
        /// 基于OCR内容进行智能分类推荐，适用于文件自动归类场景
        /// 如果分类模板中存在动态分面，需要通过dynamicFacetInfoList参数传入动态分面信息数组（如案卷信息）来创建动态分面分类
        /// 文件与动态分面的映射方式：
        /// 1. 优先使用文件自身携带的DynamicFacetCatalogueName字段（通过fileFacetMapping从FormData读取）
        /// 2. 如果没有，则使用dynamicFacetInfoList数组，按文件顺序对应（向后兼容）
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="prefix">文件前缀</param>
        /// <returns>智能分类推荐结果列表</returns>
        [Route("smart-classification/upload")]
        [HttpPost]
        public virtual async Task<List<SmartClassificationResultDto>> CreateFilesWithSmartClassificationAsync(
            [FromQuery] Guid catalogueId, 
            [FromQuery] string? prefix = null)
        {
            var files = Request.Form.Files;
            var inputs = new List<AttachFileCreateDto>();
            
            // 从FormData中读取文件与动态分面的映射关系（文件名 -> 动态分面分类名称）
            Dictionary<string, string>? fileFacetMapping = null;
            if (Request.Form.ContainsKey("fileFacetMapping"))
            {
                var mappingJson = Request.Form["fileFacetMapping"].ToString();
                if (!string.IsNullOrEmpty(mappingJson))
                {
                    fileFacetMapping = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(mappingJson);
                }
            }
            
            foreach (var file in files)
            {
                using var stream = file.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                
                var fileName = file.FileName ?? Guid.NewGuid().ToString();
                var fileDto = new AttachFileCreateDto
                {
                    FileAlias = fileName,
                    DocumentContent = memoryStream.ToArray(),
                    SequenceNumber = null // 让服务层自动分配序号
                };
                
                // 如果存在文件与动态分面的映射关系，设置文件的动态分面分类名称
                if (fileFacetMapping != null && fileFacetMapping.TryGetValue(fileName, out var facetCatalogueName))
                {
                    fileDto.DynamicFacetCatalogueName = facetCatalogueName;
                }
                
                inputs.Add(fileDto);
            }

            // 从FormData中读取dynamicFacetInfoList（JSON字符串，用于向后兼容）
            List<DynamicFacetInfoDto>? dynamicFacetInfoList = null;
            if (Request.Form.ContainsKey("dynamicFacetInfoList"))
            {
                var jsonString = Request.Form["dynamicFacetInfoList"].ToString();
                if (!string.IsNullOrEmpty(jsonString))
                {
                    dynamicFacetInfoList = System.Text.Json.JsonSerializer.Deserialize<List<DynamicFacetInfoDto>>(jsonString);
                }
            }
            // 如果FormData中没有，尝试从Request Body中读取（兼容直接发送JSON的情况）
            else if (Request.ContentType?.Contains("application/json") == true)
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var bodyString = await reader.ReadToEndAsync();
                if (!string.IsNullOrEmpty(bodyString))
                {
                    dynamicFacetInfoList = System.Text.Json.JsonSerializer.Deserialize<List<DynamicFacetInfoDto>>(bodyString);
                }
            }

            return await AttachCatalogueAppService.CreateFilesWithSmartClassificationAsync(catalogueId, inputs, prefix, dynamicFacetInfoList);
        }

        /// <summary>
        /// 确定文件分类
        /// 将文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="ocrContent">OCR全文内容（可选）</param>
        /// <returns>更新后的文件信息</returns>
        [Route("confirm-classification")]
        [HttpPost]
        public virtual Task<AttachFileDto> ConfirmFileClassificationAsync(
            [FromQuery] Guid fileId, 
            [FromQuery] Guid catalogueId, 
            [FromBody] string? ocrContent = null)
        {
            return AttachCatalogueAppService.ConfirmFileClassificationAsync(fileId, catalogueId, ocrContent);
        }

        /// <summary>
        /// 批量确定文件分类
        /// 将多个文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="requests">文件分类请求列表</param>
        /// <returns>更新后的文件信息列表</returns>
        [Route("confirm-classifications")]
        [HttpPost]
        public virtual Task<List<AttachFileDto>> ConfirmFileClassificationsAsync(
            [FromBody] List<ConfirmFileClassificationRequest> requests)
        {
            return AttachCatalogueAppService.ConfirmFileClassificationsAsync(requests);
        }

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表
        /// 查询未归档的文件列表
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>文件列表</returns>
        [Route("files/by-reference-template")]
        [HttpGet]
        public virtual Task<List<AttachFileDto>> GetFilesByReferenceAndTemplatePurposeAsync(
            [FromQuery] string reference, 
            [FromQuery] TemplatePurpose templatePurpose)
        {
            return AttachCatalogueAppService.GetFilesByReferenceAndTemplatePurposeAsync(reference, templatePurpose);
        }

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表并进行智能分类推荐
        /// 查询未归档的文件列表，并为每个文件提供分类推荐
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>智能分类推荐结果列表</returns>
        [Route("files/by-reference-template-with-classification")]
        [HttpGet]
        public virtual Task<List<SmartClassificationResultDto>> GetFilesWithSmartClassificationByReferenceAndTemplatePurposeAsync(
            [FromQuery] string reference, 
            [FromQuery] TemplatePurpose templatePurpose)
        {
            return AttachCatalogueAppService.GetFilesWithSmartClassificationByReferenceAndTemplatePurposeAsync(reference, templatePurpose);
        }

        // ============= 归档管理接口 =============

        /// <summary>
        /// 根据归档状态查询分类
        /// </summary>
        /// <param name="isArchived">归档状态</param>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <returns>匹配的分类列表</returns>
        [Route("search/by-archived-status")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> GetByArchivedStatusAsync(
            [FromQuery] bool isArchived, 
            [FromQuery] string? reference = null, 
            [FromQuery] int? referenceType = null)
        {
            return AttachCatalogueAppService.GetByArchivedStatusAsync(isArchived, reference, referenceType);
        }

        /// <summary>
        /// 批量设置归档状态
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新的记录数</returns>
        [Route("archive/batch-set")]
        [HttpPut]
        public virtual Task<int> SetArchivedStatusAsync(
            [FromBody] List<Guid> catalogueIds, 
            [FromQuery] bool isArchived)
        {
            return AttachCatalogueAppService.SetArchivedStatusAsync(catalogueIds, isArchived);
        }

        /// <summary>
        /// 设置分类归档状态
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新后的分类信息</returns>
        [Route("archive/set")]
        [HttpPut]
        public virtual Task<AttachCatalogueDto?> SetCatalogueArchivedStatusAsync(
            [FromQuery] Guid id, 
            [FromQuery] bool isArchived)
        {
            return AttachCatalogueAppService.SetCatalogueArchivedStatusAsync(id, isArchived);
        }

        /// <summary>
        /// 设置分类概要信息
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="input">概要信息输入</param>
        /// <returns>更新后的分类信息</returns>
        [Route("summary/set")]
        [HttpPut]
        public virtual Task<AttachCatalogueDto?> SetCatalogueSummaryAsync(
            [FromQuery] Guid id, 
            [FromBody] SetCatalogueSummaryInputDto input)
        {
            return AttachCatalogueAppService.SetCatalogueSummaryAsync(id, input?.Summary);
        }

        /// <summary>
        /// 设置分类标签
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="tags">标签列表</param>
        /// <returns>更新后的分类信息</returns>
        [Route("tags/set")]
        [HttpPut]
        public virtual Task<AttachCatalogueDto?> SetCatalogueTagsAsync(
            [FromQuery] Guid id, 
            [FromBody] List<string>? tags)
        {
            return AttachCatalogueAppService.SetCatalogueTagsAsync(id, tags);
        }

        /// <summary>
        /// 获取指定分类下的所有叶子节点选项
        /// 用于智能分类和文件归类场景，返回可分类的叶子节点列表
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <returns>叶子节点选项列表</returns>
        [Route("leaf-categories")]
        [HttpGet]
        public virtual Task<List<LeafCategoryOptionDto>> GetLeafCategoriesAsync(
            [FromQuery] Guid catalogueId)
        {
            return AttachCatalogueAppService.GetLeafCategoriesAsync(catalogueId);
        }

        // ============= 智能分析接口 =============

        /// <summary>
        /// 智能分析分类信息
        /// 基于分类下的文件内容，自动生成概要信息、分类标签、全文内容和元数据
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="forceUpdate">是否强制更新（默认false，只更新空值）</param>
        /// <returns>智能分析结果</returns>
        [Route("intelligent-analysis")]
        [HttpPost]
        public virtual Task<IntelligentAnalysisResultDto> AnalyzeCatalogueIntelligentlyAsync(
            [FromQuery] Guid id, 
            [FromQuery] bool forceUpdate = false)
        {
            return AttachCatalogueAppService.AnalyzeCatalogueIntelligentlyAsync(id, forceUpdate);
        }
    }
}
