using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 模板结构DTO - 优化版本
    /// 基于行业最佳实践设计，统一管理当前版本和历史版本
    /// </summary>
    public class TemplateStructureDto
    {
        /// <summary>
        /// 模板的所有版本（包含当前版本和历史版本）
        /// 按版本号降序排列，第一个为当前版本
        /// </summary>
        public List<AttachCatalogueTemplateDto> Versions { get; set; } = [];

        /// <summary>
        /// 当前版本（最新版本）
        /// 从 Versions 中提取 IsLatest = true 的版本
        /// </summary>
        public AttachCatalogueTemplateDto? CurrentVersion => Versions.FirstOrDefault(v => v.IsLatest);

        /// <summary>
        /// 历史版本列表
        /// 从 Versions 中提取 IsLatest = false 的版本
        /// </summary>
        public List<AttachCatalogueTemplateDto> HistoryVersions => [.. Versions.Where(v => !v.IsLatest)];

        /// <summary>
        /// 模板基本信息（基于当前版本）
        /// </summary>
        public TemplateBasicInfoDto? BasicInfo => CurrentVersion != null ? new TemplateBasicInfoDto
        {
            Id = CurrentVersion.Id,
            Name = CurrentVersion.Name,
            Description = CurrentVersion.Description,
            Version = CurrentVersion.Version,
            IsLatest = CurrentVersion.IsLatest,
            FacetType = CurrentVersion.FacetType,
            TemplatePurpose = CurrentVersion.TemplatePurpose,
            CreationTime = CurrentVersion.CreationTime,
            LastModificationTime = CurrentVersion.LastModificationTime
        } : null;

        /// <summary>
        /// 版本统计信息
        /// </summary>
        public TemplateVersionStatsDto VersionStats => new()
        {
            TotalVersions = Versions.Count,
            CurrentVersionNumber = CurrentVersion?.Version ?? 0,
            HasHistory = HistoryVersions.Count != 0,
            LatestVersionId = CurrentVersion?.Id,
            FirstCreatedTime = Versions.Min(v => v.CreationTime),
            LastModifiedTime = Versions.Max(v => v.LastModificationTime ?? v.CreationTime)
        };
    }

    /// <summary>
    /// 模板基本信息DTO
    /// </summary>
    public class TemplateBasicInfoDto
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 模板描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否为最新版本
        /// </summary>
        public bool IsLatest { get; set; }

        /// <summary>
        /// 分面类型
        /// </summary>
        public FacetType FacetType { get; set; }

        /// <summary>
        /// 模板用途
        /// </summary>
        public TemplatePurpose TemplatePurpose { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; set; }
    }

    /// <summary>
    /// 模板版本统计信息DTO
    /// </summary>
    public class TemplateVersionStatsDto
    {
        /// <summary>
        /// 总版本数
        /// </summary>
        public int TotalVersions { get; set; }

        /// <summary>
        /// 当前版本号
        /// </summary>
        public int CurrentVersionNumber { get; set; }

        /// <summary>
        /// 是否有历史版本
        /// </summary>
        public bool HasHistory { get; set; }

        /// <summary>
        /// 最新版本ID
        /// </summary>
        public Guid? LatestVersionId { get; set; }

        /// <summary>
        /// 首次创建时间
        /// </summary>
        public DateTime FirstCreatedTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModifiedTime { get; set; }
    }
}

