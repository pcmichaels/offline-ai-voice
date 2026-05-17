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

    public bool HasEntries => _lines.Count > 0;
}
