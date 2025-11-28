namespace Hx.Abp.Attachment.Domain.Shared.KnowledgeGraph
{
    /// <summary>
    /// 知识图谱实体类型常量
    /// 统一管理所有实体类型字符串值，确保代码中实体类型使用的一致性
    /// </summary>
    public static class EntityType
    {
        /// <summary>
        /// 分类实体类型（对应 AttachCatalogue）
        /// </summary>
        public const string Catalogue = "Catalogue";

        /// <summary>
        /// 人员实体类型（对应 IdentityUser）
        /// </summary>
        public const string Person = "Person";

        /// <summary>
        /// 部门实体类型（对应 OrganizationUnit）
        /// </summary>
        public const string Department = "Department";

        /// <summary>
        /// 业务实体类型（外部业务实体）
        /// </summary>
        public const string BusinessEntity = "BusinessEntity";

        /// <summary>
        /// 工作流实体类型
        /// </summary>
        public const string Workflow = "Workflow";

        /// <summary>
        /// 所有支持的实体类型数组
        /// </summary>
        public static readonly string[] All = { Catalogue, Person, Department, BusinessEntity, Workflow };

        /// <summary>
        /// 检查实体类型是否有效
        /// </summary>
        /// <param name="entityType">实体类型字符串</param>
        /// <returns>如果实体类型有效返回 true，否则返回 false</returns>
        public static bool IsValid(string? entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return false;

            return All.Contains(entityType, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 规范化实体类型（首字母大写，其余小写）
        /// </summary>
        /// <param name="entityType">实体类型字符串</param>
        /// <returns>规范化后的实体类型，如果无效返回 null</returns>
        public static string? Normalize(string? entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                return null;

            // 使用大小写不敏感比较查找匹配的常量值
            return All.FirstOrDefault(e => string.Equals(e, entityType, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 比较两个实体类型是否相等（大小写不敏感）
        /// </summary>
        /// <param name="type1">实体类型1</param>
        /// <param name="type2">实体类型2</param>
        /// <returns>如果相等返回 true，否则返回 false</returns>
        public static bool Equals(string? type1, string? type2)
        {
            if (string.IsNullOrWhiteSpace(type1) || string.IsNullOrWhiteSpace(type2))
                return false;

            return string.Equals(type1, type2, StringComparison.OrdinalIgnoreCase);
        }
    }
}

