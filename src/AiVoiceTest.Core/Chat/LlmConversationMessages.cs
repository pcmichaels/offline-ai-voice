namespace AiVoiceTest.Core.Chat;

/// <summary>
/// Builds the OpenAI-style <c>messages</c> array for LM Studio from session history.
/// </summary>
public static class LlmConversationMessages
{
    public static List<ChatMessage> BuildPayload(
        string? systemPrompt,
        IReadOnlyList<ChatMessage> sessionHistory,
        int maxHistoryMessages)
    {
        ArgumentNullException.ThrowIfNull(sessionHistory);

        var history = sessionHistory.ToList();
        ChatHistoryTrimmer.TrimInPlace(history, maxHistoryMessages);

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage(ChatRoles.System, systemPrompt));
        }

        messages.AddRange(history);
        return messages;
    }
}
