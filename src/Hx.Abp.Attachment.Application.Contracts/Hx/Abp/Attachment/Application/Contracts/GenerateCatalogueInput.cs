using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class GenerateCatalogueInput
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public Guid TemplateId { get; set; }

        /// <summary>
        /// 业务引用
        /// </summary>
        public required string Reference { get; set; }

        /// <summary>
        /// 业务类型
        /// </summary>
        public int ReferenceType { get; set; }

        /// <summary>
        /// 上下文数据
        /// </summary>
        public Dictionary<string, object>? ContextData { get; set; }

        /// <summary>
        /// 根节点分类名称（用于修改生成的根节点分类名称）
        /// </summary>
        [StringLength(500, ErrorMessage = "分类名称长度不能超过500个字符")]
        public string? TemplateName { get; set; }

        /// <summary>
        /// 元数据字段集合（用于修改生成的根节点分类的元数据）
        /// </summary>
        public List<CreateUpdateMetaFieldDto>? MetaFields { get; set; }
    }
}
