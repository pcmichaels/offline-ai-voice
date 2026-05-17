namespace AiVoiceTest.Core.Models;

public sealed class TranscriptionResult
{
    public required string UserDisplayText { get; init; }

    public bool HasSpeech { get; init; }
}
