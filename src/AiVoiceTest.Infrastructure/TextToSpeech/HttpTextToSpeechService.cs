using System.Net.Http.Json;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;

namespace AiVoiceTest.Infrastructure.TextToSpeech;

public sealed class HttpTextToSpeechService : ITextToSpeechService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TtsOptions _options;
    private readonly string _tempDirectory;

    public HttpTextToSpeechService(
        IHttpClientFactory httpClientFactory,
        IOptions<TtsOptions> options,
        string repositoryRoot)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _tempDirectory = Path.Combine(repositoryRoot, "data", "temp");
        Directory.CreateDirectory(_tempDirectory);
    }

    public async Task<string> SynthesizeToWavFileAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text to synthesize cannot be empty.", nameof(text));
        }

        var endpoint = CombineUrl(_options.ServiceUrl, "/v1/synthesize");
        var client = _httpClientFactory.CreateClient(TextToSpeechServiceCollectionExtensions.HttpClientName);

        using var response = await client.PostAsJsonAsync(
            endpoint,
            new { text },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var outputPath = Path.Combine(_tempDirectory, $"tts-{Guid.NewGuid():N}.wav");
        await using var output = File.Create(outputPath);
        await response.Content.CopyToAsync(output, cancellationToken);

        return outputPath;
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}{path}";
    }
}
