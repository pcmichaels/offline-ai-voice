namespace AiVoiceTest.Core.Chat;

/// <summary>
/// Caps in-memory session history sent to the LLM per <c>Session:MaxHistoryMessages</c>.
/// </summary>
public static class ChatHistoryTrimmer
{
    public static void TrimInPlace(IList<ChatMessage> history, int maxHistoryMessages)
    {
        ArgumentNullException.ThrowIfNull(history);

        var max = NormalizeMax(maxHistoryMessages);
        if (history.Count <= max)
        {
            return;
        }

        var removeCount = history.Count - max;
        for (var i = 0; i < removeCount; i++)
        {
            history.RemoveAt(0);
        }
    }

    public static int NormalizeMax(int maxHistoryMessages) => Math.Max(2, maxHistoryMessages);
}
