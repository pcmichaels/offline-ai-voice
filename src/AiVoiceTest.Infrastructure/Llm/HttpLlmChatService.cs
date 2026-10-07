using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiVoiceTest.Core.Chat;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;

namespace AiVoiceTest.Infrastructure.Llm;

public sealed class HttpLlmChatService : ILlmChatService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmOptions _llmOptions;
    private readonly SessionOptions _sessionOptions;
    private readonly List<ChatMessage> _history = [];

    public HttpLlmChatService(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> llmOptions,
        IOptions<SessionOptions> sessionOptions)
    {
        _httpClientFactory = httpClientFactory;
        _llmOptions = llmOptions.Value;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<ServiceHealthReport> CheckConnectivityAsync(
        CancellationToken cancellationToken = default)
    {
        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/models");

        try
        {
            var client = _httpClientFactory.CreateClient(LlmServiceCollectionExtensions.HttpClientName);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await client.GetAsync(endpoint, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return Unreachable(endpoint, $"HTTP {(int)response.StatusCode}");
            }

            return new ServiceHealthReport(
                "LM Studio",
                endpoint,
                IsHealthy: true,
                StatusLabel: "healthy",
                _llmOptions.Model);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unreachable(endpoint, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return Unreachable(endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            return Unreachable(endpoint, ex.Message);
        }
    }

    public async Task<string> SendUserMessageAsync(
        string userText,
        IProgress<string>? streamChunks = null,
        CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatMessage(ChatRoles.User, userText));
        ChatHistoryTrimmer.TrimInPlace(_history, _sessionOptions.MaxHistoryMessages);

        var request = new LlmChatCompletionRequest
        {
            Model = _llmOptions.Model,
            Messages = BuildMessagesPayload(),
            Temperature = _llmOptions.Temperature,
            MaxTokens = _llmOptions.MaxTokens,
            Stream = _llmOptions.StreamResponses && streamChunks is not null,
        };

        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/chat/completions");
        var client = _httpClientFactory.CreateClient(LlmServiceCollectionExtensions.HttpClientName);

        LlmConsoleEcho.LogRequest(_llmOptions, "assistant-chat", endpoint, request);

        try
        {
            var assistantText = request.Stream
                ? await SendStreamingAsync(endpoint, request, client, streamChunks!, cancellationToken)
                : await SendBufferedAsync(endpoint, request, client, cancellationToken);

            if (string.IsNullOrWhiteSpace(assistantText))
            {
                throw new InvalidOperationException("LM Studio returned an empty assistant message.");
            }

            _history.Add(new ChatMessage(ChatRoles.Assistant, assistantText));
            ChatHistoryTrimmer.TrimInPlace(_history, _sessionOptions.MaxHistoryMessages);

            LlmConsoleEcho.LogResponse(_llmOptions, "assistant-chat", assistantText);

            return assistantText;
        }
        catch
        {
            RemoveTrailingUserMessage(userText);
            throw;
        }
    }

    private async Task<string> SendBufferedAsync(
        string endpoint,
        LlmChatCompletionRequest request,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        await LlmConsoleEcho.EnsureSuccessAsync(response, "assistant-chat", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LlmChatCompletionResponse>(cancellationToken);
        return payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
    }

    private async Task<string> SendStreamingAsync(
        string endpoint,
        LlmChatCompletionRequest request,
        HttpClient client,
        IProgress<string> streamChunks,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(request),
        };

        using var response = await client.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await LlmConsoleEcho.EnsureSuccessAsync(response, "assistant-chat", cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var builder = new StringBuilder();
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (data == "[DONE]")
            {
                break;
            }

            var chunk = ParseStreamDeltaContent(data);
            if (string.IsNullOrEmpty(chunk))
            {
                continue;
            }

            builder.Append(chunk);
            streamChunks.Report(chunk);
        }

        return builder.ToString().Trim();
    }

    private static string? ParseStreamDeltaContent(string jsonData)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonData);
            if (!doc.RootElement.TryGetProperty("choices", out var choices)
                || choices.GetArrayLength() == 0)
            {
                return null;
            }

            var delta = choices[0].GetProperty("delta");
            if (delta.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private List<LlmChatMessageDto> BuildMessagesPayload()
    {
        var messages = LlmConversationMessages.BuildPayload(
            _llmOptions.SystemPrompt,
            _history,
            _sessionOptions.MaxHistoryMessages);

        return messages.Select(m => new LlmChatMessageDto(m.Role, m.Content)).ToList();
    }

    private void RemoveTrailingUserMessage(string userText)
    {
        if (_history.Count == 0)
        {
            return;
        }

        var last = _history[^1];
        if (last.Role == ChatRoles.User
            && string.Equals(last.Content, userText, StringComparison.Ordinal))
        {
            _history.RemoveAt(_history.Count - 1);
        }
    }

    private static ServiceHealthReport Unreachable(string endpoint, string detail) =>
        new(
            "LM Studio",
            endpoint,
            IsHealthy: false,
            StatusLabel: "unreachable",
            detail);

    private static string CombineUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}{path}";
    }

}
