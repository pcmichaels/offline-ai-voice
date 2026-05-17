using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Core.Session;
using Microsoft.Extensions.Options;

namespace AiVoiceTest.Infrastructure.Llm;

public sealed class HttpTranslationService : ITranslationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LlmOptions _llmOptions;

    public HttpTranslationService(
        IHttpClientFactory httpClientFactory,
        IOptions<LlmOptions> llmOptions)
    {
        _httpClientFactory = httpClientFactory;
        _llmOptions = llmOptions.Value;
    }

    public async Task<string> TranslateAsync(
        string sourceText,
        string targetLanguageCode,
        CancellationToken cancellationToken = default)
    {
        if (!TranslationLanguages.TryGetByCode(targetLanguageCode, out var language) || language is null)
        {
            throw new ArgumentException($"Unsupported translation language code: {targetLanguageCode}", nameof(targetLanguageCode));
        }

        var targetLanguage = language.Value;

        var request = new ChatCompletionRequest
        {
            Model = _llmOptions.Model,
            Messages =
            [
                new ChatMessageDto(
                    "system",
                    "You are a translator. Output only the translation, no commentary."),
                new ChatMessageDto(
                    "user",
                    $"Translate the following text to {targetLanguage.LlmLanguageName}:\n\n{sourceText}"),
            ],
            Temperature = 0.3,
            MaxTokens = _llmOptions.MaxTokens,
        };

        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/chat/completions");
        var client = _httpClientFactory.CreateClient(LlmServiceCollectionExtensions.HttpClientName);

        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
        var translated = payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(translated))
        {
            throw new InvalidOperationException("LM Studio returned an empty translation.");
        }

        return translated;
    }

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

    private sealed class ChatMessageDto(string role, string content)
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = role;

        [JsonPropertyName("content")]
        public string Content { get; set; } = content;
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
