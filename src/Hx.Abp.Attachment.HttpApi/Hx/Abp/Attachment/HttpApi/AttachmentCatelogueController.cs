using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.AspNetCore.Mvc;
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
        public virtual Task DeleteByReferenceAsync(List<AttachCatalogueCreateDto> inputs)
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


        // 新增的API端点

        [Route("permissions/set")]
        [HttpPut]
        public virtual Task SetPermissionsAsync(Guid id, [FromBody] List<AttachCatalogueTemplatePermissionDto> permissions)
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
    }
}
