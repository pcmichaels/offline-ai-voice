using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.TextToSpeech;

public static class TextToSpeechServiceCollectionExtensions
{
    public const string HttpClientName = "TextToSpeech";

    public static IServiceCollection AddTextToSpeechServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string repositoryRoot)
    {
        var ttsSection = configuration.GetSection(TtsOptions.SectionName);
        var mode = ttsSection.GetValue<string>(nameof(TtsOptions.Mode)) ?? "http";

        services.AddHttpClient(HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        if (!string.Equals(mode, "http", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"TTS mode '{mode}' is not implemented. Use 'http' with the Docker TTS service.");
        }

        services.AddSingleton<ITextToSpeechService>(sp =>
            new HttpTextToSpeechService(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TtsOptions>>(),
                repositoryRoot));

        return services;
    }
}
