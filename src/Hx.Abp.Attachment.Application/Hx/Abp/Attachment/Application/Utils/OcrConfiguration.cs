namespace Hx.Abp.Attachment.Application.Utils
{
    /// <summary>
    /// OCR配置
    /// </summary>
    public class OcrConfiguration
    {
        /// <summary>
        /// 阿里云OCR配置
        /// </summary>
        public AliyunOcrConfig AliyunOcr { get; set; } = new();

        /// <summary>
        /// PDF转图片配置
        /// </summary>
        public PdfToImageConfig PdfToImage { get; set; } = new();

        /// <summary>
        /// 文件服务器配置
        /// </summary>
        public FileServerConfig FileServer { get; set; } = new();
    }

    /// <summary>
    /// 阿里云OCR配置
    /// </summary>
    public class AliyunOcrConfig
    {
        /// <summary>
        /// Access Key ID
        /// </summary>
        public string AccessKeyId { get; set; } = string.Empty;

        /// <summary>
        /// Access Key Secret
        /// </summary>
        public string AccessKeySecret { get; set; } = string.Empty;

        /// <summary>
        /// 端点
        /// </summary>
        public string Endpoint { get; set; } = "ocr.cn-shanghai.aliyuncs.com";

        /// <summary>
        /// 最小置信度
        /// </summary>
        public double MinConfidence { get; set; } = 0.8;

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;
    }

    /// <summary>
    /// PDF转图片配置
    /// </summary>
    public class PdfToImageConfig
    {
        /// <summary>
        /// 输出图片格式
        /// </summary>
        public string ImageFormat { get; set; } = "jpg";

        /// <summary>
        /// DPI分辨率
        /// </summary>
        public int Dpi { get; set; } = 300;

        /// <summary>
        /// 临时目录
        /// </summary>
        public string TempDirectory { get; set; } = string.Empty;

        /// <summary>
        /// 是否保留临时文件（调试用）
        /// </summary>
        public bool KeepTempFiles { get; set; } = false;

        /// <summary>
        /// 最大并发处理数
        /// </summary>
        public int MaxConcurrency { get; set; } = 3;
    }

    /// <summary>
    /// 文件服务器配置
    /// </summary>
    public class FileServerConfig
    {
        /// <summary>
        /// 基础URL
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:5000";

        /// <summary>
        /// 基础路径
        /// </summary>
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// 附件目录
        /// </summary>
        public string AttachmentPath { get; set; } = "host/attachment";
    }
}
