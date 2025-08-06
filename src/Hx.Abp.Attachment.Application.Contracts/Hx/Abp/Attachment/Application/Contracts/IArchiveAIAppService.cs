using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public interface IArchiveAIAppService : IApplicationService
    {
        Task OcrFullTextAsync(List<Guid> ids);
    }
}
