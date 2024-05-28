using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hx.Abp.Attachment.Domain
{
    public static class AppGlobalProperties
    {
        public static string ServerBaseUrl = "StaticFile:CmsFile";
        public static string FileServerBasePath = "App:SelfUrl";
        public static string StaticFilesBasePathSign = "StaticFiles-BasePath";
        public static string AttachmentBasicPath = $"/attachment/{DateTime.Now.Year}{DateTime.Now.Year:D2}";
    }
}