using AiVoiceTest.Core.Services;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class MainMenuRunner
{
    private readonly IServiceHealthChecker _healthChecker;
    private readonly TypedTextReadAloudRunner _typedTextRunner;
    private readonly RecordPlaybackRunner _recordPlaybackRunner;
    private readonly RecordTranslateReadAloudRunner _recordTranslateRunner;

    public MainMenuRunner(
        IServiceHealthChecker healthChecker,
        TypedTextReadAloudRunner typedTextRunner,
        RecordPlaybackRunner recordPlaybackRunner,
        RecordTranslateReadAloudRunner recordTranslateRunner)
    {
        _healthChecker = healthChecker;
        _typedTextRunner = typedTextRunner;
        _recordPlaybackRunner = recordPlaybackRunner;
        _recordTranslateRunner = recordTranslateRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Main menu[/] — choose a mode ([dim]health status shown above[/]):");

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<MainMenuChoice>()
                    .Title("What would you like to do?")
                    .UseConverter(DescribeChoice)
                    .AddChoices(
                        MainMenuChoice.ReadTypedText,
                        MainMenuChoice.RecordAndPlayback,
                        MainMenuChoice.RecordTranslateAndSpeak,
                        MainMenuChoice.Exit));

            switch (choice)
            {
                case MainMenuChoice.ReadTypedText:
                    if (!await EnsureTtsHealthyAsync())
                    {
                        break;
                    }

                    await _typedTextRunner.RunAsync(cancellationToken);
                    break;

                case MainMenuChoice.RecordAndPlayback:
                    await _recordPlaybackRunner.RunAsync(cancellationToken);
                    break;

                case MainMenuChoice.RecordTranslateAndSpeak:
                    if (!await EnsureSttHealthyAsync()
                        || !await EnsureLlmHealthyAsync()
                        || !await EnsureTtsHealthyAsync())
                    {
                        break;
                    }

                    await _recordTranslateRunner.RunAsync(cancellationToken);
                    break;

                case MainMenuChoice.Exit:
                    AnsiConsole.MarkupLine("[dim]Goodbye.[/]");
                    return;
            }
        }
    }

    private static string DescribeChoice(MainMenuChoice choice) =>
        choice switch
        {
            MainMenuChoice.ReadTypedText => "Read text that is typed",
            MainMenuChoice.RecordAndPlayback => "Record and optionally playback voice recording",
            MainMenuChoice.RecordTranslateAndSpeak =>
                "Record and translate voice (transcribe, translate, read back in target language)",
            MainMenuChoice.Exit => "Exit",
            _ => choice.ToString(),
        };

    private async Task<bool> EnsureTtsHealthyAsync()
    {
        var report = await _healthChecker.CheckTtsAsync();
        if (report.IsHealthy)
        {
            return true;
        }

        AnsiConsole.MarkupLine(
            $"[red]TTS is not available[/] ({Markup.Escape(report.StatusLabel)}). " +
            "Start Docker STT/TTS with [yellow].\\utils\\run-docker.ps1[/].");
        return false;
    }

    private async Task<bool> EnsureSttHealthyAsync()
    {
        var report = await _healthChecker.CheckSttAsync();
        if (report.IsHealthy)
        {
            return true;
        }

        AnsiConsole.MarkupLine(
            $"[red]STT is not available[/] ({Markup.Escape(report.StatusLabel)}). " +
            "Start Docker with [yellow].\\utils\\run-docker.ps1[/].");
        return false;
    }

    private async Task<bool> EnsureLlmHealthyAsync()
    {
        var report = await _healthChecker.CheckLlmAsync();
        if (report.IsHealthy)
        {
            return true;
        }

        AnsiConsole.MarkupLine(
            $"[red]LM Studio is not reachable[/] ({Markup.Escape(report.StatusLabel)}). " +
            "Load a model and start the local server before translating.");
        return false;
    }
}
