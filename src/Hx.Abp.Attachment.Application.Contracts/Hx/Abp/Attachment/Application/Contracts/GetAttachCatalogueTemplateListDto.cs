using Hx.Abp.Attachment.Domain;
using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Application.Dtos;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 分类模板查询输入DTO
    /// </summary>
    public class GetAttachCatalogueTemplateListDto : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 模板名称（模糊查询）
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 附件类型
        /// </summary>
        public AttachReceiveType? AttachReceiveType { get; set; }

        /// <summary>
        /// 分面类型 - 标识模板的层级和用途
        /// </summary>
        public FacetType? FacetType { get; set; }

        /// <summary>
        /// 模板用途 - 标识模板的具体用途
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }

        /// <summary>
        /// 是否必收
        /// </summary>
        public bool? IsRequired { get; set; }

        /// <summary>
        /// 是否静态
        /// </summary>
        public bool? IsStatic { get; set; }

        /// <summary>
        /// 是否最新版本
        /// </summary>
        public bool? IsLatest { get; set; }

        /// <summary>
        /// 父模板ID
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 是否包含向量
        /// </summary>
        public bool? HasVector { get; set; }

        /// <summary>
        /// 向量维度范围（最小值）
        /// </summary>
        public int? MinVectorDimension { get; set; }

        /// <summary>
        /// 向量维度范围（最大值）
        /// </summary>
        public int? MaxVectorDimension { get; set; }

        /// <summary>
        /// 语义查询（用于向量相似度搜索）
        /// </summary>
        public string? SemanticQuery { get; set; }

        /// <summary>
        /// 相似度阈值（0.0-1.0）
        /// </summary>
        public double? SimilarityThreshold { get; set; } = 0.7;
    }

    /// <summary>
    /// 分类模板统计DTO
    /// 基于动态分类树业务需求，使用简单类型便于前端展示和API传输
    /// </summary>
    public class AttachCatalogueTemplateStatisticsDto
    {
        /// <summary>
        /// 总模板数量
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 根节点模板数量
        /// </summary>
        public int RootTemplateCount { get; set; }

        /// <summary>
        /// 子节点模板数量
        /// </summary>
        public int ChildTemplateCount { get; set; }

        /// <summary>
        /// 最新版本模板数量
        /// </summary>
        public int LatestVersionCount { get; set; }

        /// <summary>
        /// 历史版本模板数量
        /// </summary>
        public int HistoryVersionCount { get; set; }

        /// <summary>
        /// 通用分面模板数量
        /// </summary>
        public int GeneralFacetCount { get; set; }

        /// <summary>
        /// 专业领域分面模板数量
        /// </summary>
        public int DisciplineFacetCount { get; set; }

        /// <summary>
        /// 分类管理用途模板数量
        /// </summary>
        public int ClassificationPurposeCount { get; set; }

        /// <summary>
        /// 文档管理用途模板数量
        /// </summary>
        public int DocumentPurposeCount { get; set; }

        /// <summary>
        /// 工作流用途模板数量
        /// </summary>
        public int WorkflowPurposeCount { get; set; }

        /// <summary>
        /// 有向量的模板数量
        /// </summary>
        public int TemplatesWithVector { get; set; }

        /// <summary>
        /// 平均向量维度
        /// </summary>
        public double AverageVectorDimension { get; set; }

        /// <summary>
        /// 最大树深度
        /// </summary>
        public int MaxTreeDepth { get; set; }

        /// <summary>
        /// 平均子节点数量
        /// </summary>
        public double AverageChildrenCount { get; set; }

        /// <summary>
        /// 最近创建时间
        /// </summary>
        public string? LatestCreationTime { get; set; }

        /// <summary>
        /// 最近修改时间
        /// </summary>
        public string? LatestModificationTime { get; set; }
    }
}
