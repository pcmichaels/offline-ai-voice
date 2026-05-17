using AiVoiceTest.Core.Configuration;
using Xunit;

namespace AiVoiceTest.Core.Tests.Configuration;

public sealed class AppConfigurationValidatorTests
{
    [Fact]
    public void Validate_WhenDefaultsFromAppSettings_ReturnsNoErrors()
    {
        var errors = AppConfigurationValidator.Validate(
            new LlmOptions
            {
                BaseUrl = "http://localhost:1234",
                Model = "local-model",
            },
            new SttOptions { ServiceUrl = "http://localhost:5001" },
            new TtsOptions { ServiceUrl = "http://localhost:5002" },
            new AudioOptions { SampleRate = 16000 },
            new SessionOptions { MaxHistoryMessages = 20 },
            ValidSelfTest());

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_WhenLlmBaseUrlMissing_ReturnsError()
    {
        var errors = AppConfigurationValidator.Validate(
            new LlmOptions { BaseUrl = "", Model = "m" },
            ValidStt(),
            ValidTts(),
            ValidAudio(),
            ValidSession(),
            ValidSelfTest());

        Assert.Contains(errors, e => e.Contains("Llm:BaseUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenSttHttpUrlInvalid_ReturnsError()
    {
        var errors = AppConfigurationValidator.Validate(
            ValidLlm(),
            new SttOptions { Mode = "http", ServiceUrl = "not-a-url" },
            ValidTts(),
            ValidAudio(),
            ValidSession(),
            ValidSelfTest());

        Assert.Contains(errors, e => e.Contains("Stt:ServiceUrl", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenSampleRateZero_ReturnsError()
    {
        var errors = AppConfigurationValidator.Validate(
            ValidLlm(),
            ValidStt(),
            ValidTts(),
            new AudioOptions { SampleRate = 0 },
            ValidSession(),
            ValidSelfTest());

        Assert.Contains(errors, e => e.Contains("Audio:SampleRate", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenSelfTestDurationOutOfRange_ReturnsError()
    {
        var errors = AppConfigurationValidator.Validate(
            ValidLlm(),
            ValidStt(),
            ValidTts(),
            ValidAudio(),
            ValidSession(),
            new SelfTestOptions { Phrase = "test", DurationSeconds = 1 });

        Assert.Contains(errors, e => e.Contains("SelfTest:DurationSeconds", StringComparison.Ordinal));
    }

    private static LlmOptions ValidLlm() =>
        new() { BaseUrl = "http://localhost:1234", Model = "local-model" };

    private static SttOptions ValidStt() =>
        new() { ServiceUrl = "http://localhost:5001" };

    private static TtsOptions ValidTts() =>
        new() { ServiceUrl = "http://localhost:5002" };

    private static AudioOptions ValidAudio() =>
        new() { SampleRate = 16000 };

    private static SessionOptions ValidSession() =>
        new() { MaxHistoryMessages = 20 };

    private static SelfTestOptions ValidSelfTest() =>
        new()
        {
            Phrase = SelfTestOptions.DefaultPhrase,
            DurationSeconds = 10,
        };
}
