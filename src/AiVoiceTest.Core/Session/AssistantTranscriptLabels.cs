namespace AiVoiceTest.Core.Session;

public static class AssistantTranscriptLabels
{
    public const string Prefix = "Assistant:";

    public static string FormatLine(string replyText) => $"{Prefix} {replyText}";
}
