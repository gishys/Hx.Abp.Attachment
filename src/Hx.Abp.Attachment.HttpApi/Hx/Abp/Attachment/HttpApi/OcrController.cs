using Hx.Abp.Attachment.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi
{
    /// <summary>
    /// OCR处理控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OcrController(
        IOcrService ocrService) : AbpController
    {
        private readonly IOcrService _ocrService = ocrService;

        /// <summary>
        /// 处理单个文件的OCR
        /// </summary>
        [HttpPost("files/{fileId}")]
        public async Task<IActionResult> ProcessFile(Guid fileId)
        {
            try
            {
                var result = await _ocrService.ProcessFileAsync(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"OCR处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量处理文件的OCR
        /// </summary>
        [HttpPost("files/batch")]
        public async Task<IActionResult> ProcessFiles([FromBody] List<Guid> fileIds)
        {
            try
            {
                if (fileIds == null || fileIds.Count == 0)
                    return BadRequest("文件ID列表不能为空");

                var results = await _ocrService.ProcessFilesAsync(fileIds);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest($"批量OCR处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理目录下所有文件的OCR
        /// </summary>
        [HttpPost("catalogues/{catalogueId}")]
        public async Task<IActionResult> ProcessCatalogue(Guid catalogueId)
        {
            try
            {
                var result = await _ocrService.ProcessCatalogueAsync(catalogueId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"目录OCR处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查文件是否支持OCR
        /// </summary>
        [HttpGet("files/{fileId}/supported")]
        public async Task<IActionResult> IsFileSupported(Guid fileId)
        {
            try
            {
                var result = await _ocrService.GetFileOcrStatusAsync(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"检查文件支持状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件的OCR内容
        /// </summary>
        [HttpGet("files/{fileId}/content")]
        public async Task<IActionResult> GetFileOcrContent(Guid fileId)
        {
            try
            {
                var result = await _ocrService.GetFileOcrContentAsync(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取OCR内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件的OCR内容（包含文本块信息）
        /// </summary>
        [HttpGet("files/{fileId}/content-with-blocks")]
        public async Task<IActionResult> GetFileOcrContentWithBlocks(Guid fileId)
        {
            try
            {
                var result = await _ocrService.GetFileOcrContentWithBlocksAsync(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取OCR内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文件的文本块列表
        /// </summary>
        [HttpGet("files/{fileId}/text-blocks")]
        public async Task<IActionResult> GetFileTextBlocks(Guid fileId)
        {
            try
            {
                var result = await _ocrService.GetFileTextBlocksAsync(fileId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取文本块失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取文本块详情
        /// </summary>
        [HttpGet("text-blocks/{textBlockId}")]
        public async Task<IActionResult> GetTextBlock(Guid textBlockId)
        {
            try
            {
                var result = await _ocrService.GetTextBlockAsync(textBlockId);
                if (result == null)
                {
                    return NotFound($"文本块不存在: {textBlockId}");
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取文本块详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取目录的全文内容
        /// </summary>
        [HttpGet("catalogues/{catalogueId}/content")]
        public async Task<IActionResult> GetCatalogueFullTextContent(Guid catalogueId)
        {
            try
            {
                var result = await _ocrService.GetCatalogueFullTextAsync(catalogueId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取全文内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取目录的OCR内容（包含文本块信息）
        /// </summary>
        [HttpGet("catalogues/{catalogueId}/content-with-blocks")]
        public async Task<IActionResult> GetCatalogueOcrContentWithBlocks(Guid catalogueId)
        {
            try
            {
                var result = await _ocrService.GetCatalogueOcrContentWithBlocksAsync(catalogueId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取目录OCR内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取OCR统计信息
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetOcrStatistics()
        {
            try
            {
                var result = await _ocrService.GetOcrStatisticsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"获取OCR统计信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理孤立的文本块
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupOrphanedTextBlocks()
        {
            try
            {
                var result = await _ocrService.CleanupOrphanedTextBlocksAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"清理孤立文本块失败: {ex.Message}");
            }
        }
    }
}
