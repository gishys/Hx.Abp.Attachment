using System.Text.Json;
using Volo.Abp.Domain.Entities;

namespace Hx.Abp.Attachment.Domain
{
    /// <summary>
    /// OCR文本块 - 存储OCR识别结果的文本位置信息
    /// </summary>
    public class OcrTextBlock : Entity<Guid>
    {
        /// <summary>
        /// 关联的文件ID
        /// </summary>
        public virtual Guid AttachFileId { get; protected set; }

        /// <summary>
        /// 文本内容
        /// </summary>
        public virtual string Text { get; protected set; }

        /// <summary>
        /// 置信度
        /// </summary>
        public virtual float Probability { get; protected set; }

        /// <summary>
        /// 页面索引（PDF多页时使用）
        /// </summary>
        public virtual int PageIndex { get; protected set; }

        /// <summary>
        /// 文本位置信息（JSON格式）
        /// </summary>
        public virtual string PositionData { get; protected set; }

        /// <summary>
        /// 文本块在文档中的位置（用于排序）
        /// </summary>
        public virtual int BlockOrder { get; protected set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public virtual DateTime CreationTime { get; protected set; }

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        protected OcrTextBlock() { }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。

        public OcrTextBlock(
            Guid id,
            Guid attachFileId,
            string text,
            float probability,
            int pageIndex,
            string positionData,
            int blockOrder)
        {
            Id = id;
            AttachFileId = attachFileId;
            Text = text ?? string.Empty;
            Probability = probability;
            PageIndex = pageIndex;
            PositionData = positionData ?? string.Empty;
            BlockOrder = blockOrder;
            CreationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 获取位置信息
        /// </summary>
        public virtual TextPosition? GetPosition()
        {
            if (string.IsNullOrWhiteSpace(PositionData))
                return null;

            try
            {
                return JsonSerializer.Deserialize<TextPosition>(PositionData);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 设置位置信息
        /// </summary>
        public virtual void SetPosition(TextPosition position)
        {
            PositionData = JsonSerializer.Serialize(position);
        }
    }

    /// <summary>
    /// 文本位置信息
    /// </summary>
    public class TextPosition
    {
        public long Angle { get; set; }
        public long Height { get; set; }
        public long Left { get; set; }
        public long Top { get; set; }
        public long Width { get; set; }

        /// <summary>
        /// 计算文本块的中心点
        /// </summary>
        public (long X, long Y) GetCenter()
        {
            return (Left + Width / 2, Top + Height / 2);
        }

        /// <summary>
        /// 检查点是否在文本块内
        /// </summary>
        public bool ContainsPoint(int x, int y)
        {
            return x >= Left && x <= Left + Width && y >= Top && y <= Top + Height;
        }
    }
}
