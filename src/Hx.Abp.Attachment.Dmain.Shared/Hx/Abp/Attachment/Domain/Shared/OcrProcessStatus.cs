namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// OCR处理状态
    /// </summary>
    public enum OcrProcessStatus
    {
        /// <summary>
        /// 未处理
        /// </summary>
        NotProcessed = 0,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing = 1,

        /// <summary>
        /// 处理完成
        /// </summary>
        Completed = 2,

        /// <summary>
        /// 处理失败
        /// </summary>
        Failed = 3,

        /// <summary>
        /// 跳过（不支持的文件类型）
        /// </summary>
        Skipped = 4
    }
}
