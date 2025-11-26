namespace Hx.Abp.Attachment.Application.Contracts.KnowledgeGraph
{
    /// <summary>
    /// 知识图谱实体视图模型基类
    /// 注意：这是图谱查询的视图模型，不是数据库实体
    /// 实际数据存储在现有实体表中（APPATTACH_CATALOGUES等）
    /// 通过 EntityId 关联到现有实体的 Id 字段
    /// </summary>
    public abstract class KnowledgeGraphEntityViewModel
    {
        /// <summary>
        /// 关联到现有实体表的ID（如 AttachCatalogue.Id）
        /// 使用 EntityId 而不是 Id，避免与继承类的标识字段冲突
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// 实体类型（Catalogue, Person, Department, BusinessEntity, Workflow）
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// 实体名称（从现有实体表获取）
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签列表（从现有实体表获取）
        /// </summary>
        public List<string> Tags { get; set; } = [];

        /// <summary>
        /// 统一状态管理（ACTIVE, ARCHIVED, DELETED等）
        /// </summary>
        public string Status { get; set; } = "ACTIVE";

        /// <summary>
        /// 图谱特有属性（如重要性评分、中心度等）
        /// </summary>
        public Dictionary<string, object> GraphProperties { get; set; } = [];

        // 注意：其他业务字段（如CatalogueName等）通过JOIN现有实体表获取
        // CreatedBy/UpdatedBy等信息通过ABP框架的审计字段获取
    }
}

