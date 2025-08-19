using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// 全文搜索控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FullTextSearchController(IFullTextSearchAppService fullTextSearchAppService) : AbpController
    {
        private readonly IFullTextSearchAppService _fullTextSearchAppService = fullTextSearchAppService;

        /// <summary>
        /// 全文搜索目录
        /// </summary>
        [HttpPost("catalogues")]
        public async Task<IActionResult> SearchCatalogues([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.SearchCataloguesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"搜索目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        [HttpPost("files")]
        public async Task<IActionResult> SearchFiles([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.SearchFilesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"搜索文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        [HttpPost("catalogues/fuzzy")]
        public async Task<IActionResult> FuzzySearchCatalogues([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.FuzzySearchCataloguesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"模糊搜索目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        [HttpPost("files/fuzzy")]
        public async Task<IActionResult> FuzzySearchFiles([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.FuzzySearchFilesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"模糊搜索文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 组合搜索目录（全文 + 模糊）
        /// </summary>
        [HttpPost("catalogues/combined")]
        public async Task<IActionResult> CombinedSearchCatalogues([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.CombinedSearchCataloguesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"组合搜索目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 组合搜索文件（全文 + 模糊）
        /// </summary>
        [HttpPost("files/combined")]
        public async Task<IActionResult> CombinedSearchFiles([FromBody] FullTextSearchInputDto input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input.Query))
                    return BadRequest("搜索查询不能为空");

                var results = await _fullTextSearchAppService.CombinedSearchFilesAsync(input);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"组合搜索文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        [HttpGet("test/fulltext")]
        public async Task<IActionResult> TestFullTextSearch([FromQuery] string testText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testText))
                    return BadRequest("测试文本不能为空");

                var result = await _fullTextSearchAppService.TestFullTextSearchAsync(testText);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"测试全文搜索失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试模糊搜索功能
        /// </summary>
        [HttpGet("test/fuzzy")]
        public async Task<IActionResult> TestFuzzySearch([FromQuery] string testText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(testText))
                    return BadRequest("测试文本不能为空");

                var result = await _fullTextSearchAppService.TestFuzzySearchAsync(testText);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"测试模糊搜索失败: {ex.Message}");
            }
        }
    }
}
