using AiVoiceTest.Core.Services;
using Spectre.Console;

namespace AiVoiceTest.UI;

public static class ServiceHealthPanelRenderer
{
    public static async Task RenderAsync(
        IServiceHealthChecker healthChecker,
        CancellationToken cancellationToken = default)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Checking service endpoints...", async ctx =>
            {
                var stt = await healthChecker.CheckSttAsync(cancellationToken);
                var tts = await healthChecker.CheckTtsAsync(cancellationToken);
                var llm = await healthChecker.CheckLlmAsync(cancellationToken);

                ctx.Status("Service checks complete");

                var table = new Table()
                    .Border(TableBorder.Rounded)
                    .Title("[bold]Service health[/]")
                    .AddColumn("Service")
                    .AddColumn("Endpoint")
                    .AddColumn("Status")
                    .AddColumn("Detail");

                table.AddRow(
                    stt.ServiceName,
                    Markup.Escape(stt.Endpoint),
                    FormatStatus(stt),
                    Markup.Escape(stt.Detail ?? "-"));

                table.AddRow(
                    tts.ServiceName,
                    Markup.Escape(tts.Endpoint),
                    FormatStatus(tts),
                    Markup.Escape(tts.Detail ?? "-"));

                table.AddRow(
                    llm.ServiceName,
                    Markup.Escape(llm.Endpoint),
                    FormatStatus(llm),
                    Markup.Escape(llm.Detail ?? "-"));

                AnsiConsole.Write(table);
            });
    }

    private static string FormatStatus(ServiceHealthReport report)
    {
        if (report.IsHealthy)
        {
            return "[green]healthy[/]";
        }

        return report.StatusLabel.Equals("unreachable", StringComparison.OrdinalIgnoreCase)
            ? "[red]unreachable[/]"
            : "[yellow]unhealthy[/]";
    }
}
