namespace AiVoiceTest.Core.Services;

public interface ISpeechToTextService
{
    Task<string> TranscribeAsync(string wavFilePath, CancellationToken cancellationToken = default);
}
