using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 模板使用统计DTO
    /// </summary>
    public class TemplateUsageStatsDto : EntityDto<Guid>
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 唯一引用数量
        /// </summary>
        public int UniqueReferences { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedTime { get; set; }

        /// <summary>
        /// 平均使用频率（每天）
        /// </summary>
        public double AverageUsagePerDay { get; set; }

        /// <summary>
        /// 最近30天使用次数
        /// </summary>
        public int RecentUsageCount { get; set; }
    }

    /// <summary>
    /// 模板使用趋势DTO
    /// </summary>
    public class TemplateUsageTrendDto
    {
        /// <summary>
        /// 使用日期
        /// </summary>
        public DateTime UsageDate { get; set; }

        /// <summary>
        /// 当日使用次数
        /// </summary>
        public int DailyCount { get; set; }

        /// <summary>
        /// 累计使用次数
        /// </summary>
        public int CumulativeCount { get; set; }
    }

    /// <summary>
    /// 批量模板使用统计DTO
    /// </summary>
    public class BatchTemplateUsageStatsDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 使用统计
        /// </summary>
        public TemplateUsageStatsDto Stats { get; set; } = new();

        /// <summary>
        /// 使用趋势
        /// </summary>
        public List<TemplateUsageTrendDto> Trends { get; set; } = [];

        /// <summary>
        /// 是否成功获取
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 模板使用统计查询输入DTO
    /// </summary>
    public class TemplateUsageStatsInputDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 趋势分析天数（默认30天）
        /// </summary>
        public int DaysBack { get; set; } = 30;

        /// <summary>
        /// 是否包含趋势数据
        /// </summary>
        public bool IncludeTrends { get; set; } = true;
    }

    /// <summary>
    /// 批量模板使用统计查询输入DTO
    /// </summary>
    public class BatchTemplateUsageStatsInputDto
    {
        /// <summary>
        /// 模板ID列表
        /// </summary>
        public List<Guid> TemplateIds { get; set; } = [];

        /// <summary>
        /// 趋势分析天数（默认30天）
        /// </summary>
        public int DaysBack { get; set; } = 30;

        /// <summary>
        /// 是否包含趋势数据
        /// </summary>
        public bool IncludeTrends { get; set; } = true;
    }

    /// <summary>
    /// 热门模板查询输入DTO
    /// </summary>
    public class HotTemplatesInputDto
    {
        /// <summary>
        /// 查询天数（默认30天）
        /// </summary>
        public int DaysBack { get; set; } = 30;

        /// <summary>
        /// 返回数量（默认10个）
        /// </summary>
        public int TopN { get; set; } = 10;

        /// <summary>
        /// 最小使用次数阈值
        /// </summary>
        public int MinUsageCount { get; set; } = 1;
    }

    /// <summary>
    /// 热门模板DTO
    /// </summary>
    public class HotTemplateDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 排名
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 使用频率（每天）
        /// </summary>
        public double UsageFrequency { get; set; }
    }
}
