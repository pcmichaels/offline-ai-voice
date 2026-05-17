namespace AiVoiceTest.Core.Session;

public sealed class SessionTranscriptLog
{
    private readonly List<string> _lines = [];

    public IReadOnlyList<string> Lines => _lines;

    public int CompletedTurns { get; private set; }

    public void AddUserUtterance(string transcriptText)
    {
        _lines.Add(UserTranscriptLabels.FormatLine(transcriptText));
    }

    public void AddAssistantReply(string replyText)
    {
        _lines.Add(AssistantTranscriptLabels.FormatLine(replyText));
        CompletedTurns++;
    }

    public void AddTranslation(string languageDisplayName, string translatedText)
    {
        _lines.Add(TranslationLabels.FormatLine(languageDisplayName, translatedText));
    }

    public void AddReadback(string transcriptText)
    {
        _lines.Add(ReadbackLabels.FormatLine(transcriptText));
    }

    public bool HasEntries => _lines.Count > 0;
}
