namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 工作流关系语义类型枚举（用于 WorkflowRelatesToWorkflow 关系的 semanticType 属性）
    /// </summary>
    public enum WorkflowSemanticType
    {
        Version,              // 版本关系
        Replaces              // 替换关系
    }
}

