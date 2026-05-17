using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AiVoiceTest.Infrastructure.Llm;

public static class LlmServiceCollectionExtensions
{
    public const string HttpClientName = "LlmChat";

    public static IServiceCollection AddLlmServices(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        services.AddSingleton<ILlmChatService, HttpLlmChatService>();

        return services;
    }
}
