using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueTemplateAppService :
        ICrudAppService<
            AttachCatalogueTemplateDto,
            Guid,
            PagedAndSortedResultRequestDto,
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
    }
}
