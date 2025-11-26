namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 分类实体视图模型（核心）- 引用AttachCatalogue
    /// 只包含图谱查询需要的核心字段
    /// </summary>
    public class CatalogueEntityViewModel : KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 业务引用ID（用于关联BusinessEntity）
        /// </summary>
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// 业务类型标识（用于关联BusinessEntity）
        /// </summary>
        public int ReferenceType { get; set; }

        /// <summary>
        /// 父分类ID（用于建立树形关系和分类间关系）
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 重要性评分（用于影响分析，可选，存储在kg_entity_graph_metadata表）
        /// </summary>
        public double? ImportanceScore { get; set; }

        /// <summary>
        /// 关系数量（用于中心度计算，可选，存储在kg_entity_graph_metadata表）
        /// </summary>
        public int? RelationshipCount { get; set; }

        // 注意：其他字段（CatalogueName、FacetType、AttachCount等）通过JOIN APPATTACH_CATALOGUES表获取
        // 模板和分面信息已体现在分类的FacetType等字段中，无需单独维度
    }
}

