using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Domain.Shared
{
    public class GetAttachListInput
    {
        public required string Reference {  get; set; }
        public int ReferenceType {  get; set; }
    }
}
