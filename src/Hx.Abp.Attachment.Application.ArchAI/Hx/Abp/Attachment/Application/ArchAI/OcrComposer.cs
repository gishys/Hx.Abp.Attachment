using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using System.Text.RegularExpressions;

namespace OcrTextComposer
{
    public static partial class OcrComposer
    {
        private static readonly Regex _meaninglessRegex = InvalidCharacter();   // 1. 过滤整串无意义字符

        public static string Compose(RecognizeCharacterDto page, double minProbability = 0.8)
        {
            var lines = new List<TextLine>();

#pragma warning disable CS8604 // 引用类型参数可能为 null。
            var blocks = page.Results
                             .Where(b => b.Probability >= minProbability)
                             .Where(b => !string.IsNullOrWhiteSpace(b.Text))
                             .Where(b => !_meaninglessRegex.IsMatch(b.Text)) // 2. 正则过滤
                             .ToList();
#pragma warning restore CS8604 // 引用类型参数可能为 null。

            foreach (var block in blocks)
            {
                var rect = block.TextRectangles;
                if (rect == null) continue;
                int angle = rect.Angle;
                var text = block.Text;

                // 4. 把块加入虚拟行
                var line = lines.FirstOrDefault(l => l.CanMerge(block, angle));
                if (line == null)
                {
                    line = new TextLine();
                    lines.Add(line);
                }
#pragma warning disable CS8604 // 引用类型参数可能为 null。
                line.Add(block, text, angle);
#pragma warning restore CS8604 // 引用类型参数可能为 null。
            }

            // 5. 行排序并输出
            var orderedLines = lines
                .OrderBy(l => l.Top)
                .ThenBy(l => l.Left);

            return string.Join(Environment.NewLine,
                               orderedLines.Select(l => l.Compose()));
        }
        /* ---------- 行实现 ---------- */
        private sealed class TextLine
        {
            private readonly List<(RecognizeCharacterDataDto Block, string Text, double Angle)> _items = [];

            public int Left => _items.Count > 0 ? _items.Min(i => i.Block.TextRectangles.Left) : 0;
            public int Top => _items.Count > 0 ? _items.Min(i => i.Block.TextRectangles.Top) : 0;
            public int Right => _items.Count > 0 ? _items.Max(i => i.Block.TextRectangles.Left + i.Block.TextRectangles.Width) : 0;
            public int Bottom => _items.Count > 0 ? _items.Max(i => i.Block.TextRectangles.Top + i.Block.TextRectangles.Height) : 0;

            private const int MaxRowGap = 10;
            private const int MaxColGap = 50;

            public bool CanMerge(RecognizeCharacterDataDto block, double angle)
            {
                if (!(_items.Count > 0)) return true;

                var baseAngle = _items.First().Angle;
                var box1 = RotatedRect.Create(Left, Top, Right - Left, Bottom - Top, baseAngle);
                var box2 = RotatedRect.Create(block.TextRectangles.Left,
                                              block.TextRectangles.Top,
                                              block.TextRectangles.Width,
                                              block.TextRectangles.Height,
                                              angle);

                double dy = Math.Abs(box2.CenterY - box1.CenterY);
                double dx = Math.Abs(box2.CenterX - box1.CenterX);

                bool yOverlap = dy <= MaxRowGap + (box1.Height + box2.Height) * 0.5;
                bool xOverlap = dx <= MaxColGap + (box1.Width + box2.Width) * 0.5;

                return yOverlap && xOverlap;
            }

            public void Add(RecognizeCharacterDataDto block, string text, double angle) =>
                _items.Add((block, text, angle));

            public string Compose()
            {
                // 保持阿里云OCR的原始顺序，按照位置坐标排序
                var ordered = _items.OrderBy(i => i.Block.TextRectangles.Left)
                                   .ThenBy(i => i.Block.TextRectangles.Top);

                return string.Concat(ordered.Select(i => i.Text));
            }

            private record RotatedRect(double CenterX, double CenterY, double Width, double Height)
            {
                public static RotatedRect Create(int left, int top, int w, int h, double angleDeg)
                {
                    var rad = angleDeg * Math.PI / 180.0;
                    double cx = left + w / 2.0;
                    double cy = top + h / 2.0;

                    double cos = Math.Abs(Math.Cos(rad));
                    double sin = Math.Abs(Math.Sin(rad));
                    double newW = w * cos + h * sin;
                    double newH = w * sin + h * cos;

                    return new RotatedRect(cx, cy, newW, newH);
                }
            }
        }

        [GeneratedRegex(@"^[\s●×·•―~…]+$", RegexOptions.Compiled)]
        private static partial Regex InvalidCharacter();
    }
}
