using System.ComponentModel.DataAnnotations;

namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 确定文件分类请求
    /// </summary>
    public class ConfirmFileClassificationRequest
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        [Required]
        public Guid FileId { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        [Required]
        public Guid CatalogueId { get; set; }

        /// <summary>
        /// OCR全文内容（可选）
        /// </summary>
        public string? OcrContent { get; set; }
    }
}
