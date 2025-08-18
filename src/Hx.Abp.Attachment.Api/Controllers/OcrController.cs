using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Domain.Repositories;

namespace Hx.Abp.Attachment.Api.Controllers
{
    /// <summary>
    /// OCR处理控制器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class OcrController : AbpController
    {
        private readonly IOcrService _ocrService;
        private readonly IRepository<AttachFile, Guid> _fileRepository;
        private readonly IRepository<AttachCatalogue, Guid> _catalogueRepository;

        public OcrController(
            IOcrService ocrService,
            IRepository<AttachFile, Guid> fileRepository,
            IRepository<AttachCatalogue, Guid> catalogueRepository)
        {
            _ocrService = ocrService;
            _fileRepository = fileRepository;
            _catalogueRepository = catalogueRepository;
        }

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
                if (fileIds == null || !fileIds.Any())
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
    }
}
