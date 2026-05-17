using AiVoiceTest.Core.Session;
using Xunit;

namespace AiVoiceTest.Core.Tests.Session;

public sealed class SessionTranscriptLogTests
{
    [Fact]
    public void AddUserAndAssistant_LinesUseEchoLabels()
    {
        var log = new SessionTranscriptLog();

        log.AddUserUtterance("question");
        log.AddAssistantReply("answer");

        Assert.Equal(2, log.Lines.Count);
        Assert.StartsWith(UserTranscriptLabels.Prefix, log.Lines[0], StringComparison.Ordinal);
        Assert.StartsWith(AssistantTranscriptLabels.Prefix, log.Lines[1], StringComparison.Ordinal);
        Assert.Equal(1, log.CompletedTurns);
    }

    [Fact]
    public void AddUserOnly_DoesNotIncrementCompletedTurns()
    {
        var log = new SessionTranscriptLog();
        log.AddUserUtterance("solo");

        Assert.Single(log.Lines);
        Assert.Equal(0, log.CompletedTurns);
        Assert.True(log.HasEntries);
    }
}
