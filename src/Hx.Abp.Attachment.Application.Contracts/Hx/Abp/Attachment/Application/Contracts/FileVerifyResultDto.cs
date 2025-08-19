namespace Hx.Abp.Attachment.Application.Contracts
{
    public class FileVerifyResultDto
    {
        public required List<DetailedFileVerifyResultDto> DetailedInfo {  get; set; }
        public required List<ProfileFileVerifyResultDto> ProfileInfo { get; set; }
    }
}
