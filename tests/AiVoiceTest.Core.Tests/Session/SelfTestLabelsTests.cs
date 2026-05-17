using AiVoiceTest.Core.Session;
using Xunit;

namespace AiVoiceTest.Core.Tests.Session;

public sealed class SelfTestLabelsTests
{
    [Fact]
    public void FormatBroadcast_IncludesBroadcastPrefix()
    {
        var line = SelfTestLabels.FormatBroadcast("test phrase");

        Assert.StartsWith(SelfTestLabels.BroadcastPrefix, line, StringComparison.Ordinal);
        Assert.Contains("test phrase", line, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatHeard_IncludesHeardPrefix()
    {
        var line = SelfTestLabels.FormatHeard("captured text");

        Assert.StartsWith(SelfTestLabels.HeardPrefix, line, StringComparison.Ordinal);
        Assert.Contains("captured text", line, StringComparison.Ordinal);
    }
}
