namespace AiVoiceTest.Core.Configuration;

public sealed class AudioOptions
{
    public const string SectionName = "Audio";

    public string? InputDeviceId { get; set; }

    public int SampleRate { get; set; } = 16000;
}
