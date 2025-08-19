namespace Hx.Abp.Attachment.Application.ArchAI.Contracts
{
    public class RecognizeCharacterDataDto(float? probability, string? text, RecognizeCharacterDataRectangles textRectangles)
    {
        public float? Probability { get; set; } = probability;
        public string? Text { get; set; } = text;
        public RecognizeCharacterDataRectangles TextRectangles { get; set; } = textRectangles;
    }
}
