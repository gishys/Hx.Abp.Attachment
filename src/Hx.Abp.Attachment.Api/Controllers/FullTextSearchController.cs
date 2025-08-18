using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Hx.Abp.Attachment.Application;
using Hx.Abp.Attachment.Application.Contracts;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.Api.Controllers
{
    /// <summary>
    /// 全文搜索控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FullTextSearchController : AbpController
    {
        private readonly FullTextSearchService _searchService;

        public FullTextSearchController(FullTextSearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// 全文搜索目录
        /// </summary>
        [HttpGet("catalogues")]
        public async Task<IActionResult> SearchCatalogues([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.SearchCataloguesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 全文搜索文件
        /// </summary>
        [HttpGet("files")]
        public async Task<IActionResult> SearchFiles([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.SearchFilesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 模糊搜索目录
        /// </summary>
        [HttpGet("catalogues/fuzzy")]
        public async Task<IActionResult> FuzzySearchCatalogues([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.FuzzySearchCataloguesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 模糊搜索文件
        /// </summary>
        [HttpGet("files/fuzzy")]
        public async Task<IActionResult> FuzzySearchFiles([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.FuzzySearchFilesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 组合搜索目录
        /// </summary>
        [HttpGet("catalogues/combined")]
        public async Task<IActionResult> CombinedSearchCatalogues([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.CombinedSearchCataloguesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 组合搜索文件
        /// </summary>
        [HttpGet("files/combined")]
        public async Task<IActionResult> CombinedSearchFiles([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("搜索关键词不能为空");

            var results = await _searchService.CombinedSearchFilesAsync(query);
            return Ok(results);
        }

        /// <summary>
        /// 测试全文搜索功能
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestSearch([FromQuery] string text = "测试中文搜索")
        {
            var result = await _searchService.TestFullTextSearchAsync(text);
            return Ok(new { 
                Input = text, 
                Result = result,
                Message = "全文搜索功能测试完成"
            });
        }
    }
}
