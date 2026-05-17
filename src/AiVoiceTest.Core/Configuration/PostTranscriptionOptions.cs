namespace AiVoiceTest.Core.Configuration;

public sealed class PostTranscriptionOptions
{
    public const string SectionName = "PostTranscription";

    public bool OfferReadback { get; set; } = true;

    public bool ReadbackUsesTts { get; set; } = true;

    public bool OfferRecordingPlayback { get; set; }

    public bool OfferTranslation { get; set; } = true;
}
