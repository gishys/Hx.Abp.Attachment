using Hx.Abp.Attachment.Application.ArchAI;
using Hx.Abp.Attachment.Application.ArchAI.Contracts;
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
        IAttachCatalogueTemplateRepository templateRepository,
        AIServiceFactory aiServiceFactory,
        IAttachCatalogueManager attachCatalogueManager,
        AttachCataloguePermissionChecker permissionChecker
        ) : AttachmentService, IAttachCatalogueAppService
    {
        private readonly IEfCoreAttachCatalogueRepository CatalogueRepository = catalogueRepository;
        private readonly IBlobContainer BlobContainer = blobContainerFactory.Create("attachment");
        private readonly IConfiguration Configuration = configuration;
        private readonly IEfCoreAttachFileRepository EfCoreAttachFileRepository = efCoreAttachFileRepository;
        private readonly IAbpDistributedLock DistributedLock = distributedLock;
        private readonly OcrService OcrService = ocrService;
        private readonly IAttachCatalogueTemplateRepository TemplateRepository = templateRepository;
        private readonly AIServiceFactory AIServiceFactory = aiServiceFactory;
        private readonly IAttachCatalogueManager AttachCatalogueManager = attachCatalogueManager;
        private readonly AttachCataloguePermissionChecker PermissionChecker = permissionChecker;
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
                    input.TemplateRole,
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
            catalogue.SetTemplateRole(input.TemplateRole);
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
        public virtual async Task SetPermissionsAsync(Guid id, List<CreateAttachCatalogueTemplatePermissionDto> permissions)
        {
            var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");

            var permissionEntities = new List<AttachCatalogueTemplatePermission>();

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

                    permissionEntities.Add(permission);
                }
            }

            catalogue.SetPermissions(permissionEntities);

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
        /// 集成ABP vNext权限系统与业务权限的最佳实践实现
        /// 权限检查优先级：
        /// 1. ABP系统权限（全局权限）
        /// 2. 分类特定权限（业务权限：用户权限、角色权限）
        /// 3. 继承权限（从父分类继承）
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="userId">用户ID</param>
        /// <param name="action">权限操作</param>
        /// <returns>是否具有权限</returns>
        public virtual async Task<bool> HasPermissionAsync(Guid id, Guid userId, PermissionAction action)
        {
            var catalogue = await CatalogueRepository.GetAsync(id);
            if (catalogue == null)
            {
                Logger.LogWarning("分类不存在，权限检查失败，分类ID: {CatalogueId}, 用户ID: {UserId}, 操作: {Action}",
                    id, userId, action);
                return false;
            }

            // 使用权限检查器进行权限检查（集成ABP权限系统）
            return await PermissionChecker.CheckPermissionAsync(catalogue, action, userId);
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
        public virtual async Task DeleteByReferenceAsync(List<GetAttachListInput> inputs, bool softDeleted = false)
        {
            var deletePara = inputs.Select(d => new GetAttachListInput() { Reference = d.Reference, ReferenceType = d.ReferenceType }).Distinct().ToList();
            if (softDeleted)
            {
                var entitys = await CatalogueRepository.FindByReferenceAsync(deletePara);
                foreach (var entity in entitys)
                {
                    await AttachCatalogueManager.SoftDeleteCatalogueWithChildrenAsync(entity);
                }
            }
            else
            {
                var entitys = await CatalogueRepository.FindByReferenceAsync(deletePara);
                foreach (var entity in entitys)
                {
                    await AttachCatalogueManager.HardDeleteCatalogueWithChildrenAsync(entity);
                }
            }
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
                    input.TemplateRole,
                    input.Tags,
                    input.TextVector,
                    input.MetaFields?.Select(mf => new MetaField(
                        mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                        mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                        mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                    )).ToList(),
                    path,
                    input.IsArchived,
                    input.Summary);
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
                        Reference = file.Reference ?? catologue.Reference,
                        TemplatePurpose = file.TemplatePurpose ?? catologue.CataloguePurpose,
                        IsCategorized = file.IsCategorized,
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

                    // 设置从AttachCatalogue获取的属性
                    if (catalogue != null)
                    {
                        tempFile.SetFromAttachCatalogue(catalogue);
                    }

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
                        Reference = tempFile.Reference,
                        TemplatePurpose = tempFile.TemplatePurpose,
                        IsCategorized = tempFile.IsCategorized,
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
                // 设置从AttachCatalogue获取的属性
                tempFile.SetFromAttachCatalogue(catalogue);
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
                // 设置从AttachCatalogue获取的属性
                tempFile.SetFromAttachCatalogue(entity);
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
                        // 如果文件没有Reference和TemplatePurpose，从分类中获取
                        fileDto.Reference = file.Reference ?? cat.Reference;
                        fileDto.TemplatePurpose = file.TemplatePurpose ?? cat.CataloguePurpose;
                        fileDto.IsCategorized = file.IsCategorized;
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
        /// <param name="searchText">搜索文本（可选）</param>
        /// <param name="reference">业务引用</param>
        /// <param name="referenceType">业务类型</param>
        /// <param name="limit">返回数量限制</param>
        /// <param name="queryTextVector">查询文本向量</param>
        /// <param name="similarityThreshold">相似度阈值</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> SearchByHybridAsync(string? searchText = null, string? reference = null, int? referenceType = null, int limit = 10, string? queryTextVector = null, float similarityThreshold = 0.7f)
        {
            var results = await CatalogueRepository.SearchByHybridAsync(searchText, reference, referenceType, limit, queryTextVector, similarityThreshold);
            return ConvertSrc(results);
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
                TemplateRole = catalogue.TemplateRole,
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

        /// <summary>
        /// 智能分类文件上传和推荐
        /// 基于OCR内容进行智能分类推荐，适用于文件自动归类场景
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="inputs">文件列表</param>
        /// <param name="prefix">文件前缀</param>
        /// <returns>智能分类推荐结果列表</returns>
        public virtual async Task<List<SmartClassificationResultDto>> CreateFilesWithSmartClassificationAsync(
            Guid catalogueId,
            List<AttachFileCreateDto> inputs,
            string? prefix = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var results = new List<SmartClassificationResultDto>();

            try
            {
                // 1. 查找分类实体及其所有子实体
                var catalogue = await CatalogueRepository.GetAsync(catalogueId) ?? throw new UserFriendlyException($"分类不存在: {catalogueId}");

                // 获取所有子分类（叶子节点）
                var leafCategories = await CatalogueRepository.GetCatalogueWithAllChildrenAsync(catalogueId);
                // 构建分类选项列表
                var categoryOptions = leafCategories.Where(c => c.TemplateRole == TemplateRole.Leaf).Select(c => c.CatalogueName).ToList();
                if (categoryOptions.Count == 0)
                {
                    throw new UserFriendlyException("没有找到可用的叶子分类节点");
                }

                // 2. 处理序号分配
                await ProcessSequenceNumbersAsync(catalogueId, inputs);

                // 3. 处理每个文件
                foreach (var input in inputs)
                {
                    var result = new SmartClassificationResultDto();
                    var fileStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    try
                    {
                        // 创建文件实体
                        var attachId = GuidGenerator.Create();
                        var fileName = $"{attachId}{Path.GetExtension(input.FileAlias)}";
                        var fileUrl = $"{AppGlobalProperties.AttachmentBasicPath}/{catalogue.Reference}/{fileName}";
                        var fileExtension = Path.GetExtension(input.FileAlias).ToLowerInvariant();

                        var tempFile = new AttachFile(
                            attachId,
                            input.FileAlias,
                            input.SequenceNumber ?? 1, // 使用处理后的序号
                            fileName,
                            fileUrl,
                            fileExtension,
                            input.DocumentContent.Length,
                            0,
                            catalogueId);

                        // 设置从AttachCatalogue获取的属性
                        tempFile.SetFromAttachCatalogue(catalogue);

                        // 保存文件到Blob存储
                        await BlobContainer.SaveAsync(fileUrl, input.DocumentContent, overrideExisting: true);
                        // 保存文件到数据库
                        await EfCoreAttachFileRepository.InsertAsync(tempFile);

                        // 3. OCR处理 - 使用UnitOfWork确保数据已提交
                        string? ocrContent = null;
                        if (tempFile.IsSupportedForOcr())
                        {
                            try
                            {
                                // 使用UnitOfWork确保文件数据已提交到数据库，OCR服务可以读取
                                if (CurrentUnitOfWork != null)
                                    await CurrentUnitOfWork.SaveChangesAsync();

                                var ocrResult = await OcrService.ProcessFileAsync(tempFile.Id);
                                ocrContent = ocrResult?.ExtractedText;
                                tempFile.SetOcrContent(ocrContent);
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(ex, "OCR处理失败，文件ID: {FileId}", tempFile.Id);
                                result.Status = SmartClassificationStatus.OcrFailed;
                                result.ErrorMessage = $"OCR处理失败: {ex.Message}";
                            }
                        }
                        else
                        {
                            result.Status = SmartClassificationStatus.FileNotSupportedForOcr;
                            result.ErrorMessage = "文件类型不支持OCR处理";
                        }

                        // 4. 智能分类推荐
                        ClassificationResult? classificationResult = null;

                        // 检查catalogue本身是否为叶子节点
                        if (catalogue.TemplateRole == TemplateRole.Leaf)
                        {
                            // 如果catalogue本身就是叶子节点，直接推荐该分类
                            result.Classification = new ClassificationExtentResult
                            {
                                RecommendedCategory = catalogue.CatalogueName,
                                RecommendedCategoryId = catalogue.Id,
                                Confidence = 1.0f // 叶子节点直接分类，置信度为1.0
                            };

                            // 设置分类ID
                            tempFile.SetAttachCatalogueId(catalogue.Id);

                            // 设置从AttachCatalogue获取的属性
                            tempFile.SetFromAttachCatalogue(catalogue);

                            // 标记为已归类
                            tempFile.SetIsCategorized(true);

                            result.Status = SmartClassificationStatus.Success;
                        }
                        else
                        {
                            // 如果catalogue不是叶子节点，进行智能分类推荐
                            if (!string.IsNullOrEmpty(ocrContent) && result.Status != SmartClassificationStatus.OcrFailed)
                            {
                                try
                                {
                                    var classificationService = AIServiceFactory.GetIntelligentClassificationService();
                                    classificationResult = await classificationService.RecommendDocumentCategoryAsync(ocrContent, categoryOptions);
                                    result.Status = SmartClassificationStatus.Success;
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogWarning(ex, "智能分类推荐失败，文件ID: {FileId}", tempFile.Id);
                                    result.Status = SmartClassificationStatus.ClassificationFailed;
                                    result.ErrorMessage = $"智能分类推荐失败: {ex.Message}";
                                }
                            }
                            if (classificationResult != null && !string.IsNullOrEmpty(classificationResult.RecommendedCategory))
                            {
                                result.Classification = new ClassificationExtentResult
                                {
                                    RecommendedCategory = classificationResult.RecommendedCategory,
                                    RecommendedCategoryId = leafCategories.First(leaf => leaf.CatalogueName == classificationResult.RecommendedCategory).Id,
                                    Confidence = classificationResult?.Confidence ?? 0.5
                                };
                            }
                            else
                            {
                                // 如果没有分类结果，默认选择第一个叶子分类
                                var defaultCategory = leafCategories.Where(l => l.TemplateRole == TemplateRole.Leaf).First();
                                result.Classification = new ClassificationExtentResult
                                {
                                    RecommendedCategory = defaultCategory.CatalogueName,
                                    RecommendedCategoryId = defaultCategory.Id,
                                    Confidence = 0.5f // 默认置信度
                                };
                            }
                            if (result.Status != SmartClassificationStatus.ClassificationFailed)
                                tempFile.SetAttachCatalogueId(result.Classification.RecommendedCategoryId);
                        }
                        
                        // 更新内容到数据库
                        await EfCoreAttachFileRepository.UpdateAsync(tempFile);
                        // 5. 构建返回结果
                        result.FileInfo = new AttachFileDto
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
                            Reference = tempFile.Reference,
                            TemplatePurpose = tempFile.TemplatePurpose,
                            IsCategorized = tempFile.IsCategorized
                        };

                        // 可选分类列表
                        result.AvailableCategories = [.. leafCategories.Where(l=>l.TemplateRole==TemplateRole.Leaf).Select(c => new CategoryOptionDto
                        {
                            Id = c.Id,
                            Name = c.CatalogueName,
                            Path = c.Path,
                            ParentId = c.ParentId
                        })];

                        result.OcrContent = ocrContent;
                        result.ProcessingTimeMs = fileStopwatch.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "处理文件失败: {FileName}", input.FileAlias);
                        result.Status = SmartClassificationStatus.SystemError;
                        result.ErrorMessage = $"处理文件失败: {ex.Message}";
                        result.ProcessingTimeMs = fileStopwatch.ElapsedMilliseconds;
                    }
                    finally
                    {
                        fileStopwatch.Stop();
                    }

                    results.Add(result);
                }

                stopwatch.Stop();
                Logger.LogInformation("智能分类文件上传完成，处理文件数: {FileCount}, 总耗时: {TotalTime}ms",
                    inputs.Count, stopwatch.ElapsedMilliseconds);

                return results;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "智能分类文件上传失败");
                throw new UserFriendlyException($"智能分类文件上传失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理序号分配
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="inputs">文件输入列表</param>
        private async Task ProcessSequenceNumbersAsync(Guid catalogueId, List<AttachFileCreateDto> inputs)
        {
            // 获取该分类下现有的最大序号
            var maxSequenceNumber = await EfCoreAttachFileRepository.GetMaxSequenceNumberByCatalogueIdAsync(catalogueId);

            // 获取现有文件列表用于冲突检查
            var existingFiles = await EfCoreAttachFileRepository.GetListByCatalogueIdAsync(catalogueId);
            var existingSequenceNumbers = existingFiles.Select(f => f.SequenceNumber).ToHashSet();

            // 处理每个文件的序号
            var usedSequenceNumbers = new HashSet<int>();

            foreach (var input in inputs)
            {
                if (input.SequenceNumber.HasValue)
                {
                    // 如果传入了序号，检查是否重复
                    var sequenceNumber = input.SequenceNumber.Value;

                    // 如果序号重复，则依次+1直到不重复
                    while (usedSequenceNumbers.Contains(sequenceNumber) ||
                           existingSequenceNumbers.Contains(sequenceNumber))
                    {
                        sequenceNumber++;
                    }

                    input.SequenceNumber = sequenceNumber;
                }
                else
                {
                    // 如果没有传入序号，则分配下一个可用序号
                    maxSequenceNumber++;
                    input.SequenceNumber = maxSequenceNumber;
                }

                usedSequenceNumbers.Add(input.SequenceNumber.Value);
            }
        }

        /// <summary>
        /// 确定文件分类
        /// 将文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="fileId">文件ID</param>
        /// <param name="catalogueId">分类ID</param>
        /// <param name="ocrContent">OCR全文内容</param>
        /// <returns>更新后的文件信息</returns>
        public virtual async Task<AttachFileDto> ConfirmFileClassificationAsync(Guid fileId, Guid catalogueId, string? ocrContent = null)
        {
            try
            {
                // 1. 获取文件实体
                var file = await EfCoreAttachFileRepository.GetAsync(fileId) ?? throw new UserFriendlyException($"文件不存在: {fileId}");

                // 2. 获取分类实体
                var catalogue = await CatalogueRepository.GetAsync(catalogueId) ?? throw new UserFriendlyException($"分类不存在: {catalogueId}");

                // 3. 更新文件属性
                // 设置分类ID
                file.SetAttachCatalogueId(catalogueId);

                // 设置从AttachCatalogue获取的属性
                file.SetFromAttachCatalogue(catalogue);

                // 标记为已归类
                file.SetIsCategorized(true);

                // 4. 处理OCR内容
                if (!string.IsNullOrEmpty(ocrContent))
                {
                    // 设置OCR内容并标记为完成
                    file.SetOcrContent(ocrContent);
                }
                else if (file.IsSupportedForOcr() && string.IsNullOrEmpty(file.OcrContent))
                {
                    // 如果文件支持OCR但还没有OCR内容，尝试进行OCR处理
                    try
                    {
                        var ocrResult = await OcrService.ProcessFileAsync(fileId);
                        if (ocrResult?.IsSuccess == true && !string.IsNullOrEmpty(ocrResult.ExtractedText))
                        {
                            file.SetOcrContent(ocrResult.ExtractedText);
                        }
                        else
                        {
                            // OCR处理失败，设置处理状态为失败
                            file.SetOcrProcessStatus(OcrProcessStatus.Failed);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "OCR处理失败，文件ID: {FileId}", fileId);
                        file.SetOcrProcessStatus(OcrProcessStatus.Failed);
                    }
                }

                // 5. 保存文件更新
                await EfCoreAttachFileRepository.UpdateAsync(file);

                // 6. 构建返回的DTO
                var fileDto = new AttachFileDto
                {
                    Id = file.Id,
                    FileAlias = file.FileAlias,
                    FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}",
                    SequenceNumber = file.SequenceNumber,
                    FileName = file.FileName,
                    FileType = file.FileType,
                    FileSize = file.FileSize,
                    DownloadTimes = file.DownloadTimes,
                    AttachCatalogueId = file.AttachCatalogueId,
                    Reference = file.Reference,
                    TemplatePurpose = file.TemplatePurpose,
                    IsCategorized = file.IsCategorized
                };

                Logger.LogInformation("文件分类确认成功，文件ID: {FileId}, 分类ID: {CatalogueId}", fileId, catalogueId);
                return fileDto;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "确定文件分类失败，文件ID: {FileId}, 分类ID: {CatalogueId}", fileId, catalogueId);
                throw new UserFriendlyException($"确定文件分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量确定文件分类
        /// 将多个文件归类到指定分类，并更新相关属性
        /// </summary>
        /// <param name="requests">文件分类请求列表</param>
        /// <returns>更新后的文件信息列表</returns>
        public virtual async Task<List<AttachFileDto>> ConfirmFileClassificationsAsync(List<ConfirmFileClassificationRequest> requests)
        {
            try
            {
                // 参数验证
                if (requests == null || requests.Count == 0)
                {
                    throw new UserFriendlyException("文件分类请求列表不能为空");
                }

                var results = new List<AttachFileDto>();

                // 批量处理文件分类
                foreach (var request in requests)
                {
                    try
                    {
                        // 调用单个文件分类方法
                        var result = await ConfirmFileClassificationAsync(request.FileId, request.CatalogueId, request.OcrContent);
                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "批量确定文件分类失败，文件ID: {FileId}, 分类ID: {CatalogueId}",
                            request.FileId, request.CatalogueId);

                        // 继续处理其他文件，不中断整个批量操作
                        // 可以考虑添加错误信息到结果中
                        Logger.LogWarning("跳过失败的文件分类，文件ID: {FileId}", request.FileId);
                    }
                }

                Logger.LogInformation("批量确定文件分类完成，请求数量: {RequestCount}, 成功数量: {SuccessCount}",
                    requests.Count, results.Count);

                return results;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量确定文件分类失败");
                throw new UserFriendlyException($"批量确定文件分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表
        /// 查询未归档的文件列表
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>文件列表</returns>
        public virtual async Task<List<AttachFileDto>> GetFilesByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(reference))
                {
                    throw new UserFriendlyException("业务引用不能为空");
                }

                // 查询未归档的文件
                var files = await EfCoreAttachFileRepository.GetListByReferenceAndTemplatePurposeAsync(reference, templatePurpose);

                // 转换为DTO
                var fileDtos = new List<AttachFileDto>();
                foreach (var file in files)
                {
                    var fileDto = new AttachFileDto
                    {
                        Id = file.Id,
                        FileAlias = file.FileAlias,
                        FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}",
                        SequenceNumber = file.SequenceNumber,
                        FileName = file.FileName,
                        FileType = file.FileType,
                        FileSize = file.FileSize,
                        DownloadTimes = file.DownloadTimes,
                        AttachCatalogueId = file.AttachCatalogueId,
                        Reference = file.Reference,
                        TemplatePurpose = file.TemplatePurpose,
                        IsCategorized = file.IsCategorized
                    };
                    fileDtos.Add(fileDto);
                }

                Logger.LogInformation("根据业务引用和模板用途获取文件列表成功，Reference: {Reference}, TemplatePurpose: {TemplatePurpose}, 文件数量: {Count}",
                    reference, templatePurpose, fileDtos.Count);

                return fileDtos;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据业务引用和模板用途获取文件列表失败，Reference: {Reference}, TemplatePurpose: {TemplatePurpose}",
                    reference, templatePurpose);
                throw new UserFriendlyException($"获取文件列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据业务引用和模板用途获取文件列表并进行智能分类推荐
        /// 查询未归档的文件列表，并为每个文件提供分类推荐
        /// </summary>
        /// <param name="reference">业务引用</param>
        /// <param name="templatePurpose">模板用途</param>
        /// <returns>智能分类推荐结果列表</returns>
        public virtual async Task<List<SmartClassificationResultDto>> GetFilesWithSmartClassificationByReferenceAndTemplatePurposeAsync(string reference, TemplatePurpose templatePurpose)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(reference))
                {
                    throw new UserFriendlyException("业务引用不能为空");
                }

                // 查找根分类
                var rootCatalogue = await CatalogueRepository.FindRootCataloguesAsync(reference, templatePurpose)
                    ?? throw new UserFriendlyException($"未找到匹配的根分类，Reference: {reference}, TemplatePurpose: {templatePurpose}");

                // 获取所有子分类（叶子节点）
                var leafCategories = await CatalogueRepository.GetCatalogueWithAllChildrenAsync(rootCatalogue.Id);
                // 构建分类选项列表
                var categoryOptions = leafCategories.Where(c => c.TemplateRole == TemplateRole.Leaf).Select(c => c.CatalogueName).ToList();
                if (categoryOptions.Count == 0)
                {
                    throw new UserFriendlyException("没有找到可用的叶子分类节点");
                }

                // 查询未归档的文件
                var files = await EfCoreAttachFileRepository.GetListByReferenceAndTemplatePurposeAsync(reference, templatePurpose);

                // 转换为智能分类结果
                var results = new List<SmartClassificationResultDto>();
                foreach (var file in files)
                {
                    var result = new SmartClassificationResultDto();
                    var fileDto = new AttachFileDto
                    {
                        Id = file.Id,
                        FileAlias = file.FileAlias,
                        FilePath = $"{Configuration[AppGlobalProperties.FileServerBasePath]}/host/attachment/{file.FilePath}",
                        SequenceNumber = file.SequenceNumber,
                        FileName = file.FileName,
                        FileType = file.FileType,
                        FileSize = file.FileSize,
                        DownloadTimes = file.DownloadTimes,
                        AttachCatalogueId = file.AttachCatalogueId,
                        Reference = file.Reference,
                        TemplatePurpose = file.TemplatePurpose,
                        IsCategorized = file.IsCategorized
                    };

                    result.FileInfo = fileDto;
                    result.Status = SmartClassificationStatus.Success;

                    // 如果文件已经有分类，使用现有分类；否则使用默认分类
                    var defaultCategory = file.AttachCatalogueId.HasValue
                        ? leafCategories.Where(l => l.TemplateRole == TemplateRole.Leaf).FirstOrDefault(d => d.Id == file.AttachCatalogueId) ?? leafCategories.Where(l => l.TemplateRole == TemplateRole.Leaf).First()
                        : leafCategories.Where(l => l.TemplateRole == TemplateRole.Leaf).First();

                    result.Classification = new ClassificationExtentResult
                    {
                        RecommendedCategory = defaultCategory.CatalogueName,
                        RecommendedCategoryId = defaultCategory.Id,
                        Confidence = file.AttachCatalogueId.HasValue ? 0.8f : 0.5f // 已有分类的置信度更高
                    };

                    // 可选分类列表
                    result.AvailableCategories = [.. leafCategories.Where(l => l.TemplateRole == TemplateRole.Leaf).Select(c => new CategoryOptionDto
                    {
                        Id = c.Id,
                        Name = c.CatalogueName,
                        Path = c.Path,
                        ParentId = c.ParentId
                    })];

                    result.OcrContent = file.OcrContent;
                    result.ProcessingTimeMs = 0;
                    results.Add(result);
                }

                Logger.LogInformation("根据业务引用和模板用途获取文件列表并进行智能分类推荐成功，Reference: {Reference}, TemplatePurpose: {TemplatePurpose}, 文件数量: {Count}",
                    reference, templatePurpose, results.Count);

                return results;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据业务引用和模板用途获取文件列表并进行智能分类推荐失败，Reference: {Reference}, TemplatePurpose: {TemplatePurpose}",
                    reference, templatePurpose);
                throw new UserFriendlyException($"获取文件列表并进行智能分类推荐失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据归档状态查询分类
        /// </summary>
        /// <param name="isArchived">归档状态</param>
        /// <param name="reference">业务引用过滤</param>
        /// <param name="referenceType">业务类型过滤</param>
        /// <returns>匹配的分类列表</returns>
        public virtual async Task<List<AttachCatalogueDto>> GetByArchivedStatusAsync(bool isArchived, string? reference = null, int? referenceType = null)
        {
            try
            {
                var catalogues = await CatalogueRepository.GetByArchivedStatusAsync(isArchived, reference, referenceType);

                var catalogueDtos = new List<AttachCatalogueDto>();
                foreach (var catalogue in catalogues)
                {
                    var catalogueDto = new AttachCatalogueDto
                    {
                        Id = catalogue.Id,
                        Reference = catalogue.Reference,
                        AttachReceiveType = catalogue.AttachReceiveType,
                        ReferenceType = catalogue.ReferenceType,
                        CatalogueName = catalogue.CatalogueName,
                        Tags = catalogue.Tags ?? [],
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
                        TemplateRole = catalogue.TemplateRole,
                        TextVector = catalogue.TextVector,
                        VectorDimension = catalogue.VectorDimension,
                        Path = catalogue.Path,
                        IsArchived = catalogue.IsArchived,
                        Summary = catalogue.Summary,
                        CreationTime = catalogue.CreationTime,
                        CreatorId = catalogue.CreatorId,
                        LastModificationTime = catalogue.LastModificationTime,
                        LastModifierId = catalogue.LastModifierId,
                        IsDeleted = catalogue.IsDeleted,
                        DeleterId = catalogue.DeleterId,
                        DeletionTime = catalogue.DeletionTime
                    };
                    catalogueDtos.Add(catalogueDto);
                }

                Logger.LogInformation("根据归档状态查询分类成功，IsArchived: {IsArchived}, Reference: {Reference}, ReferenceType: {ReferenceType}, 数量: {Count}",
                    isArchived, reference, referenceType, catalogueDtos.Count);

                return catalogueDtos;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "根据归档状态查询分类失败，IsArchived: {IsArchived}, Reference: {Reference}, ReferenceType: {ReferenceType}",
                    isArchived, reference, referenceType);
                throw new UserFriendlyException($"查询分类失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量设置归档状态
        /// </summary>
        /// <param name="catalogueIds">分类ID列表</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新的记录数</returns>
        public virtual async Task<int> SetArchivedStatusAsync(List<Guid> catalogueIds, bool isArchived)
        {
            try
            {
                if (catalogueIds == null || catalogueIds.Count == 0)
                {
                    throw new UserFriendlyException("分类ID列表不能为空");
                }

                var updatedCount = await CatalogueRepository.SetArchivedStatusAsync(catalogueIds, isArchived);

                Logger.LogInformation("批量设置归档状态成功，CatalogueIds: {CatalogueIds}, IsArchived: {IsArchived}, 更新数量: {Count}",
                    string.Join(",", catalogueIds), isArchived, updatedCount);

                return updatedCount;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "批量设置归档状态失败，CatalogueIds: {CatalogueIds}, IsArchived: {IsArchived}",
                    string.Join(",", catalogueIds), isArchived);
                throw new UserFriendlyException($"批量设置归档状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置分类归档状态
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="isArchived">归档状态</param>
        /// <returns>更新后的分类信息</returns>
        public virtual async Task<AttachCatalogueDto?> SetCatalogueArchivedStatusAsync(Guid id, bool isArchived)
        {
            try
            {
                var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
                catalogue.SetIsArchived(isArchived);
                await CatalogueRepository.UpdateAsync(catalogue);

                var catalogueDto = new AttachCatalogueDto
                {
                    Id = catalogue.Id,
                    Reference = catalogue.Reference,
                    AttachReceiveType = catalogue.AttachReceiveType,
                    ReferenceType = catalogue.ReferenceType,
                    CatalogueName = catalogue.CatalogueName,
                    Tags = catalogue.Tags ?? [],
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
                    TemplateRole = catalogue.TemplateRole,
                    TextVector = catalogue.TextVector,
                    VectorDimension = catalogue.VectorDimension,
                    Path = catalogue.Path,
                    IsArchived = catalogue.IsArchived,
                    Summary = catalogue.Summary,
                    CreationTime = catalogue.CreationTime,
                    CreatorId = catalogue.CreatorId,
                    LastModificationTime = catalogue.LastModificationTime,
                    LastModifierId = catalogue.LastModifierId,
                    IsDeleted = catalogue.IsDeleted,
                    DeleterId = catalogue.DeleterId,
                    DeletionTime = catalogue.DeletionTime
                };

                Logger.LogInformation("设置分类归档状态成功，Id: {Id}, IsArchived: {IsArchived}", id, isArchived);
                return catalogueDto;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "设置分类归档状态失败，Id: {Id}, IsArchived: {IsArchived}", id, isArchived);
                throw new UserFriendlyException($"设置分类归档状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置分类概要信息
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="summary">概要信息</param>
        /// <returns>更新后的分类信息</returns>
        public virtual async Task<AttachCatalogueDto?> SetCatalogueSummaryAsync(Guid id, string? summary)
        {
            try
            {
                var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
                catalogue.SetSummary(summary);
                await CatalogueRepository.UpdateAsync(catalogue);

                var catalogueDto = new AttachCatalogueDto
                {
                    Id = catalogue.Id,
                    Reference = catalogue.Reference,
                    AttachReceiveType = catalogue.AttachReceiveType,
                    ReferenceType = catalogue.ReferenceType,
                    CatalogueName = catalogue.CatalogueName,
                    Tags = catalogue.Tags ?? [],
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
                    TemplateRole = catalogue.TemplateRole,
                    TextVector = catalogue.TextVector,
                    VectorDimension = catalogue.VectorDimension,
                    Path = catalogue.Path,
                    IsArchived = catalogue.IsArchived,
                    Summary = catalogue.Summary,
                    CreationTime = catalogue.CreationTime,
                    CreatorId = catalogue.CreatorId,
                    LastModificationTime = catalogue.LastModificationTime,
                    LastModifierId = catalogue.LastModifierId,
                    IsDeleted = catalogue.IsDeleted,
                    DeleterId = catalogue.DeleterId,
                    DeletionTime = catalogue.DeletionTime
                };

                Logger.LogInformation("设置分类概要信息成功，Id: {Id}, Summary: {Summary}", id, summary);
                return catalogueDto;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "设置分类概要信息失败，Id: {Id}, Summary: {Summary}", id, summary);
                throw new UserFriendlyException($"设置分类概要信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置分类标签
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="tags">标签列表</param>
        /// <returns>更新后的分类信息</returns>
        public virtual async Task<AttachCatalogueDto?> SetCatalogueTagsAsync(Guid id, List<string>? tags)
        {
            try
            {
                var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
                catalogue.SetTags(tags);
                await CatalogueRepository.UpdateAsync(catalogue);

                var catalogueDto = new AttachCatalogueDto
                {
                    Id = catalogue.Id,
                    Reference = catalogue.Reference,
                    AttachReceiveType = catalogue.AttachReceiveType,
                    ReferenceType = catalogue.ReferenceType,
                    CatalogueName = catalogue.CatalogueName,
                    Tags = catalogue.Tags ?? [],
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
                    TemplateRole = catalogue.TemplateRole,
                    TextVector = catalogue.TextVector,
                    VectorDimension = catalogue.VectorDimension,
                    Path = catalogue.Path,
                    IsArchived = catalogue.IsArchived,
                    Summary = catalogue.Summary,
                    CreationTime = catalogue.CreationTime,
                    CreatorId = catalogue.CreatorId,
                    LastModificationTime = catalogue.LastModificationTime,
                    LastModifierId = catalogue.LastModifierId,
                    IsDeleted = catalogue.IsDeleted,
                    DeleterId = catalogue.DeleterId,
                    DeletionTime = catalogue.DeletionTime
                };

                Logger.LogInformation("设置分类标签成功，Id: {Id}, Tags: {Tags}", id, string.Join(", ", tags ?? []));
                return catalogueDto;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "设置分类标签失败，Id: {Id}, Tags: {Tags}", id, string.Join(", ", tags ?? []));
                throw new UserFriendlyException($"设置分类标签失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取指定分类下的所有叶子节点选项
        /// 用于智能分类和文件归类场景，返回可分类的叶子节点列表
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <returns>叶子节点选项列表</returns>
        public virtual async Task<List<LeafCategoryOptionDto>> GetLeafCategoriesAsync(Guid catalogueId)
        {
            try
            {
                // 1. 验证分类是否存在
                var catalogue = await CatalogueRepository.GetAsync(catalogueId) ?? throw new UserFriendlyException($"分类不存在: {catalogueId}");

                // 2. 获取所有子分类（叶子节点）
                var leafCategories = await CatalogueRepository.GetCatalogueWithAllChildrenAsync(catalogueId);
                
                // 3. 过滤出叶子节点并构建选项列表
                var leafOptions = leafCategories
                    .Where(c => c.TemplateRole == TemplateRole.Leaf)
                    .OrderBy(c => c.SequenceNumber)
                    .ThenBy(c => c.CreationTime)
                    .Select(c => new LeafCategoryOptionDto
                    {
                        Id = c.Id,
                        CatalogueName = c.CatalogueName,
                        Path = c.Path,
                        SequenceNumber = c.SequenceNumber,
                        ParentId = c.ParentId,
                        Reference = c.Reference,
                        TemplatePurpose = c.CataloguePurpose
                    })
                    .ToList();

                Logger.LogInformation("获取叶子节点选项成功，分类ID: {CatalogueId}, 叶子节点数量: {Count}", catalogueId, leafOptions.Count);
                return leafOptions;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取叶子节点选项失败，分类ID: {CatalogueId}", catalogueId);
                throw new UserFriendlyException($"获取叶子节点选项失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 智能分析分类信息
        /// 基于分类下的文件内容，自动生成概要信息、分类标签、全文内容和元数据
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="forceUpdate">是否强制更新（默认false，只更新空值）</param>
        /// <returns>智能分析结果</returns>
        public virtual async Task<IntelligentAnalysisResultDto> AnalyzeCatalogueIntelligentlyAsync(Guid id, bool forceUpdate = false)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new IntelligentAnalysisResultDto
            {
                CatalogueId = id,
                Status = AnalysisStatus.Success,
                Statistics = new AnalysisStatistics()
            };

            try
            {
                // 1. 获取分类信息
                var catalogue = await CatalogueRepository.GetAsync(id) ?? throw new UserFriendlyException($"分类不存在: {id}");
                result.CatalogueName = catalogue.CatalogueName;

                // 2. 获取分类及其所有子分类的文件
                var files = await GetCatalogueFilesWithChildrenAsync(id);
                result.Statistics.TotalFilesProcessed = files.Count;

                if (files.Count == 0)
                {
                    result.Status = AnalysisStatus.Skipped;
                    result.ErrorMessage = "分类下没有文件，跳过分析";
                    return result;
                }

                // 3. 分析全文内容（OCR处理）
                var fullTextResult = await AnalyzeFullTextContentAsync(files);
                result.FullTextAnalysis = fullTextResult;
                result.Statistics.SuccessfulFilesProcessed = fullTextResult.SuccessfulFilesCount;
                result.Statistics.TotalExtractedTextLength = fullTextResult.ExtractedTextLength;

                // 4. 分析概要信息、关键字和标签（合并处理）
                var summaryResult = await AnalyzeSummaryAndKeywordsAsync(catalogue, fullTextResult, forceUpdate);
                result.SummaryAnalysis = summaryResult;

                // 5. 标签分析结果（从概要分析中获取）
                var tagsResult = new TagsAnalysisResult
                {
                    OriginalTags = catalogue.Tags ?? [],
                    GeneratedTags = summaryResult.Keywords ?? [],
                    IsUpdated = summaryResult.IsUpdated && (summaryResult.Keywords?.Count > 0),
                    TagConfidences = summaryResult.Keywords?.ToDictionary(k => k, _ => summaryResult.Confidence) ?? []
                };
                result.TagsAnalysis = tagsResult;
                result.Statistics.GeneratedTagsCount = tagsResult.GeneratedTags.Count;

                // 6. 分析元数据
                var metaDataResult = await AnalyzeMetaDataAsync(catalogue, fullTextResult, forceUpdate);
                result.MetaDataAnalysis = metaDataResult;
                result.Statistics.RecognizedEntitiesCount = metaDataResult.RecognizedEntities.Count;

                // 7. 更新分类信息
                await UpdateCatalogueWithAnalysisResultsAsync(catalogue, summaryResult, tagsResult, metaDataResult);

                // 8. 统计更新字段
                result.UpdatedFields = GetUpdatedFields(summaryResult, tagsResult, fullTextResult, metaDataResult);
                result.Statistics.UpdatedFieldsCount = result.UpdatedFields.Count;

                // 9. 设置最终状态
                result.Status = result.UpdatedFields.Count > 0 ? AnalysisStatus.Success : AnalysisStatus.Skipped;

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.Statistics.TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                Logger.LogInformation("智能分析分类完成，分类ID: {CatalogueId}, 处理文件数: {FileCount}, 更新字段数: {UpdatedFieldsCount}, 耗时: {ProcessingTime}ms",
                    id, files.Count, result.UpdatedFields.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Status = AnalysisStatus.Failed;
                result.ErrorMessage = ex.Message;
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

                Logger.LogError(ex, "智能分析分类失败，分类ID: {CatalogueId}", id);
                return result;
            }
        }

        /// <summary>
        /// 分析全文内容（OCR处理）
        /// </summary>
        private async Task<FullTextAnalysisResult> AnalyzeFullTextContentAsync(List<AttachFile> files)
        {
            var result = new FullTextAnalysisResult
            {
                ProcessedFilesCount = files.Count
            };

            var processingDetails = new List<FileProcessingDetail>();

            foreach (var file in files)
            {
                var detail = new FileProcessingDetail
                {
                    FileId = file.Id,
                    FileName = file.FileAlias,
                    Status = FileProcessingStatus.Skipped
                };

                var fileStopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    // 首先检查是否已有OCR内容
                    if (file.OcrProcessStatus == OcrProcessStatus.Completed || (!string.IsNullOrEmpty(file.OcrContent)))
                    {
                        // 已有OCR内容，直接使用
                        detail.Status = FileProcessingStatus.Success;
                        detail.ExtractedText = file.OcrContent;
                        detail.ExtractedTextLength = file.OcrContent?.Length ?? 0;
                        result.SuccessfulFilesCount++;
                        result.ExtractedTextLength += file.OcrContent?.Length ?? 0;
                        detail.ErrorMessage = "使用已存在的OCR内容";
                    }
                    else if (!file.IsSupportedForOcr())
                    {
                        // 文件不支持OCR处理
                        detail.Status = FileProcessingStatus.Skipped;
                        detail.ErrorMessage = "文件不支持OCR处理";
                    }
                    else
                    {
                        // 执行OCR处理
                        var ocrResult = await OcrService.ProcessFileAsync(file.Id);
                        if (ocrResult?.IsSuccess == true && !string.IsNullOrEmpty(ocrResult.ExtractedText))
                        {
                            file.SetOcrContent(ocrResult.ExtractedText);
                            await EfCoreAttachFileRepository.UpdateAsync(file);

                            detail.Status = FileProcessingStatus.Success;
                            detail.ExtractedText = ocrResult.ExtractedText;
                            detail.ExtractedTextLength = ocrResult.ExtractedText.Length;
                            result.SuccessfulFilesCount++;
                            result.ExtractedTextLength += ocrResult.ExtractedText.Length;
                        }
                        else
                        {
                            detail.Status = FileProcessingStatus.Failed;
                            detail.ErrorMessage = "OCR处理失败";
                            result.FailedFilesCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    detail.Status = FileProcessingStatus.Failed;
                    detail.ErrorMessage = ex.Message;
                    result.FailedFilesCount++;
                    Logger.LogWarning(ex, "OCR处理文件失败，文件ID: {FileId}", file.Id);
                }
                finally
                {
                    fileStopwatch.Stop();
                    detail.ProcessingTimeMs = fileStopwatch.ElapsedMilliseconds;
                }

                processingDetails.Add(detail);
            }

            result.ProcessingDetails = processingDetails;
            result.IsUpdated = result.SuccessfulFilesCount > 0;

            return result;
        }

        /// <summary>
        /// 分析概要信息和关键字
        /// </summary>
        private async Task<SummaryAnalysisResult> AnalyzeSummaryAndKeywordsAsync(AttachCatalogue catalogue, FullTextAnalysisResult fullTextResult, bool forceUpdate)
        {
            var result = new SummaryAnalysisResult
            {
                OriginalSummary = catalogue.Summary
            };

            // 检查是否需要更新概要信息
            if (!forceUpdate && !string.IsNullOrEmpty(catalogue.Summary))
            {
                return result;
            }
            try
            {
                // 收集所有OCR内容
                var allTextContent = string.Join("\n", fullTextResult.ProcessingDetails
                    .Where(d => d.Status == FileProcessingStatus.Success && !string.IsNullOrWhiteSpace(d.ExtractedText))
                    .Select(d => d.ExtractedText!)
                    .Where(content => !string.IsNullOrWhiteSpace(content)));

                if (string.IsNullOrWhiteSpace(allTextContent))
                {
                    return result;
                }

                // 使用文档智能分析服务
                var documentAnalysisService = AIServiceFactory.GetDocumentAnalysisService();
                var analysisInput = new TextAnalysisInputDto
                {
                    Text = allTextContent,
                    MaxSummaryLength = 500,
                    KeywordCount = 10,
                    GenerateSemanticVector = true, // 启用语义向量生成
                    ExtractEntities = false
                };

                var analysisResult = await documentAnalysisService.AnalyzeDocumentAsync(analysisInput);

                if (analysisResult != null)
                {
                    result.GeneratedSummary = analysisResult.Summary;
                    result.Keywords = analysisResult.Keywords ?? [];
                    result.Confidence = (float)analysisResult.Confidence;
                    result.IsUpdated = true;
                    result.SemanticVector = analysisResult.SemanticVector; // 添加语义向量
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "概要信息分析失败，分类ID: {CatalogueId}", catalogue.Id);
            }

            return result;
        }


        /// <summary>
        /// 分析元数据
        /// </summary>
        private async Task<MetaDataAnalysisResult> AnalyzeMetaDataAsync(AttachCatalogue catalogue, FullTextAnalysisResult fullTextResult, bool forceUpdate)
        {
            var result = new MetaDataAnalysisResult
            {
                OriginalMetaFieldsCount = catalogue.MetaFields?.Count ?? 0
            };

            // 检查是否需要更新元数据
            if (!forceUpdate && (catalogue.MetaFields?.Count > 0))
            {
                return result;
            }

            try
            {
                // 收集所有OCR内容
                var allTextContent = string.Join("\n", fullTextResult.ProcessingDetails
                    .Where(d => d.Status == FileProcessingStatus.Success && !string.IsNullOrWhiteSpace(d.ExtractedText))
                    .Select(d => d.ExtractedText!)
                    .Where(content => !string.IsNullOrWhiteSpace(content)));

                if (string.IsNullOrWhiteSpace(allTextContent))
                {
                    return result;
                }

                // 使用实体识别服务
                var entityRecognitionService = AIServiceFactory.GetEntityRecognitionService();

                // 从分类的元数据字段中获取实体类型
                var entityTypes = catalogue.MetaFields?
                    .Where(mf => mf.IsEnabled && !string.IsNullOrEmpty(mf.FieldName))
                    .Select(mf => mf.FieldName)
                    .Distinct()
                    .ToList() ?? ["人名", "地名", "机构", "时间", "数字"]; // 默认实体类型

                var recognitionInput = new EntityRecognitionInputDto
                {
                    Text = allTextContent,
                    EntityTypes = entityTypes,
                    IncludePosition = false
                };

                var recognitionResult = await entityRecognitionService.RecognizeEntitiesAsync(recognitionInput);

                if (recognitionResult != null && recognitionResult.Entities?.Count > 0)
                {
                    result.RecognizedEntities = [.. recognitionResult.Entities.Select(e => new Contracts.RecognizedEntity
                    {
                        Name = e.Name,
                        Type = e.Type,
                        Confidence = (float)e.Confidence
                    })];

                    // 根据识别的实体生成元数据字段
                    result.GeneratedMetaFields = GenerateMetaFieldsFromEntities(result.RecognizedEntities, catalogue.MetaFields?.ToList());
                    result.GeneratedMetaFieldsCount = result.GeneratedMetaFields.Count;
                    result.IsUpdated = result.GeneratedMetaFieldsCount > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "元数据分析失败，分类ID: {CatalogueId}", catalogue.Id);
            }

            return result;
        }

        /// <summary>
        /// 根据识别的实体生成元数据字段
        /// </summary>
        private static List<MetaFieldDto> GenerateMetaFieldsFromEntities(List<Contracts.RecognizedEntity> entities, List<MetaField>? existingMetaFields = null)
        {
            var metaFields = new List<MetaFieldDto>();

            // 按实体类型分组
            var entityGroups = entities.GroupBy(e => e.Type).ToList();

            foreach (var group in entityGroups)
            {
                var entityType = group.Key;
                var entitiesOfType = group.ToList();

                // 查找是否已存在相同类型的元数据字段
                var existingField = existingMetaFields?.FirstOrDefault(mf =>
                    mf.FieldName.Equals(entityType, StringComparison.OrdinalIgnoreCase));

                // 根据数据类型进行数据校验和转换
                var (validatedValue, dataType) = ValidateAndConvertEntityValues(entitiesOfType, existingField?.DataType ?? "string");

                var metaField = new MetaFieldDto
                {
                    EntityType = "AttachCatalogue",
                    FieldKey = existingField?.FieldKey ?? entityType.ToLowerInvariant().Replace(" ", "_"),
                    FieldName = entityType,
                    DataType = dataType,
                    IsRequired = existingField?.IsRequired ?? false,
                    Unit = existingField?.Unit,
                    RegexPattern = existingField?.RegexPattern,
                    Options = existingField?.Options,
                    Description = existingField?.Description ?? $"自动识别的{entityType}实体",
                    DefaultValue = string.Join(", ", entitiesOfType.Select(e => e.Name)),
                    FieldValue = validatedValue, // 设置字段值
                    Order = existingField?.Order ?? (metaFields.Count + 1),
                    IsEnabled = true,
                    Group = existingField?.Group ?? "智能识别",
                    ValidationRules = existingField?.ValidationRules,
                    Tags = existingField?.Tags?.Concat([entityType, "智能分析"]).Distinct().ToList() ?? [entityType, "智能分析"]
                };

                metaFields.Add(metaField);
            }

            return metaFields;
        }

        /// <summary>
        /// 根据数据类型进行数据校验和转换
        /// </summary>
        /// <param name="entities">实体列表</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>校验后的值和数据类型</returns>
        private static (string validatedValue, string dataType) ValidateAndConvertEntityValues(List<Contracts.RecognizedEntity> entities, string dataType)
        {
            if (entities.Count == 0)
            {
                return (string.Empty, dataType);
            }

            var values = entities.Select(e => e.Name).ToList();
            var validatedValues = new List<string>();

            // 支持的数据类型
            var validDataTypes = new[] { "string", "number", "date", "boolean", "array", "object", "select" };
            
            // 如果数据类型不在支持列表中，默认为string
            if (!validDataTypes.Contains(dataType.ToLowerInvariant()))
            {
                dataType = "string";
            }

            switch (dataType.ToLowerInvariant())
            {
                case "number":
                    foreach (var value in values)
                    {
                        if (double.TryParse(value, out var numberValue))
                        {
                            validatedValues.Add(numberValue.ToString());
                        }
                    }
                    return (string.Join(", ", validatedValues), "number");

                case "date":
                    foreach (var value in values)
                    {
                        if (DateTime.TryParse(value, out var dateValue))
                        {
                            validatedValues.Add(dateValue.ToString("yyyy-MM-dd"));
                        }
                        else if (DateTime.TryParseExact(value, ["yyyy-MM-dd", "yyyy/MM/dd", "MM/dd/yyyy", "dd/MM/yyyy"], 
                            null, System.Globalization.DateTimeStyles.None, out var exactDateValue))
                        {
                            validatedValues.Add(exactDateValue.ToString("yyyy-MM-dd"));
                        }
                    }
                    return (string.Join(", ", validatedValues), "date");

                case "boolean":
                    foreach (var value in values)
                    {
                        var boolValue = value.ToLowerInvariant() switch
                        {
                            "是" or "true" or "1" or "yes" or "y" => "true",
                            "否" or "false" or "0" or "no" or "n" => "false",
                            _ => null
                        };
                        if (boolValue != null)
                        {
                            validatedValues.Add(boolValue);
                        }
                    }
                    return (string.Join(", ", validatedValues), "boolean");

                case "array":
                    // 数组类型：将多个值用逗号分隔
                    foreach (var value in values)
                    {
                        var cleanedValue = value.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanedValue))
                        {
                            validatedValues.Add(cleanedValue);
                        }
                    }
                    return ($"[{string.Join(", ", validatedValues.Select(v => $"\"{v}\""))}]", "array");

                case "object":
                    // 对象类型：将实体转换为JSON对象
                    var objectData = new Dictionary<string, object>();
                    foreach (var entity in entities)
                    {
                        objectData[entity.Type] = entity.Name;
                    }
                    return (System.Text.Json.JsonSerializer.Serialize(objectData), "object");

                case "select":
                    // 选择类型：返回第一个有效值
                    var firstValidValue = values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.Trim()));
                    return (firstValidValue ?? string.Empty, "select");

                case "string":
                default:
                    // 字符串类型：进行基本的清理和验证
                    foreach (var value in values)
                    {
                        var cleanedValue = value.Trim();
                        if (!string.IsNullOrWhiteSpace(cleanedValue))
                        {
                            validatedValues.Add(cleanedValue);
                        }
                    }
                    return (string.Join(", ", validatedValues), "string");
            }
        }

        /// <summary>
        /// 更新分类信息
        /// </summary>
        private async Task UpdateCatalogueWithAnalysisResultsAsync(
            AttachCatalogue catalogue,
            SummaryAnalysisResult summaryResult,
            TagsAnalysisResult tagsResult,
            MetaDataAnalysisResult metaDataResult)
        {
            var hasUpdates = false;

            // 更新概要信息
            if (summaryResult.IsUpdated && !string.IsNullOrEmpty(summaryResult.GeneratedSummary))
            {
                catalogue.SetSummary(summaryResult.GeneratedSummary);
                hasUpdates = true;
            }

            // 更新标签
            if (tagsResult.IsUpdated && tagsResult.GeneratedTags.Count > 0)
            {
                catalogue.SetTags(tagsResult.GeneratedTags);
                hasUpdates = true;
            }

            // 更新元数据字段
            if (metaDataResult.IsUpdated && metaDataResult.GeneratedMetaFields.Count > 0)
            {
                var newMetaFields = metaDataResult.GeneratedMetaFields.Select(mf => new MetaField(
                    mf.EntityType, mf.FieldKey, mf.FieldName, mf.DataType, mf.IsRequired,
                    mf.Unit, mf.RegexPattern, mf.Options, mf.Description, mf.DefaultValue,
                    mf.Order, mf.IsEnabled, mf.Group, mf.ValidationRules, mf.Tags
                )).ToList();

                catalogue.SetMetaFields(newMetaFields);
                hasUpdates = true;
            }

            // 保存更新
            if (hasUpdates)
            {
                await CatalogueRepository.UpdateAsync(catalogue);
            }
        }

        /// <summary>
        /// 获取更新的字段列表
        /// </summary>
        private static List<string> GetUpdatedFields(
            SummaryAnalysisResult summaryResult,
            TagsAnalysisResult tagsResult,
            FullTextAnalysisResult fullTextResult,
            MetaDataAnalysisResult metaDataResult)
        {
            var updatedFields = new List<string>();

            if (summaryResult.IsUpdated)
                updatedFields.Add("Summary");

            if (tagsResult.IsUpdated)
                updatedFields.Add("Tags");

            if (fullTextResult.IsUpdated)
                updatedFields.Add("FullTextContent");

            if (metaDataResult.IsUpdated)
                updatedFields.Add("MetaFields");

            return updatedFields;
        }

        /// <summary>
        /// 获取分类及其所有子分类的文件
        /// </summary>
        /// <param name="catalogueId">分类ID</param>
        /// <returns>所有文件列表</returns>
        private async Task<List<AttachFile>> GetCatalogueFilesWithChildrenAsync(Guid catalogueId)
        {
            try
            {
                // 1. 获取所有子分类（包括叶子节点）
                var allCategories = await CatalogueRepository.GetCatalogueWithAllChildrenAsync(catalogueId);
                
                if (allCategories.Count == 0)
                {
                    Logger.LogWarning("分类 {CatalogueId} 下没有找到任何子分类", catalogueId);
                    return [];
                }

                // 2. 筛选出叶子节点分类（只有叶子节点有文件）
                var leafCategories = allCategories.Where(c => c.TemplateRole == TemplateRole.Leaf).ToList();
                
                if (leafCategories.Count == 0)
                {
                    Logger.LogWarning("分类 {CatalogueId} 下没有叶子节点分类，总分类数: {TotalCount}", 
                        catalogueId, allCategories.Count);
                    
                    // 如果当前分类本身就是叶子节点，直接获取其文件
                    var currentCategory = allCategories.FirstOrDefault(c => c.Id == catalogueId);
                    if (currentCategory?.TemplateRole == TemplateRole.Leaf)
                    {
                        var currentFiles = await EfCoreAttachFileRepository.GetListByCatalogueIdAsync(catalogueId);
                        Logger.LogInformation("当前分类 {CatalogueId} 是叶子节点，获取文件数: {FileCount}", 
                            catalogueId, currentFiles.Count);
                        return currentFiles;
                    }
                    
                    return [];
                }

                // 3. 获取所有叶子节点的文件
                var leafCategoryIds = leafCategories.Select(c => c.Id).ToList();
                
                // 批量获取文件，提高性能
                var files = await EfCoreAttachFileRepository.GetListByCatalogueIdsAsync(leafCategoryIds);

                Logger.LogInformation("获取分类 {CatalogueId} 及其子分类的文件，总分类数: {TotalCount}, 叶子节点数: {LeafCount}, 文件数: {FileCount}", 
                    catalogueId, allCategories.Count, leafCategories.Count, files.Count);

                return files;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取分类 {CatalogueId} 及其子分类的文件失败", catalogueId);
                
                // 降级处理：只获取当前分类的文件
                try
                {
                    var fallbackFiles = await EfCoreAttachFileRepository.GetListByCatalogueIdAsync(catalogueId);
                    Logger.LogWarning("降级处理：只获取当前分类 {CatalogueId} 的文件，文件数: {FileCount}", 
                        catalogueId, fallbackFiles.Count);
                    return fallbackFiles;
                }
                catch (Exception fallbackEx)
                {
                    Logger.LogError(fallbackEx, "降级处理也失败，分类 {CatalogueId}", catalogueId);
                    return [];
                }
            }
        }
    }
}
