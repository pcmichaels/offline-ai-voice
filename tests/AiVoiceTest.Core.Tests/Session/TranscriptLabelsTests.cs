using AiVoiceTest.Core.Session;
using Xunit;

namespace AiVoiceTest.Core.Tests.Session;

public sealed class TranscriptLabelsTests
{
    [Fact]
    public void UserFormatLine_IncludesYouSaidPrefix()
    {
        var line = UserTranscriptLabels.FormatLine("hello world");

        Assert.StartsWith(UserTranscriptLabels.Prefix, line, StringComparison.Ordinal);
        Assert.Contains("hello world", line, StringComparison.Ordinal);
    }

    [Fact]
    public void AssistantFormatLine_IncludesAssistantPrefix()
    {
        var line = AssistantTranscriptLabels.FormatLine("reply text");

        Assert.StartsWith(AssistantTranscriptLabels.Prefix, line, StringComparison.Ordinal);
        Assert.Contains("reply text", line, StringComparison.Ordinal);
    }

    [Fact]
    public void NoSpeechDetected_IsStableLiteral()
    {
        Assert.Equal("(no speech detected)", UserTranscriptLabels.NoSpeechDetected);
    }

    [Fact]
    public void SelfTestBroadcastFormat_IncludesBroadcastPrefix()
    {
        var line = SelfTestLabels.FormatBroadcast("I never did mind about the little things");

        Assert.StartsWith(SelfTestLabels.BroadcastPrefix, line, StringComparison.Ordinal);
        Assert.Contains("little things", line, StringComparison.Ordinal);
    }

    [Fact]
    public void SelfTestHeardFormat_IncludesHeardPrefix()
    {
        var line = SelfTestLabels.FormatHeard("hello");

        Assert.StartsWith(SelfTestLabels.HeardPrefix, line, StringComparison.Ordinal);
    }
}
