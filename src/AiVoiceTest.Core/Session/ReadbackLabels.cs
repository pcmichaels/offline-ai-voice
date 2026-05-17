namespace AiVoiceTest.Core.Session;

public static class ReadbackLabels
{
    public const string Prefix = "Readback:";

    public static string FormatLine(string text) => $"{Prefix} {text}";
}
