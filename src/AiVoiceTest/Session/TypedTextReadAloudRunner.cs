using AiVoiceTest.Core.Services;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class TypedTextReadAloudRunner
{
    private readonly ITextToSpeechService _textToSpeech;
    private readonly IAudioPlaybackService _audioPlayback;

    public TypedTextReadAloudRunner(
        ITextToSpeechService textToSpeech,
        IAudioPlaybackService audioPlayback)
    {
        _textToSpeech = textToSpeech;
        _audioPlayback = audioPlayback;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Read typed text[/] — enter text to hear via Piper.");

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            var text = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold]Text to speak[/] ([dim]empty to return to menu[/])")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            string? ttsPath = null;
            try
            {
                ttsPath = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Synthesizing speech...", async _ =>
                        await _textToSpeech.SynthesizeToWavFileAsync(text.Trim(), cancellationToken));

                AnsiConsole.MarkupLine($"[cyan]Speaking:[/] {Markup.Escape(text.Trim())}");
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Star)
                    .StartAsync("Playing...", async _ =>
                        await _audioPlayback.PlayWavFileAsync(ttsPath, cancellationToken));
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
            }
            finally
            {
                AudioRecordingHelper.TryDeleteFile(ttsPath);
            }
        }
    }
}
