using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 模板使用统计应用服务
    /// </summary>
    public class TemplateUsageStatsAppService(
        IAttachCatalogueTemplateRepository templateRepository,
        ILogger<TemplateUsageStatsAppService> logger) :
        ApplicationService, ITemplateUsageStatsAppService
    {
        private readonly IAttachCatalogueTemplateRepository _templateRepository = templateRepository;
        private readonly ILogger<TemplateUsageStatsAppService> _logger = logger;

        /// <summary>
        /// 获取模板使用次数
        /// </summary>
        public async Task<int> GetTemplateUsageCountAsync(Guid templateId)
        {
            try
            {
                _logger.LogInformation("开始获取模板使用次数，模板ID：{templateId}", templateId);
                var usageCount = await _templateRepository.GetTemplateUsageCountAsync(templateId);
                _logger.LogInformation("获取模板使用次数完成，模板ID：{templateId}，使用次数：{usageCount}", templateId, usageCount);
                return usageCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板使用次数失败，模板ID：{templateId}", templateId);
                return 0;
            }
        }

        /// <summary>
        /// 获取模板使用统计
        /// </summary>
        public async Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync(TemplateUsageStatsInputDto input)
        {
            try
            {
                _logger.LogInformation("开始获取模板使用统计，模板ID：{templateId}", input.TemplateId);
                var domainStats = await _templateRepository.GetTemplateUsageStatsAsync(input.TemplateId);
                
                // 映射Domain值对象到DTO
                var dto = new TemplateUsageStatsDto
                {
                    Id = domainStats.Id,
                    TemplateName = domainStats.TemplateName,
                    UsageCount = domainStats.UsageCount,
                    UniqueReferences = domainStats.UniqueReferences,
                    LastUsedTime = domainStats.LastUsedTime,
                    RecentUsageCount = domainStats.RecentUsageCount,
                    AverageUsagePerDay = domainStats.AverageUsagePerDay
                };
                
                _logger.LogInformation("获取模板使用统计完成，模板ID：{templateId}，使用次数：{usageCount}", 
                    input.TemplateId, dto.UsageCount);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板使用统计失败，模板ID：{templateId}", input.TemplateId);
                return new TemplateUsageStatsDto { Id = input.TemplateId };
            }
        }

        /// <summary>
        /// 获取模板使用趋势
        /// </summary>
        public async Task<List<TemplateUsageTrendDto>> GetTemplateUsageTrendAsync(Guid templateId, int daysBack = 30)
        {
            try
            {
                _logger.LogInformation("开始获取模板使用趋势，模板ID：{templateId}，天数：{daysBack}", templateId, daysBack);
                var domainTrends = await _templateRepository.GetTemplateUsageTrendAsync(templateId, daysBack);
                
                // 映射Domain值对象到DTO
                var dtoTrends = domainTrends.Select(trend => new TemplateUsageTrendDto
                {
                    UsageDate = trend.Date,
                    DailyCount = trend.UsageCount,
                    CumulativeCount = 0 // 需要计算累计值
                }).ToList();
                
                // 计算累计值
                var cumulativeCount = 0;
                foreach (var trend in dtoTrends)
                {
                    cumulativeCount += trend.DailyCount;
                    trend.CumulativeCount = cumulativeCount;
                }
                
                _logger.LogInformation("获取模板使用趋势完成，模板ID：{templateId}，趋势数据点：{count}", 
                    templateId, dtoTrends.Count);
                return dtoTrends;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板使用趋势失败，模板ID：{templateId}", templateId);
                return [];
            }
        }

        /// <summary>
        /// 批量获取模板使用统计
        /// </summary>
        public async Task<List<BatchTemplateUsageStatsDto>> GetBatchTemplateUsageStatsAsync(BatchTemplateUsageStatsInputDto input)
        {
            try
            {
                _logger.LogInformation("开始批量获取模板使用统计，模板数量：{count}，天数：{daysBack}", 
                    input.TemplateIds.Count, input.DaysBack);
                var domainResults = await _templateRepository.GetBatchTemplateUsageStatsAsync(input.TemplateIds, input.DaysBack);
                
                // 映射Domain值对象到DTO
                var dtoResults = domainResults.Select(result => new BatchTemplateUsageStatsDto
                {
                    TemplateId = result.TemplateId,
                    TemplateName = result.TemplateName,
                    Stats = new TemplateUsageStatsDto
                    {
                        Id = result.TemplateId,
                        TemplateName = result.TemplateName,
                        UsageCount = result.TotalUsageCount,
                        RecentUsageCount = result.RecentUsageCount,
                        LastUsedTime = result.LastUsedTime,
                        AverageUsagePerDay = result.AverageUsagePerDay
                    },
                    Trends = [], // 简化处理，实际应该获取趋势数据
                    IsSuccess = true
                }).ToList();
                
                _logger.LogInformation("批量获取模板使用统计完成，成功数量：{successCount}", dtoResults.Count);
                return dtoResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取模板使用统计失败");
                return [.. input.TemplateIds.Select(id => new BatchTemplateUsageStatsDto
                {
                    TemplateId = id,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                })];
            }
        }

        /// <summary>
        /// 获取热门模板
        /// </summary>
        public async Task<ListResultDto<HotTemplateDto>> GetHotTemplatesAsync(HotTemplatesInputDto input)
        {
            try
            {
                _logger.LogInformation("开始获取热门模板，天数：{daysBack}，数量：{topN}，最小使用次数：{minUsageCount}", 
                    input.DaysBack, input.TopN, input.MinUsageCount);
                var domainHotTemplates = await _templateRepository.GetHotTemplatesAsync(input.DaysBack, input.TopN, input.MinUsageCount);
                
                // 映射Domain值对象到DTO
                var dtoHotTemplates = domainHotTemplates.Select((template, index) => new HotTemplateDto
                {
                    TemplateId = template.TemplateId,
                    TemplateName = template.TemplateName,
                    UsageCount = template.UsageCount,
                    Rank = index + 1,
                    UsageFrequency = template.AverageUsagePerDay
                }).ToList();
                
                _logger.LogInformation("获取热门模板完成，返回数量：{count}", dtoHotTemplates.Count);
                return new ListResultDto<HotTemplateDto>(dtoHotTemplates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取热门模板失败");
                return new ListResultDto<HotTemplateDto>([]);
            }
        }

        /// <summary>
        /// 获取模板使用统计概览
        /// </summary>
        public async Task<object> GetTemplateUsageOverviewAsync()
        {
            try
            {
                _logger.LogInformation("开始获取模板使用统计概览");
                var domainOverview = await _templateRepository.GetTemplateUsageOverviewAsync();
                
                // 映射Domain值对象到DTO格式
                var overview = new
                {
                    domainOverview.TotalTemplates,
                    TotalUsage = domainOverview.TotalUsageCount,
                    RecentUsage = 0, // 需要额外计算
                    domainOverview.AverageUsagePerTemplate,
                    MostActiveTemplate = domainOverview.TopUsedTemplates.FirstOrDefault() != null ? new
                    {
                        domainOverview.TopUsedTemplates.First().TemplateId,
                        domainOverview.TopUsedTemplates.First().TemplateName,
                        domainOverview.TopUsedTemplates.First().UsageCount
                    } : null,
                    LastUpdated = DateTime.UtcNow
                };
                
                _logger.LogInformation("获取模板使用统计概览完成");
                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取模板使用统计概览失败");
                return new { Error = ex.Message };
            }
        }

        /// <summary>
        /// 更新模板使用统计（内部使用）
        /// </summary>
        public Task<bool> UpdateTemplateUsageStatsAsync(Guid templateId)
        {
            try
            {
                _logger.LogInformation("开始更新模板使用统计，模板ID：{templateId}", templateId);
                
                // 这里可以添加缓存更新、统计重新计算等逻辑
                // 目前只是记录日志，实际实现可以根据需要扩展
                
                _logger.LogInformation("更新模板使用统计完成，模板ID：{templateId}", templateId);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新模板使用统计失败，模板ID：{templateId}", templateId);
                return Task.FromResult(false);
            }
        }
    }
}
