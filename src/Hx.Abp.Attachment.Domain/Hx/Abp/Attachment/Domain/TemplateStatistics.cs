using System;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 模板统计信息值对象
    /// 基于动态分类树业务需求，使用简单类型的统计数据
    /// </summary>
    public class TemplateStatistics
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
        public DateTime? LatestCreationTime { get; set; }

        /// <summary>
        /// 最近修改时间
        /// </summary>
        public DateTime? LatestModificationTime { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TemplateStatistics()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TemplateStatistics(
            int totalCount,
            int rootTemplateCount,
            int childTemplateCount,
            int latestVersionCount,
            int historyVersionCount,
            int generalFacetCount,
            int disciplineFacetCount,
            int classificationPurposeCount,
            int documentPurposeCount,
            int workflowPurposeCount,
            int templatesWithVector,
            double averageVectorDimension,
            int maxTreeDepth,
            double averageChildrenCount,
            DateTime? latestCreationTime,
            DateTime? latestModificationTime)
        {
            TotalCount = totalCount;
            RootTemplateCount = rootTemplateCount;
            ChildTemplateCount = childTemplateCount;
            LatestVersionCount = latestVersionCount;
            HistoryVersionCount = historyVersionCount;
            GeneralFacetCount = generalFacetCount;
            DisciplineFacetCount = disciplineFacetCount;
            ClassificationPurposeCount = classificationPurposeCount;
            DocumentPurposeCount = documentPurposeCount;
            WorkflowPurposeCount = workflowPurposeCount;
            TemplatesWithVector = templatesWithVector;
            AverageVectorDimension = averageVectorDimension;
            MaxTreeDepth = maxTreeDepth;
            AverageChildrenCount = averageChildrenCount;
            LatestCreationTime = latestCreationTime;
            LatestModificationTime = latestModificationTime;
        }
    }
}
