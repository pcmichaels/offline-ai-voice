namespace AiVoiceTest.Core.Services;

public interface IAudioPlaybackService
{
    Task PlayWavFileAsync(string wavFilePath, CancellationToken cancellationToken = default);
}
