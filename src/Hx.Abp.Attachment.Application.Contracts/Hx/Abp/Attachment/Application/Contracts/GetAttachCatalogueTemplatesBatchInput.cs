using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 批量获取模板输入参数
    /// </summary>
    public class GetAttachCatalogueTemplatesBatchInput
    {
        /// <summary>
        /// 模板ID列表
        /// </summary>
        [Required(ErrorMessage = "模板ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要提供一个模板ID")]
        [MaxLength(100, ErrorMessage = "单次最多支持100个模板ID")]
        public List<Guid> Ids { get; set; } = [];

        /// <summary>
        /// 是否包含树形结构（默认false）
        /// </summary>
        public bool IncludeTreeStructure { get; set; } = false;

        /// <summary>
        /// 是否返回根节点（默认false）
        /// </summary>
        public bool ReturnRoot { get; set; } = false;
    }
}
