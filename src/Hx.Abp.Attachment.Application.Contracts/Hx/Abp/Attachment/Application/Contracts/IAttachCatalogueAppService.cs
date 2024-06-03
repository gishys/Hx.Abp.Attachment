using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IAttachCatalogueAppService : IApplicationService
    {
        Task<AttachCatalogueDto> CreateAsync(AttachCatalogueCreateDto create);
        Task<List<AttachCatalogueDto>> FindByReferenceAsync(string reference);
        Task<AttachCatalogueDto> UpdateAsync(Guid id, AttachCatalogueUpdateDto input);
        Task DeleteAsync(Guid id);
       
        Task DeleteSingleFileAsync(Guid catalogueId, Guid attachFileId);
        Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input);
        Task<AttachFileDto> QuerySingleFileAsync(Guid catalogueId, Guid attachFileId);
        Task<List<AttachFileDto>> DownloadFilesAsync(Guid catalogueId);
        Task DeleteFilesAsync(Guid catalogueId);
        Task<AttachFileDto> DownloadSingleFileAsync(Guid catalogueId, Guid attachFileId);
        Task<AttachFileDto> CreateSingleFileAsync(Guid id, AttachFileCreateDto input);
    }
}
