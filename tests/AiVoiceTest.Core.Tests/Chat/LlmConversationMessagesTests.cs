using AiVoiceTest.Core.Chat;
using Xunit;

namespace AiVoiceTest.Core.Tests.Chat;

public sealed class LlmConversationMessagesTests
{
    [Fact]
    public void BuildPayload_IncludesSystemPromptAndCapsHistory()
    {
        var history = Enumerable.Range(1, 10)
            .Select(i => new ChatMessage(
                i % 2 == 1 ? ChatRoles.User : ChatRoles.Assistant,
                $"message-{i}"))
            .ToList();

        var payload = LlmConversationMessages.BuildPayload(
            systemPrompt: "You are helpful.",
            history,
            maxHistoryMessages: 4);

        Assert.Equal(ChatRoles.System, payload[0].Role);
        Assert.Equal("You are helpful.", payload[0].Content);
        Assert.Equal(5, payload.Count);
        Assert.Equal("message-7", payload[1].Content);
        Assert.Equal("message-10", payload[^1].Content);
    }

    [Fact]
    public void BuildPayload_WhenSystemPromptEmpty_OmitsSystemMessage()
    {
        var history = new List<ChatMessage> { new(ChatRoles.User, "hello") };

        var payload = LlmConversationMessages.BuildPayload(
            systemPrompt: "   ",
            history,
            maxHistoryMessages: 20);

        Assert.Single(payload);
        Assert.Equal(ChatRoles.User, payload[0].Role);
    }
}
