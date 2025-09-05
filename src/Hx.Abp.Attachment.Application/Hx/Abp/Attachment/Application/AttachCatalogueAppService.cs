using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Uow;

namespace Hx.Abp.Attachment.Application
{
    [Dependency(ServiceLifetime.Singleton, ReplaceServices = true)]
    public class AttachCatalogueAppService(
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IConfiguration configuration,
        IBlobContainerFactory blobContainerFactory,
        IEfCoreAttachFileRepository efCoreAttachFileRepository,
        IAbpDistributedLock distributedLock) : AttachmentService, IAttachCatalogueAppService
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");
        private readonly IConfiguration Configuration = configuration;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        private readonly IAbpDistributedLock DistributedLock = distributedLock;
        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public virtual async Task<AttachCatalogueDto?> CreateAsync(AttachCatalogueCreateDto input, CatalogueCreateMode? createMode)
        {
            // 生成分布式锁的key
            var lockKey = $"AttachCatalogue:{input.Reference}:{input.ReferenceType}:{input.CatalogueName}";

            await using var handle = await DistributedLock.TryAcquireAsync(lockKey, TimeSpan.FromSeconds(30)) ?? throw new UserFriendlyException("系统繁忙，请稍后重试！");

            try
            {
                // 检查是否已存在相同的分类
                var existingCatalogue = await CatalogueRepository.FindByReferenceAndNameAsync(input.Reference, input.ReferenceType, input.CatalogueName);
                if (existingCatalogue != null)
                {
                    throw new UserFriendlyException($"已存在相同的分类名称: {input.CatalogueName}");
                }
                int maxNumber = await CatalogueRepository.GetMaxSequenceNumberByReferenceAsync(input.ParentId, input.Reference, input.ReferenceType);
                // 创建新的分类实体
                var catalogue = new AttachCatalogue(
                    GuidGenerator.Create(),
                    input.AttachReceiveType,
                    input.CatalogueName,
                    ++maxNumber,
                    input.Reference,
                    input.ReferenceType,
                    input.ParentId,
                    input.IsRequired,
                    input.IsVerification,
                    input.VerificationPassed,
                    input.IsStatic,
                    0,
                    0,
                    input.TemplateId,
                    input.CatalogueFacetType,
                    input.CataloguePurpose,
                    input.Tags,
                    input.TextVector
                );

                // 验证配置
                catalogue.ValidateConfiguration();

                // 保存到数据库
                await CatalogueRepository.InsertAsync(catalogue);

                // 返回创建的DTO
                return await MapToDtoAsync(catalogue);
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException($"创建分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新分类信息
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="input">更新输入</param>
        /// <returns></returns>
        public virtual async Task<AttachCatalogueDto?> UpdateAsync(Guid id, AttachCatalogueCreateDto input)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");

            // 更新基础字段
            catalogue.SetCatalogueName(input.CatalogueName);
            catalogue.SetAttachReceiveType(input.AttachReceiveType);
            catalogue.SetReference(input.Reference, input.ReferenceType);
            if (input.SequenceNumber.HasValue)
            {
                catalogue.SetSequenceNumber(input.SequenceNumber.Value);
            }
            catalogue.SetIsVerification(input.IsVerification);
            catalogue.SetIsRequired(input.IsRequired);
            catalogue.SetIsStatic(input.IsStatic);
            catalogue.SetTemplateId(input.TemplateId);
            catalogue.SetTags(input.Tags);

            // 更新新增字段
            catalogue.SetCatalogueIdentifiers(input.CatalogueFacetType, input.CataloguePurpose);
            catalogue.SetTextVector(input.TextVector);

            // 验证配置
            catalogue.ValidateConfiguration();

            // 保存到数据库
            await CatalogueRepository.UpdateAsync(catalogue);

            // 返回更新的DTO
            return await MapToDtoAsync(catalogue);
        }

        /// <summary>
        /// 设置分类权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="permissions">权限列表</param>
        /// <returns></returns>
        public virtual async Task SetPermissionsAsync(Guid id, List<AttachCatalogueTemplatePermissionDto> permissions)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");

            // 清空现有权限
            catalogue.Permissions?.Clear();

            // 添加新权限
            if (permissions != null)
            {
                foreach (var permissionDto in permissions)
                {
                    var permission = new AttachCatalogueTemplatePermission(
                        permissionDto.PermissionType,
                        permissionDto.PermissionTarget,
                        permissionDto.Action,
                        permissionDto.Effect,
                        permissionDto.AttributeConditions,
                        permissionDto.EffectiveTime,
                        permissionDto.ExpirationTime,
                        permissionDto.Description
                    );
                    catalogue.AddPermission(permission);
                }
            }

            // 保存到数据库
            await CatalogueRepository.UpdateAsync(catalogue);
        }

        /// <summary>
        /// 获取分类权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueTemplatePermissionDto>> GetPermissionsAsync(Guid id)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
            var result = new List<AttachCatalogueTemplatePermissionDto>();
            
            if (catalogue.Permissions != null)
            {
                foreach (var permission in catalogue.Permissions)
                {
                    result.Add(new AttachCatalogueTemplatePermissionDto
                    {
                        PermissionType = permission.PermissionType,
                        PermissionTarget = permission.PermissionTarget,
                        Action = permission.Action,
                        Effect = permission.Effect,
                        AttributeConditions = permission.AttributeConditions,
                        IsEnabled = permission.IsEnabled,
                        EffectiveTime = permission.EffectiveTime,
                        ExpirationTime = permission.ExpirationTime,
                        Description = permission.Description
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// 检查用户权限
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="action">权限操作</param>
        /// <returns></returns>
        public virtual async Task<bool> HasPermissionAsync(Guid id, Guid userId, PermissionAction action)
        {
            var catalogue = await CatalogueRepository.GetAsync(id);
            if (catalogue == null)
            {
                return false;
            }

            return catalogue.HasPermission(userId, action);
        }

        /// <summary>
        /// 获取分类标识描述
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        public virtual async Task<string> GetCatalogueIdentifierDescriptionAsync(Guid id)
        {
            var catalogue = await CatalogueRepository.GetAsync(id);
            return catalogue == null ? throw new UserFriendlyException($"分类不存在: {id}") : catalogue.GetCatalogueIdentifierDescription();
        }

        /// <summary>
        /// 根据分类标识查询
        /// </summary>
        /// <param name="catalogueType">分类类型</param>
        /// <param name="cataloguePurpose">分类用途</param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> GetByCatalogueIdentifierAsync(FacetType? catalogueFacetType = null, TemplatePurpose? cataloguePurpose = null)
        {
            var catalogues = await CatalogueRepository.GetListAsync();
            
            var filteredCatalogues = catalogues.Where(c => 
                c.MatchesCatalogueIdentifier(catalogueFacetType, cataloguePurpose)).ToList();

            var result = new List<AttachCatalogueDto>();
            foreach (var catalogue in filteredCatalogues)
            {
                result.Add(await MapToDtoAsync(catalogue));
            }

            return result;
        }

        /// <summary>
        /// 根据向量维度查询
        /// </summary>
        /// <param name="minDimension">最小维度</param>
        /// <param name="maxDimension">最大维度</param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> GetByVectorDimensionAsync(int? minDimension = null, int? maxDimension = null)
        {
            var catalogues = await CatalogueRepository.GetListAsync();
            
            var filteredCatalogues = catalogues.Where(c => 
                (minDimension == null || c.VectorDimension >= minDimension) &&
                (maxDimension == null || c.VectorDimension <= maxDimension)).ToList();

            var result = new List<AttachCatalogueDto>();
            foreach (var catalogue in filteredCatalogues)
            {
                result.Add(await MapToDtoAsync(catalogue));
            }

            return result;
        }

        /// <summary>
        /// 创建文件夹(Many)
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="createMode">创建模式</param>
        /// <returns></returns>
        public virtual async Task<List<AttachCatalogueDto>> CreateManyAsync(List<AttachCatalogueCreateDto> inputs, CatalogueCreateMode createMode)
        {
            // 为所有输入生成一个组合锁key
            var lockKey = $"AttachCatalogues:{string.Join(",", inputs.Select(x => $"{x.Reference}:{x.ReferenceType}:{x.CatalogueName}"))}";

            await using var handle = await DistributedLock.TryAcquireAsync(lockKey, TimeSpan.FromSeconds(60)) ?? throw new UserFriendlyException("系统繁忙，请稍后重试！");
            using var uow = UnitOfWorkManager.Begin();
            try
            {
                var deletePara = inputs.Select(d => new GetAttachListInput() { Reference = d.Reference, ReferenceType = d.ReferenceType }).Distinct().ToList();
                List<AttachCatalogueCreateDto> skipAppend = [];
                if (createMode == CatalogueCreateMode.Rebuild)
                {
                    await CatalogueRepository.DeleteByReferenceAsync(deletePara);
                }
                else if (createMode == CatalogueCreateMode.Overlap)
                {
                    await CatalogueRepository.DeleteRootCatalogueAsync([.. inputs.Select(d =>
                        new GetCatalogueInput()
                        {
                            CatalogueName = d.CatalogueName,
                            Reference = d.Reference,
                            ReferenceType = d.ReferenceType,
                            ParentId = d.ParentId,
                        })]);
                }
                else if (createMode == CatalogueCreateMode.Append)
                {
                    var existingCatalogues = await CatalogueRepository.AnyByNameAsync([.. inputs.Select(d =>
                        new GetCatalogueInput()
                        {
                            CatalogueName = d.CatalogueName,
                            Reference = d.Reference,
                            ReferenceType = d.ReferenceType,
                            ParentId = d.ParentId,
                        })]);
                    if (existingCatalogues.Count > 0)
                    {
                        throw new UserFriendlyException($"{existingCatalogues
                            .Select(d => d.CatalogueName)
                            .Aggregate((pre, next) => $"{pre},{next}")} 名称重复，请先删除现有名称再创建！");
                    }
                }
                else if (createMode == CatalogueCreateMode.SkipExistAppend)
                {
                    var existingCatalogues = await CatalogueRepository.AnyByNameAsync([.. inputs.Select(d =>
                        new GetCatalogueInput()
                        {
                            CatalogueName = d.CatalogueName,
                            Reference = d.Reference,
                            ReferenceType = d.ReferenceType,
                            ParentId = d.ParentId,
                        })], false);
                    skipAppend = [.. inputs.Where(e => !existingCatalogues.Any(d =>
                        d.CatalogueName == e.CatalogueName &&
                        d.Reference == e.Reference &&
                        d.ReferenceType == e.ReferenceType &&
                        d.ParentId == e.ParentId))];
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
                var entitys = await CatalogueRepository.AnyByNameAsync([.. inputs.Select(d =>
                        new GetCatalogueInput()
                        {
                            CatalogueName = d.CatalogueName,
                            Reference = d.Reference,
                            ReferenceType = d.ReferenceType,
                            ParentId = d.ParentId,
                        })]);
                await uow.CompleteAsync();
                return ConvertSrc(entitys);
            }
            catch (Exception)
            {
                await uow.RollbackAsync();
                throw;
            }
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
                    input.IsRequired,
                    input.IsVerification,
                    input.VerificationPassed,
                    input.IsStatic,
                    0,
                    0,
                    input.TemplateId,
                    input.CatalogueFacetType,
                    input.CataloguePurpose,
                    input.Tags,
                    input.TextVector);
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
        public virtual async Task<List<AttachFileDto>> CreateFilesAsync(Guid? id, List<AttachFileCreateDto> inputs, string? prefix = null)
        {
            using var uow = UnitOfWorkManager.Begin();
            CreateAttachFileCatalogueInfo? catalogue = null;
            if (id.HasValue)
            {
                catalogue = await CatalogueRepository.ByIdMaxSequenceAsync(id.Value);
            }
            var result = new List<AttachFileDto>();
            var entitys = new List<AttachFile>();
            if (inputs.Count > 0)
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
                    var tempSequenceNumber = catalogue == null ? 0 : catalogue.SequenceNumber;
                    var fileName = $"{attachId}{fileExtension}";
                    var fileUrl = "";
                    if (catalogue != null)
                    {
                        fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{catalogue.Reference}/{fileName}";
                    }
                    else
                    {
                        fileUrl = $"{prefix ?? "uploads"}/{fileName}";
                    }
                    var tempFile = new AttachFile(
                        attachId,
                        input.FileAlias,
                        ++tempSequenceNumber,
                        fileName,
                        fileUrl,
                        fileExtension,
                        input.DocumentContent.Length,
                        0,
                        catalogue?.Id);
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
                throw new BusinessException(message: "没有查询到分类！");
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
            var entity = await CatalogueRepository.FindAsync(catalogueId) ?? throw new UserFriendlyException(message: "替换文件的分类不存在！");
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
        private static List<ProfileFileVerifyResultDto> SimpleVerifyCatalogues(ICollection<AttachCatalogue> cats, ref string parentMessage)
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
        /// 删除分类
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

        /// <summary>
        /// 全文检索分类
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> SearchByFullTextAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new UserFriendlyException("搜索文本不能为空");
            }

            var results = await CatalogueRepository.SearchByFullTextAsync(searchText, reference, referenceType, limit);
            return ObjectMapper.Map<List<AttachCatalogue>, List<AttachCatalogueDto>>(results);
        }

        /// <summary>
        /// 混合检索分类：结合全文检索和文本向量检索
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> SearchByHybridAsync(string searchText, string? reference = null, int? referenceType = null, int limit = 10, string? queryTextVector = null, float similarityThreshold = 0.7f)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new UserFriendlyException("搜索文本不能为空");
            }

            var results = await CatalogueRepository.SearchByHybridAsync(searchText, reference, referenceType, limit, queryTextVector, similarityThreshold);
            return ObjectMapper.Map<List<AttachCatalogue>, List<AttachCatalogueDto>>(results);
        }


        /// <summary>
        /// 映射到DTO
        /// </summary>
        /// <param name="catalogue">分类实体</param>
        /// <returns></returns>
        private static async Task<AttachCatalogueDto> MapToDtoAsync(AttachCatalogue catalogue)
        {
            var dto = new AttachCatalogueDto
            {
                Id = catalogue.Id,
                Reference = catalogue.Reference,
                AttachReceiveType = catalogue.AttachReceiveType,
                ReferenceType = catalogue.ReferenceType,
                CatalogueName = catalogue.CatalogueName,
                SequenceNumber = catalogue.SequenceNumber,
                ParentId = catalogue.ParentId,
                IsRequired = catalogue.IsRequired,
                AttachCount = catalogue.AttachCount,
                PageCount = catalogue.PageCount,
                IsStatic = catalogue.IsStatic,
                IsVerification = catalogue.IsVerification,
                VerificationPassed = catalogue.VerificationPassed,
                TemplateId = catalogue.TemplateId,
                FullTextContent = catalogue.FullTextContent,
                FullTextContentUpdatedTime = catalogue.FullTextContentUpdatedTime,
                CatalogueFacetType = catalogue.CatalogueFacetType,
                CataloguePurpose = catalogue.CataloguePurpose,
                TextVector = catalogue.TextVector,
                VectorDimension = catalogue.VectorDimension,
                CreationTime = catalogue.CreationTime,
                CreatorId = catalogue.CreatorId,
                LastModificationTime = catalogue.LastModificationTime,
                LastModifierId = catalogue.LastModifierId,
                IsDeleted = catalogue.IsDeleted,
                DeleterId = catalogue.DeleterId,
                DeletionTime = catalogue.DeletionTime
            };

            // 映射权限集合
            if (catalogue.Permissions != null && catalogue.Permissions.Count != 0)
            {
                dto.Permissions = [.. catalogue.Permissions.Select(p => new AttachCatalogueTemplatePermissionDto
                {
                    PermissionType = p.PermissionType,
                    PermissionTarget = p.PermissionTarget,
                    Action = p.Action,
                    Effect = p.Effect,
                    AttributeConditions = p.AttributeConditions,
                    IsEnabled = p.IsEnabled,
                    EffectiveTime = p.EffectiveTime,
                    ExpirationTime = p.ExpirationTime,
                    Description = p.Description
                })];
            }

            // 映射子分类
            if (catalogue.Children != null && catalogue.Children.Count != 0)
            {
                dto.Children = [];
                foreach (var child in catalogue.Children)
                {
                    dto.Children.Add(await MapToDtoAsync(child));
                }
            }

            // 映射附件文件
            if (catalogue.AttachFiles != null && catalogue.AttachFiles.Count != 0)
            {
                dto.AttachFiles = [];
                foreach (var file in catalogue.AttachFiles)
                {
                    // 这里需要实现AttachFile到AttachFileDto的映射
                    // 暂时跳过，避免循环依赖
                }
            }

            return dto;
        }
    }
}
