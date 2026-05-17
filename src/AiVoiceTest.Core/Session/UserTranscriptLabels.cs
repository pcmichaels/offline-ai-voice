namespace AiVoiceTest.Core.Session;

public static class UserTranscriptLabels
{
    public const string Prefix = "You said:";

    public const string NoSpeechDetected = "(no speech detected)";

    public static string FormatLine(string transcriptText) => $"{Prefix} {transcriptText}";
}
