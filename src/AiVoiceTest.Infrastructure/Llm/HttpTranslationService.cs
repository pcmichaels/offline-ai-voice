using System.Net.Http.Json;
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

        var request = new LlmChatCompletionRequest
        {
            Model = _llmOptions.Model,
            Messages =
            [
                new LlmChatMessageDto(
                    "system",
                    "You are a translator. Output only the translation, no commentary."),
                new LlmChatMessageDto(
                    "user",
                    $"Translate the following text to {targetLanguage.LlmLanguageName}:\n\n{sourceText}"),
            ],
            Temperature = 0.3,
            MaxTokens = _llmOptions.MaxTokens,
        };

        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/chat/completions");
        var client = _httpClientFactory.CreateClient(LlmServiceCollectionExtensions.HttpClientName);

        LlmConsoleEcho.LogRequest(_llmOptions, "translation", endpoint, request);

        using var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
        await LlmConsoleEcho.EnsureSuccessAsync(response, "translation", cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<LlmChatCompletionResponse>(cancellationToken);
        var translated = payload?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(translated))
        {
            throw new InvalidOperationException("LM Studio returned an empty translation.");
        }

        LlmConsoleEcho.LogResponse(_llmOptions, "translation", translated);

        return translated;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}{path}";
    }
}
