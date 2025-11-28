namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 关系角色常量
    /// 统一管理 PersonRelatesToCatalogue 和 PersonRelatesToWorkflow 关系的 role 属性值
    /// 基于 PersonRole 枚举，提供字符串常量和规范化方法
    /// </summary>
    public static class Role
    {
        // ========== PersonRelatesToCatalogue 关系的角色（基于 PersonRole 枚举） ==========
        
        /// <summary>
        /// 创建者角色
        /// </summary>
        public const string Creator = "Creator";

        /// <summary>
        /// 管理者角色
        /// </summary>
        public const string Manager = "Manager";

        /// <summary>
        /// 项目经理角色
        /// </summary>
        public const string ProjectManager = "ProjectManager";

        /// <summary>
        /// 审核人角色
        /// </summary>
        public const string Reviewer = "Reviewer";

        /// <summary>
        /// 专家角色
        /// </summary>
        public const string Expert = "Expert";

        /// <summary>
        /// 责任人角色
        /// </summary>
        public const string Responsible = "Responsible";

        /// <summary>
        /// 联系人角色
        /// </summary>
        public const string Contact = "Contact";

        /// <summary>
        /// 参与者角色
        /// </summary>
        public const string Participant = "Participant";

        // ========== PersonRelatesToWorkflow 关系的角色 ==========
        
        /// <summary>
        /// 工作流执行人角色
        /// </summary>
        public const string Executor = "Executor";

        /// <summary>
        /// 工作流审批人角色
        /// </summary>
        public const string Approver = "Approver";

        /// <summary>
        /// 工作流观察者角色
        /// </summary>
        public const string Observer = "Observer";

        /// <summary>
        /// PersonRelatesToCatalogue 关系支持的所有角色
        /// </summary>
        public static readonly string[] CatalogueRoles = 
        [ 
            Creator, Manager, ProjectManager, Reviewer, Expert, Responsible, Contact, Participant 
        ];

        /// <summary>
        /// PersonRelatesToWorkflow 关系支持的所有角色
        /// </summary>
        public static readonly string[] WorkflowRoles = 
        [ 
            Manager, Executor, Approver, Observer, Creator, Responsible 
        ];

        /// <summary>
        /// 所有支持的角色数组（合并 CatalogueRoles 和 WorkflowRoles）
        /// </summary>
        public static readonly string[] All = [.. CatalogueRoles.Union(WorkflowRoles).Distinct()];

        /// <summary>
        /// 检查角色是否有效（针对 PersonRelatesToCatalogue 关系）
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <returns>如果角色有效返回 true，否则返回 false</returns>
        public static bool IsValidCatalogueRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return CatalogueRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查角色是否有效（针对 PersonRelatesToWorkflow 关系）
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <returns>如果角色有效返回 true，否则返回 false</returns>
        public static bool IsValidWorkflowRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return WorkflowRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查角色是否有效（通用方法，检查所有支持的角色）
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <returns>如果角色有效返回 true，否则返回 false</returns>
        public static bool IsValid(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return false;

            return All.Contains(role, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 规范化角色（首字母大写，其余小写）
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <returns>规范化后的角色，如果无效返回 null</returns>
        public static string? Normalize(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return null;

            // 使用大小写不敏感比较查找匹配的常量值
            return All.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 规范化角色（针对特定关系类型）
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <param name="relationshipType">关系类型</param>
        /// <returns>规范化后的角色，如果无效返回 null</returns>
        public static string? Normalize(string? role, RelationshipType relationshipType)
        {
            if (string.IsNullOrWhiteSpace(role))
                return null;

            var validRoles = relationshipType switch
            {
                RelationshipType.PersonRelatesToCatalogue => CatalogueRoles,
                RelationshipType.PersonRelatesToWorkflow => WorkflowRoles,
                _ => All
            };

            return validRoles.FirstOrDefault(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 比较两个角色是否相等（大小写不敏感）
        /// </summary>
        /// <param name="role1">角色1</param>
        /// <param name="role2">角色2</param>
        /// <returns>如果相等返回 true，否则返回 false</returns>
        public static bool Equals(string? role1, string? role2)
        {
            if (string.IsNullOrWhiteSpace(role1) || string.IsNullOrWhiteSpace(role2))
                return false;

            return string.Equals(role1, role2, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 检查角色是否为空或空字符串
        /// </summary>
        /// <param name="role">角色字符串</param>
        /// <returns>如果为空或空字符串返回 true，否则返回 false</returns>
        public static bool IsNullOrEmpty(string? role)
        {
            return string.IsNullOrWhiteSpace(role);
        }
    }
}

