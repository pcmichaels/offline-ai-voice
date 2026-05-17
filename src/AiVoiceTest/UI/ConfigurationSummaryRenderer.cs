using AiVoiceTest.Core.Configuration;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.UI;

public static class ConfigurationSummaryRenderer
{
    public static void Render(
        IOptions<AppOptions> app,
        IOptions<LlmOptions> llm,
        IOptions<SttOptions> stt,
        IOptions<TtsOptions> tts,
        IOptions<AudioOptions> audio,
        IOptions<SessionOptions> session,
        IOptions<SelfTestOptions> selfTest,
        string configPath)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Configuration summary[/]")
            .AddColumn("Section")
            .AddColumn("Setting")
            .AddColumn("Value");

        table.AddRow("App", "Milestone", app.Value.Milestone);
        table.AddRow("Llm", "BaseUrl", llm.Value.BaseUrl);
        table.AddRow("Llm", "Model", llm.Value.Model);
        table.AddRow("Stt", "Mode", stt.Value.Mode);
        table.AddRow("Stt", "ServiceUrl", stt.Value.ServiceUrl);
        table.AddRow("Stt", "ModelSize", stt.Value.ModelSize);
        table.AddRow("Tts", "Mode", tts.Value.Mode);
        table.AddRow("Tts", "ServiceUrl", tts.Value.ServiceUrl);
        table.AddRow("Tts", "VoiceModelPath", tts.Value.VoiceModelPath);
        table.AddRow(
            "Audio",
            "InputDeviceId",
            string.IsNullOrWhiteSpace(audio.Value.InputDeviceId)
                ? "(default)"
                : audio.Value.InputDeviceId);
        table.AddRow("Audio", "SampleRate", audio.Value.SampleRate.ToString());
        table.AddRow("Session", "MaxHistoryMessages", session.Value.MaxHistoryMessages.ToString());
        table.AddRow("SelfTest", "Phrase", selfTest.Value.Phrase);
        table.AddRow("SelfTest", "DurationSeconds", selfTest.Value.DurationSeconds.ToString());

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine($"[dim]Config file:[/] {Markup.Escape(configPath)}");
    }
}
