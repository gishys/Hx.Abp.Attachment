using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using iTextSharp.text.pdf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Uow;

namespace Hx.Abp.Attachment.Application
{
    [Dependency(ServiceLifetime.Singleton, ReplaceServices = true)]
    public class AttachCatalogueAppService(
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IConfiguration configuration,
        IBlobContainerFactory blobContainerFactory) : AttachmentService, IAttachCatalogueAppService
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");
        private readonly IConfiguration Configuration = configuration;
        private readonly IBlobContainerFactory BlobContainerFactory = blobContainerFactory;
        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual async Task<AttachCatalogueDto> CreateAsync(AttachCatalogueCreateDto input)
        {
            using var uow = UnitOfWorkManager.Begin();
            var existingCatalogue = await CatalogueRepository.AnyByNameAsync(input.CatalogueName);
            if (existingCatalogue)
            {
                await CatalogueRepository.DeleteByNameAsync(input.CatalogueName);
                await uow.SaveChangesAsync();
            }
            var maxNumber = 1;
            if (input.ParentId.HasValue)
            {
                var entitys = await CatalogueRepository.FindByParentIdAsync(input.ParentId.Value);
                maxNumber = entitys.Max(d => d.SequenceNumber) + 1;
            }
            var attachCatalogue = new AttachCatalogue(
                    GuidGenerator.Create(),
                    input.AttachReceiveType,
                    input.CatalogueName,
                    maxNumber,
                    input.BusinessId,
                    input.ParentId,
                    isRequired: input.IsRequired,
                    isVerification: input.IsVerification,
                    verificationPassed: input.VerificationPassed);
            await CatalogueRepository.InsertAsync(attachCatalogue);
            await uow.SaveChangesAsync();
            return ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(attachCatalogue);
        }
        /// <summary>
        /// 批量下载
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <returns></returns>
        public virtual async Task<List<AttachFileDto>> DownloadFilesAsync(Guid catalogueId)
        {
            var catologue = await CatalogueRepository.FindAsync(catalogueId);
            List<AttachFileDto> files = [];
            if (catologue?.AttachFiles?.Count > 0)
            {
                foreach (var file in catologue.AttachFiles)
                {           
                    var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}{file.FilePath}";
                    var tempFile = new AttachFileDto()
                    {
                        FilePath = src,
                        Id = file.Id,
                        FileAlias = file.FileAlias,
                        SequenceNumber = file.SequenceNumber
                    };
                    files.Add(tempFile);
                }
            }
            return files;
        }
        /// <summary>
        /// 单个文件下载
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <returns></returns>
        public virtual async Task<AttachFileDto> DownloadSingleFileAsync(Guid catalogueId, Guid attachFileId)
        {
            var entity = await CatalogueRepository.FindAsync(catalogueId);
            var attachFile = (entity?.AttachFiles?.FirstOrDefault(d => d.Id == attachFileId)) ?? throw new BusinessException(message: "没有查询到有效的文件！");
            var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}{attachFile.FilePath}";
            var tempFile = new AttachFileDto()
            {
                FilePath = src,
                Id = attachFile.Id,
                FileAlias = attachFile.FileAlias,
                SequenceNumber = attachFile.SequenceNumber
            };
            return tempFile;
        }
        /// <summary>
        /// 创建文件(区分pdf计算页数完)，文件存储要有结构
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task<AttachFileDto> CreateSingleFileAsync(Guid id, AttachFileCreateDto input)
        {
            using var uow = UnitOfWorkManager.Begin();
            var attachId = GuidGenerator.Create();
            var tempAttachFile = await CatalogueRepository.FindAsync(id);
            if (tempAttachFile != null)
            {
                var tempSequenceNumber =
                    tempAttachFile.AttachFiles?.Count > 0 ?
                    tempAttachFile.AttachFiles.Max(d => d.SequenceNumber) : 0;
                var fileName = $"{attachId}{Path.GetExtension(input.FileAlias)}";
                var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{tempAttachFile.BusinessId}/{fileName}";
                var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}{fileUrl}";
                var tempFile = new AttachFile(
                    attachId,
                    input.FileAlias,
                    ++tempSequenceNumber,
                    fileName,
                    fileUrl);
                await BlobContainer.SaveAsync(fileName, input.DocumentContent);
                string fileExtension = Path.GetExtension(input.FileAlias).ToLowerInvariant();
                var pagesToAdd = fileExtension switch
                {
                    ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => 1,
                    ".pdf" => CalculatePdfPages(input.DocumentContent),
                    _ => 0,
                };
                tempAttachFile.AddAttachFile(tempFile, tempAttachFile.PageCount + pagesToAdd);
                await CatalogueRepository.UpdateAsync(tempAttachFile);
                await uow.SaveChangesAsync();
                return new AttachFileDto()
                {
                    Id = attachId,
                    FileAlias = input.FileAlias,
                    FilePath = src,
                    SequenceNumber = tempFile.SequenceNumber
                };
            }
            else
            {
                throw new BusinessException(message: "没有查询到目录！");
            }
        }
        private static int CalculatePdfPages(byte[] pdfContent)
        {
            using var memoryStream = new MemoryStream(pdfContent);
            var reader = new PdfReader(memoryStream);
            return reader.NumberOfPages;
        }
        /// <summary>
        /// 删除单个文件
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <param name="attachFileId"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task DeleteSingleFileAsync(Guid catalogueId, Guid attachFileId)
        {
            using var uow = UnitOfWorkManager.Begin();
            var entity = await CatalogueRepository.FindAsync(catalogueId) ?? throw new UserFriendlyException(message: "删除文件的目录不存在！");
            if (entity?.AttachFiles?.Count > 0)
            {
                var attachFile = (entity.AttachFiles?.FirstOrDefault(d => d.Id == attachFileId)) ?? throw new UserFriendlyException(message: "没有查询到有效的文件！");
                entity.AttachFiles?.Remove(attachFile);
                await BlobContainer.DeleteAsync(attachFile.FileName);
                await CatalogueRepository.UpdateAsync(entity);
                await uow.SaveChangesAsync();
            }
            else
            {
                throw new UserFriendlyException(message: "没有查询到有效的文件！");
            }
        }
        /// <summary>
        /// 清空文件夹
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task DeleteFilesAsync(Guid catalogueId)
        {
            var entity = await CatalogueRepository.FindAsync(catalogueId) ?? throw new UserFriendlyException(message: "删除文件的目录不存在！");
            if (entity.AttachFiles?.Count > 0)
            {
                foreach (var file in entity.AttachFiles)
                {
                    await BlobContainer.DeleteAsync(file.FileName);
                }
                entity.RemoveFiles(_ => true, 0);
                await CatalogueRepository.UpdateAsync(entity, true);
            }
        }
        /// <summary>
        /// 替换文件（存储的文件没有替换核实对错）
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <param name="attachFileId"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task<AttachFileDto> UpdateSingleFileAsync(Guid catalogueId, Guid attachFileId, AttachFileCreateDto input)
        {
            using var uow = UnitOfWorkManager.Begin();
            var entity = await CatalogueRepository.FindAsync(catalogueId) ?? throw new UserFriendlyException(message: "替换文件的目录不存在！");
            var target = entity?.AttachFiles?.FirstOrDefault(d => d.Id == attachFileId);
            if (entity != null && target != null)
            {
                entity.AttachFiles.RemoveAll(d => d.Id == attachFileId);
                await BlobContainer.DeleteAsync(target.FileName);
                var attachId = GuidGenerator.Create();
                var fileName = $"{attachId}{Path.GetExtension(input.FileAlias)}";
                var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{entity.BusinessId}/{fileName}";
                var tempFile = new AttachFile(
                    attachId,
                    input.FileAlias,
                    target.SequenceNumber,
                    fileName,
                    fileUrl);
                await BlobContainer.SaveAsync(fileName, input.DocumentContent);
                entity.AddAttachFile(tempFile, entity.PageCount + 1);
                await CatalogueRepository.UpdateAsync(entity);
                await uow.SaveChangesAsync();
                return ObjectMapper.Map<AttachFile, AttachFileDto>(target);
            }
            else
            {
                throw new UserFriendlyException(message: "没有可替换文件！");
            }
        }
        /// <summary>
        /// 查询单个文件
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <param name="attachFileId"></param>
        /// <returns></returns>
        public virtual async Task<AttachFileDto> QuerySingleFileAsync(Guid catalogueId, Guid attachFileId)
        {
            var entity = await CatalogueRepository.FindSingleAttachFileAsync(catalogueId, attachFileId) ?? throw new UserFriendlyException(message: "没有查到文件！");
            var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}{entity.FilePath}";
            return new AttachFileDto()
            {
                FilePath = src,
                Id = entity.Id,
                FileAlias = entity.FileAlias,
                SequenceNumber = entity.SequenceNumber
            };
        }
        /// <summary>
        /// 通过业务编号获取所有的附件（文件夹及文件）
        /// </summary>
        /// <param name="businessId"></param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> FindByBusinessIdAsync(string businessId)
        {
            var entity = await CatalogueRepository.FindByBusinessIdAsync(businessId);
            return ObjectMapper.Map<List<AttachCatalogue>, List<AttachCatalogueDto>>(entity);
        }
        /// <summary>
        /// 修改目录
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual async Task<AttachCatalogueDto> UpdateAsync(Guid id, AttachCatalogueUpdateDto input)
        {
            using var uow = UnitOfWorkManager.Begin();
            var entity = await CatalogueRepository.GetAsync(id);
            if (entity.AttachReceiveType != input.AttachReceiveType)
            {
                entity.SetAttachReceiveType(input.AttachReceiveType);
            }
            if (!string.Equals(entity.CatalogueName, input.CatalogueName, StringComparison.InvariantCultureIgnoreCase))
            {
                entity.SetCatalogueName(input.CatalogueName);
            }
            if (!string.Equals(entity.BusinessId, input.BusinessId, StringComparison.InvariantCultureIgnoreCase))
            {
                entity.SetBusinessId(input.BusinessId);
            }
            await CatalogueRepository.UpdateAsync(entity);
            await uow.SaveChangesAsync();
            return ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(entity);
        }
        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task DeleteAsync(Guid id)
        {
            using var uow = UnitOfWorkManager.Begin();
            var entity = await CatalogueRepository.FindAsync(id) ?? throw new BusinessException(message: "没有查询到有效的目录");
            if (entity.AttachFiles?.Count > 0)
            {
                System.Collections.ObjectModel.Collection<AttachFile>? attachs = entity?.AttachFiles;
                for (int i = 0; i < attachs?.Count; i++)
                {
                    AttachFile? file = attachs[i];
                    await BlobContainer.DeleteAsync(file.FileName);
                }
            }
            await CatalogueRepository.DeleteAsync(id);
            await uow.SaveChangesAsync();
        }
    }
}
