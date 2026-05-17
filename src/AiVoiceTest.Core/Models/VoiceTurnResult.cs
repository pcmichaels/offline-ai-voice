namespace AiVoiceTest.Core.Models;

public sealed class VoiceTurnResult
{
    public required string UserDisplayText { get; init; }

    public bool HasSpeech { get; init; }

    public string? AssistantReply { get; init; }

    public string? LlmError { get; init; }
}
