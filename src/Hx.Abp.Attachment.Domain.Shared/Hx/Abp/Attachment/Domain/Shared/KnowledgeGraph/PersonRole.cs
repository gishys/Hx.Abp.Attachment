namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 人员角色枚举（用于 PersonRelatesToCatalogue 关系的 role 属性）
    /// </summary>
    public enum PersonRole
    {
        Creator,              // 创建者
        Manager,              // 管理者
        ProjectManager,       // 项目经理
        Reviewer,             // 审核人
        Expert,               // 专家
        Responsible,          // 责任人
        Contact,              // 联系人
        Participant           // 参与者
    }
}

