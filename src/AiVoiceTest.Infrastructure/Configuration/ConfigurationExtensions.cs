using AiVoiceTest.Core.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.Configuration;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddAiVoiceTestConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        services.Configure<SttOptions>(configuration.GetSection(SttOptions.SectionName));
        services.Configure<TtsOptions>(configuration.GetSection(TtsOptions.SectionName));
        services.Configure<AudioOptions>(configuration.GetSection(AudioOptions.SectionName));
        services.Configure<SessionOptions>(configuration.GetSection(SessionOptions.SectionName));
        services.Configure<SelfTestOptions>(configuration.GetSection(SelfTestOptions.SectionName));

        return services;
    }
}
