using Hx.Abp.Attachment.Application.Contracts;
using Hx.Abp.Attachment.Domain.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Hx.Abp.Attachment.HttpApi.Controllers
{
    /// <summary>
    /// 权限管理控制器 - 基于ABP vNext权限系统简化实现
    /// </summary>
    [Route("api/permissions")]
    [ApiController]
    [Authorize]
    public class PermissionController : AbpController
    {
        private readonly IPermissionService _permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        /// <summary>
        /// 检查用户权限
        /// </summary>
        [HttpGet("check")]
        public async Task<ActionResult<bool>> CheckPermission(
            [FromQuery] Guid userId,
            [FromQuery] Guid templateId,
            [FromQuery] PermissionAction action)
        {
            try
            {
                var hasPermission = await _permissionService.HasPermissionAsync(userId, templateId, action);
                return Ok(hasPermission);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 批量检查用户权限
        /// </summary>
        [HttpPost("check-batch")]
        public async Task<ActionResult<Dictionary<Guid, bool>>> CheckPermissionsBatch(
            [FromQuery] Guid userId,
            [FromQuery] PermissionAction action,
            [FromBody] List<Guid> templateIds)
        {
            try
            {
                var results = await _permissionService.HasPermissionsAsync(userId, templateIds, action);
                return Ok(results);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取模板权限配置
        /// </summary>
        [HttpGet("template/{templateId}")]
        public async Task<ActionResult<List<AttachCatalogueTemplatePermissionResultDto>>> GetTemplatePermissions(Guid templateId)
        {
            try
            {
                var permissions = await _permissionService.GetTemplatePermissionsAsync(templateId);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 设置模板权限
        /// </summary>
        [HttpPut("template/{templateId}")]
        [Authorize("Attachment.Templates.ManagePermissions")]
        public async Task<ActionResult> SetTemplatePermissions(
            Guid templateId,
            [FromBody] List<CreateAttachCatalogueTemplatePermissionDto> permissions)
        {
            try
            {
                await _permissionService.SetTemplatePermissionsAsync(templateId, permissions);
                return Ok(new { message = "权限设置成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 添加模板权限
        /// </summary>
        [HttpPost("template/{templateId}")]
        [Authorize("Attachment.Templates.ManagePermissions")]
        public async Task<ActionResult> AddTemplatePermission(
            Guid templateId,
            [FromBody] CreateAttachCatalogueTemplatePermissionDto permission)
        {
            try
            {
                await _permissionService.AddTemplatePermissionAsync(templateId, permission);
                return Ok(new { message = "权限添加成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 更新模板权限
        /// </summary>
        [HttpPut("template/{templateId}/permission/{permissionId}")]
        [Authorize("Attachment.Templates.ManagePermissions")]
        public async Task<ActionResult> UpdateTemplatePermission(
            Guid templateId,
            Guid permissionId,
            [FromBody] UpdateAttachCatalogueTemplatePermissionDto permission)
        {
            try
            {
                await _permissionService.UpdateTemplatePermissionAsync(templateId, permissionId, permission);
                return Ok(new { message = "权限更新成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 移除模板权限
        /// </summary>
        [HttpDelete("template/{templateId}/permission/{permissionId}")]
        [Authorize("Attachment.Templates.ManagePermissions")]
        public async Task<ActionResult> RemoveTemplatePermission(
            Guid templateId,
            Guid permissionId)
        {
            try
            {
                await _permissionService.RemoveTemplatePermissionAsync(templateId, permissionId);
                return Ok(new { message = "权限移除成功" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 获取模板权限摘要
        /// </summary>
        [HttpGet("template/{templateId}/summary")]
        public async Task<ActionResult<string>> GetTemplatePermissionSummary(Guid templateId)
        {
            try
            {
                var summary = await _permissionService.GetTemplatePermissionSummaryAsync(templateId);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 检查权限冲突
        /// </summary>
        [HttpGet("template/{templateId}/conflicts")]
        [Authorize("Attachment.Templates.ManagePermissions")]
        public async Task<ActionResult<List<string>>> CheckPermissionConflicts(Guid templateId)
        {
            try
            {
                var conflicts = await _permissionService.CheckPermissionConflictsAsync(templateId);
                return Ok(conflicts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 一行代码权限检查示例
        /// </summary>
        [HttpGet("simple-check")]
        public async Task<ActionResult<object>> SimplePermissionCheck(
            [FromQuery] Guid userId,
            [FromQuery] Guid templateId,
            [FromQuery] PermissionAction action)
        {
            try
            {
                var hasPermission = await _permissionService.HasPermissionAsync(userId, templateId, action);
                
                return Ok(new
                {
                    userId,
                    templateId,
                    action,
                    hasPermission,
                    message = hasPermission ? "用户具有权限" : "用户无权限",
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
