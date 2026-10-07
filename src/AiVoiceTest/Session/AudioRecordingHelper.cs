using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Spectre.Console;

namespace AiVoiceTest.Session;

internal static class AudioRecordingHelper
{
    public static async Task<string> RecordUtteranceAsync(
        IAudioCaptureService audioCapture,
        AudioOptions audioOptions,
        CancellationToken cancellationToken = default)
    {
        var stopRequested = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordTask = audioCapture.RecordToWavFileAsync(stopRequested.Task, cancellationToken);

        if (audioOptions.UseVoiceActivityDetection)
        {
            AnsiConsole.MarkupLine(
                "[yellow]Recording — speak now.[/] Stops after silence or press [bold]Enter[/].");
            _ = Task.Run(
                () =>
                {
                    Console.ReadLine();
                    stopRequested.TrySetResult();
                },
                cancellationToken);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Recording — speak now.[/] Press [bold]Enter[/] when finished.");
            await Task.Run(Console.ReadLine, cancellationToken);
            stopRequested.TrySetResult();
        }

        var path = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Finishing capture...", async _ => await recordTask);

        AnsiConsole.MarkupLine(
            $"[dim]Captured audio from[/] [cyan]{Markup.Escape(audioCapture.CaptureDeviceName)}[/]");

        return path;
    }

    public static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup for temp WAV files.
        }
    }
}
