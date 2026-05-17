using AiVoiceTest.Core.Models;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;

namespace AiVoiceTest.Infrastructure.Orchestration;

public sealed class VoiceSessionOrchestrator : IVoiceSessionOrchestrator
{
    private readonly ISpeechToTextService _speechToText;
    private readonly ILlmChatService _llmChat;

    public VoiceSessionOrchestrator(
        ISpeechToTextService speechToText,
        ILlmChatService llmChat)
    {
        _speechToText = speechToText;
        _llmChat = llmChat;
    }

    public async Task<VoiceTurnResult> ProcessTurnAsync(
        string recordedWavPath,
        CancellationToken cancellationToken = default)
    {
        var transcription = await TranscribeUtteranceAsync(recordedWavPath, cancellationToken);
        if (!transcription.HasSpeech)
        {
            return new VoiceTurnResult
            {
                UserDisplayText = transcription.UserDisplayText,
                HasSpeech = false,
            };
        }

        return await CompleteTurnFromUserTextAsync(transcription.UserDisplayText, cancellationToken);
    }

    public async Task<TranscriptionResult> TranscribeUtteranceAsync(
        string recordedWavPath,
        CancellationToken cancellationToken = default)
    {
        var transcript = await _speechToText.TranscribeAsync(recordedWavPath, cancellationToken);
        var userDisplayText = string.IsNullOrWhiteSpace(transcript)
            ? UserTranscriptLabels.NoSpeechDetected
            : transcript.Trim();

        var hasSpeech = !string.Equals(
            userDisplayText,
            UserTranscriptLabels.NoSpeechDetected,
            StringComparison.Ordinal);

        return new TranscriptionResult
        {
            UserDisplayText = userDisplayText,
            HasSpeech = hasSpeech,
        };
    }

    public async Task<VoiceTurnResult> CompleteTurnFromUserTextAsync(
        string userDisplayText,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var assistantReply = await _llmChat.SendUserMessageAsync(userDisplayText, cancellationToken);

            return new VoiceTurnResult
            {
                UserDisplayText = userDisplayText,
                HasSpeech = true,
                AssistantReply = assistantReply,
            };
        }
        catch (Exception ex)
        {
            return new VoiceTurnResult
            {
                UserDisplayText = userDisplayText,
                HasSpeech = true,
                LlmError = ex.Message,
            };
        }
    }
}
