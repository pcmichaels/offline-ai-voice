namespace AiVoiceTest.Core.Configuration;

public sealed class AudioOptions
{
    public const string SectionName = "Audio";

    public string? InputDeviceId { get; set; }

    public int SampleRate { get; set; } = 16000;

    public bool UseVoiceActivityDetection { get; set; }

    public int SilenceDurationMs { get; set; } = 1500;

    public float SpeechThreshold { get; set; } = 0.02f;

    public int MaxRecordingSeconds { get; set; } = 60;
}
