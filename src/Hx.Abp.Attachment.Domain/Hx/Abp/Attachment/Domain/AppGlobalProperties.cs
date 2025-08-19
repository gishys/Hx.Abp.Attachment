namespace Hx.Abp.Attachment.Domain
{
    public static class AppGlobalProperties
    {
        public const string ServerBaseUrl = "StaticFile:CmsFile";
        public const string FileServerBasePath = "App:SelfUrl";
        public const string StaticFilesBasePathSign = "StaticFiles-BasePath";
        public static string AttachmentBasicPath => $"{DateTime.Now.Year}{DateTime.Now.Month:D2}";
    }
}
