using Hx.Abp.Attachment.Domain.Shared;
using Volo.Abp.Domain.Services;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 附件文件管理领域服务接口
    /// 负责文件OCR处理、文本块管理等核心业务逻辑
    /// </summary>
    public interface IAttachFileManager : IDomainService
    {
        /// <summary>
        /// 检查文件是否支持OCR处理
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <returns>是否支持OCR</returns>
        bool IsSupportedFileType(string fileType);

        /// <summary>
        /// 检查文件是否已完成OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <returns>是否已完成OCR处理</returns>
        bool IsOcrProcessed(AttachFile attachFile);

        /// <summary>
        /// 开始OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        void StartOcrProcessing(AttachFile attachFile);

        /// <summary>
        /// 完成OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="extractedText">提取的文本内容</param>
        /// <param name="textBlocks">文本块集合</param>
        void CompleteOcrProcessing(AttachFile attachFile, string extractedText, List<OcrTextBlock>? textBlocks = null);

        /// <summary>
        /// 标记OCR处理失败
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="reason">失败原因</param>
        void MarkOcrProcessingFailed(AttachFile attachFile, string? reason = null);

        /// <summary>
        /// 跳过OCR处理
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        /// <param name="reason">跳过原因</param>
        void SkipOcrProcessing(AttachFile attachFile, string? reason = null);

        /// <summary>
        /// 清除文件的OCR内容
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        void ClearOcrContent(AttachFile attachFile);

        /// <summary>
        /// 重新处理OCR
        /// </summary>
        /// <param name="attachFile">附件文件</param>
        void ResetOcrProcessing(AttachFile attachFile);

        /// <summary>
        /// 更新分类的全文内容
        /// </summary>
        /// <param name="catalogue">附件分类</param>
        void UpdateCatalogueFullTextContent(AttachCatalogue catalogue);
    }
}
