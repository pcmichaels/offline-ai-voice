using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using AiVoiceTest.UI;
using Microsoft.Extensions.Options;
using NAudio.Wave;
using Spectre.Console;

namespace AiVoiceTest.Session;

public static class SelfTestRunner
{
    public static async Task<int> RunAsync(
        IOptions<SelfTestOptions> selfTestOptions,
        IAudioCaptureService capture,
        ITextToSpeechService textToSpeech,
        IAudioPlaybackService playback,
        ISpeechToTextService? speechToText,
        bool sttAvailable,
        int durationSeconds,
        CancellationToken cancellationToken = default)
    {
        var options = selfTestOptions.Value;
        var phrase = string.IsNullOrWhiteSpace(options.Phrase)
            ? SelfTestOptions.DefaultPhrase
            : options.Phrase.Trim();

        durationSeconds = ClampDuration(durationSeconds);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "[bold]Self-test[/] — mic capture and TTS broadcast run [bold]at the same time[/]. " +
            "No LM Studio required.");
        AnsiConsole.WriteLine();
        AudioDeviceListRenderer.Render();
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(SelfTestLabels.FormatBroadcast(phrase))}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[yellow]Listening and broadcasting for {durationSeconds} seconds[/] " +
            "starting in [bold]3[/]… [bold]2[/]… [bold]1[/]… [green]GO[/].");

        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        string? capturedPath = null;
        string? ttsPath = null;

        try
        {
            var stopAfter = Task.Delay(TimeSpan.FromSeconds(durationSeconds), cancellationToken);

            var recordTask = capture.RecordToWavFileAsync(stopAfter, cancellationToken);
            var broadcastTask = BroadcastPhraseAsync(
                textToSpeech,
                playback,
                phrase,
                cancellationToken);

            await Task.WhenAll(recordTask, broadcastTask);
            capturedPath = await recordTask;

            await ReportLevelsAsync(capturedPath, capture.CaptureDeviceName, cancellationToken);

            if (sttAvailable && speechToText is not null)
            {
                await ReportHeardAsync(speechToText, capturedPath, phrase, cancellationToken);
            }
            else
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(
                    "[dim]STT not available — skipping Heard line.[/] " +
                    "Run [yellow].\\utils\\run-docker.ps1 -SelfTest[/] with Docker up for transcription.");
            }

            AnsiConsole.WriteLine();
            if (AnsiConsole.Confirm("Play back the captured microphone recording?", defaultValue: false))
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Playing capture...", async _ =>
                        await playback.PlayWavFileAsync(capturedPath, cancellationToken));
                AnsiConsole.MarkupLine("[green]Capture playback finished.[/]");
            }
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            return 1;
        }
        finally
        {
            TryDelete(capturedPath);
            TryDelete(ttsPath);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "[green]Self-test complete.[/] Run without [yellow]--self-test[/] for the full voice session.");
        return 0;

        async Task BroadcastPhraseAsync(
            ITextToSpeechService tts,
            IAudioPlaybackService audioPlayback,
            string text,
            CancellationToken ct)
        {
            ttsPath = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Synthesizing broadcast...", async _ =>
                    await tts.SynthesizeToWavFileAsync(text, ct));

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Star)
                .StartAsync("Broadcasting...", async _ =>
                    await audioPlayback.PlayWavFileAsync(ttsPath, ct));
        }
    }

    private static int ClampDuration(int seconds)
    {
        if (seconds < 2 || seconds > 60)
        {
            return 10;
        }

        return seconds;
    }

    private static async Task ReportHeardAsync(
        ISpeechToTextService speechToText,
        string wavPath,
        string broadcastPhrase,
        CancellationToken cancellationToken)
    {
        string heard;
        try
        {
            heard = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Transcribing capture...", async _ =>
                {
                    var text = await speechToText.TranscribeAsync(wavPath, cancellationToken);
                    return string.IsNullOrWhiteSpace(text)
                        ? UserTranscriptLabels.NoSpeechDetected
                        : text.Trim();
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Could not transcribe capture:[/] {Markup.Escape(ex.Message)}");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(SelfTestLabels.FormatHeard(heard))}[/]");

        if (string.Equals(heard, UserTranscriptLabels.NoSpeechDetected, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine(
                "[yellow]Mic did not pick up speech — check input level, mute, or move closer to speakers.[/]");
            return;
        }

        if (!PhrasesLikelyRelated(broadcastPhrase, heard))
        {
            AnsiConsole.MarkupLine(
                "[yellow]Heard text differs from the broadcast phrase — room noise, echo, or wrong input device may apply.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Heard text looks related to the broadcast phrase.[/]");
        }
    }

    private static bool PhrasesLikelyRelated(string broadcast, string heard)
    {
        var broadcastWords = SignificantWords(broadcast);
        var heardWords = SignificantWords(heard);
        if (broadcastWords.Count == 0 || heardWords.Count == 0)
        {
            return false;
        }

        return broadcastWords.Any(w => heardWords.Contains(w, StringComparer.OrdinalIgnoreCase));
    }

    private static HashSet<string> SignificantWords(string text) =>
        text.Split([' ', '\t', '\r', '\n', ',', '.', '!', '?', ';', ':', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static async Task ReportLevelsAsync(
        string wavPath,
        string deviceName,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        using var reader = new AudioFileReader(wavPath);
        var peak = GetPeak(reader);
        var duration = reader.TotalTime;

        var db = peak > 0.0001f
            ? 20.0 * Math.Log10(peak)
            : -96.0;

        var barWidth = 40;
        var filled = (int)Math.Clamp(Math.Round(peak * barWidth), 0, barWidth);
        var bar = new string('█', filled) + new string('░', barWidth - filled);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Device:[/] {Markup.Escape(deviceName)}");
        AnsiConsole.MarkupLine($"[bold]Duration:[/] {duration.TotalSeconds:F1} s");
        AnsiConsole.MarkupLine($"[bold]Peak level:[/] {peak:P0} (~{db:F1} dBFS)");
        AnsiConsole.MarkupLine($"[dim]{bar}[/]");

        if (peak < 0.001f)
        {
            AnsiConsole.MarkupLine("[red]Very weak signal — check mute, device selection, and privacy settings.[/]");
        }
        else if (peak < 0.01f)
        {
            AnsiConsole.MarkupLine(
                "[yellow]Signal is quite low.[/] Raise mic input level or move closer to the speakers.");
        }
    }

    private static float GetPeak(AudioFileReader reader)
    {
        var buffer = new float[reader.WaveFormat.SampleRate];
        float peak = 0;
        int read;
        reader.Position = 0;

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

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrEmpty(path))
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
            // best-effort
        }
    }
}
