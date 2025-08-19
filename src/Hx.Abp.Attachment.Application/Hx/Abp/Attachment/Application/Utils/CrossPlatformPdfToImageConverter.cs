using Microsoft.Extensions.Logging;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Hx.Abp.Attachment.Application.Utils
{
    /// <summary>
    /// 跨平台PDF转图片工具类
    /// </summary>
    public class CrossPlatformPdfToImageConverter(ILogger<CrossPlatformPdfToImageConverter> logger)
    {
        private readonly ILogger<CrossPlatformPdfToImageConverter> _logger = logger;

        /// <summary>
        /// 将PDF文件转换为图片列表
        /// </summary>
        /// <param name="pdfFilePath">PDF文件路径</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="dpi">DPI分辨率</param>
        /// <returns>生成的图片文件路径列表</returns>
        public async Task<List<string>> ConvertPdfToImagesAsync(string pdfFilePath, string outputDirectory, string imageFormat = "jpg", int dpi = 300)
        {
            var imagePaths = new List<string>();
            
            try
            {
                if (!File.Exists(pdfFilePath))
                {
                    throw new FileNotFoundException($"PDF文件不存在: {pdfFilePath}");
                }

                // 确保输出目录存在
                Directory.CreateDirectory(outputDirectory);

                // 使用PdfPig加载PDF文档
                using var document = PdfDocument.Open(pdfFilePath);
                var pageCount = document.NumberOfPages;
                
                _logger.LogInformation("开始转换PDF文件 {FilePath}，共 {PageCount} 页", pdfFilePath, pageCount);

                // 设置DPI和缩放比例
                var scale = dpi / 72.0f; // PDF默认72 DPI

                for (int pageIndex = 1; pageIndex <= pageCount; pageIndex++)
                {
                    var imagePath = await ConvertPageToImageAsync(document, pageIndex, outputDirectory, imageFormat, scale);
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        imagePaths.Add(imagePath);
                    }
                }

                _logger.LogInformation("PDF转换完成，生成了 {ImageCount} 张图片", imagePaths.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF转图片失败: {FilePath}", pdfFilePath);
                throw;
            }

            return imagePaths;
        }

        /// <summary>
        /// 将PDF的单个页面转换为图片
        /// </summary>
        /// <param name="document">PDF文档</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="outputDirectory">输出目录</param>
        /// <param name="imageFormat">图片格式</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>生成的图片文件路径</returns>
        private async Task<string> ConvertPageToImageAsync(PdfDocument document, int pageNumber, string outputDirectory, string imageFormat, float scale)
        {
            try
            {
                var page = document.GetPage(pageNumber);
                
                // 使用标准A4尺寸作为默认值，然后根据DPI缩放
                // A4尺寸：595 x 842 points (72 DPI)
                var width = (int)(595 * scale);
                var height = (int)(842 * scale);

                // 生成输出文件名
                var fileName = $"page_{pageNumber:D4}.{imageFormat.ToLower()}";
                var outputPath = Path.Combine(outputDirectory, fileName);

                // 创建空白图片
                using var image = new Image<Rgba32>(width, height);
                
                // 设置白色背景
                image.Mutate(x => x.Fill(Color.White));

                // 渲染PDF页面内容（简化实现）
                await RenderPageContentAsync(image, page, scale);

                // 保存图片
                await SaveImageAsync(image, outputPath, imageFormat);

                _logger.LogDebug("页面 {PageNumber} 转换完成: {OutputPath}", pageNumber, outputPath);
                return outputPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "页面 {PageNumber} 转换失败", pageNumber);
                return string.Empty;
            }
        }

        // 替换 RenderPageContentAsync 方法中的 DrawText 调用，修复 CS1503 和 IDE0057
        private async Task RenderPageContentAsync(Image<Rgba32> image, Page page, float scale)
        {
            try
            {
                // 这里是一个简化的实现
                // 实际项目中，您可能需要更复杂的PDF渲染逻辑

                // 获取页面文本内容
                var text = page.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    // 限制文本长度，IDE0057: Substring 可以简化
                    var drawText = text.Length > 100 ? text[..100] : text;

                    // 在图片上绘制文本（修复 CS1503: 参数 2 需要 DrawingOptions）
                    await Task.Run(() =>
                    {
                        image.Mutate(x => x.DrawText(
                            new DrawingOptions(),
                            drawText,
                            SystemFonts.Collection.Families.First().CreateFont(12),
                            Color.Black,
                            new PointF(10, 10)
                        ));
                    });
                }

                // 可以在这里添加更多渲染逻辑，如：
                // - 绘制图形
                // - 处理图像
                // - 处理字体
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "渲染页面内容时出现警告");
            }
        }

        /// <summary>
        /// 保存图片到文件
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="outputPath">输出路径</param>
        /// <param name="imageFormat">图片格式</param>
        private static async Task SaveImageAsync(Image<Rgba32> image, string outputPath, string imageFormat)
        {
            await Task.Run(() =>
            {
                switch (imageFormat.ToLowerInvariant())
                {
                    case "jpg":
                    case "jpeg":
                        image.Save(outputPath, new JpegEncoder { Quality = 90 });
                        break;
                    case "png":
                        image.Save(outputPath, new PngEncoder());
                        break;
                    default:
                        image.Save(outputPath, new JpegEncoder { Quality = 90 });
                        break;
                }
            });
        }

        /// <summary>
        /// 清理临时图片文件
        /// </summary>
        /// <param name="imagePaths">图片文件路径列表</param>
        public async Task CleanupTempImagesAsync(List<string> imagePaths)
        {
            await Task.Run(() =>
            {
                foreach (var imagePath in imagePaths)
                {
                    try
                    {
                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                            _logger.LogDebug("删除临时图片文件: {ImagePath}", imagePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除临时图片文件失败: {ImagePath}", imagePath);
                    }
                }
            });
        }
    }
}
