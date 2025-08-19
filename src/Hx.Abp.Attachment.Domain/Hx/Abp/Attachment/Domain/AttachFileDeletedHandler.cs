using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;

namespace Hx.Abp.Attachment.Domain
{
    public class AttachFileDeletedHandler(IEfCoreAttachCatalogueRepository catalogueRepository, IBlobContainerFactory blobContainerFactory) : ILocalEventHandler<EntityDeletedEventData<AttachFile>>, ITransientDependency
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");

        public async Task HandleEventAsync(EntityDeletedEventData<AttachFile> eventData)
        {
            if (eventData.Entity.AttachCatalogueId.HasValue)
            {
                var entity = await CatalogueRepository.FindAsync(eventData.Entity.AttachCatalogueId.Value);
                if (entity != null)
                {
                    var pageCount = FileHelper.CalculateFilePages(
                        eventData.Entity.FileType,
                        await BlobContainer.GetAllBytesOrNullAsync(eventData.Entity.FilePath));
                    entity.CalculatePageCount(-pageCount);
                    entity.AddAttachCount(-1);
                    await CatalogueRepository.UpdateAsync(entity);
                }
            }
        }
    }
}
