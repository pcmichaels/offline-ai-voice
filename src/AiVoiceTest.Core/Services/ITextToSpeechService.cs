namespace AiVoiceTest.Core.Services;

public interface ITextToSpeechService
{
    /// <summary>
    /// Synthesizes speech and writes a WAV file under the repository temp directory.
    /// </summary>
    Task<string> SynthesizeToWavFileAsync(string text, CancellationToken cancellationToken = default);
}
