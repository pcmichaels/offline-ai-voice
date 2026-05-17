using AiVoiceTest.Core.Services;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AiVoiceTest.Infrastructure.Audio;

public sealed class NaudioAudioPlaybackService : IAudioPlaybackService
{
    public Task PlayWavFileAsync(string wavFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(wavFilePath))
        {
            throw new FileNotFoundException("Playback audio file was not found.", wavFilePath);
        }

        return PlayInternalAsync(wavFilePath, cancellationToken);
    }

    private static async Task PlayInternalAsync(string wavFilePath, CancellationToken cancellationToken)
    {
        var peak = MeasurePeak(wavFilePath);
        var gain = PlaybackGainForPeak(peak);

        using var reader = new AudioFileReader(wavFilePath);
        var amplified = new VolumeSampleProvider(reader.ToSampleProvider()) { Volume = gain };
        var waveProvider = amplified.ToWaveProvider();

        // WaveOutEvent uses legacy MME device mapping and often targets the wrong output on Windows 10/11.
        // WasapiOut follows the default WASAPI render endpoint (same path as most desktop apps).
        using var waveOut = new WasapiOut();
        var playbackFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        waveOut.Init(waveProvider);
        waveOut.PlaybackStopped += (_, e) =>
        {
            if (e.Exception is not null)
            {
                playbackFinished.TrySetException(e.Exception);
            }
            else
            {
                playbackFinished.TrySetResult();
            }
        };

        await using var registration = cancellationToken.Register(() =>
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
            }
        });

        waveOut.Play();

        try
        {
            await playbackFinished.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                waveOut.Stop();
            }

            await playbackFinished.Task.WaitAsync(CancellationToken.None);
            throw;
        }
    }

    private static float MeasurePeak(string wavFilePath)
    {
        using var reader = new AudioFileReader(wavFilePath);
        var buffer = new float[Math.Max(1024, reader.WaveFormat.SampleRate)];
        float peak = 0;
        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var a = Math.Abs(buffer[i]);
                if (a > peak)
                {
                    peak = a;
                }
            }
        }

        return peak;
    }

    /// <summary>
    /// Quiet mic tests (low dBFS) are easy to miss on speakers; scale toward a modest peak for playback only.
    /// </summary>
    private static float PlaybackGainForPeak(float peak)
    {
        if (peak < 1e-5f)
        {
            return 1f;
        }

        const float targetPeak = 0.25f;
        return Math.Clamp(targetPeak / peak, 1f, 10f);
    }
}
