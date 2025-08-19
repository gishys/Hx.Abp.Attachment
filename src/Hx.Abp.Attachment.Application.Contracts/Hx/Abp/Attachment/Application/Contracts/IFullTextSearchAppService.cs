namespace Hx.Abp.Attachment.Application.Contracts
{
    /// <summary>
    /// 全文搜索应用服务接口
    /// </summary>
    public interface IFullTextSearchAppService
    {
        /// <summary>
        /// 全文搜索目录
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<CatalogueSearchResultDto>> SearchCataloguesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<FileSearchResultDto>> SearchFilesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<CatalogueSearchResultDto>> FuzzySearchCataloguesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<FileSearchResultDto>> FuzzySearchFilesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<CatalogueSearchResultDto>> CombinedSearchCataloguesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        /// <param name="input">搜索输入</param>
        /// <returns>搜索结果</returns>
        Task<FullTextSearchResultDto<FileSearchResultDto>> CombinedSearchFilesAsync(FullTextSearchInputDto input);

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        /// <param name="testText">测试文本</param>
        /// <returns>测试结果</returns>
        Task<string> TestFullTextSearchAsync(string testText);

        /// <summary>
        /// 测试模糊搜索功能
        /// </summary>
        /// <param name="testText">测试文本</param>
        /// <returns>测试结果</returns>
        Task<string> TestFuzzySearchAsync(string testText);
    }
}
