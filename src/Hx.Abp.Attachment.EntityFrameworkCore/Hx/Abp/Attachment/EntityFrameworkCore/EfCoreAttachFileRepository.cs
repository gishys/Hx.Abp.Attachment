using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public class EfCoreAttachFileRepository(
        IDbContextProvider<AttachmentDbContext> provider)
                : EfCoreRepository<AttachmentDbContext, AttachFile, Guid>(provider),
        IEfCoreAttachFileRepository
    {
        public async Task<int> DeleteByCatalogueAsync(Guid catalogueId)
        {
            return await (await GetDbSetAsync()).Where(d => d.AttachCatalogueId == catalogueId).ExecuteDeleteAsync();
        }
    }
}