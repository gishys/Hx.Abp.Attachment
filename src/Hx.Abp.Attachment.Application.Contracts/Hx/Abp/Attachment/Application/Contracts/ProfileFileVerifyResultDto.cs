namespace Hx.Abp.Attachment.Application.Contracts
{
    public class ProfileFileVerifyResultDto
    {
        public required string Reference {  get; set; }
        public required List<string> Message {  get; set; }
    }
}
