using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Domain
{
    public class CreateAttachFileCatalogueInfo
    {
        public int SequenceNumber {  get; set; }
        public required string Reference {  get; set; }
    }
}
