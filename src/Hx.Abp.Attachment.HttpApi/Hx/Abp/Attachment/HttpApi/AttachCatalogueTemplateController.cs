using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    [ApiController]
    [Route("api/app/attachment-template")]
    public class AttachCatalogueTemplateController(IAttachCatalogueTemplateAppService attachCatalogueTemplateAppService) : AbpControllerBase
    {
        protected IAttachCatalogueTemplateAppService AttachCatalogueTemplateAppService { get; } = attachCatalogueTemplateAppService;

        /// <summary>
        /// 创建模板
        /// </summary>
        [HttpPost]
        public virtual Task<AttachCatalogueTemplateDto> CreateAsync(CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.CreateAsync(input);
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        [HttpPut("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> UpdateAsync(Guid id, CreateUpdateAttachCatalogueTemplateDto input)
        {
            return AttachCatalogueTemplateAppService.UpdateAsync(id, input);
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        [HttpDelete("{id}")]
        public virtual Task DeleteAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.DeleteAsync(id);
        }

        /// <summary>
        /// 获取模板
        /// </summary>
        [HttpGet("{id}")]
        public virtual Task<AttachCatalogueTemplateDto> GetAsync(Guid id)
        {
            return AttachCatalogueTemplateAppService.GetAsync(id);
        }

        /// <summary>
        /// 获取模板列表
        /// </summary>
        [HttpGet]
        public virtual Task<PagedResultDto<AttachCatalogueTemplateDto>> GetListAsync([FromQuery] PagedAndSortedResultRequestDto input)
        {
            return AttachCatalogueTemplateAppService.GetListAsync(input);
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
        public virtual Task<AttachCatalogueStructureDto> GetTemplateStructureAsync(Guid id, [FromQuery] bool includeHistory = false)
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
        [HttpPost("{id}/versions")]
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
        /// 获取版本历史
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
    }
}
