using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;

namespace Hx.Abp.Attachment.Application
{
    [Dependency(ServiceLifetime.Singleton, ReplaceServices = true)]
    public class AttachCatalogueAppService(
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IConfiguration configuration,
        IBlobContainerFactory blobContainerFactory,
        IEfCoreAttachFileRepository efCoreAttachFileRepository) : AttachmentService, IAttachCatalogueAppService
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");
        private readonly IConfiguration Configuration = configuration;
        private readonly IBlobContainerFactory BlobContainerFactory = blobContainerFactory;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual async Task<AttachCatalogueDto?> CreateAsync(AttachCatalogueCreateDto input, CatalogueCreateMode? createMode)
        {
            using var uow = UnitOfWorkManager.Begin();
            var existingCatalogue = await CatalogueRepository.AnyByNameAsync(input.ParentId, input.CatalogueName, input.Reference, input.ReferenceType);
            if (createMode != CatalogueCreateMode.SkipExistAppend && existingCatalogue)
            {
                throw new UserFriendlyException("名称重复，请先删除现有名称再创建！");
            }
            AttachCatalogue? attachCatalogue;
            if (createMode == CatalogueCreateMode.SkipExistAppend && existingCatalogue)
            {
                attachCatalogue = await CatalogueRepository.GetAsync(input.ParentId, input.CatalogueName, input.Reference, input.ReferenceType);
            }
            else
            {
                int maxNumber = await CatalogueRepository.GetMaxSequenceNumberByReferenceAsync(input.ParentId, input.Reference, input.ReferenceType);
                attachCatalogue = new AttachCatalogue(
                        GuidGenerator.Create(),
                        input.AttachReceiveType,
                        input.CatalogueName,
                        ++maxNumber,
                        input.Reference,
                        input.ReferenceType,
                        input.ParentId,
                        isRequired: input.IsRequired,
                        isVerification: input.IsVerification,
                        verificationPassed: input.VerificationPassed,
                        isStatic: input.IsStatic);
                await CatalogueRepository.InsertAsync(attachCatalogue);
                await uow.SaveChangesAsync();
            }
            var result = attachCatalogue != null ? ConvertSrc([attachCatalogue]) : null;
            return result?.First();
        }
        /// <summary>
        /// 创建文件夹(Many)
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="createMode">创建模式</param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode createMode)
        {
            using var uow = UnitOfWorkManager.Begin();
            var deletePara = inputs.Select(d => new GetAttachListInput() { Reference = d.Reference, ReferenceType = d.ReferenceType }).Distinct().ToList();
            List<AttachCatalogueCreateDto> skipAppend = [];
            if (createMode == CatalogueCreateMode.Rebuild)
            {
                await CatalogueRepository.DeleteByReferenceAsync(deletePara);
            }
            else if (createMode == CatalogueCreateMode.Overlap)
            {
                await CatalogueRepository.DeleteRootCatalogueAsync(inputs.Select(d =>
                new GetCatalogueInput()
                {
                    CatalogueName = d.CatalogueName,
                    Reference = d.Reference,
                    ReferenceType = d.ReferenceType,
                    ParentId = d.ParentId,
                }).ToList());
            }
            else if (createMode == CatalogueCreateMode.Append)
            {
                var existingCatalogues = await CatalogueRepository.AnyByNameAsync(inputs.Select(d =>
                new GetCatalogueInput()
                {
                    CatalogueName = d.CatalogueName,
                    Reference = d.Reference,
                    ReferenceType = d.ReferenceType,
                    ParentId = d.ParentId,
                }).ToList());
                if (existingCatalogues.Count > 0)
                {
                    throw new UserFriendlyException($"{existingCatalogues
                        .Select(d => d.CatalogueName)
                        .Aggregate((pre, next) => $"{pre},{next}")} 名称重复，请先删除现有名称再创建！");
                }
            }
            else if (createMode == CatalogueCreateMode.SkipExistAppend)
            {
                var existingCatalogues = await CatalogueRepository.AnyByNameAsync(inputs.Select(d =>
                new GetCatalogueInput()
                {
                    CatalogueName = d.CatalogueName,
                    Reference = d.Reference,
                    ReferenceType = d.ReferenceType,
                    ParentId = d.ParentId,
                }).ToList(), false);
                skipAppend = inputs.Where(e => !existingCatalogues.Any(d =>
                d.CatalogueName == e.CatalogueName &&
                d.Reference == e.Reference &&
                d.ReferenceType == e.ReferenceType &&
                d.ParentId == e.ParentId)).ToList();
            }
            var number = 0;
            foreach (var input in inputs.Select(d => new { d.ParentId, d.Reference, d.ReferenceType }).Distinct())
            {
                var tmp = await CatalogueRepository.GetMaxSequenceNumberByReferenceAsync(input.ParentId, input.Reference, input.ReferenceType);
                number = tmp > number ? tmp : number;
            }
            //跳过已存在的文件夹（只检查根路径）
            if (createMode == CatalogueCreateMode.SkipExistAppend)
            {
                if (skipAppend.Count > 0)
                {
                    List<AttachCatalogue> attachCatalogues = await GetEntitys(skipAppend, number);
                    await CatalogueRepository.InsertManyAsync(attachCatalogues);
                    await uow.SaveChangesAsync();
                }
            }
            else
            {
                if (inputs.Count > 0)
                {
                    List<AttachCatalogue> attachCatalogues = await GetEntitys(inputs, number);
                    await CatalogueRepository.InsertManyAsync(attachCatalogues);
                    await uow.SaveChangesAsync();
                }
            }
            var entitys = await CatalogueRepository.AnyByNameAsync(inputs.Select(d =>
                new GetCatalogueInput()
                {
                    CatalogueName = d.CatalogueName,
                    Reference = d.Reference,
                    ReferenceType = d.ReferenceType,
                    ParentId = d.ParentId,
                }).ToList());
            return ConvertSrc(entitys);
        }
        public virtual async Task DeleteByReferenceAsync(List<AttachCatalogueCreateDto> inputs)
        {
            var deletePara = inputs.Select(d => new GetAttachListInput() { Reference = d.Reference, ReferenceType = d.ReferenceType }).Distinct().ToList();
            await CatalogueRepository.DeleteByReferenceAsync(deletePara);
        }
        private async Task<List<AttachCatalogue>> GetEntitys(List<AttachCatalogueCreateDto> inputs, int maxNumber)
        {
            List<AttachCatalogue> attachCatalogues = [];
            foreach (var input in inputs)
            {
                var attachCatalogue = new AttachCatalogue(
                        GuidGenerator.Create(),
                        input.AttachReceiveType,
                        input.CatalogueName,
                        ++maxNumber,
                        input.Reference,
                        input.ReferenceType,
                        input.ParentId,
                        isRequired: input.IsRequired,
                        isVerification: input.IsVerification,
                        verificationPassed: input.VerificationPassed,
                        isStatic: input.IsStatic);
                if (input.Children?.Count > 0)
                {
                    var children = await GetEntitys([.. input.Children], 0);
                    foreach (var item in children)
                    {
                        attachCatalogue.AddAttachCatalogue(item);
                    }
                }
                if (input.AttachFiles?.Count > 0)
                {
                    var files = await CreateFiles(attachCatalogue, input.AttachFiles);
                    files.ForEach(item => attachCatalogue.AddAttachFile(item, 1));
                }
                attachCatalogues.Add(attachCatalogue);
            }
            return attachCatalogues;
        }
        /// <summary>
        /// 批量下载
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <returns></returns>
        public virtual async Task<List<AttachFileDto>> QueryFilesAsync(Guid catalogueId)
        {
            var catologue = await CatalogueRepository.FindAsync(catalogueId);
            List<AttachFileDto> files = [];
            if (catologue?.AttachFiles?.Count > 0)
            {
                foreach (var file in catologue.AttachFiles)
                {
                    var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                    var tempFile = new AttachFileDto()
                    {
                        FilePath = src,
                        Id = file.Id,
                        FileAlias = file.FileAlias,
                        SequenceNumber = file.SequenceNumber,
                        FileName = file.FileName,
                        FileType = file.FileType,
                        FileSize = file.FileSize,
                        DownloadTimes = file.DownloadTimes,
                        AttachCatalogueId = file.AttachCatalogueId,
                    };
                    files.Add(tempFile);
                    file.Download();
                }
                await CatalogueRepository.UpdateAsync(catologue);
            }
            return files;
        }
        /// <summary>
        /// 单个文件下载
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <returns></returns>
        public virtual async Task<AttachFileDto> QueryFileAsync(Guid attachFileId)
        {
            using var uow = UnitOfWorkManager.Begin();
            var attachFile = await EfCoreAttachFileRepository.FindAsync(attachFileId) ?? throw new BusinessException(message: "没有查询到有效的文件！");
            var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{attachFile.FilePath}";
            var tempFile = new AttachFileDto()
            {
                FilePath = src,
                Id = attachFile.Id,
                FileAlias = attachFile.FileAlias,
                SequenceNumber = attachFile.SequenceNumber,
                FileName = attachFile.FileName,
                FileType = attachFile.FileType,
                FileSize = attachFile.FileSize,
                DownloadTimes = attachFile.DownloadTimes,
                AttachCatalogueId = attachFile.AttachCatalogueId,
            };
            attachFile.Download();
            await EfCoreAttachFileRepository.UpdateAsync(attachFile);
            await uow.SaveChangesAsync();
            return tempFile;
        }
        /// <summary>
        /// 创建文件(区分pdf计算页数完)，文件存储要有结构
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task<List<AttachFileDto>> CreateFilesAsync(Guid id, List<AttachFileCreateDto> inputs)
        {
            using var uow = UnitOfWorkManager.Begin();
            var catalogue = await CatalogueRepository.ByIdMaxSequenceAsync(id);
            var result = new List<AttachFileDto>();
            var entitys = new List<AttachFile>();
            if (catalogue != null && inputs.Count > 0)
            {
                foreach (var input in inputs)
                {
                    var attachId = GuidGenerator.Create();
                    string fileExtension = Path.GetExtension(input.FileAlias).ToLowerInvariant();
                    if (fileExtension == ".tif" || fileExtension == ".tiff")
                    {
                        input.DocumentContent = await ImageHelper.ConvertTiffToImage(input.DocumentContent);
                        fileExtension = ".jpeg";
                        input.FileAlias = $"{Path.GetFileNameWithoutExtension(input.FileAlias)}{fileExtension}";
                    }
                    var tempSequenceNumber = catalogue.SequenceNumber;
                    var fileName = $"{attachId}{fileExtension}";
                    var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{catalogue.Reference}/{fileName}";
                    var tempFile = new AttachFile(
                        attachId,
                        input.FileAlias,
                        ++tempSequenceNumber,
                        fileName,
                        fileUrl,
                        fileExtension,
                        input.DocumentContent.Length,
                        0,
                        catalogue.Id);
                    await BlobContainer.SaveAsync(fileUrl, input.DocumentContent, overrideExisting: true);
                    entitys.Add(tempFile);
                    var src = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{fileUrl}";
                    var retFile = new AttachFileDto()
                    {
                        Id = tempFile.Id,
                        FileAlias = input.FileAlias,
                        FilePath = src,
                        SequenceNumber = tempFile.SequenceNumber,
                        FileName = fileName,
                        FileType = tempFile.FileType,
                        FileSize = tempFile.FileSize,
                        DownloadTimes = tempFile.DownloadTimes,
                        AttachCatalogueId = tempFile.AttachCatalogueId,
                    };
                    result.Add(retFile);
                }
                await EfCoreAttachFileRepository.InsertManyAsync(entitys);
                await uow.CompleteAsync();
                return result;
            }
            else
            {
                throw new BusinessException(message: "没有查询到目录！");
            }
        }
        private async Task<List<AttachFile>> CreateFiles(AttachCatalogue catalogue, List<AttachFileCreateDto> inputs)
        {
            var entitys = new List<AttachFile>();
            foreach (var input in inputs)
            {
                var attachId = GuidGenerator.Create();
                string fileExtension = Path.GetExtension(input.FileAlias).ToLowerInvariant();
                if (fileExtension == ".tif" || fileExtension == ".tiff")
                {
                    input.DocumentContent = await ImageHelper.ConvertTiffToImage(input.DocumentContent);
                    fileExtension = ".jpeg";
                    input.FileAlias = $"{Path.GetFileNameWithoutExtension(input.FileAlias)}{fileExtension}";
                }
                var tempSequenceNumber = catalogue.SequenceNumber;
                var fileName = $"{attachId}{fileExtension}";
                var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{catalogue.Reference}/{fileName}";
                var tempFile = new AttachFile(
                    attachId,
                    input.FileAlias,
                    ++tempSequenceNumber,
                    fileName,
                    fileUrl,
                    fileExtension,
                    input.DocumentContent.Length,
                    0,
                    catalogue.Id);
                await BlobContainer.SaveAsync(fileUrl, input.DocumentContent, overrideExisting: true);
                entitys.Add(tempFile);
            }
            return entitys;
        }
        /// <summary>
        /// 删除单个文件
        /// </summary>
        /// <param name="catalogueId"></param>
        /// <param name="attachFileId"></param>
        /// <returns></returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task DeleteSingleFileAsync(Guid attachFileId)
        {
            using var uow = UnitOfWorkManager.Begin();
            var entity = await EfCoreAttachFileRepository.FindAsync(attachFileId) ?? throw new UserFriendlyException(message: "没有查询到有效的文件！");
            await EfCoreAttachFileRepository.DeleteAsync(entity);
            await uow.CompleteAsync();
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
                var attachId = GuidGenerator.Create();
                var fileName = $"{attachId}{Path.GetExtension(input.FileAlias)}";
                var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{entity.Reference}/{fileName}";
                var tempFile = new AttachFile(
                    attachId,
                    input.FileAlias,
                    target.SequenceNumber,
                    fileName,
                    fileUrl,
                    Path.GetExtension(input.FileAlias),
                    input.DocumentContent.Length,
                    0);
                await BlobContainer.SaveAsync(fileName, input.DocumentContent, overrideExisting: true);
                entity.AddAttachFile(tempFile, entity.PageCount + 1);
                await uow.SaveChangesAsync();
                return new AttachFileDto()
                {
                    Id = tempFile.Id,
                    FileAlias = input.FileAlias,
                    FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{fileUrl}",
                    SequenceNumber = tempFile.SequenceNumber,
                    FileName = fileName,
                    FileType = tempFile.FileType,
                    FileSize = tempFile.FileSize,
                    DownloadTimes = tempFile.DownloadTimes,
                    AttachCatalogueId = tempFile.AttachCatalogueId,
                };
            }
            else
            {
                throw new UserFriendlyException(message: "没有可替换文件！");
            }
        }
        /// <summary>
        /// 通过业务编号获取所有的附件（文件夹及文件）
        /// </summary>
        /// <param name="Reference"></param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> FindByReferenceAsync(List<GetAttachListInput> inputs)
        {
            var entity = await CatalogueRepository.FindByReferenceAsync(inputs);
            return ConvertSrc(entity);
        }
        private List<AttachCatalogueDto> ConvertSrc(List<AttachCatalogue> cats)
        {
            var result = new List<AttachCatalogueDto>();
            cats.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            foreach (var cat in cats)
            {
                var catalogueDto = ObjectMapper.Map<AttachCatalogue, AttachCatalogueDto>(cat);
                if (cat.AttachFiles?.Count > 0)
                {
                    catalogueDto.AttachFiles = [];
                    foreach (var file in cat.AttachFiles.OrderBy(d => d.SequenceNumber))
                    {
                        var fileDto = ObjectMapper.Map<AttachFile, AttachFileDto>(file);
                        fileDto.FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                        catalogueDto.AttachFiles.Add(fileDto);
                    }
                }
                if (cat.Children?.Count > 0)
                {
                    catalogueDto.Children = ConvertSrc([.. cat.Children]);
                }
                result.Add(catalogueDto);
            }
            return result;
        }
        /// <summary>
        /// 验证是否已上传所有必填文件
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public virtual async Task<FileVerifyResultDto> VerifyUploadAsync(List<GetAttachListInput> inputs, bool details = false)
        {
            var list = await CatalogueRepository.VerifyUploadAsync(inputs);
            var parentMessage = "";
            return new FileVerifyResultDto()
            {
                DetailedInfo = details ? VerifyCatalogues(list) : [],
                ProfileInfo = details ? [] : SimpleVerifyCatalogues(list, ref parentMessage)
            };
        }
        private static List<DetailedFileVerifyResultDto> VerifyCatalogues(ICollection<AttachCatalogue> cats)
        {
            List<DetailedFileVerifyResultDto> result = [];
            foreach (var cat in cats)
            {
                var entity = new DetailedFileVerifyResultDto()
                {
                    Id = cat.Id,
                    IsRequired = cat.IsRequired,
                    Reference = cat.Reference,
                    Name = cat.CatalogueName,
                    Uploaded = cat.Children?.Count > 0 || (cat.Children?.Count <= 0 && cat.IsRequired && cat.AttachFiles?.Count > 0)
                };
                if (cat.Children?.Count > 0)
                    entity.Children = VerifyCatalogues(cat.Children);
                result.Add(entity);
            }
            return result;
        }
        private List<ProfileFileVerifyResultDto> SimpleVerifyCatalogues(ICollection<AttachCatalogue> cats, ref string parentMessage)
        {
            List<ProfileFileVerifyResultDto> list = [];
            foreach (var cat in cats)
            {
                string message = cat.Children?.Count > 0 || (cat.Children?.Count <= 0 && cat.IsRequired && cat.AttachFiles?.Count <= 0) ? cat.CatalogueName : "";
                parentMessage = parentMessage != "" && message != "" ? $"{parentMessage}-{message}" : message;
                if (cat.Children?.Count > 0)
                {
                    SimpleVerifyCatalogues(cat.Children, ref parentMessage);
                }
                if (!string.IsNullOrEmpty(parentMessage))
                {
                    ProfileFileVerifyResultDto tmp;
                    if (!list.Any(d => d.Reference == cat.Reference))
                    {
                        tmp = new ProfileFileVerifyResultDto() { Reference = cat.Reference, Message = [parentMessage] };
                        list.Add(tmp);
                    }
                    else
                    {
                        tmp = list.First();
                        tmp.Message.Add(parentMessage);
                    }
                }
            }
            return list;
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
            if (!string.Equals(entity.Reference, input.Reference, StringComparison.InvariantCultureIgnoreCase))
            {
                entity.SetReference(input.Reference, input.ReferenceType);
            }
            if (entity.ParentId != input.ParentId)
            {
                entity.RemoveTo(input.ParentId);
            }
            if (entity.IsVerification != input.IsVerification)
            {
                entity.SetIsVerification(input.IsVerification);
            }
            if (entity.IsRequired != input.IsRequired)
            {
                entity.SetIsRequired(input.IsRequired);
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
            var keys = new List<Guid>();
            var fileKeys = new List<Guid>();
            GetChildrenKeys(entity, ref keys, ref fileKeys);
            await EfCoreAttachFileRepository.DeleteManyAsync(fileKeys);
            await CatalogueRepository.DeleteManyAsync(keys);
            await uow.SaveChangesAsync();
        }
        private static void GetChildrenKeys(AttachCatalogue attachCatalogue, ref List<Guid> keys, ref List<Guid> fileKeys)
        {
            keys.Add(attachCatalogue.Id);
            foreach (var child in attachCatalogue.Children)
            {
                GetChildrenKeys(child, ref keys, ref fileKeys);
            }
            fileKeys.AddRange(attachCatalogue.AttachFiles.Select(d => d.Id));
        }
        public async Task<AttachCatalogueDto?> GetAttachCatalogueByFileIdAsync(Guid fileId)
        {
            return ObjectMapper.Map<AttachCatalogue?, AttachCatalogueDto?>(await CatalogueRepository.GetByFileIdAsync(fileId));
        }
    }
}