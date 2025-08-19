using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application
{
    /// <summary>
    /// 全文搜索应用服务
    /// </summary>
    public class FullTextSearchAppService(IFullTextSearchRepository searchRepository) : ApplicationService, IFullTextSearchAppService
    {
        private readonly IFullTextSearchRepository _searchRepository = searchRepository;

        /// <summary>
        /// 全文搜索目录
        /// </summary>
        public async Task<FullTextSearchResultDto<CatalogueSearchResultDto>> SearchCataloguesAsync(FullTextSearchInputDto input)
        {
            var catalogues = await _searchRepository.SearchCataloguesAsync(input.Query);
            var result = new FullTextSearchResultDto<CatalogueSearchResultDto>
            {
                Query = input.Query,
                SearchType = "FullText",
                TotalCount = catalogues.Count,
                Items = [.. catalogues.Select(MapToCatalogueSearchResultDto)]
            };
            return result;
        }

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        public async Task<FullTextSearchResultDto<FileSearchResultDto>> SearchFilesAsync(FullTextSearchInputDto input)
        {
            var files = await _searchRepository.SearchFilesAsync(input.Query);
            var result = new FullTextSearchResultDto<FileSearchResultDto>
            {
                Query = input.Query,
                SearchType = "FullText",
                TotalCount = files.Count,
                Items = [.. files.Select(MapToFileSearchResultDto)]
            };
            return result;
        }

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        public async Task<FullTextSearchResultDto<CatalogueSearchResultDto>> FuzzySearchCataloguesAsync(FullTextSearchInputDto input)
        {
            var catalogues = await _searchRepository.FuzzySearchCataloguesAsync(input.Query);
            var result = new FullTextSearchResultDto<CatalogueSearchResultDto>
            {
                Query = input.Query,
                SearchType = "Fuzzy",
                TotalCount = catalogues.Count,
                Items = [.. catalogues.Select(MapToCatalogueSearchResultDto)]
            };
            return result;
        }

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        public async Task<FullTextSearchResultDto<FileSearchResultDto>> FuzzySearchFilesAsync(FullTextSearchInputDto input)
        {
            var files = await _searchRepository.FuzzySearchFilesAsync(input.Query);
            var result = new FullTextSearchResultDto<FileSearchResultDto>
            {
                Query = input.Query,
                SearchType = "Fuzzy",
                TotalCount = files.Count,
                Items = [.. files.Select(MapToFileSearchResultDto)]
            };
            return result;
        }

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        public async Task<FullTextSearchResultDto<CatalogueSearchResultDto>> CombinedSearchCataloguesAsync(FullTextSearchInputDto input)
        {
            var catalogues = await _searchRepository.CombinedSearchCataloguesAsync(input.Query);
            var result = new FullTextSearchResultDto<CatalogueSearchResultDto>
            {
                Query = input.Query,
                SearchType = "Combined",
                TotalCount = catalogues.Count,
                Items = [.. catalogues.Select(MapToCatalogueSearchResultDto)]
            };
            return result;
        }

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        public async Task<FullTextSearchResultDto<FileSearchResultDto>> CombinedSearchFilesAsync(FullTextSearchInputDto input)
        {
            var files = await _searchRepository.CombinedSearchFilesAsync(input.Query);
            var result = new FullTextSearchResultDto<FileSearchResultDto>
            {
                Query = input.Query,
                SearchType = "Combined",
                TotalCount = files.Count,
                Items = [.. files.Select(MapToFileSearchResultDto)]
            };
            return result;
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

        /// <summary>
        /// 映射到目录搜索结果DTO
        /// </summary>
        private static CatalogueSearchResultDto MapToCatalogueSearchResultDto(AttachCatalogue catalogue)
        {
            return new CatalogueSearchResultDto
            {
                Id = catalogue.Id,
                CatalogueName = catalogue.CatalogueName,
                ParentCatalogueId = catalogue.ParentId,
                FullTextContent = catalogue.FullTextContent,
                AttachCount = catalogue.AttachCount,
                CreationTime = catalogue.CreationTime,
                LastModificationTime = catalogue.LastModificationTime,
                SearchScore = 1.0f, // 默认分数，实际应该从搜索结果中获取
                MatchedText = catalogue.FullTextContent // 简化处理，实际应该提取匹配片段
            };
        }

        /// <summary>
        /// 映射到文件搜索结果DTO
        /// </summary>
        private static FileSearchResultDto MapToFileSearchResultDto(AttachFile file)
        {
            return new FileSearchResultDto
            {
                Id = file.Id,
                FileName = file.FileName,
                FileType = file.FileType,
                FileSize = file.FileSize,
                AttachCatalogueId = file.AttachCatalogueId,
                CatalogueName = string.Empty,
                OcrContent = file.OcrContent,
                OcrProcessStatus = file.OcrProcessStatus.ToString(),
                CreationTime = file.CreationTime,
                LastModificationTime = file.LastModificationTime,
                SearchScore = 1.0f, // 默认分数，实际应该从搜索结果中获取
                MatchedText = file.OcrContent // 简化处理，实际应该提取匹配片段
            };
        }
    }
}
