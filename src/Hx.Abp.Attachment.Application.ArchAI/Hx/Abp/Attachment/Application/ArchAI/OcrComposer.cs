using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using System.Text.RegularExpressions;

namespace OcrTextComposer
{
    public static partial class OcrComposer
    {
        private static readonly Regex _meaninglessRegex = InvalidCharacter();   // 1. 过滤整串无意义字符

        public static string Compose(RecognizeCharacterDto page, double minProbability = 0.8)
        {
#pragma warning disable CS8604 // 引用类型参数可能为 null。
            var blocks = page.Results
                             .Where(b => b.Probability >= minProbability)
                             .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                             .Where(b => !_meaninglessRegex.IsMatch(b.Text)) // 2. 正则过滤
                             .ToList();
#pragma warning restore CS8604 // 引用类型参数可能为 null。

            // 3. 直接按坐标排序，阿里云OCR已经按正确顺序返回
            var orderedBlocks = blocks
                .OrderBy(b => b.TextRectangles?.Top ?? 0)      // 首先按Y坐标（从上到下）
                .ThenBy(b => b.TextRectangles?.Left ?? 0);     // 然后按X坐标（从左到右）

            // 4. 直接拼接文本
            return string.Join(Environment.NewLine,
                               orderedBlocks.Select(b => b.Text));
        }



        [GeneratedRegex(@"^[\s●×·•―~…]+$", RegexOptions.Compiled)]
        private static partial Regex InvalidCharacter();
    }
}
