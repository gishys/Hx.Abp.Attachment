using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Domain.Shared
{
    /// <summary>
    /// 目录创建方式
    /// </summary>
    public enum CatalogueCreateMode
    {
        /// <summary>
        /// 重建
        /// </summary>
        Rebuild = 1,
        /// <summary>
        /// 追加
        /// </summary>
        Append = 2,
        /// <summary>
        /// 覆盖
        /// </summary>
        Overlap = 3
    }
}
