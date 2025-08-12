namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    public class RecognizeCharacterDto(
        string requestId)
    {
        public string RequestId { get; set; } = requestId;
        public string? FileId { get; set; }
        public List<RecognizeCharacterDataDto> Results { get; set; } = [];
    }
}