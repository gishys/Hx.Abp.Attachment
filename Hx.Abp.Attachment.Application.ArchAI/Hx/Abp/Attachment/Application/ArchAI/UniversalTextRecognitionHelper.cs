using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tea;

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
            AlibabaCloud.OpenApiClient.Models.Config config = new AlibabaCloud.OpenApiClient.Models.Config
            {
                AccessKeyId = accessKeyId,
                AccessKeySecret = accessKeySecret,
            };
            // 访问的域名
            config.Endpoint = "ocr.cn-shanghai.aliyuncs.com";
            return new AlibabaCloud.SDK.Ocr20191230.Client(config);
        }
        public async static void JpgUniversalTextRecognition(string accessKeyId, string accessKeySecret, string imageUrl)
        {
            // 创建AccessKey ID和AccessKey Secret，请参考https://help.aliyun.com/document_detail/175144.html
            // 如果您使用的是RAM用户的AccessKey，还需要为子账号授予权限AliyunVIAPIFullAccess，请参考https://help.aliyun.com/document_detail/145025.html
            // 从环境变量读取配置的AccessKey ID和AccessKey Secret。运行代码示例前必须先配置环境变量。
            AlibabaCloud.SDK.Ocr20191230.Client client = CreateClient(accessKeyId, accessKeySecret);
            AlibabaCloud.SDK.Ocr20191230.Models.RecognizeCharacterAdvanceRequest recognizeCharacterAdvanceRequest = new AlibabaCloud.SDK.Ocr20191230.Models.RecognizeCharacterAdvanceRequest
            ();
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
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            try
            {
                AlibabaCloud.SDK.Ocr20191230.Models.RecognizeCharacterResponse recognizeCharacterResponse = client.RecognizeCharacterAdvance(recognizeCharacterAdvanceRequest, runtime);
                // 获取整体结果
                Console.WriteLine(AlibabaCloud.TeaUtil.Common.ToJSONString(recognizeCharacterResponse.Body));
                // 获取单个字段
                Console.WriteLine(AlibabaCloud.TeaUtil.Common.ToJSONString(recognizeCharacterResponse.Body.Data));
            }
            catch (TeaException error)
            {
                // 如有需要，请打印 error
                AlibabaCloud.TeaUtil.Common.AssertAsString(error.Message);
            }
            catch (Exception _error)
            {
                TeaException error = new TeaException(new Dictionary<string, object>
                {
                    { "message", _error.Message }
                });
                // 如有需要，请打印 error
                Console.WriteLine(error.Message);
            }
        }
    }
}
