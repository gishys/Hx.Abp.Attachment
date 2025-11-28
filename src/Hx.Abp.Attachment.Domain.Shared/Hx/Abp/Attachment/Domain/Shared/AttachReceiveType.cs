namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 收取附件类型
    /// </summary>
    public enum AttachReceiveType
    {
        /// <summary>
        /// 原件
        /// </summary>
        OriginalScript = 1,
        /// <summary>
        ///复印件
        /// </summary>
        Copy = 2,
        /// <summary>
        ///原件副本
        /// </summary>
        Duplicate = 3,
        /// <summary>
        ///副本复印件
        /// </summary>
        DuplicateCopy = 4,
        /// <summary>
        ///手稿
        /// </summary>
        Manuscript = 5,
        /// <summary>
        ///原件或复印件
        /// </summary>
        OriginalScriptOrCopy = 6,
        /// <summary>
        ///其它
        /// </summary>
        Other = 99
    }
}
