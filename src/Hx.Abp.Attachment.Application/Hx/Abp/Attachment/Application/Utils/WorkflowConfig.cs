using System.Text.Json;

namespace Hx.Abp.Attachment.Application.Utils
{
    /// <summary>
    /// 工作流配置
    /// </summary>
    public class WorkflowConfig
    {
        /// <summary>
        /// 是否启用OCR识别
        /// </summary>
        public bool EnableOcr { get; set; } = false;

        /// <summary>
        /// OCR识别配置
        /// </summary>
        public OcrWorkflowConfig? OcrConfig { get; set; }

        public static JsonSerializerOptions GetOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// 从JSON字符串解析工作流配置
        /// </summary>
        /// <param name="jsonString">JSON字符串</param>
        /// <returns>工作流配置对象</returns>
        public static WorkflowConfig? ParseFromJson(string? jsonString, JsonSerializerOptions options)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
                return null;

            try
            {
                return JsonSerializer.Deserialize<WorkflowConfig>(jsonString, options);
            }
            catch (JsonException)
            {
                // 如果JSON解析失败，返回null
                return null;
            }
        }

        /// <summary>
        /// 检查是否启用OCR识别
        /// </summary>
        /// <returns>是否启用OCR</returns>
        public bool IsOcrEnabled()
        {
            return EnableOcr && OcrConfig != null;
        }

        /// <summary>
        /// 获取OCR配置，如果未配置则返回默认配置
        /// </summary>
        /// <returns>OCR配置</returns>
        public OcrWorkflowConfig GetOcrConfigOrDefault()
        {
            return OcrConfig ?? new OcrWorkflowConfig();
        }
    }

    /// <summary>
    /// OCR工作流配置
    /// </summary>
    public class OcrWorkflowConfig
    {
        /// <summary>
        /// 是否启用OCR识别
        /// </summary>
        public bool EnableOcr { get; set; } = true;

        /// <summary>
        /// 支持的文件类型（为空则使用默认支持的类型）
        /// </summary>
        public List<string>? SupportedFileTypes { get; set; }

        /// <summary>
        /// 最小文件大小（字节），小于此大小的文件不进行OCR
        /// </summary>
        public long? MinFileSize { get; set; }

        /// <summary>
        /// 最大文件大小（字节），大于此大小的文件不进行OCR
        /// </summary>
        public long? MaxFileSize { get; set; }

        /// <summary>
        /// 检查文件是否支持OCR
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <param name="fileSize">文件大小</param>
        /// <returns>是否支持OCR</returns>
        public bool IsFileSupportedForOcr(string fileType, long fileSize)
        {
            if (!EnableOcr)
                return false;

            // 检查文件类型
            if (SupportedFileTypes != null && SupportedFileTypes.Count > 0)
            {
                var lowerFileType = fileType.ToLowerInvariant();
                if (!SupportedFileTypes.Any(t => t.Equals(lowerFileType, StringComparison.InvariantCultureIgnoreCase)))
                    return false;
            }

            // 检查文件大小
            if (MinFileSize.HasValue && fileSize < MinFileSize.Value)
                return false;

            if (MaxFileSize.HasValue && fileSize > MaxFileSize.Value)
                return false;

            return true;
        }
    }
}
