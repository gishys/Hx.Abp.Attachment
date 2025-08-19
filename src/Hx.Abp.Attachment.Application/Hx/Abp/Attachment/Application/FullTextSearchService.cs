using Hx.Abp.Attachment.Domain;
using Volo.Abp.Domain.Services;

namespace Hx.Abp.Attachment.Application
{
    public class FullTextSearchService(IFullTextSearchRepository searchRepository) : DomainService
    {
        private readonly IFullTextSearchRepository _searchRepository = searchRepository;

        /// <summary>
        /// 全文搜索目录
        /// </summary>
        public async Task<List<AttachCatalogue>> SearchCataloguesAsync(string query)
        {
            return await _searchRepository.SearchCataloguesAsync(query);
        }

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        public async Task<List<AttachFile>> SearchFilesAsync(string query)
        {
            return await _searchRepository.SearchFilesAsync(query);
        }

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        public async Task<List<AttachCatalogue>> FuzzySearchCataloguesAsync(string query)
        {
            return await _searchRepository.FuzzySearchCataloguesAsync(query);
        }

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        public async Task<List<AttachFile>> FuzzySearchFilesAsync(string query)
        {
            return await _searchRepository.FuzzySearchFilesAsync(query);
        }

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        public async Task<List<AttachCatalogue>> CombinedSearchCataloguesAsync(string query)
        {
            return await _searchRepository.CombinedSearchCataloguesAsync(query);
        }

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        public async Task<List<AttachFile>> CombinedSearchFilesAsync(string query)
        {
            return await _searchRepository.CombinedSearchFilesAsync(query);
        }

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        public async Task<string> TestFullTextSearchAsync(string testText)
        {
            return await _searchRepository.TestFullTextSearchAsync(testText);
        }

        /// <summary>
        /// 测试模糊搜索功能
        /// </summary>
        public async Task<string> TestFuzzySearchAsync(string testText)
        {
            return await _searchRepository.TestFuzzySearchAsync(testText);
        }
    }
}
