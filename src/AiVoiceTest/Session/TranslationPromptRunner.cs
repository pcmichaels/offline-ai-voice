using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using AiVoiceTest.UI;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class TranslationPromptRunner
{
    private readonly PostTranscriptionOptions _postTranscriptionOptions;
    private readonly TranslationOptions _translationOptions;
    private readonly ITranslationService _translationService;

    public TranslationPromptRunner(
        IOptions<PostTranscriptionOptions> postTranscriptionOptions,
        IOptions<TranslationOptions> translationOptions,
        ITranslationService translationService)
    {
        _postTranscriptionOptions = postTranscriptionOptions.Value;
        _translationOptions = translationOptions.Value;
        _translationService = translationService;
    }

    public async Task<string?> RunAsync(
        string sourceText,
        SessionTranscriptLog transcriptLog,
        CancellationToken cancellationToken = default)
    {
        if (!_postTranscriptionOptions.OfferTranslation)
        {
            return null;
        }

        TranslationLanguage? selected = null;
        if (TranslationLanguages.TryGetByCode(_translationOptions.DefaultTargetLanguage, out var preset))
        {
            selected = preset;
        }
        else
        {
            selected = PromptLanguage();
        }

        if (selected is null)
        {
            return null;
        }

        var language = selected.Value;

        try
        {
            var translated = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync($"Translating to {language.DisplayName}...", async _ =>
                    await _translationService.TranslateAsync(sourceText, language.Code, cancellationToken));

            transcriptLog.AddTranslation(language.DisplayName, translated);
            AnsiConsole.WriteLine();
            SessionTranscriptRenderer.Render(transcriptLog);
            return translated;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Translation failed:[/] {Markup.Escape(ex.Message)} [dim](continuing with assistant reply)[/]");
            AnsiConsole.WriteLine();
            return null;
        }
    }

    private static TranslationLanguage? PromptLanguage()
    {
        var prompt = new SelectionPrompt<string>()
            .Title("Translate this utterance?")
            .AddChoices(TranslationChoiceResolver.PromptChoices);

        var choice = AnsiConsole.Prompt(prompt);
        return TranslationChoiceResolver.ResolvePromptChoice(choice);
    }
}
