using AiVoiceTest.Core.Chat;
using Xunit;

namespace AiVoiceTest.Core.Tests.Chat;

public sealed class ChatHistoryTrimmerTests
{
    [Fact]
    public void TrimInPlace_WhenOverMax_RemovesOldestMessages()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRoles.User, "first"),
            new(ChatRoles.Assistant, "reply-1"),
            new(ChatRoles.User, "second"),
            new(ChatRoles.Assistant, "reply-2"),
            new(ChatRoles.User, "third"),
        };

        ChatHistoryTrimmer.TrimInPlace(history, maxHistoryMessages: 4);

        Assert.Equal(4, history.Count);
        Assert.Equal("reply-1", history[0].Content);
        Assert.Equal("third", history[^1].Content);
    }

    [Fact]
    public void TrimInPlace_WhenUnderMax_LeavesHistoryUnchanged()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRoles.User, "hello"),
            new(ChatRoles.Assistant, "hi"),
        };

        ChatHistoryTrimmer.TrimInPlace(history, maxHistoryMessages: 20);

        Assert.Equal(2, history.Count);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(20, 20)]
    public void NormalizeMax_EnforcesMinimumOfTwo(int configured, int expected)
    {
        Assert.Equal(expected, ChatHistoryTrimmer.NormalizeMax(configured));
    }
}
