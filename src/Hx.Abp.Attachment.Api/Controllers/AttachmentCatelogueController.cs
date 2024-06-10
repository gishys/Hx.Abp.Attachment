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
        [Route("downloadfiles")]
        [HttpGet]
        public virtual Task<List<AttachFileDto>> DownloadFilesAsync(Guid catalogueId)
        {
            return AttachCatalogueAppService.DownloadFilesAsync(catalogueId);
        }
        [Route("downloadsinglefiles")]
        [HttpGet]
        public virtual Task<AttachFileDto> DownloadSingleFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.DownloadSingleFileAsync(attachFileId);
        }
        [Route("createsinglefiles")]
        [HttpPost]
        public virtual async Task<AttachFileDto> CreateSingleFileAsync(Guid id)
        {
            var files = Request.Form.Files;
            if (files.Count > 0)
            {
                var file = files[0];
                byte[] fileBytes;
                using (var fileStream = file.OpenReadStream())
                using (var ms = new MemoryStream())
                {
                    fileStream.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }
                return await AttachCatalogueAppService.CreateSingleFileAsync(id, new AttachFileCreateDto() { DocumentContent = fileBytes, FileAlias = file.FileName });

            }
            throw new UserFriendlyException("上传文件为空！");

        }
        [Route("deletesinglefile")]
        [HttpDelete]
        public virtual Task DeleteSingleFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.DeleteSingleFileAsync(attachFileId);
        }
        [Route("deletefile")]
        [HttpDelete]
        public virtual Task DeleteFilesAsync(Guid catalogueId)
        {
            return AttachCatalogueAppService.DeleteFilesAsync(catalogueId);
        }
        [Route("updatesinglefile")]
        [HttpPut]
        public virtual Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input)
        {
            return AttachCatalogueAppService.UpdateSingleFileAsync(catalogueId, attachFileId, input);
        }
        [Route("querysinglefile")]
        [HttpGet]
        public virtual Task<AttachFileDto> QuerySingleFileAsync(Guid attachFileId)
        {
            return AttachCatalogueAppService.QuerySingleFileAsync(attachFileId);
        }
        [Route("findbyreference")]
        [HttpGet]
        public virtual Task<List<AttachCatalogueDto>> FindByReferenceAsync(string Reference)
        {
            return AttachCatalogueAppService.FindByReferenceAsync(Reference);
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
