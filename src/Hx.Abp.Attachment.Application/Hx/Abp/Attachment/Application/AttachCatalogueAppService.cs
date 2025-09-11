using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Application.Utils;
using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Hx.Abp.Attachment.Application
{
    [Dependency(ServiceLifetime.Singleton, ReplaceServices = true)]
    public class AttachCatalogueAppService(
        IEfCoreAttachCatalogueRepository catalogueRepository,
        IConfiguration configuration,
        IBlobContainerFactory blobContainerFactory,
        IEfCoreAttachFileRepository efCoreAttachFileRepository,
        IAbpDistributedLock distributedLock,
        OcrService ocrService,
        IAttachCatalogueTemplateRepository templateRepository
        ) : AttachmentService, IAttachCatalogueAppService
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");
        private readonly IConfiguration Configuration = configuration;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        private readonly IAbpDistributedLock DistributedLock = distributedLock;
        private readonly OcrService OcrService = ocrService;
        private readonly IAttachCatalogueTemplateRepository TemplateRepository = templateRepository;
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
                // 计算路径
                string? path = input.Path;
                if (string.IsNullOrEmpty(path) && input.ParentId.HasValue)
                {
                    // 有父级：获取父级路径，然后查找同级最大路径
                    var parentCatalogue = await CatalogueRepository.GetAsync(input.ParentId.Value);
                    if (parentCatalogue != null)
                    {
                        var maxPathAtSameLevel = await CatalogueRepository.GetMaxPathAtSameLevelAsync(parentPath: parentCatalogue.Path);

                        if (string.IsNullOrEmpty(maxPathAtSameLevel))
                        {
                            // 没有同级，创建第一个子路径
                            path = AttachCatalogue.AppendPathCode(parentCatalogue.Path, "0000001");
                        }
                        else
                        {
                            // 有同级，获取最大路径的最后一个单元代码并+1
                            var lastUnitCode = AttachCatalogue.GetLastUnitPathCode(maxPathAtSameLevel);
                            var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                            var nextUnitCode = nextNumber.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}");
                            path = AttachCatalogue.AppendPathCode(parentCatalogue.Path, nextUnitCode);
                        }
                    }
                }
                else if (string.IsNullOrEmpty(path))
                {
                    // 没有父级：查找根级别最大路径
                    var maxPathAtRootLevel = await CatalogueRepository.GetMaxPathAtSameLevelAsync();

                    if (string.IsNullOrEmpty(maxPathAtRootLevel))
                    {
                        // 没有根级别分类，创建第一个
                        path = AttachCatalogue.CreatePathCode(1);
                    }
                    else
                    {
                        // 有根级别分类，获取最大路径并+1
                        var nextNumber = Convert.ToInt32(maxPathAtRootLevel) + 1;
                        path = AttachCatalogue.CreatePathCode(nextNumber);
                    }
                }

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
                    input.TemplateVersion,
                    input.CatalogueFacetType,
                    input.CataloguePurpose,
                    input.Tags,
                    input.TextVector,
                    input.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    path
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
            catalogue.SetTemplate(input.TemplateId, input.TemplateVersion);
            catalogue.SetTags(input.Tags);

            // 更新新增字段
            catalogue.SetCatalogueIdentifiers(input.CatalogueFacetType, input.CataloguePurpose);
            catalogue.SetTextVector(input.TextVector);
            
            // 更新路径
            if (!string.IsNullOrEmpty(input.Path))
            {
                catalogue.SetPath(input.Path);
            }
            
            // 更新元数据字段
            if (input.MetaFields != null)
            {
                catalogue.SetMetaFields([.. input.MetaFields.Select(mf => new MetaField(
                    mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                    mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                    mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                ))]);
            }

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
        /// 批量设置元数据字段（创建、更新、删除）
        /// 基于行业最佳实践，使用批量操作提高性能和确保数据一致性
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="metaFields">元数据字段列表</param>
        /// <returns></returns>
        public virtual async Task SetMetaFieldsAsync(Guid id, List<CreateUpdateMetaFieldDto> metaFields)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
            
            // 验证输入参数
            if (metaFields == null)
            {
                throw new UserFriendlyException("元数据字段列表不能为空");
            }

            // 验证字段键名唯一性
            var fieldKeys = metaFields.Select(m => m.FieldKey).ToList();
            if (fieldKeys.Count != fieldKeys.Distinct().Count())
            {
                throw new UserFriendlyException("元数据字段键名必须唯一");
            }

            // 转换为领域对象
            var domainMetaFields = metaFields.Select(dto => new MetaField(
                dto.EntityType,
                dto.FieldKey,
                dto.FieldName,
                dto.DataType,
                dto.IsRequired,
                dto.Unit,
                dto.RegexPattern,
                dto.Options,
                dto.Description,
                dto.DefaultValue,
                dto.Order,
                dto.IsEnabled,
                dto.Group,
                dto.ValidationRules,
                dto.Tags
            )).ToList();

            // 使用领域对象的批量设置方法
            catalogue.SetMetaFields(domainMetaFields);
            
            // 保存到数据库
            await CatalogueRepository.UpdateAsync(catalogue);
        }

        /// <summary>
        /// 获取元数据字段
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="fieldKey">字段键名</param>
        /// <returns></returns>
        public virtual async Task<MetaFieldDto?> GetMetaFieldAsync(Guid id, string fieldKey)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
            var metaField = catalogue.GetMetaField(fieldKey);
            
            if (metaField == null)
                return null;
                
            return new MetaFieldDto
            {
                EntityType = metaField.EntityType,
                FieldKey = metaField.FieldKey,
                FieldName = metaField.FieldName,
                DataType = metaField.DataType,
                Unit = metaField.Unit,
                IsRequired = metaField.IsRequired,
                RegexPattern = metaField.RegexPattern,
                Options = metaField.Options,
                Description = metaField.Description,
                DefaultValue = metaField.DefaultValue,
                Order = metaField.Order,
                IsEnabled = metaField.IsEnabled,
                Group = metaField.Group,
                ValidationRules = metaField.ValidationRules,
                Tags = metaField.Tags
            };
        }

        /// <summary>
        /// 获取所有启用的元数据字段
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <returns></returns>
        public virtual async Task<List<MetaFieldDto>> GetEnabledMetaFieldsAsync(Guid id)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
            var metaFields = catalogue.GetEnabledMetaFields();
            
            return [.. metaFields.Select(mf => new MetaFieldDto
            {
                EntityType = mf.EntityType,
                FieldKey = mf.FieldKey,
                FieldName = mf.FieldName,
                DataType = mf.DataType,
                Unit = mf.Unit,
                IsRequired = mf.IsRequired,
                RegexPattern = mf.RegexPattern,
                Options = mf.Options,
                Description = mf.Description,
                DefaultValue = mf.DefaultValue,
                Order = mf.Order,
                IsEnabled = mf.IsEnabled,
                Group = mf.Group,
                ValidationRules = mf.ValidationRules,
                Tags = mf.Tags
            })];
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
                // 计算路径
                string? path = input.Path;
                if (string.IsNullOrEmpty(path) && input.ParentId.HasValue)
                {
                    // 有父级：获取父级路径，然后查找同级最大路径
                    var parentCatalogue = await CatalogueRepository.GetAsync(input.ParentId.Value);
                    if (parentCatalogue != null)
                    {
                        var maxPathAtSameLevel = await CatalogueRepository.GetMaxPathAtSameLevelAsync(parentPath: parentCatalogue.Path);

                        if (string.IsNullOrEmpty(maxPathAtSameLevel))
                        {
                            // 没有同级，创建第一个子路径
                            path = AttachCatalogue.AppendPathCode(parentCatalogue.Path, "0000001");
                        }
                        else
                        {
                            // 有同级，获取最大路径的最后一个单元代码并+1
                            var lastUnitCode = AttachCatalogue.GetLastUnitPathCode(maxPathAtSameLevel);
                            var nextNumber = Convert.ToInt32(lastUnitCode) + 1;
                            var nextUnitCode = nextNumber.ToString($"D{AttachmentConstants.PATH_CODE_DIGITS}");
                            path = AttachCatalogue.AppendPathCode(parentCatalogue.Path, nextUnitCode);
                        }
                    }
                }
                else if (string.IsNullOrEmpty(path))
                {
                    // 没有父级：查找根级别最大路径
                    var maxPathAtRootLevel = await CatalogueRepository.GetMaxPathAtSameLevelAsync();

                    if (string.IsNullOrEmpty(maxPathAtRootLevel))
                    {
                        // 没有根级别分类，创建第一个
                        path = AttachCatalogue.CreatePathCode(1);
                    }
                    else
                    {
                        // 有根级别分类，获取最大路径并+1
                        var nextNumber = Convert.ToInt32(maxPathAtRootLevel) + 1;
                        path = AttachCatalogue.CreatePathCode(nextNumber);
                    }
                }

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
                    input.TemplateVersion,
                    input.CatalogueFacetType,
                    input.CataloguePurpose,
                    input.Tags,
                    input.TextVector,
                    input.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    path);
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
        /// <param name="id">分类ID</param>
        /// <param name="inputs">文件创建输入</param>
        /// <param name="prefix">文件前缀路径</param>
        /// <returns>创建的文件信息列表</returns>
        /// <exception cref="BusinessException"></exception>
        public virtual async Task<List<AttachFileDto>> CreateFilesAsync(Guid? id, List<AttachFileCreateDto> inputs, string? prefix = null)
        {
            using var uow = UnitOfWorkManager.Begin();
            AttachCatalogue? catalogue = null;
            AttachCatalogueTemplate? template = null;
            WorkflowConfig? workflowConfig = null;

            if (id.HasValue)
            {
                catalogue = await CatalogueRepository.GetAsync(id.Value);
                
                // 获取分类关联的模板信息
                if (catalogue != null && catalogue.TemplateId.HasValue)
                {
                    template = await TemplateRepository.GetByVersionAsync(catalogue.TemplateId.Value, catalogue.TemplateVersion ?? 1);
                    
                    // 解析工作流配置
                    if (template != null && !string.IsNullOrEmpty(template.WorkflowConfig))
                    {
                        workflowConfig = WorkflowConfig.ParseFromJson(template.WorkflowConfig, WorkflowConfig.GetOptions());
                    }
                }
            }

            var result = new List<AttachFileDto>();
            var entitys = new List<AttachFile>();
            
            if (inputs.Count > 0)
            {
                foreach (var input in inputs)
                {
                    var attachId = GuidGenerator.Create();
                    string fileExtension = Path.GetExtension(input.FileAlias).ToLowerInvariant();
                    
                    // 处理TIFF文件转换
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
                    
                    // 保存文件到存储
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
                
                // 批量插入文件实体
                await EfCoreAttachFileRepository.InsertManyAsync(entitys);
                
                // 文件入库后，检查是否需要OCR识别
                if (workflowConfig != null && workflowConfig.IsOcrEnabled())
                {
                    var ocrConfig = workflowConfig.GetOcrConfigOrDefault();
                    
                    foreach (var file in entitys)
                    {
                        // 检查文件是否支持OCR
                        if (ocrConfig.IsFileSupportedForOcr(file.FileType, file.FileSize))
                        {
                            try
                            {
                                // 执行OCR识别
                                var ocrResult = await OcrService.ProcessFileAsync(file.Id);
                                
                                // 更新文件的OCR状态和内容
                                if (ocrResult.IsSuccess && !string.IsNullOrEmpty(ocrResult.ExtractedText))
                                {
                                    file.SetOcrContent(ocrResult.ExtractedText);
                                    file.SetOcrProcessStatus(OcrProcessStatus.Completed);
                                    
                                    // 更新文件实体到数据库
                                    await EfCoreAttachFileRepository.UpdateAsync(file);
                                }
                                else
                                {
                                    file.SetOcrProcessStatus(OcrProcessStatus.Failed);
                                    await EfCoreAttachFileRepository.UpdateAsync(file);
                                    Logger.LogWarning("文件 {FileName} OCR识别失败: {ErrorMessage}", 
                                        file.FileName, ocrResult.ErrorMessage);
                                }
                            }
                            catch (Exception ex)
                            {
                                file.SetOcrProcessStatus(OcrProcessStatus.Failed);
                                await EfCoreAttachFileRepository.UpdateAsync(file);
                                Logger.LogError(ex, "文件 {FileName} OCR识别异常", file.FileName);
                            }
                        }
                        else
                        {
                            // 不需要OCR识别，设置为跳过状态
                            file.SetOcrProcessStatus(OcrProcessStatus.Skipped);
                            await EfCoreAttachFileRepository.UpdateAsync(file);
                        }
                    }
                }
                else
                {
                    // 没有OCR配置，将所有文件设置为跳过状态
                    foreach (var file in entitys)
                    {
                        file.SetOcrProcessStatus(OcrProcessStatus.Skipped);
                        await EfCoreAttachFileRepository.UpdateAsync(file);
                    }
                }
                
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
                TemplateVersion = catalogue.TemplateVersion,
                FullTextContent = catalogue.FullTextContent,
                FullTextContentUpdatedTime = catalogue.FullTextContentUpdatedTime,
                CatalogueFacetType = catalogue.CatalogueFacetType,
                CataloguePurpose = catalogue.CataloguePurpose,
                TextVector = catalogue.TextVector,
                VectorDimension = catalogue.VectorDimension,
                Path = catalogue.Path,
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

            // 映射元数据字段集合
            if (catalogue.MetaFields != null && catalogue.MetaFields.Count != 0)
            {
                dto.MetaFields = [.. catalogue.MetaFields.Select(mf => new MetaFieldDto
                {
                    EntityType = mf.EntityType,
                    FieldKey = mf.FieldKey,
                    FieldName = mf.FieldName,
                    DataType = mf.DataType,
                    Unit = mf.Unit,
                    IsRequired = mf.IsRequired,
                    RegexPattern = mf.RegexPattern,
                    Options = mf.Options,
                    Description = mf.Description,
                    DefaultValue = mf.DefaultValue,
                    Order = mf.Order,
                    IsEnabled = mf.IsEnabled,
                    Group = mf.Group,
                    ValidationRules = mf.ValidationRules,
                    Tags = mf.Tags
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

        /// <summary>
        /// 根据模板ID和版本查找分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="templateVersion">模板版本号，null表示查找所有版本</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> FindByTemplateAsync(Guid templateId, int? templateVersion = null)
        {
            var catalogues = await CatalogueRepository.FindByTemplateAsync(templateId, templateVersion);
            return ObjectMapper.Map<List<AttachCatalogue>, List<AttachCatalogueDto>>(catalogues);
        }

        /// <summary>
        /// 根据模板ID查找所有版本的分类
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> FindByTemplateIdAsync(Guid templateId)
        {
            var catalogues = await CatalogueRepository.FindByTemplateIdAsync(templateId);
            return ObjectMapper.Map<List<AttachCatalogue>, List<AttachCatalogueDto>>(catalogues);
        }

        /// <summary>
        /// 获取分类树形结构（用于树状展示）
        /// 基于行业最佳实践，支持多种查询条件和性能优化
        /// 参考 AttachCatalogueTemplateRepository 的最佳实践，使用路径优化
        /// </summary>
        public virtual async Task<List<AttachCatalogueTreeDto>> GetCataloguesTreeAsync(
            string? reference = null,
            int? referenceType = null,
            FacetType? catalogueFacetType = null,
            TemplatePurpose? cataloguePurpose = null,
            bool includeChildren = true,
            bool includeFiles = false,
            string? fulltextQuery = null,
            Guid? templateId = null,
            int? templateVersion = null)
        {
            try
            {
                // 调用仓储方法获取分类树形结构
                var catalogues = await CatalogueRepository.GetCataloguesTreeAsync(
                    reference, referenceType, catalogueFacetType, cataloguePurpose,
                    includeChildren, includeFiles, fulltextQuery, templateId, templateVersion);

                // 转换为树形DTO
                var treeDtos = new List<AttachCatalogueTreeDto>();
                foreach (var catalogue in catalogues)
                {
                    var treeDto = await ConvertToTreeDtoAsync(catalogue, includeFiles);
                    treeDtos.Add(treeDto);
                }

                return treeDtos;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException($"获取分类树形结构失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将分类实体转换为树形DTO
        /// 基于行业最佳实践，递归转换并保持树形结构
        /// </summary>
        private async Task<AttachCatalogueTreeDto> ConvertToTreeDtoAsync(AttachCatalogue catalogue, bool includeFiles)
        {
            var treeDto = ObjectMapper.Map<AttachCatalogue, AttachCatalogueTreeDto>(catalogue);

            // 转换子节点
            if (catalogue.Children != null && catalogue.Children.Count > 0)
            {
                treeDto.Children = [];
                foreach (var child in catalogue.Children)
                {
                    var childDto = await ConvertToTreeDtoAsync(child, includeFiles);
                    treeDto.Children.Add(childDto);
                }
            }

            // 转换附件文件（如果需要）
            if (includeFiles && catalogue.AttachFiles != null && catalogue.AttachFiles.Count > 0)
            {
                treeDto.AttachFiles = [];
                foreach (var file in catalogue.AttachFiles)
                {
                    var fileDto = ObjectMapper.Map<AttachFile, AttachFileDto>(file);
                    fileDto.FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}";
                    treeDto.AttachFiles.Add(fileDto);
                }
            }

            return treeDto;
        }
    }
}
