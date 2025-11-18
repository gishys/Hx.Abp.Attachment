namespace Hx.Abp.Attachment.Application.Contracts
{
    public class AttachFileCreateDto
    {
        /// <summary>
        /// 文件别名
        /// </summary>
        public required virtual string FileAlias { get; set; }
        /// <summary>
        /// 文件内容
        /// </summary>
        public required virtual byte[] DocumentContent { get; set; }
        /// <summary>
        /// 序号（可选，如果为空则自动分配）
        /// </summary>
        public virtual int? SequenceNumber { get; set; }
        /// <summary>
        /// 动态分面分类名称（可选，用于标识该文件属于哪个动态分面）
        /// 如果提供此字段，文件将被分配到对应的动态分面分类下
        /// </summary>
        public virtual string? DynamicFacetCatalogueName { get; set; }

        /// <summary>
        /// 子文件夹路径（可选，相对于动态分面文件夹的路径，如 "材料类型/正本"）
        /// 用于动态创建子分类，支持嵌套文件夹结构
        /// </summary>
        public virtual string? SubFolderPath { get; set; }
    }
}
