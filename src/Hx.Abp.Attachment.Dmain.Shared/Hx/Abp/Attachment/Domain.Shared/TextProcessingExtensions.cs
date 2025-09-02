using System.Buffers;

namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 文本处理扩展方法，使用 .NET 8 优化特性
    /// </summary>
    public static class TextProcessingExtensions
    {
        /// <summary>
        /// 高效的字符串分割方法，使用 Span<T> 优化性能
        /// </summary>
        /// <param name="text">要分割的文本</param>
        /// <param name="separators">分隔符集合</param>
        /// <returns>分割后的字符串列表</returns>
        public static List<string> SplitEfficient(this string text, ReadOnlySpan<char> separators)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            var result = new List<string>();
            var currentStart = 0;
            var textSpan = text.AsSpan();

            for (int i = 0; i < textSpan.Length; i++)
            {
                if (separators.Contains(textSpan[i]))
                {
                    if (i > currentStart)
                    {
                        result.Add(textSpan[currentStart..i].ToString());
                    }
                    currentStart = i + 1;
                }
            }

            // 添加最后一个部分
            if (currentStart < textSpan.Length)
            {
                result.Add(textSpan[currentStart..].ToString());
            }

            return result;
        }

        /// <summary>
        /// 提取关键词的优化方法
        /// </summary>
        /// <param name="text">要处理的文本</param>
        /// <param name="minLength">最小关键词长度</param>
        /// <param name="maxCount">最大关键词数量</param>
        /// <returns>提取的关键词列表</returns>
        public static List<string> ExtractKeywordsEfficient(this string text, int minLength = 2, int maxCount = 10)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            // 使用 ArrayPool 优化内存分配
            var separators = ArrayPool<char>.Shared.Rent(8);
            try
            {
                separators[0] = ' ';
                separators[1] = ',';
                separators[2] = '.';
                separators[3] = ';';
                separators[4] = ':';
                separators[5] = '!';
                separators[6] = '?';
                separators[7] = '\n';

                var words = text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= minLength)
                    .Select(w => w.Trim('"', '\'', '(', ')', '[', ']', '{', '}'))
                    .Where(w => w.Length >= minLength)
                    .GroupBy(w => w.ToLower())
                    .OrderByDescending(g => g.Count())
                    .Take(maxCount)
                    .Select(g => g.Key)
                    .ToList();

                return words;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(separators);
            }
        }

        /// <summary>
        /// 高效的文本清理方法
        /// </summary>
        /// <param name="text">要清理的文本</param>
        /// <returns>清理后的文本</returns>
        public static string CleanTextEfficient(this string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var textSpan = text.AsSpan();
            var result = new System.Text.StringBuilder(text.Length);

            for (int i = 0; i < textSpan.Length; i++)
            {
                var c = textSpan[i];
                if (char.IsWhiteSpace(c))
                {
                    if (result.Length > 0 && result[^1] != ' ')
                    {
                        result.Append(' ');
                    }
                }
                else if (char.IsLetterOrDigit(c) || char.IsPunctuation(c))
                {
                    result.Append(c);
                }
            }

            return result.ToString().Trim();
        }

        /// <summary>
        /// 分割并过滤文本的方法
        /// </summary>
        /// <param name="text">要处理的文本</param>
        /// <param name="separators">分隔符集合</param>
        /// <param name="filter">过滤条件</param>
        /// <returns>过滤后的字符串列表</returns>
        public static List<string> SplitAndFilter(this string text, ReadOnlySpan<char> separators, Func<string, bool> filter)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            // 使用 ArrayPool 优化内存分配
            var separatorArray = ArrayPool<char>.Shared.Rent(separators.Length);
            try
            {
                separators.CopyTo(separatorArray);

                return [.. text.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries).Where(filter)];
            }
            finally
            {
                ArrayPool<char>.Shared.Return(separatorArray);
            }
        }
    }
}
