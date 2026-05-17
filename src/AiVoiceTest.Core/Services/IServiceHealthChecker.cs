namespace AiVoiceTest.Core.Services;

public interface IServiceHealthChecker
{
    Task<ServiceHealthReport> CheckSttAsync(CancellationToken cancellationToken = default);

    Task<ServiceHealthReport> CheckTtsAsync(CancellationToken cancellationToken = default);

    Task<ServiceHealthReport> CheckLlmAsync(CancellationToken cancellationToken = default);
}
