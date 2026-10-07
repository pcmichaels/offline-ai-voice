using System.Text;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Models;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using AiVoiceTest.UI;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace AiVoiceTest.Session;

public sealed class VoiceSessionRunner
{
    private readonly IAudioCaptureService _audioCapture;
    private readonly IVoiceSessionOrchestrator _orchestrator;
    private readonly ITextToSpeechService _textToSpeech;
    private readonly IAudioPlaybackService _audioPlayback;
    private readonly AudioOptions _audioOptions;
    private readonly LlmOptions _llmOptions;
    private readonly PostTranscriptionReadbackRunner _readbackRunner;
    private readonly TranslationPromptRunner _translationRunner;
    private readonly SessionTranscriptLog _transcriptLog = new();

    public VoiceSessionRunner(
        IAudioCaptureService audioCapture,
        IVoiceSessionOrchestrator orchestrator,
        ITextToSpeechService textToSpeech,
        IAudioPlaybackService audioPlayback,
        IOptions<AudioOptions> audioOptions,
        IOptions<LlmOptions> llmOptions,
        PostTranscriptionReadbackRunner readbackRunner,
        TranslationPromptRunner translationRunner)
    {
        _audioCapture = audioCapture;
        _orchestrator = orchestrator;
        _textToSpeech = textToSpeech;
        _audioPlayback = audioPlayback;
        _audioOptions = audioOptions.Value;
        _llmOptions = llmOptions.Value;
        _readbackRunner = readbackRunner;
        _translationRunner = translationRunner;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Multi-turn voice session[/]:");
        if (_audioOptions.UseVoiceActivityDetection)
        {
            AnsiConsole.MarkupLine(
                "  1. At [yellow]Ready[/], press [yellow]Enter[/] to record — stops after silence (VAD) or press [yellow]Enter[/] again.");
        }
        else
        {
            AnsiConsole.MarkupLine("  1. At [yellow]Ready[/], press [yellow]Enter[/] to start recording.");
            AnsiConsole.MarkupLine("  2. [bold]Speak[/], then press [yellow]Enter[/] again to stop.");
        }

        AnsiConsole.MarkupLine("  2. Optional [dim]readback[/] and [dim]translation[/] after each transcript (see config).");
        AnsiConsole.MarkupLine("  3. Repeat for follow-up questions — prior lines stay in the log.");
        AnsiConsole.MarkupLine("  4. Type [yellow]q[/] + Enter at Ready to exit.");
        if (_llmOptions.StreamResponses)
        {
            AnsiConsole.MarkupLine("  [dim]LM streaming enabled — assistant text appears as it is generated.[/]");
        }

        AnsiConsole.WriteLine();

        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.Markup("[bold]Ready — press Enter to record[/] ([yellow]q[/] to exit): ");
            var command = Console.ReadLine();

            if (SessionInput.IsReturnToMenu(command))
            {
                break;
            }

            string? recordedPath = null;
            string? ttsPath = null;

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

                _transcriptLog.AddUserUtterance(transcription.UserDisplayText);
                AnsiConsole.WriteLine();
                SessionTranscriptRenderer.Render(_transcriptLog);

                if (!transcription.HasSpeech)
                {
                    AnsiConsole.WriteLine();
                    continue;
                }

                var readbackPerformed = await _readbackRunner.RunAsync(
                    transcription.UserDisplayText,
                    recordedPath,
                    _transcriptLog,
                    cancellationToken);

                if (readbackPerformed)
                {
                    AnsiConsole.WriteLine();
                    SessionTranscriptRenderer.Render(_transcriptLog);
                }

                await _translationRunner.RunAsync(
                    transcription.UserDisplayText,
                    _transcriptLog,
                    cancellationToken);

                VoiceTurnResult turn;
                if (_llmOptions.StreamResponses)
                {
                    turn = await RunStreamingAssistantTurnAsync(transcription.UserDisplayText, cancellationToken);
                }
                else
                {
                    turn = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Thinking...", async _ =>
                            await _orchestrator.CompleteTurnFromUserTextAsync(
                                transcription.UserDisplayText,
                                streamChunks: null,
                                cancellationToken));
                }

                if (!string.IsNullOrWhiteSpace(turn.LlmError))
                {
                    AnsiConsole.MarkupLine($"[red]Assistant reply failed:[/] {Markup.Escape(turn.LlmError)}");
                    AnsiConsole.WriteLine();
                    continue;
                }

                if (!turn.HasSpeech || string.IsNullOrWhiteSpace(turn.AssistantReply))
                {
                    AnsiConsole.WriteLine();
                    continue;
                }

                _transcriptLog.AddAssistantReply(turn.AssistantReply);
                AnsiConsole.WriteLine();
                SessionTranscriptRenderer.Render(_transcriptLog);

                try
                {
                    ttsPath = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Synthesizing speech...", async _ =>
                            await _textToSpeech.SynthesizeToWavFileAsync(
                                turn.AssistantReply,
                                cancellationToken));

                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Star)
                        .StartAsync("Speaking...", async _ =>
                            await _audioPlayback.PlayWavFileAsync(ttsPath, cancellationToken));
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]Audio playback failed:[/] {Markup.Escape(ex.Message)} [dim](assistant text remains above)[/]");
                }

                AnsiConsole.WriteLine();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.WriteLine();
            }
            finally
            {
                AudioRecordingHelper.TryDeleteFile(recordedPath);
                AudioRecordingHelper.TryDeleteFile(ttsPath);
            }
        }
    }

    private async Task<VoiceTurnResult> RunStreamingAssistantTurnAsync(
        string userDisplayText,
        CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[green]Assistant:[/] ");

        var buffer = new StringBuilder();
        var progress = new Progress<string>(chunk =>
        {
            buffer.Append(chunk);
            AnsiConsole.Markup(Markup.Escape(chunk));
        });

        var turn = await _orchestrator.CompleteTurnFromUserTextAsync(
            userDisplayText,
            progress,
            cancellationToken);

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        return turn;
    }

}
