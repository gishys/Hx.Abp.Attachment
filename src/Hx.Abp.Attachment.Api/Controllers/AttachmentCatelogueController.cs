using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.Api.Controllers
{
    [ApiController]
    [Route("api/app/attachment")]
    public class AttachmentCatelogueController : AbpControllerBase
    {
        protected IAttachCatalogueAppService AttachCatalogueAppService { get; }
        public AttachmentCatelogueController(IAttachCatalogueAppService attachCatalogueAppService)
        {
            AttachCatalogueAppService = attachCatalogueAppService;
        }
        [HttpPost]
        public virtual Task<AttachCatalogueDto> CreateAsync(AttachCatalogueCreateDto input)
        {
            return AttachCatalogueAppService.CreateAsync(input);
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
        public virtual async Task<List<AttachFileDto>> CreateFilesAsync(Guid id)
        {
            var files = Request.Form.Files;
            if (files.Count > 0)
            {
                var inputs = new List<AttachFileCreateDto>();
                foreach (var file in files)
                {
                    byte[] fileBytes;
                    using (var fileStream = file.OpenReadStream())
                    using (var ms = new MemoryStream())
                    {
                        fileStream.CopyTo(ms);
                        fileBytes = ms.ToArray();
                    }
                    var attachFile = new AttachFileCreateDto() { DocumentContent = fileBytes, FileAlias = file.FileName };
                    inputs.Add(attachFile);
                }
                return await AttachCatalogueAppService.CreateFilesAsync(id, inputs);
            }
            throw new UserFriendlyException("上传文件为空！");
        }
        [Route("deletesinglefile")]
        [HttpDelete]
        public virtual Task DeleteSingleFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.DeleteSingleFileAsync(attachFileId);
        }
        [Route("updatesinglefile")]
        [HttpPut]
        public virtual Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input)
        {
            return AttachCatalogueAppService.UpdateSingleFileAsync(catalogueId, attachFileId, input);
        }
        [Route("findbyreference")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> FindByReferenceAsync(string reference, int referenceType)
        {
            return AttachCatalogueAppService.FindByReferenceAsync(reference, referenceType);
        }
        [Route("update")]
        [HttpPut]
        public virtual Task<AttachCatalogueDto> UpdateAsync(Guid id, AttachCatalogueUpdateDto input)
        {
            return AttachCatalogueAppService.UpdateAsync(id, input);
        }
        [Route("delete")]
        [HttpDelete]
        public virtual Task DeleteAsync(Guid id)
        {
            return AttachCatalogueAppService.DeleteAsync(id);
        }
    }
}