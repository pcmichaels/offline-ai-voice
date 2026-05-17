using AiVoiceTest.Core.Models;

namespace AiVoiceTest.Core.Services;
public interface IVoiceSessionOrchestrator
{
    Task<VoiceTurnResult> ProcessTurnAsync(
        string recordedWavPath,
        CancellationToken cancellationToken = default);

    Task<TranscriptionResult> TranscribeUtteranceAsync(
        string recordedWavPath,
        CancellationToken cancellationToken = default);

    Task<VoiceTurnResult> CompleteTurnFromUserTextAsync(
        string userDisplayText,
        CancellationToken cancellationToken = default);
}
