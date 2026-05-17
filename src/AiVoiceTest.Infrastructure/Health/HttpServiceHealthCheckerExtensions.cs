using AiVoiceTest.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.Health;

public static class HttpServiceHealthCheckerExtensions
{
    public const string HttpClientName = "ServiceHealth";

    public static IServiceCollection AddServiceHealthChecking(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<IServiceHealthChecker, HttpServiceHealthChecker>();

        return services;
    }
}
