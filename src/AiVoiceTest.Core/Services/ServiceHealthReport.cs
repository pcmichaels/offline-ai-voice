namespace AiVoiceTest.Core.Services;

public sealed record ServiceHealthReport(
    string ServiceName,
    string Endpoint,
    bool IsHealthy,
    string StatusLabel,
    string? Detail = null);
