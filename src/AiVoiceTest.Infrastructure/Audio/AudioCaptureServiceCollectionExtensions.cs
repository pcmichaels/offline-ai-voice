using AiVoiceTest.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.Audio;

public static class AudioCaptureServiceCollectionExtensions
{
    public static IServiceCollection AddAudioCaptureServices(
        this IServiceCollection services,
        string repositoryRoot)
    {
        services.AddSingleton<IAudioCaptureService>(
            sp => new NaudioAudioCaptureService(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Configuration.AudioOptions>>(),
                repositoryRoot));

        services.AddSingleton<IAudioPlaybackService, NaudioAudioPlaybackService>();

        return services;
    }
}
