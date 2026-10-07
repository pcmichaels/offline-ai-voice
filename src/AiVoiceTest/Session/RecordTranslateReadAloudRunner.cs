using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using AiVoiceTest.UI;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class RecordTranslateReadAloudRunner
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly IVoiceSessionOrchestrator _orchestrator;
    private readonly ITranslationService _translationService;
    private readonly ITextToSpeechService _textToSpeech;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly AudioOptions _audioOptions;
    private readonly TranslationOptions _translationOptions;

    public RecordTranslateReadAloudRunner(
        IAudioCaptureService audioCapture,
        IVoiceSessionOrchestrator orchestrator,
        ITranslationService translationService,
        ITextToSpeechService textToSpeech,
        IAudioPlaybackService audioPlayback,
        IOptions<AudioOptions> audioOptions,
        IOptions<TranslationOptions> translationOptions)
    {
        _audioCapture = audioCapture;
        _orchestrator = orchestrator;
        _translationService = translationService;
        _textToSpeech = textToSpeech;
        _audioPlayback = audioPlayback;
        _audioOptions = audioOptions.Value;
        _translationOptions = translationOptions.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "[bold]Record, translate, and speak[/] — STT, translate via LM Studio, then read translation aloud.");

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Markup("[bold]Ready — press Enter to record[/] ([yellow]q[/] to return to menu): ");
            if (SessionInput.IsReturnToMenu(Console.ReadLine()))
            {
                return;
            }

            string? recordedPath = null;
            string? ttsPath = null;
            var log = new SessionTranscriptLog();

            try
            {
                recordedPath = await AudioRecordingHelper.RecordUtteranceAsync(
                    _audioCapture,
                    _audioOptions,
                    cancellationToken);

                var transcription = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Transcribing...", async _ =>
                        await _orchestrator.TranscribeUtteranceAsync(recordedPath, cancellationToken));

                log.AddUserUtterance(transcription.UserDisplayText);
                AnsiConsole.WriteLine();
                SessionTranscriptRenderer.Render(log);

                if (!transcription.HasSpeech)
                {
                    continue;
                }

                var language = PromptTargetLanguage();
                if (language is null)
                {
                    AnsiConsole.MarkupLine("[yellow]Cancelled — no language selected.[/]");
                    continue;
                }

                var translated = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync($"Translating to {language.Value.DisplayName}...", async _ =>
                        await _translationService.TranslateAsync(
                            transcription.UserDisplayText,
                            language.Value.Code,
                            cancellationToken));

                log.AddTranslation(language.Value.DisplayName, translated);
                AnsiConsole.WriteLine();
                SessionTranscriptRenderer.Render(log);

                ttsPath = await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Synthesizing translated speech...", async _ =>
                        await _textToSpeech.SynthesizeToWavFileAsync(translated, cancellationToken));

                AnsiConsole.MarkupLine(
                    $"[magenta]Speaking translation ({language.Value.DisplayName}):[/] {Markup.Escape(translated)}");

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
                AudioRecordingHelper.TryDeleteFile(recordedPath);
                AudioRecordingHelper.TryDeleteFile(ttsPath);
            }
        }
    }

    private TranslationLanguage? PromptTargetLanguage()
    {
        if (TranslationLanguages.TryGetByCode(_translationOptions.DefaultTargetLanguage, out var preset))
        {
            return preset;
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Choose target language")
                .AddChoices(TranslationChoiceResolver.LanguagePromptChoices));

        return TranslationChoiceResolver.ResolveLanguageChoice(choice);
    }
}
