using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;

namespace AiVoiceTest.Infrastructure.SpeechToText;

public sealed class HttpSpeechToTextService : ISpeechToTextService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SttOptions _options;

    public HttpSpeechToTextService(
        IHttpClientFactory httpClientFactory,
        IOptions<SttOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<string> TranscribeAsync(
        string wavFilePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(wavFilePath))
        {
            throw new FileNotFoundException("Recorded audio file was not found.", wavFilePath);
        }

        var endpoint = CombineUrl(_options.ServiceUrl, "/v1/transcribe");
        var client = _httpClientFactory.CreateClient(SpeechToTextServiceCollectionExtensions.HttpClientName);

        await using var fileStream = File.OpenRead(wavFilePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(fileContent, "file", Path.GetFileName(wavFilePath));

        using var response = await client.PostAsync(endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TranscribeResponse>(cancellationToken);
        return payload?.Text ?? string.Empty;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}{path}";
    }

    private sealed class TranscribeResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
