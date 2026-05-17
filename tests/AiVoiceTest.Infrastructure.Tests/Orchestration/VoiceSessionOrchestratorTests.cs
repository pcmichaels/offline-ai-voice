using AiVoiceTest.Core.Models;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using AiVoiceTest.Infrastructure.Orchestration;
using Xunit;

namespace AiVoiceTest.Infrastructure.Tests.Orchestration;

public sealed class VoiceSessionOrchestratorTests
{
    [Fact]
    public async Task ProcessTurnAsync_WhenSilence_SkipsLlm()
    {
        var stt = new FakeSpeechToTextService(string.Empty);
        var llm = new FakeLlmChatService("ignored");
        var orchestrator = new VoiceSessionOrchestrator(stt, llm);

        var result = await orchestrator.ProcessTurnAsync("recording.wav");

        Assert.False(result.HasSpeech);
        Assert.Equal(UserTranscriptLabels.NoSpeechDetected, result.UserDisplayText);
        Assert.Null(result.AssistantReply);
        Assert.Empty(llm.Calls);
    }

    [Fact]
    public async Task ProcessTurnAsync_WhenSpeech_ReturnsAssistantReply()
    {
        var stt = new FakeSpeechToTextService("What is two plus two?");
        var llm = new FakeLlmChatService("Four.");
        var orchestrator = new VoiceSessionOrchestrator(stt, llm);

        var result = await orchestrator.ProcessTurnAsync("recording.wav");

        Assert.True(result.HasSpeech);
        Assert.Equal("What is two plus two?", result.UserDisplayText);
        Assert.Equal("Four.", result.AssistantReply);
        Assert.Single(llm.Calls);
    }

    [Fact]
    public async Task CompleteTurnFromUserTextAsync_WhenLlmFails_SetsLlmError()
    {
        var orchestrator = new VoiceSessionOrchestrator(
            new FakeSpeechToTextService("unused"),
            new FakeLlmChatService("unused", shouldThrow: true));

        var result = await orchestrator.CompleteTurnFromUserTextAsync("hello");

        Assert.True(result.HasSpeech);
        Assert.NotNull(result.LlmError);
        Assert.Null(result.AssistantReply);
    }

    [Fact]
    public async Task TranscribeUtteranceAsync_WhenWhitespace_MarksNoSpeech()
    {
        var orchestrator = new VoiceSessionOrchestrator(
            new FakeSpeechToTextService("   "),
            new FakeLlmChatService("unused"));

        var transcription = await orchestrator.TranscribeUtteranceAsync("recording.wav");

        Assert.False(transcription.HasSpeech);
        Assert.Equal(UserTranscriptLabels.NoSpeechDetected, transcription.UserDisplayText);
    }

    private sealed class FakeSpeechToTextService(string transcript) : ISpeechToTextService
    {
        public Task<string> TranscribeAsync(string wavFilePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(transcript);
    }

    private sealed class FakeLlmChatService : ILlmChatService
    {
        private readonly string _reply;
        private readonly bool _shouldThrow;

        public FakeLlmChatService(string reply, bool shouldThrow = false)
        {
            _reply = reply;
            _shouldThrow = shouldThrow;
        }

        public List<string> Calls { get; } = [];

        public Task<ServiceHealthReport> CheckConnectivityAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new ServiceHealthReport("LM Studio", "http://localhost", true, "healthy", null));

        public Task<string> SendUserMessageAsync(string userText, CancellationToken cancellationToken = default)
        {
            Calls.Add(userText);
            if (_shouldThrow)
            {
                throw new InvalidOperationException("LLM unavailable");
            }

            return Task.FromResult(_reply);
        }
    }
}
