namespace AiVoiceTest.Core.Configuration;

public sealed class TtsOptions
{
    public const string SectionName = "Tts";

    public string Mode { get; set; } = "http";

    public string ServiceUrl { get; set; } = "http://localhost:5002";

    public string PiperPath { get; set; } = string.Empty;

    public string VoiceModelPath { get; set; } = "data/voices/en_US-lessac-medium.onnx";
}
