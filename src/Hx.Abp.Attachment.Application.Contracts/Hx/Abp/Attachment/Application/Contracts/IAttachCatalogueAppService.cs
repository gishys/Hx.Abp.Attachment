using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueAppService : IApplicationService
    {
        Task<AttachCatalogueDto> CreateAsync(AttachCatalogueCreateDto create);
        Task<List<AttachCatalogueDto>> FindByReferenceAsync(List<GetAttachListInput> inputs);
        Task<AttachCatalogueDto> UpdateAsync(Guid id, AttachCatalogueUpdateDto input);
        Task DeleteAsync(Guid id);
        Task DeleteSingleFileAsync(Guid attachFileId);
        Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input);
        Task<List<AttachFileDto>> QueryFilesAsync(Guid catalogueId);
        Task<AttachFileDto> QueryFileAsync(Guid attachFileId);
        Task<List<AttachFileDto>> CreateFilesAsync(Guid id, List<AttachFileCreateDto> input);
        Task<FileVerifyResultDto> VerifyUploadAsync(List<GetAttachListInput> inputs, bool details = false);
        Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode createMode);
        Task DeleteByReferenceAsync(List<AttachCatalogueCreateDto> inputs);
    }
}
