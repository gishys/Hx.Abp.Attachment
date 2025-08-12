using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    public interface IArchiveAIAppService : IApplicationService
    {
        Task<List<RecognizeCharacterDto>> OcrFullTextAsync(List<Guid> ids);
    }
}
