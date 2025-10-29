using Hx.Abp.Attachment.Domain.Shared;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 叶子分类选项DTO
    /// 用于返回可分类的叶子节点信息，包含分类ID和分类名称
    /// </summary>
    public class LeafCategoryOptionDto
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 分类路径（可选，用于显示层级关系）
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// 序号（用于排序）
        /// </summary>
        public int SequenceNumber { get; set; }

        /// <summary>
        /// 父分类ID（可选）
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 业务引用（可选）
        /// </summary>
        public string? Reference { get; set; }

        /// <summary>
        /// 模板用途（可选）
        /// </summary>
        public TemplatePurpose? TemplatePurpose { get; set; }
    }
}
