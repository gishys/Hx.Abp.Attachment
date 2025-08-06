using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class GetOcrFullTextInput
    {
        public required List<Guid> Ids { get; set; }
    }
}
