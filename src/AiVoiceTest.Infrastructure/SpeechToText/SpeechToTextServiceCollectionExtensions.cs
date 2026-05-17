using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.SpeechToText;

public static class SpeechToTextServiceCollectionExtensions
{
    public const string HttpClientName = "SpeechToText";

    public static IServiceCollection AddSpeechToTextServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string repositoryRoot)
    {
        var sttSection = configuration.GetSection(SttOptions.SectionName);
        var mode = sttSection.GetValue<string>(nameof(SttOptions.Mode)) ?? "http";

        services.AddHttpClient(HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });

        if (!string.Equals(mode, "http", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"STT mode '{mode}' is not implemented. Use 'http' with the Docker STT service.");
        }

        services.AddSingleton<ISpeechToTextService, HttpSpeechToTextService>();

        return services;
    }
}
