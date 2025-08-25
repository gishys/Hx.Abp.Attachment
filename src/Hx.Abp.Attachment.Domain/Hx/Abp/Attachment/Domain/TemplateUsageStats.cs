using System;
using System.Collections.Generic;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 模板使用统计值对象
    /// </summary>
    public class TemplateUsageStats
    {
        public Guid Id { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int UniqueReferences { get; set; }
        public DateTime? LastUsedTime { get; set; }
        public int RecentUsageCount { get; set; }
        public double AverageUsagePerDay { get; set; }
    }

    /// <summary>
    /// 模板使用趋势值对象
    /// </summary>
    public class TemplateUsageTrend
    {
        public DateTime Date { get; set; }
        public int UsageCount { get; set; }
        public int UniqueReferences { get; set; }
    }

    /// <summary>
    /// 批量模板使用统计值对象
    /// </summary>
    public class BatchTemplateUsageStats
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int TotalUsageCount { get; set; }
        public int RecentUsageCount { get; set; }
        public DateTime? LastUsedTime { get; set; }
        public double AverageUsagePerDay { get; set; }
    }

    /// <summary>
    /// 热门模板值对象
    /// </summary>
    public class HotTemplate
    {
        public Guid TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public int UsageCount { get; set; }
        public int RecentUsageCount { get; set; }
        public double AverageUsagePerDay { get; set; }
        public DateTime? LastUsedTime { get; set; }
    }

    /// <summary>
    /// 模板使用统计概览值对象
    /// </summary>
    public class TemplateUsageOverview
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int TotalUsageCount { get; set; }
        public double AverageUsagePerTemplate { get; set; }
        public List<HotTemplate> TopUsedTemplates { get; set; } = new();
        public Dictionary<string, int> UsageByMonth { get; set; } = new();
    }
}
