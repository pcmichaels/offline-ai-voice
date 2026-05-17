using AiVoiceTest.Infrastructure.Audio;
using Spectre.Console;

namespace AiVoiceTest.UI;

public static class AudioDeviceListRenderer
{
    public static void Render()
    {
        var devices = AudioInputDeviceLister.ListActiveCaptureDevices();

        AnsiConsole.MarkupLine("[bold]Microphone devices[/] [dim](set Audio:InputDeviceId in appsettings to index)[/]");

        if (devices.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No active capture devices found.[/]");
            return;
        }

        foreach (var device in devices)
        {
            var suffix = device.IsDefault ? " [green](default)[/]" : string.Empty;
            AnsiConsole.MarkupLine($"  [yellow]{device.Index}[/] {Markup.Escape(device.Name)}{suffix}");
        }
    }
}
