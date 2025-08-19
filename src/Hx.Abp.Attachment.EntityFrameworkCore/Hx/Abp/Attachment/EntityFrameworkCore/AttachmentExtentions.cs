using Hx.Abp.Attachment.Domain;
using Microsoft.EntityFrameworkCore;

namespace Hx.Abp.Attachment.EntityFrameworkCore
{
    public static class AttachmentExtentions
    {
        public static IQueryable<AttachCatalogue> IncludeDetails(
            this IQueryable<AttachCatalogue> queryable,
            bool include = true)
        {
            if (!include)
                return queryable;
            return queryable
                .Include(d => d.AttachFiles)
                .Include(d => d.Children)
                .ThenInclude(d => d.AttachFiles)
                .Include(d => d.Children)
                .ThenInclude(d => d.Children)
                .ThenInclude(d => d.AttachFiles)
                .Include(d => d.Children)
                .ThenInclude(d => d.Children)
                .ThenInclude(d => d.Children)
                .ThenInclude(d => d.AttachFiles);
        }
    }
}
