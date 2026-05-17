namespace AiVoiceTest.Infrastructure.Audio;

/// <summary>
/// Stops recording after sustained silence following speech, or when max duration is reached.
/// </summary>
internal sealed class VoiceActivityMonitor
{
    private readonly float _speechThreshold;
    private readonly TimeSpan _silenceDuration;
    private readonly TimeSpan _maxDuration;
    private readonly TaskCompletionSource _stopRequested =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private DateTime _lastSpeechUtc = DateTime.MinValue;
    private bool _sawSpeech;

    public VoiceActivityMonitor(float speechThreshold, int silenceDurationMs, int maxRecordingSeconds)
    {
        _speechThreshold = speechThreshold;
        _silenceDuration = TimeSpan.FromMilliseconds(Math.Max(300, silenceDurationMs));
        _maxDuration = TimeSpan.FromSeconds(Math.Max(5, maxRecordingSeconds));
    }

    public Task StopTask => _stopRequested.Task;

    public void ProcessPeak(float peak)
    {
        if (_stopRequested.Task.IsCompleted)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (peak >= _speechThreshold)
        {
            _sawSpeech = true;
            _lastSpeechUtc = now;
        }

        if (_sawSpeech && now - _lastSpeechUtc >= _silenceDuration)
        {
            _stopRequested.TrySetResult();
            return;
        }

        if (now - _startedUtc >= _maxDuration)
        {
            _stopRequested.TrySetResult();
        }
    }
}
