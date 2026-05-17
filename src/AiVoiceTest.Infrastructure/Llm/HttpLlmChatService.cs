using System.Net.Http.Json;
using System.Text.Json.Serialization;
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
        CancellationToken cancellationToken = default)
    {
        _history.Add(new ChatMessage(ChatRoles.User, userText));
        ChatHistoryTrimmer.TrimInPlace(_history, _sessionOptions.MaxHistoryMessages);

        var request = new ChatCompletionRequest
        {
            Model = _llmOptions.Model,
            Messages = BuildMessagesPayload(),
            Temperature = _llmOptions.Temperature,
            MaxTokens = _llmOptions.MaxTokens,
        };

        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/chat/completions");
        var client = _httpClientFactory.CreateClient(LlmServiceCollectionExtensions.HttpClientName);

        try
        {
            using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
            var assistantText = payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

            if (string.IsNullOrWhiteSpace(assistantText))
            {
                throw new InvalidOperationException("LM Studio returned an empty assistant message.");
            }

            _history.Add(new ChatMessage(ChatRoles.Assistant, assistantText));
            ChatHistoryTrimmer.TrimInPlace(_history, _sessionOptions.MaxHistoryMessages);

            return assistantText;
        }
        catch
        {
            RemoveTrailingUserMessage(userText);
            throw;
        }
    }

    private List<ChatMessageDto> BuildMessagesPayload()
    {
        var messages = LlmConversationMessages.BuildPayload(
            _llmOptions.SystemPrompt,
            _history,
            _sessionOptions.MaxHistoryMessages);

        return messages.Select(m => new ChatMessageDto(m.Role, m.Content)).ToList();
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

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }
    }

    private sealed class ChatMessageDto
    {
        public ChatMessageDto(string role, string content)
        {
            Role = role;
            Content = content;
        }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        [JsonPropertyName("message")]
        public ResponseMessage? Message { get; set; }
    }

    private sealed class ResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
