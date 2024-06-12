using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Domain.Shared
{
    public static class FileHelper
    {
        public static int CalculatePdfPages(byte[]? pdfContent)
        {
            if (pdfContent == null)
            {
                return 0;
            }
            using var memoryStream = new MemoryStream(pdfContent);
            var reader = new PdfReader(memoryStream);
            return reader.NumberOfPages;
        }
        public static int CalculateFilePages(string fileType, byte[]? bytes)
        {
            return fileType switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tif" or ".tiff" => 1,
                ".pdf" => CalculatePdfPages(bytes),
                _ => 1,
            };
        }
    }
}
