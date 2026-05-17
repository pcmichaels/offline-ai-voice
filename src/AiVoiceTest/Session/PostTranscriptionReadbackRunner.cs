using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class PostTranscriptionReadbackRunner
{
    private readonly PostTranscriptionOptions _options;
    private readonly ITextToSpeechService _textToSpeech;
    private readonly IAudioPlaybackService _audioPlayback;

    public PostTranscriptionReadbackRunner(
        IOptions<PostTranscriptionOptions> options,
        ITextToSpeechService textToSpeech,
        IAudioPlaybackService audioPlayback)
    {
        _options = options.Value;
        _textToSpeech = textToSpeech;
        _audioPlayback = audioPlayback;
    }

    public async Task<bool> RunAsync(
        string transcriptText,
        string recordedWavPath,
        SessionTranscriptLog transcriptLog,
        CancellationToken cancellationToken = default)
    {
        if (!_options.OfferReadback)
        {
            return false;
        }

        var choice = PromptReadbackChoice();
        if (choice == ReadbackChoice.Continue)
        {
            return false;
        }

        transcriptLog.AddReadback(transcriptText);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"[bold]{Markup.Escape(ReadbackLabels.FormatLine(transcriptText))}[/]");

        try
        {
            if (choice == ReadbackChoice.Tts)
            {
                var ttsPath = await _textToSpeech.SynthesizeToWavFileAsync(transcriptText, cancellationToken);
                try
                {
                    await _audioPlayback.PlayWavFileAsync(ttsPath, cancellationToken);
                }
                finally
                {
                    TryDelete(ttsPath);
                }
            }
            else if (choice == ReadbackChoice.Recording)
            {
                await _audioPlayback.PlayWavFileAsync(recordedWavPath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Readback failed:[/] {Markup.Escape(ex.Message)} [dim](continuing to next step)[/]");
        }

        AnsiConsole.WriteLine();
        return true;
    }

    private ReadbackChoice PromptReadbackChoice()
    {
        if (_options.ReadbackUsesTts && _options.OfferRecordingPlayback)
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<ReadbackChoice>()
                    .Title("Read your words back?")
                    .AddChoices(
                        ReadbackChoice.Continue,
                        ReadbackChoice.Tts,
                        ReadbackChoice.Recording)
                    .UseConverter(c => c switch
                    {
                        ReadbackChoice.Continue => "Continue (no readback)",
                        ReadbackChoice.Tts => "Hear transcript (TTS)",
                        ReadbackChoice.Recording => "Hear original recording",
                        _ => c.ToString(),
                    }));
        }

        if (_options.ReadbackUsesTts)
        {
            return AnsiConsole.Confirm("Read your words back through speakers?", defaultValue: false)
                ? ReadbackChoice.Tts
                : ReadbackChoice.Continue;
        }

        if (_options.OfferRecordingPlayback)
        {
            return AnsiConsole.Confirm("Play back your original recording?", defaultValue: false)
                ? ReadbackChoice.Recording
                : ReadbackChoice.Continue;
        }

        return ReadbackChoice.Continue;
    }

    private static void TryDelete(string path)
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
            // best-effort
        }
    }

    private enum ReadbackChoice
    {
        Continue,
        Tts,
        Recording,
    }
}
