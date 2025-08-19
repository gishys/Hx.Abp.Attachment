namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 目录搜索结果DTO
    /// </summary>
    public class CatalogueSearchResultDto
    {
        /// <summary>
        /// 目录ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 目录名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// 目录描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 父目录ID
        /// </summary>
        public Guid? ParentCatalogueId { get; set; }

        /// <summary>
        /// 全文内容
        /// </summary>
        public string? FullTextContent { get; set; }

        /// <summary>
        /// 附件数量
        /// </summary>
        public int AttachCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; set; }

        /// <summary>
        /// 搜索匹配分数
        /// </summary>
        public float SearchScore { get; set; }

        /// <summary>
        /// 匹配的文本片段
        /// </summary>
        public string? MatchedText { get; set; }
    }

    /// <summary>
    /// 文件搜索结果DTO
    /// </summary>
    public class FileSearchResultDto
    {
        /// <summary>
        /// 文件ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 所属目录ID
        /// </summary>
        public Guid? AttachCatalogueId { get; set; }

        /// <summary>
        /// 所属目录名称
        /// </summary>
        public string CatalogueName { get; set; } = string.Empty;

        /// <summary>
        /// OCR内容
        /// </summary>
        public string? OcrContent { get; set; }

        /// <summary>
        /// OCR处理状态
        /// </summary>
        public string OcrProcessStatus { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastModificationTime { get; set; }

        /// <summary>
        /// 搜索匹配分数
        /// </summary>
        public float SearchScore { get; set; }

        /// <summary>
        /// 匹配的文本片段
        /// </summary>
        public string? MatchedText { get; set; }
    }

    /// <summary>
    /// 全文搜索输入DTO
    /// </summary>
    public class FullTextSearchInputDto
    {
        /// <summary>
        /// 搜索查询
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 最大结果数
        /// </summary>
        public int MaxResultCount { get; set; } = 50;

        /// <summary>
        /// 跳过数量
        /// </summary>
        public int SkipCount { get; set; } = 0;

        /// <summary>
        /// 是否包含已删除
        /// </summary>
        public bool IncludeDeleted { get; set; } = false;
    }

    /// <summary>
    /// 全文搜索结果DTO
    /// </summary>
    public class FullTextSearchResultDto<T>
    {
        /// <summary>
        /// 搜索结果列表
        /// </summary>
        public List<T> Items { get; set; } = [];

        /// <summary>
        /// 总数量
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// 搜索查询
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// 搜索类型
        /// </summary>
        public string SearchType { get; set; } = string.Empty;

        /// <summary>
        /// 搜索时间
        /// </summary>
        public DateTime SearchTime { get; set; } = DateTime.UtcNow;
    }
}
