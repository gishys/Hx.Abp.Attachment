namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 分类关系语义类型枚举（用于 CatalogueRelatesToCatalogue 关系的 semanticType 属性）
    /// </summary>
    public enum CatalogueSemanticType
    {
        Temporal,             // 时间关系（项目阶段不同时期的档案）
        Business,             // 业务关系（按业务划分的分类关系）
        Version,              // 版本关系
        Replaces,             // 替换关系（版本演进）
        DependsOn,            // 依赖关系
        References,           // 引用关系
        SimilarTo             // 相似关系
    }
}

