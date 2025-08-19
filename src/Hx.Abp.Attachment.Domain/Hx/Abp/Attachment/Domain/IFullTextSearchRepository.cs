using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// 全文搜索仓储接口
    /// </summary>
    public interface IFullTextSearchRepository : IRepository
    {
        /// <summary>
        /// 全文搜索目录
        /// </summary>
        Task<List<AttachCatalogue>> SearchCataloguesAsync(string query);

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        Task<List<AttachFile>> SearchFilesAsync(string query);

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query);

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        Task<List<AttachFile>> FuzzySearchFilesAsync(string query);

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        Task<List<AttachCatalogue>> CombinedSearchCataloguesAsync(string query);

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        Task<List<AttachFile>> CombinedSearchFilesAsync(string query);

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        Task<string> TestFullTextSearchAsync(string testText);

        /// <summary>
        /// 测试模糊搜索功能
        /// </summary>
        Task<string> TestFuzzySearchAsync(string testText);
    }
}
