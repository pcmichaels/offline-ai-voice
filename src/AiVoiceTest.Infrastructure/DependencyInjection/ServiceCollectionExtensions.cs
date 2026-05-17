using AiVoiceTest.Core.Services;
using AiVoiceTest.Infrastructure.Audio;
using AiVoiceTest.Infrastructure.Configuration;
using AiVoiceTest.Infrastructure.Health;
using AiVoiceTest.Infrastructure.Llm;
using AiVoiceTest.Infrastructure.Orchestration;
using AiVoiceTest.Infrastructure.SpeechToText;
using AiVoiceTest.Infrastructure.TextToSpeech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAiVoiceTestInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string repositoryRoot)
    {
        services.AddAiVoiceTestConfiguration(configuration);
        services.AddServiceHealthChecking();
        services.AddAudioCaptureServices(repositoryRoot);
        services.AddSpeechToTextServices(configuration, repositoryRoot);
        services.AddLlmServices();
        services.AddTextToSpeechServices(configuration, repositoryRoot);
        services.AddSingleton<IVoiceSessionOrchestrator, VoiceSessionOrchestrator>();

        return services;
    }
}
