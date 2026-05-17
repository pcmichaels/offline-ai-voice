namespace AiVoiceTest.Core.Services;

public interface IAudioCaptureService
{
    string CaptureDeviceName { get; }

    /// <summary>
    /// Records from the microphone until <paramref name="stopRequested"/> completes or
    /// <paramref name="cancellationToken"/> is cancelled, then writes a 16 kHz mono WAV file.
    /// </summary>
    /// <returns>Absolute path to the recorded WAV file.</returns>
    Task<string> RecordToWavFileAsync(
        Task stopRequested,
        CancellationToken cancellationToken = default);
}
