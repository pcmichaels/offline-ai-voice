namespace AiVoiceTest.Core.Session;

public static class SelfTestLabels
{
    public const string BroadcastPrefix = "Broadcast:";

    public const string HeardPrefix = "Heard:";

    public static string FormatBroadcast(string phrase) => $"{BroadcastPrefix} {phrase}";

    public static string FormatHeard(string transcript) => $"{HeardPrefix} {transcript}";
}
