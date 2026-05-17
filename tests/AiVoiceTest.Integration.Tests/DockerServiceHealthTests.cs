using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using AiVoiceTest.Infrastructure.Health;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiVoiceTest.Integration.Tests;

[Trait("Category", "Integration")]
public sealed class DockerServiceHealthTests
{
    [SkippableFact]
    public async Task SttHealth_IsHealthy_WhenDockerSttIsRunning()
    {
        Skip.IfNot(IntegrationTestGate.IsEnabled, IntegrationTestGate.EnvironmentVariableName);

        var checker = CreateHealthChecker();

        var report = await checker.CheckSttAsync();

        Assert.True(report.IsHealthy, report.Detail ?? report.StatusLabel);
    }

    [SkippableFact]
    public async Task TtsHealth_IsHealthy_WhenDockerTtsIsRunning()
    {
        Skip.IfNot(IntegrationTestGate.IsEnabled, IntegrationTestGate.EnvironmentVariableName);

        var checker = CreateHealthChecker();

        var report = await checker.CheckTtsAsync();

        Assert.True(report.IsHealthy, report.Detail ?? report.StatusLabel);
    }

    [SkippableFact]
    public async Task LlmHealth_IsHealthy_WhenLmStudioIsRunning()
    {
        Skip.IfNot(IntegrationTestGate.IsEnabled, IntegrationTestGate.EnvironmentVariableName);

        var checker = CreateHealthChecker();

        var report = await checker.CheckLlmAsync();

        Assert.True(report.IsHealthy, report.Detail ?? report.StatusLabel);
    }

    private static IServiceHealthChecker CreateHealthChecker()
    {
        var services = new ServiceCollection();
        services.AddServiceHealthChecking();
        services.Configure<SttOptions>(o => o.ServiceUrl = "http://localhost:5001");
        services.Configure<TtsOptions>(o => o.ServiceUrl = "http://localhost:5002");
        services.Configure<LlmOptions>(o =>
        {
            o.BaseUrl = "http://localhost:1234";
            o.Model = "local-model";
        });

        return services.BuildServiceProvider().GetRequiredService<IServiceHealthChecker>();
    }
}
