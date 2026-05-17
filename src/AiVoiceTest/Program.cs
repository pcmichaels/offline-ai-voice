using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Infrastructure.DependencyInjection;
using AiVoiceTest.Paths;
using AiVoiceTest.Session;
using AiVoiceTest.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

return await RunAsync(args);

static async Task<int> RunAsync(string[] args)
{
    var exitCode = 0;

    try
    {
        var repositoryRoot = RepositoryPaths.FindRepositoryRoot();
        var appSettingsPath = RepositoryPaths.GetAppSettingsPath(repositoryRoot);

        if (!File.Exists(appSettingsPath))
        {
            AnsiConsole.MarkupLine(
                $"[red]Configuration file not found:[/] {Markup.Escape(appSettingsPath)}");
            return 1;
        }

        var host = Host.CreateDefaultBuilder(args)
            .UseContentRoot(repositoryRoot)
            .ConfigureLogging(logging =>
            {
                logging.AddFilter("System.Net.Http", LogLevel.Warning);
                logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
            })
            .ConfigureAppConfiguration((_, config) =>
            {
                config.Sources.Clear();
                config
                    .SetBasePath(repositoryRoot)
                    .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddAiVoiceTestInfrastructure(context.Configuration, repositoryRoot);
            })
            .Build();

        var appOptions = host.Services.GetRequiredService<IOptions<AppOptions>>().Value;
        var llmOptions = host.Services.GetRequiredService<IOptions<LlmOptions>>().Value;
        var sttOptions = host.Services.GetRequiredService<IOptions<SttOptions>>().Value;
        var ttsOptions = host.Services.GetRequiredService<IOptions<TtsOptions>>().Value;
        var audioOptions = host.Services.GetRequiredService<IOptions<AudioOptions>>().Value;
        var sessionOptions = host.Services.GetRequiredService<IOptions<SessionOptions>>().Value;
        var selfTestOptionsForValidation = host.Services.GetRequiredService<IOptions<SelfTestOptions>>().Value;

        var configErrors = AppConfigurationValidator.Validate(
            llmOptions,
            sttOptions,
            ttsOptions,
            audioOptions,
            sessionOptions,
            selfTestOptionsForValidation);

        if (configErrors.Count > 0)
        {
            AnsiConsole.MarkupLine("[red]Configuration validation failed:[/]");
            foreach (var error in configErrors)
            {
                AnsiConsole.MarkupLine($"  [red]-[/] {Markup.Escape(error)}");
            }

            return 1;
        }

        WelcomeBannerRenderer.Render(appOptions.Milestone);

        ConfigurationSummaryRenderer.Render(
            host.Services.GetRequiredService<IOptions<AppOptions>>(),
            host.Services.GetRequiredService<IOptions<LlmOptions>>(),
            host.Services.GetRequiredService<IOptions<SttOptions>>(),
            host.Services.GetRequiredService<IOptions<TtsOptions>>(),
            host.Services.GetRequiredService<IOptions<AudioOptions>>(),
            host.Services.GetRequiredService<IOptions<SessionOptions>>(),
            host.Services.GetRequiredService<IOptions<SelfTestOptions>>(),
            appSettingsPath);

        if (SelfTestArgs.IsSelfTest(args))
        {
            if (SelfTestArgs.UsesLegacyMicTestAlias(args))
            {
                AnsiConsole.MarkupLine(
                    "[yellow]Note:[/] [yellow]--mic-test[/] is deprecated; use [bold]--self-test[/] instead.");
                AnsiConsole.WriteLine();
            }

            var selfTestOptions = host.Services.GetRequiredService<IOptions<SelfTestOptions>>().Value;
            var seconds = SelfTestArgs.TryParseSecondsOverride(args)
                ?? selfTestOptions.DurationSeconds;

            var selfTestHealthChecker = host.Services.GetRequiredService<IServiceHealthChecker>();
            var selfTestTtsHealth = await selfTestHealthChecker.CheckTtsAsync();
            if (!selfTestTtsHealth.IsHealthy)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine(
                    "[red]TTS service is not healthy.[/] Self-test requires broadcast capability. " +
                    "Start Docker with [yellow].\\utils\\run-docker.ps1 -SelfTest[/] " +
                    "or ensure [yellow]Tts:ServiceUrl[/] is reachable.");
                return 1;
            }

            var selfTestSttHealth = await selfTestHealthChecker.CheckSttAsync();

            return await SelfTestRunner.RunAsync(
                host.Services.GetRequiredService<IOptions<SelfTestOptions>>(),
                host.Services.GetRequiredService<IAudioCaptureService>(),
                host.Services.GetRequiredService<ITextToSpeechService>(),
                host.Services.GetRequiredService<IAudioPlaybackService>(),
                host.Services.GetRequiredService<ISpeechToTextService>(),
                selfTestSttHealth.IsHealthy,
                seconds);
        }

        AnsiConsole.WriteLine();
        var healthChecker = host.Services.GetRequiredService<IServiceHealthChecker>();
        await ServiceHealthPanelRenderer.RenderAsync(healthChecker);

        var sttHealth = await healthChecker.CheckSttAsync();
        var ttsHealth = await healthChecker.CheckTtsAsync();
        var llmHealth = await healthChecker.CheckLlmAsync();

        if (!sttHealth.IsHealthy)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                "[red]STT service is not healthy.[/] Start Docker services with [yellow].\\utils\\run-docker.ps1[/] " +
                "and ensure port 5001 is reachable.");
            return 1;
        }

        if (!ttsHealth.IsHealthy)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                "[red]TTS service is not healthy.[/] Start Docker services with [yellow].\\utils\\run-docker.ps1[/] " +
                "and ensure port 5002 is reachable.");
            return 1;
        }

        if (!llmHealth.IsHealthy)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                "[red]LM Studio is not reachable.[/] Open LM Studio, load a model, start the local server, " +
                $"then retry. Configured URL: [yellow]{Markup.Escape(llmOptions.BaseUrl)}[/]");
            AnsiConsole.MarkupLine(
                "[dim]run-docker.ps1 does not start LM Studio for you.[/]");
            return 1;
        }

        if (ShouldSkipInteractiveSession(args))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]Services ready.[/] Exiting without voice session (non-interactive).");
            return 0;
        }

        AnsiConsole.WriteLine();
        AudioDeviceListRenderer.Render();
        AnsiConsole.WriteLine();

        var session = new VoiceSessionRunner(
            host.Services.GetRequiredService<IAudioCaptureService>(),
            host.Services.GetRequiredService<IVoiceSessionOrchestrator>(),
            host.Services.GetRequiredService<ITextToSpeechService>(),
            host.Services.GetRequiredService<IAudioPlaybackService>());

        await session.RunAsync();
    }
    catch (Exception ex)
    {
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
        exitCode = 1;
    }

    return exitCode;
}

static bool ShouldSkipInteractiveSession(string[] args) =>
    args.Contains("--no-prompt", StringComparer.OrdinalIgnoreCase)
    || Console.IsInputRedirected;
