using Spectre.Console;

namespace AiVoiceTest.UI;

public static class WelcomeBannerRenderer
{
    public static void Render(string milestone)
    {
        var rule = new Rule("[bold cyan]AI Voice Test[/]")
        {
            Justification = Justify.Center,
        };

        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Local voice conversation POC — .NET + Spectre.Console[/]");
        AnsiConsole.MarkupLine($"[yellow]Milestone:[/] [bold]{Markup.Escape(milestone)}[/]");
        AnsiConsole.WriteLine();
    }
}
