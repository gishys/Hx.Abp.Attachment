using AlibabaCloud.SDK.Ocr20191230.Models;
using Hx.Abp.Attachment.Application.ArchAI.Contracts;
using Tea;
using Volo.Abp;

namespace Hx.Abp.Attachment.Application.ArchAI
{
    public class UniversalTextRecognitionHelper
    {
        /**
            * 使用AK&SK初始化账号Client
            * @param accessKeyId
            * @param accessKeySecret
            * @return Client
            * @throws Exception
        */
        public static AlibabaCloud.SDK.Ocr20191230.Client CreateClient(string accessKeyId, string accessKeySecret)
        {
            AlibabaCloud.OpenApiClient.Models.Config config = new()
            {
                AccessKeyId = accessKeyId,
                AccessKeySecret = accessKeySecret,
                // 访问的域名
                Endpoint = "ocr.cn-shanghai.aliyuncs.com"
            };
            return new AlibabaCloud.SDK.Ocr20191230.Client(config);
        }
        public async static Task<RecognizeCharacterDto> JpgUniversalTextRecognition(string accessKeyId, string accessKeySecret, string imageUrl)
        {
            // 创建AccessKey ID和AccessKey Secret，请参考https://help.aliyun.com/document_detail/175144.html
            // 如果您使用的是RAM用户的AccessKey，还需要为子账号授予权限AliyunVIAPIFullAccess，请参考https://help.aliyun.com/document_detail/145025.html
            // 从环境变量读取配置的AccessKey ID和AccessKey Secret。运行代码示例前必须先配置环境变量。
            AlibabaCloud.SDK.Ocr20191230.Client client = CreateClient(accessKeyId, accessKeySecret);
            AlibabaCloud.SDK.Ocr20191230.Models.RecognizeCharacterAdvanceRequest recognizeCharacterAdvanceRequest = new();
            // 场景一，使用本地文件
            // System.IO.StreamReader file = new System.IO.StreamReader(@"/tmp/ColorizeImage1.jpg");
            // recognizeCharacterAdvanceRequest.ImageURLObject = file.BaseStream;
            // recognizeCharacterAdvanceRequest.MinHeight = 10;
            // recognizeCharacterAdvanceRequest.OutputProbability = true;
            // 场景二，使用任意可访问的url
            using var httpClient = new HttpClient();
            var uri = new Uri(imageUrl);
            var stream = await httpClient.GetStreamAsync(uri);
            recognizeCharacterAdvanceRequest.ImageURLObject = stream;
            recognizeCharacterAdvanceRequest.MinHeight = 10;
            recognizeCharacterAdvanceRequest.OutputProbability = true;
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new();
            try
            {
                AlibabaCloud.SDK.Ocr20191230.Models.RecognizeCharacterResponse recognizeCharacterResponse = client.RecognizeCharacterAdvance(recognizeCharacterAdvanceRequest, runtime);
                var body = recognizeCharacterResponse.Body;
                var rc = new RecognizeCharacterDto(body.RequestId);
                foreach (var item in body.Data.Results)
                {
                    var rectangle = new RecognizeCharacterDataRectangles
                    {
                        Angle = item.TextRectangles.Angle ?? 0,
                        Width = item.TextRectangles.Width ?? 0,
                        Height = item.TextRectangles.Height ?? 0,
                        Left = item.TextRectangles.Left ?? 0,
                        Top = item.TextRectangles.Top ?? 0
                    };
                    rc.Results.Add(new(item.Probability, item.Text, rectangle));
                }
                return rc;
            }
            catch (TeaException error)
            {
                // 如有需要，请打印 error
                var message = AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
                throw new UserFriendlyException(message: message);
            }
            catch (Exception _error)
            {
                throw new UserFriendlyException(message: _error.Message);
            }
        }
        public static Task<RecognizeCharacterDto> PdfUniversalTextRecognition(string accessKeyId, string accessKeySecret, string pdfUrl)
        {
            // 创建AccessKey ID和AccessKey Secret，请参考https://help.aliyun.com/document_detail/175144.html
            // 如果您使用的是RAM用户的AccessKey，还需要为子账号授予权限AliyunVIAPIFullAccess，请参考https://help.aliyun.com/document_detail/145025.html
            // 从环境变量读取配置的AccessKey ID和AccessKey Secret。运行代码示例前必须先配置环境变量。
            AlibabaCloud.SDK.Ocr20191230.Client client = CreateClient(accessKeyId, accessKeySecret);
            AlibabaCloud.SDK.Ocr20191230.Models.RecognizePdfRequest recognizePdfRequest = new()
            {
                FileURL = pdfUrl,
            };
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new();
            try
            {
                AlibabaCloud.SDK.Ocr20191230.Models.RecognizePdfResponse recognizePdfResponse = client.RecognizePdfWithOptions(recognizePdfRequest, runtime);
                var body = recognizePdfResponse.Body;
                var rc = new RecognizeCharacterDto(body.RequestId);
                foreach (var item in body.Data.WordsInfo)
                {
                    var rectangle = new RecognizeCharacterDataRectangles
                    {
                        Angle = item.Angle ?? 0,
                        Width = item.Width ?? 0,
                        Height = item.Height ?? 0,
                        Left = item.X ?? 0,
                        Top = item.Y ?? 0
                    };
                    rc.Results.Add(new(0, item.Word, rectangle));
                }
                return Task.FromResult(rc);
            }
            catch (TeaException error)
            {
                var message = AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
                throw new UserFriendlyException(message: message);
            }
            catch (Exception _error)
            {
                throw new UserFriendlyException(message: _error.Message);
            }
        }
    }
}
