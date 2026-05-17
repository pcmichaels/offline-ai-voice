using AiVoiceTest.Core.Session;
using Spectre.Console;

namespace AiVoiceTest.UI;

public static class SessionTranscriptRenderer
{
    public static void Render(SessionTranscriptLog log)
    {
        if (!log.HasEntries)
        {
            return;
        }

        var body = string.Join(Environment.NewLine, log.Lines.Select(FormatLine));

        var turnLabel = log.CompletedTurns == 1
            ? "1 turn"
            : $"{log.CompletedTurns} turns";

        var panel = new Panel(body)
        {
            Header = new PanelHeader($"[bold]Session transcript[/] [dim]({turnLabel})[/]"),
            Border = BoxBorder.Rounded,
            Expand = true,
        };

        AnsiConsole.Write(panel);
    }

    private static string FormatLine(string line)
    {
        if (line.StartsWith(UserTranscriptLabels.Prefix, StringComparison.Ordinal))
        {
            var text = line[UserTranscriptLabels.Prefix.Length..].TrimStart();
            return $"[cyan]{UserTranscriptLabels.Prefix}[/] [bold]{Markup.Escape(text)}[/]";
        }

        if (line.StartsWith(AssistantTranscriptLabels.Prefix, StringComparison.Ordinal))
        {
            var text = line[AssistantTranscriptLabels.Prefix.Length..].TrimStart();
            return $"[green]{AssistantTranscriptLabels.Prefix}[/] {Markup.Escape(text)}";
        }

        return Markup.Escape(line);
    }
}
