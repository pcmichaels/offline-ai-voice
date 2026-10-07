using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class RecordPlaybackRunner
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly AudioOptions _audioOptions;

    public RecordPlaybackRunner(
        IAudioCaptureService audioCapture,
        IAudioPlaybackService audioPlayback,
        IOptions<AudioOptions> audioOptions)
    {
        _audioCapture = audioCapture;
        _audioPlayback = audioPlayback;
        _audioOptions = audioOptions.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Record and playback[/] — capture audio; optionally hear the recording.");

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[bold]Ready — press Enter to record[/] ([yellow]q[/] to return to menu): ");
            if (SessionInput.IsReturnToMenu(Console.ReadLine()))
            {
                return;
            }

            string? recordedPath = null;
            try
            {
                recordedPath = await AudioRecordingHelper.RecordUtteranceAsync(
                    _audioCapture,
                    _audioOptions,
                    cancellationToken);

                if (AnsiConsole.Confirm("Play back the recording?", defaultValue: true))
                {
                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("Playing recording...", async _ =>
                            await _audioPlayback.PlayWavFileAsync(recordedPath, cancellationToken));
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
            finally
            {
                AudioRecordingHelper.TryDeleteFile(recordedPath);
            }
        }
    }
}
