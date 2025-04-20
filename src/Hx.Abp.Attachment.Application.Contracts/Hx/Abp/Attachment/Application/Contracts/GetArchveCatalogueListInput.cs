using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Application.Contracts
{
    public class GetArchveCatalogueListInput
    {
        public required string Reference { get; set; }
        public int ReferenceType { get; set; }
        public string? CatalogueName { get; set; }
    }
}
