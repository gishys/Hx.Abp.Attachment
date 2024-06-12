using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities.Events;
using Volo.Abp.EventBus;

namespace Hx.Abp.Attachment.Domain
{
    public class AttachFileCreatedHandler : ILocalEventHandler<EntityCreatedEventData<AttachFile>>, ITransientDependency
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository;
        private readonly IBlobContainer BlobContainer;
        public AttachFileCreatedHandler(IEfCoreAttachCatalogueRepository catalogueRepository, IBlobContainerFactory blobContainerFactory)
        {
            CatalogueRepository = catalogueRepository;
            BlobContainer = blobContainerFactory.Create("attachment");
        }
        public async Task HandleEventAsync(EntityCreatedEventData<AttachFile> eventData)
        {
            if (eventData.Entity.AttachCatalogueId.HasValue)
            {
                var entity = await CatalogueRepository.FindAsync(eventData.Entity.AttachCatalogueId.Value);
                if (entity != null)
                {
                    entity.CalculatePageCount(FileHelper.CalculateFilePages(
                        eventData.Entity.FileType,
                        await BlobContainer.GetAllBytesOrNullAsync(eventData.Entity.FilePath)));
                    entity.AddAttachCount();
                    await CatalogueRepository.UpdateAsync(entity);
                }
            }
        }
    }
}