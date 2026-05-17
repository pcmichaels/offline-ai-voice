namespace AiVoiceTest.Core.Configuration;

public sealed class SelfTestOptions
{
    public const string SectionName = "SelfTest";

    public const string DefaultPhrase = "I never did mind about the little things";

    public string Phrase { get; set; } = DefaultPhrase;

    public int DurationSeconds { get; set; } = 10;
}
