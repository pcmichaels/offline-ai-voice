namespace AiVoiceTest.Core.Configuration;

public sealed class SessionOptions
{
    public const string SectionName = "Session";

    public int MaxHistoryMessages { get; set; } = 20;
}
