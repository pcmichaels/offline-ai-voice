namespace AiVoiceTest.Core.Configuration;

public sealed class SttOptions
{
    public const string SectionName = "Stt";

    public string Mode { get; set; } = "http";

    public string ServiceUrl { get; set; } = "http://localhost:5001";

    public string WhisperScriptPath { get; set; } = "utils/transcribe.py";

    public string ModelSize { get; set; } = "small";

    public string Device { get; set; } = "cpu";
}
