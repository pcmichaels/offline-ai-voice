using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace AiVoiceTest.Infrastructure.Audio;

public sealed class NaudioAudioCaptureService : IAudioCaptureService
{
    private readonly AudioOptions _options;
    private readonly string _tempDirectory;

    public NaudioAudioCaptureService(IOptions<AudioOptions> options, string repositoryRoot)
    {
        _options = options.Value;
        _tempDirectory = Path.Combine(repositoryRoot, "data", "temp");
        Directory.CreateDirectory(_tempDirectory);
    }

    public string CaptureDeviceName { get; private set; } = "unknown";

    public async Task<string> RecordToWavFileAsync(
        Task stopRequested,
        CancellationToken cancellationToken = default)
    {
        var rawPath = Path.Combine(_tempDirectory, $"recording-{Guid.NewGuid():N}-raw.wav");
        var outputPath = Path.Combine(_tempDirectory, $"recording-{Guid.NewGuid():N}.wav");

        var device = SelectCaptureDevice();
        CaptureDeviceName = device.FriendlyName;
        using var capture = new WasapiCapture(device);

        await using (var writer = new WaveFileWriter(rawPath, capture.WaveFormat))
        {
            var recordingStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            capture.DataAvailable += (_, args) =>
            {
                if (args.BytesRecorded > 0)
                {
                    writer.Write(args.Buffer, 0, args.BytesRecorded);
                }
            };

            capture.RecordingStopped += (_, _) => recordingStopped.TrySetResult();

            capture.StartRecording();

            try
            {
                await WaitForStopAsync(stopRequested, cancellationToken);
            }
            finally
            {
                if (capture.CaptureState == CaptureState.Capturing)
                {
                    capture.StopRecording();
                }

                await recordingStopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        try
        {
            ConvertTo16KhzMono(rawPath, outputPath);
            ValidateCapturedAudio(outputPath, device.FriendlyName);
            return outputPath;
        }
        finally
        {
            TryDeleteFile(rawPath);
        }
    }

    private MMDevice SelectCaptureDevice()
    {
        using var enumerator = new MMDeviceEnumerator();

        if (int.TryParse(_options.InputDeviceId, out var deviceIndex) && deviceIndex >= 0)
        {
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();
            if (deviceIndex < devices.Count)
            {
                return devices[deviceIndex];
            }
        }

        return enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console);
    }

    private static async Task WaitForStopAsync(Task stopRequested, CancellationToken cancellationToken)
    {
        var cancelTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var completed = await Task.WhenAny(stopRequested, cancelTask);

        if (completed == cancelTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private void ConvertTo16KhzMono(string inputPath, string outputPath)
    {
        using var reader = new AudioFileReader(inputPath);
        var targetRate = _options.SampleRate;
        var resampled = new WdlResamplingSampleProvider(reader.ToSampleProvider(), targetRate);
        // ToMono() only accepts stereo; many capture devices are mono or multi-mic arrays.
        ISampleProvider mono = resampled.WaveFormat.Channels == 1
            ? resampled
            : new AveragingMonoSampleProvider(resampled);
        WaveFileWriter.CreateWaveFile16(outputPath, mono);
    }

    private static void ValidateCapturedAudio(string outputPath, string deviceName)
    {
        using var reader = new AudioFileReader(outputPath);
        if (reader.TotalTime < TimeSpan.FromMilliseconds(200))
        {
            throw new InvalidOperationException(
                $"Recording was too short ({reader.TotalTime.TotalMilliseconds:F0} ms). " +
                "Press Enter at the prompt, speak, then press Enter again to stop.");
        }

        var maxAmplitude = GetPeakAmplitude(reader);
        if (maxAmplitude < 0.001f)
        {
            throw new InvalidOperationException(
                $"No microphone signal detected from '{deviceName}'. " +
                "Check Windows sound settings: input device, mute, and app microphone permission.");
        }
    }

    private static float GetPeakAmplitude(AudioFileReader reader)
    {
        var buffer = new float[reader.WaveFormat.SampleRate];
        float peak = 0;
        int read;
        reader.Position = 0;

        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (var i = 0; i < read; i++)
            {
                var sample = Math.Abs(buffer[i]);
                if (sample > peak)
                {
                    peak = sample;
                }
            }
        }

        return peak;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    /// <summary>
    /// Downmixes any multi-channel float source to mono by averaging samples per frame.
    /// </summary>
    private sealed class AveragingMonoSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _channels;

        public AveragingMonoSampleProvider(ISampleProvider source)
        {
            _source = source;
            _channels = source.WaveFormat.Channels;
            if (_channels < 2)
            {
                throw new ArgumentException("Source must have at least 2 channels for downmix.", nameof(source));
            }

            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var temp = new float[count * _channels];
            var read = _source.Read(temp, 0, temp.Length);
            var frames = read / _channels;
            for (var i = 0; i < frames; i++)
            {
                var sum = 0f;
                for (var c = 0; c < _channels; c++)
                {
                    sum += temp[i * _channels + c];
                }

                buffer[offset + i] = sum / _channels;
            }

            return frames;
        }
    }
}
