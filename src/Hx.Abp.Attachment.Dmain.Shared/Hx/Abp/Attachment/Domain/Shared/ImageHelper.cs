using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Hx.Abp.Attachment.Domain.Shared
{
    public static class ImageHelper
    {
        public async static Task<byte[]> ConvertTiffToImage(byte[] file)
        {
            byte[] bmpBytes;
            using (var image = Image.Load<Rgba32>(file))
            {
                using var bmp = new MemoryStream();
                await image.SaveAsJpegAsync(bmp, new JpegEncoder() { Quality = 100 });
                bmpBytes = bmp.ToArray();
            }
            return bmpBytes;
        }
    }
}