using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Guids;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 预设元数据内容应用服务
    /// </summary>
    public class MetaFieldPresetAppService(
        IMetaFieldPresetRepository repository,
        IAttachCatalogueTemplateRepository templateRepository,
        IGuidGenerator guidGenerator,
        ILogger<MetaFieldPresetAppService> logger) :
        ApplicationService,
        IMetaFieldPresetAppService
    {
        private readonly IMetaFieldPresetRepository _repository = repository;
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly IGuidGenerator _guidGenerator = guidGenerator;
        private readonly ILogger<MetaFieldPresetAppService> _logger = logger;

        /// <summary>
        /// 创建预设
        /// </summary>
        public async Task<MetaFieldPresetDto> CreateAsync(CreateUpdateMetaFieldPresetDto input)
        {
            try
            {
                _logger.LogInformation("创建预设：名称={presetName}", input.PresetName);

                // 检查名称是否已存在
                if (await _repository.ExistsByNameAsync(input.PresetName, cancellationToken: default))
                {
                    throw new UserFriendlyException($"预设名称 '{input.PresetName}' 已存在");
                }

                // 转换元数据字段
                var metaFields = input.MetaFields?.Select(dto => new MetaField(
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
                    dto.Tags)).ToList();

                // 创建实体
                var preset = new MetaFieldPreset(
                    _guidGenerator.Create(),
                    input.PresetName,
                    input.Description,
                    input.Tags,
                    metaFields,
                    input.BusinessScenarios,
                    input.ApplicableFacetTypes,
                    input.ApplicableTemplatePurposes,
                    false,
                    input.SortOrder);

                preset.Validate();

                await _repository.InsertAsync(preset);

                _logger.LogInformation("创建预设成功：ID={id}, 名称={presetName}", preset.Id, preset.PresetName);

                return ObjectMapper.Map<MetaFieldPreset, MetaFieldPresetDto>(preset);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "创建预设失败：名称={presetName}", input.PresetName);
                throw new UserFriendlyException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建预设失败：名称={presetName}", input.PresetName);
                throw new UserFriendlyException($"创建预设失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 更新预设
        /// </summary>
        public async Task<MetaFieldPresetDto> UpdateAsync(Guid id, CreateUpdateMetaFieldPresetDto input)
        {
            try
            {
                _logger.LogInformation("更新预设：ID={id}, 名称={presetName}", id, input.PresetName);

                var preset = await _repository.GetAsync(id) ?? throw new UserFriendlyException($"未找到预设 {id}");

                // 检查名称是否已被其他预设使用
                if (await _repository.ExistsByNameAsync(input.PresetName, id, cancellationToken: default))
                {
                    throw new UserFriendlyException($"预设名称 '{input.PresetName}' 已被其他预设使用");
                }

                // 转换元数据字段
                var metaFields = input.MetaFields?.Select(dto => new MetaField(
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
                    dto.Tags)).ToList();

                preset.Update(
                    input.PresetName,
                    input.Description,
                    input.Tags,
                    metaFields,
                    input.BusinessScenarios,
                    input.ApplicableFacetTypes,
                    input.ApplicableTemplatePurposes,
                    input.SortOrder);

                preset.Validate();

                await _repository.UpdateAsync(preset);

                _logger.LogInformation("更新预设成功：ID={id}, 名称={presetName}", id, input.PresetName);

                return ObjectMapper.Map<MetaFieldPreset, MetaFieldPresetDto>(preset);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "更新预设失败：ID={id}, 名称={presetName}", id, input.PresetName);
                throw new UserFriendlyException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新预设失败：ID={id}, 名称={presetName}", id, input.PresetName);
                throw new UserFriendlyException($"更新预设失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 删除预设
        /// </summary>
        public async Task DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("删除预设：ID={id}", id);

                var preset = await _repository.GetAsync(id) ?? throw new UserFriendlyException($"未找到预设 {id}");
                if (preset.IsSystemPreset)
                {
                    throw new UserFriendlyException("系统预设不能删除");
                }

                await _repository.DeleteAsync(preset);

                _logger.LogInformation("删除预设成功：ID={id}", id);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除预设失败：ID={id}", id);
                throw new UserFriendlyException("删除预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 根据ID获取预设
        /// </summary>
        public async Task<MetaFieldPresetDto> GetAsync(Guid id)
        {
            try
            {
                var preset = await _repository.GetAsync(id);
                return preset == null
                    ? throw new UserFriendlyException($"未找到预设 {id}")
                    : ObjectMapper.Map<MetaFieldPreset, MetaFieldPresetDto>(preset);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取预设失败：ID={id}", id);
                throw new UserFriendlyException("获取预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 获取所有预设
        /// </summary>
        public async Task<List<MetaFieldPresetDto>> GetListAsync()
        {
            try
            {
                var presets = await _repository.GetAllEnabledAsync(cancellationToken: default);
                return ObjectMapper.Map<List<MetaFieldPreset>, List<MetaFieldPresetDto>>(presets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取预设列表失败");
                throw new UserFriendlyException("获取预设列表失败，请稍后重试");
            }
        }

        /// <summary>
        /// 搜索预设
        /// </summary>
        public async Task<List<MetaFieldPresetDto>> SearchAsync(PresetSearchRequestDto input)
        {
            try
            {
                var presets = await _repository.SearchAsync(
                    input.Keyword,
                    input.Tags,
                    input.BusinessScenario,
                    input.FacetType,
                    input.TemplatePurpose,
                    input.OnlyEnabled,
                    input.MaxResults,
                    cancellationToken: default);

                var result = ObjectMapper.Map<List<MetaFieldPreset>, List<MetaFieldPresetDto>>(presets);

                // 分页处理
                if (input.SkipCount > 0)
                {
                    result = [.. result.Skip(input.SkipCount)];
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索预设失败：关键词={keyword}", input.Keyword);
                throw new UserFriendlyException("搜索预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 获取推荐预设
        /// </summary>
        public async Task<List<PresetRecommendationDto>> GetRecommendationsAsync(PresetRecommendationRequestDto input)
        {
            try
            {
                _logger.LogInformation("获取推荐预设：业务场景={businessScenario}, 分面类型={facetType}, 模板用途={templatePurpose}",
                    input.BusinessScenario, input.FacetType, input.TemplatePurpose);

                var presets = await _repository.GetRecommendedPresetsAsync(
                    input.BusinessScenario,
                    input.FacetType,
                    input.TemplatePurpose,
                    input.Tags,
                    input.TopN,
                    input.MinWeight,
                    input.OnlyEnabled,
                    cancellationToken: default);

                var recommendations = new List<PresetRecommendationDto>();

                foreach (var preset in presets)
                {
                    var score = CalculateRecommendationScore(preset, input);
                    var reasons = GenerateRecommendationReasons(preset, input);

                    recommendations.Add(new PresetRecommendationDto
                    {
                        Preset = ObjectMapper.Map<MetaFieldPreset, MetaFieldPresetDto>(preset),
                        Score = score,
                        Reasons = reasons
                    });
                }

                // 按分数排序
                recommendations = [.. recommendations.OrderByDescending(r => r.Score)];

                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取推荐预设失败");
                throw new UserFriendlyException("获取推荐预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 获取热门预设
        /// </summary>
        public async Task<List<MetaFieldPresetDto>> GetPopularPresetsAsync(int topN = 10)
        {
            try
            {
                var presets = await _repository.GetPopularPresetsAsync(topN, true, cancellationToken: default);
                return ObjectMapper.Map<List<MetaFieldPreset>, List<MetaFieldPresetDto>>(presets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门预设失败");
                throw new UserFriendlyException("获取热门预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 启用预设
        /// </summary>
        public async Task EnableAsync(Guid id)
        {
            try
            {
                var preset = await _repository.GetAsync(id) ?? throw new UserFriendlyException($"未找到预设 {id}");
                preset.Enable();
                await _repository.UpdateAsync(preset);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用预设失败：ID={id}", id);
                throw new UserFriendlyException("启用预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 禁用预设
        /// </summary>
        public async Task DisableAsync(Guid id)
        {
            try
            {
                var preset = await _repository.GetAsync(id) ?? throw new UserFriendlyException($"未找到预设 {id}");
                preset.Disable();
                await _repository.UpdateAsync(preset);
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用预设失败：ID={id}", id);
                throw new UserFriendlyException("禁用预设失败，请稍后重试");
            }
        }

        /// <summary>
        /// 应用预设到模板
        /// </summary>
        public async Task<List<MetaFieldDto>> ApplyPresetToTemplateAsync(Guid presetId, Guid templateId)
        {
            try
            {
                _logger.LogInformation("应用预设到模板：预设ID={presetId}, 模板ID={templateId}", presetId, templateId);

                // 获取预设
                var preset = await _repository.GetAsync(presetId) ?? throw new UserFriendlyException($"未找到预设 {presetId}");

                // 获取模板（最新版本）
                var template = await _templateRepository.GetLatestVersionAsync(templateId, false)
                    ?? throw new UserFriendlyException($"未找到模板 {templateId}");

                // 将预设的元数据字段转换为模板需要的格式
                var metaFields = preset.MetaFields?.Select(field => new MetaField(
                    field.EntityType,
                    field.FieldKey,
                    field.FieldName,
                    field.DataType,
                    field.IsRequired,
                    field.Unit,
                    field.RegexPattern,
                    field.Options,
                    field.Description,
                    field.DefaultValue,
                    field.Order,
                    field.IsEnabled,
                    field.Group,
                    field.ValidationRules,
                    field.Tags
                )).ToList();

                // 更新模板的元数据字段（保持其他字段不变）
                template.Update(
                    template.TemplateName,
                    template.AttachReceiveType,
                    template.SequenceNumber,
                    template.IsRequired,
                    template.IsStatic,
                    template.WorkflowConfig,
                    template.FacetType,
                    template.TemplatePurpose,
                    template.TemplateRole,
                    template.Description,
                    template.Tags,
                    metaFields, // 更新元数据字段
                    template.TemplatePath // 保持原有路径
                );

                // 保存模板
                await _templateRepository.UpdateAsync(template);

                // 记录预设使用
                preset.IncrementUsageCount();
                await _repository.UpdateAsync(preset);

                // 返回元数据字段列表（DTO格式）
                var metaFieldDtos = preset.MetaFields?.Select(field => ObjectMapper.Map<MetaField, MetaFieldDto>(field)).ToList();

                _logger.LogInformation("应用预设到模板成功：预设ID={presetId}, 模板ID={templateId}, 字段数量={fieldCount}",
                    presetId, templateId, metaFieldDtos?.Count);

                return metaFieldDtos ?? [];
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用预设到模板失败：预设ID={presetId}, 模板ID={templateId}", presetId, templateId);
                throw new UserFriendlyException($"应用预设到模板失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 批量应用预设到模板
        /// </summary>
        public async Task<ApplyPresetsToTemplateResponseDto> ApplyPresetsToTemplateAsync(Guid templateId, ApplyPresetsToTemplateRequestDto input)
        {
            try
            {
                _logger.LogInformation("批量应用预设到模板：模板ID={templateId}, 预设数量={presetCount}, 合并策略={mergeStrategy}",
                    templateId, input.PresetIds?.Count ?? 0, input.MergeStrategy);

                // 验证输入
                if (input.PresetIds == null || input.PresetIds.Count == 0)
                {
                    throw new UserFriendlyException("预设ID列表不能为空");
                }

                // 获取模板（最新版本）
                var template = await _templateRepository.GetLatestVersionAsync(templateId, false)
                    ?? throw new UserFriendlyException($"未找到模板 {templateId}");

                // 获取所有预设
                var presets = new List<MetaFieldPreset>();
                var presetDict = new Dictionary<Guid, MetaFieldPreset>();
                foreach (var presetId in input.PresetIds)
                {
                    var preset = await _repository.GetAsync(presetId);
                    if (preset == null)
                    {
                        _logger.LogWarning("未找到预设：预设ID={presetId}", presetId);
                        continue;
                    }
                    if (!preset.IsEnabled)
                    {
                        _logger.LogWarning("预设已禁用：预设ID={presetId}", presetId);
                        continue;
                    }
                    presets.Add(preset);
                    presetDict[presetId] = preset;
                }

                if (presets.Count == 0)
                {
                    throw new UserFriendlyException("没有可用的预设");
                }

                // 收集所有预设的元数据字段
                var allPresetFields = new Dictionary<string, (MetaField Field, Guid PresetId, string PresetName)>();
                foreach (var preset in presets)
                {
                    if (preset.MetaFields == null || preset.MetaFields.Count == 0)
                    {
                        continue;
                    }

                    foreach (var field in preset.MetaFields)
                    {
                        var fieldKey = field.FieldKey;
                        if (string.IsNullOrWhiteSpace(fieldKey))
                        {
                            continue;
                        }

                        // 检查是否已存在相同的字段键名
                        if (allPresetFields.ContainsKey(fieldKey))
                        {
                            // 如果策略是覆盖，则用后面的预设字段覆盖前面的
                            if (input.MergeStrategy == MergeStrategy.Overwrite)
                            {
                                allPresetFields[fieldKey] = (field, preset.Id, preset.PresetName);
                            }
                            // 如果策略是跳过，则保留第一个
                        }
                        else
                        {
                            allPresetFields[fieldKey] = (field, preset.Id, preset.PresetName);
                        }
                    }
                }

                // 获取模板现有的元数据字段
                var existingFields = template.MetaFields?.ToList() ?? [];
                var existingFieldKeys = existingFields.ToDictionary(f => f.FieldKey, f => f);

                // 合并字段
                var finalFields = new List<MetaField>();
                var skippedFields = new List<SkippedFieldInfo>();
                var appliedFields = new List<MetaFieldDto>();

                // 如果需要保留现有字段，先添加现有字段
                if (input.KeepExistingFields)
                {
                    foreach (var existingField in existingFields)
                    {
                        var fieldKey = existingField.FieldKey;
                        if (allPresetFields.ContainsKey(fieldKey))
                        {
                            // 如果预设中有相同键名的字段
                            if (input.MergeStrategy == MergeStrategy.Overwrite)
                            {
                                // 覆盖策略：跳过现有字段，稍后用预设字段替换
                                skippedFields.Add(new SkippedFieldInfo
                                {
                                    FieldKey = fieldKey,
                                    FieldName = existingField.FieldName,
                                    Reason = "将被预设字段覆盖",
                                    PresetId = allPresetFields[fieldKey].PresetId,
                                    PresetName = allPresetFields[fieldKey].PresetName
                                });
                                // 不添加到 finalFields，稍后会被预设字段替换
                            }
                            else
                            {
                                // 跳过策略：保留现有字段，跳过预设字段
                                finalFields.Add(existingField);
                                skippedFields.Add(new SkippedFieldInfo
                                {
                                    FieldKey = fieldKey,
                                    FieldName = allPresetFields[fieldKey].Field.FieldName,
                                    Reason = "模板中已存在相同键名的字段，已跳过",
                                    PresetId = allPresetFields[fieldKey].PresetId,
                                    PresetName = allPresetFields[fieldKey].PresetName
                                });
                            }
                        }
                        else
                        {
                            // 预设中没有，保留现有字段
                            finalFields.Add(existingField);
                        }
                    }
                }

                // 添加预设字段
                foreach (var kvp in allPresetFields)
                {
                    var fieldKey = kvp.Key;
                    var (field, presetId, presetName) = kvp.Value;

                    // 检查模板中是否已存在（在跳过策略下）
                    if (input.MergeStrategy == MergeStrategy.Skip && existingFieldKeys.ContainsKey(fieldKey))
                    {
                        // 跳过策略：模板中已存在，已在上面处理，这里跳过
                        continue;
                    }

                    // 检查字段键名是否已在最终列表中（在覆盖策略下，可能已添加了现有字段）
                    var duplicateInFinal = finalFields.FirstOrDefault(f => f.FieldKey == fieldKey);
                    if (duplicateInFinal != null)
                    {
                        // 如果是覆盖策略，替换现有字段
                        if (input.MergeStrategy == MergeStrategy.Overwrite)
                        {
                            finalFields.Remove(duplicateInFinal);
                            // 继续添加预设字段
                        }
                        else
                        {
                            // 跳过策略下不应该到这里，但为了安全还是跳过
                            skippedFields.Add(new SkippedFieldInfo
                            {
                                FieldKey = fieldKey,
                                FieldName = field.FieldName,
                                Reason = "字段键名重复",
                                PresetId = presetId,
                                PresetName = presetName
                            });
                            continue;
                        }
                    }

                    // 转换为模板需要的格式并添加
                    var metaField = new MetaField(
                        field.EntityType,
                        field.FieldKey,
                        field.FieldName,
                        field.DataType,
                        field.IsRequired,
                        field.Unit,
                        field.RegexPattern,
                        field.Options,
                        field.Description,
                        field.DefaultValue,
                        field.Order,
                        field.IsEnabled,
                        field.Group,
                        field.ValidationRules,
                        field.Tags
                    );

                    finalFields.Add(metaField);
                    appliedFields.Add(ObjectMapper.Map<MetaField, MetaFieldDto>(metaField));
                }

                // 更新模板的元数据字段
                template.Update(
                    template.TemplateName,
                    template.AttachReceiveType,
                    template.SequenceNumber,
                    template.IsRequired,
                    template.IsStatic,
                    template.WorkflowConfig,
                    template.FacetType,
                    template.TemplatePurpose,
                    template.TemplateRole,
                    template.Description,
                    template.Tags,
                    finalFields, // 更新元数据字段
                    template.TemplatePath // 保持原有路径
                );

                // 保存模板
                await _templateRepository.UpdateAsync(template);

                // 记录预设使用
                foreach (var preset in presets)
                {
                    preset.IncrementUsageCount();
                    await _repository.UpdateAsync(preset);
                }

                // 构建响应
                var response = new ApplyPresetsToTemplateResponseDto
                {
                    AppliedFields = appliedFields,
                    SkippedFields = skippedFields,
                    AppliedPresetCount = presets.Count,
                    TotalFieldCount = allPresetFields.Count,
                    AppliedFieldCount = appliedFields.Count,
                    SkippedFieldCount = skippedFields.Count
                };

                _logger.LogInformation("批量应用预设到模板成功：模板ID={templateId}, 应用预设数={presetCount}, 应用字段数={appliedCount}, 跳过字段数={skippedCount}",
                    templateId, presets.Count, appliedFields.Count, skippedFields.Count);

                return response;
            }
            catch (UserFriendlyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量应用预设到模板失败：模板ID={templateId}", templateId);
                throw new UserFriendlyException($"批量应用预设到模板失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 记录预设使用
        /// </summary>
        public async Task RecordUsageAsync(Guid presetId, Guid? templateId = null)
        {
            try
            {
                var preset = await _repository.GetAsync(presetId);
                if (preset == null)
                {
                    return; // 静默失败，不影响主流程
                }

                preset.IncrementUsageCount();
                await _repository.UpdateAsync(preset);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录预设使用失败：预设ID={presetId}, 模板ID={templateId}", presetId, templateId);
                // 静默失败，不影响主流程
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public async Task<PresetStatisticsDto> GetStatisticsAsync()
        {
            try
            {
                var allPresets = await _repository.GetListAsync();
                var enabledPresets = allPresets.Where(p => p.IsEnabled).ToList();
                var systemPresets = allPresets.Where(p => p.IsSystemPreset).ToList();

                var topPresets = await _repository.GetPopularPresetsAsync(10, true, cancellationToken: default);
                var topPresetDtos = ObjectMapper.Map<List<MetaFieldPreset>, List<MetaFieldPresetDto>>(topPresets);

                // 统计业务场景
                var businessScenarioStats = new Dictionary<string, int>();
                foreach (var preset in allPresets)
                {
                    if (preset.BusinessScenarios != null)
                    {
                        foreach (var scenario in preset.BusinessScenarios)
                        {
                            businessScenarioStats.TryGetValue(scenario, out var count);
                            businessScenarioStats[scenario] = count + 1;
                        }
                    }
                }

                // 统计标签
                var tagStats = new Dictionary<string, int>();
                foreach (var preset in allPresets)
                {
                    if (preset.Tags != null)
                    {
                        foreach (var tag in preset.Tags)
                        {
                            tagStats.TryGetValue(tag, out var count);
                            tagStats[tag] = count + 1;
                        }
                    }
                }

                return new PresetStatisticsDto
                {
                    TotalCount = allPresets.Count,
                    EnabledCount = enabledPresets.Count,
                    SystemPresetCount = systemPresets.Count,
                    TotalUsageCount = allPresets.Sum(p => p.UsageCount),
                    TopPresets = topPresetDtos,
                    BusinessScenarioStats = businessScenarioStats,
                    TagStats = tagStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计信息失败");
                throw new UserFriendlyException("获取统计信息失败，请稍后重试");
            }
        }

        /// <summary>
        /// 批量更新推荐权重（用于自我进化）
        /// </summary>
        public async Task BatchUpdateRecommendationWeightsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始批量更新推荐权重");

                var allPresets = await _repository.GetListAsync(cancellationToken: cancellationToken);
                var weights = new Dictionary<Guid, double>();

                foreach (var preset in allPresets)
                {
                    var weight = CalculateRecommendationWeight(preset);
                    weights[preset.Id] = weight;
                }

                await _repository.BatchUpdateRecommendationWeightsAsync(weights, cancellationToken);

                _logger.LogInformation("批量更新推荐权重完成：更新数量={count}", weights.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新推荐权重失败");
                throw new UserFriendlyException("批量更新推荐权重失败，请稍后重试");
            }
        }

        #region 私有方法

        /// <summary>
        /// 计算推荐分数
        /// </summary>
        private static double CalculateRecommendationScore(MetaFieldPreset preset, PresetRecommendationRequestDto request)
        {
            double score = preset.RecommendationWeight;

            // 使用频率权重（0-0.3）
            var usageScore = Math.Min(preset.UsageCount / 100.0, 0.3);
            score += usageScore;

            // 业务场景匹配（0-0.2）
            if (!string.IsNullOrWhiteSpace(request.BusinessScenario) &&
                preset.BusinessScenarios?.Contains(request.BusinessScenario) == true)
            {
                score += 0.2;
            }

            // 分面类型匹配（0-0.2）
            if (request.FacetType.HasValue &&
                preset.IsApplicableToFacetType(request.FacetType.Value))
            {
                score += 0.2;
            }

            // 模板用途匹配（0-0.2）
            if (request.TemplatePurpose.HasValue &&
                preset.IsApplicableToTemplatePurpose(request.TemplatePurpose.Value))
            {
                score += 0.2;
            }

            // 标签匹配（0-0.1）
            if (request.Tags != null && request.Tags.Count > 0 &&
                preset.Tags != null && preset.Tags.Any(t => request.Tags.Contains(t)))
            {
                score += 0.1;
            }

            // 最近使用加分（0-0.1）
            if (preset.LastUsedTime.HasValue)
            {
                var daysSinceLastUse = (DateTime.UtcNow - preset.LastUsedTime.Value).TotalDays;
                if (daysSinceLastUse <= 30)
                {
                    score += 0.1 * (1 - daysSinceLastUse / 30);
                }
            }

            return Math.Min(score, 1.0);
        }

        /// <summary>
        /// 生成推荐原因
        /// </summary>
        private static List<string> GenerateRecommendationReasons(MetaFieldPreset preset, PresetRecommendationRequestDto request)
        {
            var reasons = new List<string>();

            if (preset.UsageCount > 0)
            {
                reasons.Add($"已被使用 {preset.UsageCount} 次");
            }

            if (!string.IsNullOrWhiteSpace(request.BusinessScenario) &&
                preset.BusinessScenarios?.Contains(request.BusinessScenario) == true)
            {
                reasons.Add($"适用于业务场景：{request.BusinessScenario}");
            }

            if (request.FacetType.HasValue &&
                preset.IsApplicableToFacetType(request.FacetType.Value))
            {
                reasons.Add($"适用于分面类型：{request.FacetType.Value}");
            }

            if (request.TemplatePurpose.HasValue &&
                preset.IsApplicableToTemplatePurpose(request.TemplatePurpose.Value))
            {
                reasons.Add($"适用于模板用途：{request.TemplatePurpose.Value}");
            }

            if (preset.Tags != null && preset.Tags.Count > 0)
            {
                reasons.Add($"标签：{string.Join(", ", preset.Tags)}");
            }

            return reasons;
        }

        /// <summary>
        /// 计算推荐权重（用于自我进化）
        /// </summary>
        private static double CalculateRecommendationWeight(MetaFieldPreset preset)
        {
            double weight = 0.5; // 基础权重

            // 使用频率影响（0-0.3）
            var usageWeight = Math.Min(preset.UsageCount / 100.0, 0.3);
            weight += usageWeight;

            // 最近使用影响（0-0.2）
            if (preset.LastUsedTime.HasValue)
            {
                var daysSinceLastUse = (DateTime.UtcNow - preset.LastUsedTime.Value).TotalDays;
                if (daysSinceLastUse <= 90)
                {
                    weight += 0.2 * (1 - daysSinceLastUse / 90);
                }
            }

            return Math.Min(weight, 1.0);
        }

        #endregion
    }
}

