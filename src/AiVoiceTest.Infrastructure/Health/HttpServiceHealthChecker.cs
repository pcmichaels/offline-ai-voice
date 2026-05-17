using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Services;
using Microsoft.Extensions.Options;

namespace AiVoiceTest.Infrastructure.Health;

public sealed class HttpServiceHealthChecker : IServiceHealthChecker
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SttOptions _sttOptions;
    private readonly TtsOptions _ttsOptions;
    private readonly LlmOptions _llmOptions;

    public HttpServiceHealthChecker(
        IHttpClientFactory httpClientFactory,
        IOptions<SttOptions> sttOptions,
        IOptions<TtsOptions> ttsOptions,
        IOptions<LlmOptions> llmOptions)
    {
        _httpClientFactory = httpClientFactory;
        _sttOptions = sttOptions.Value;
        _ttsOptions = ttsOptions.Value;
        _llmOptions = llmOptions.Value;
    }

    public Task<ServiceHealthReport> CheckSttAsync(CancellationToken cancellationToken = default) =>
        CheckServiceHealthEndpointAsync("STT", _sttOptions.ServiceUrl, cancellationToken);

    public Task<ServiceHealthReport> CheckTtsAsync(CancellationToken cancellationToken = default) =>
        CheckServiceHealthEndpointAsync("TTS", _ttsOptions.ServiceUrl, cancellationToken);

    public Task<ServiceHealthReport> CheckLlmAsync(CancellationToken cancellationToken = default) =>
        CheckLlmEndpointAsync(cancellationToken);

    private async Task<ServiceHealthReport> CheckLlmEndpointAsync(CancellationToken cancellationToken)
    {
        var endpoint = CombineUrl(_llmOptions.BaseUrl, "/v1/models");

        try
        {
            var client = _httpClientFactory.CreateClient(HttpServiceHealthCheckerExtensions.HttpClientName);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            using var response = await client.GetAsync(endpoint, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceHealthReport(
                    "LM Studio",
                    endpoint,
                    IsHealthy: false,
                    StatusLabel: "unhealthy",
                    Detail: $"HTTP {(int)response.StatusCode}");
            }

            return new ServiceHealthReport(
                "LM Studio",
                endpoint,
                IsHealthy: true,
                StatusLabel: "healthy",
                Detail: _llmOptions.Model);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unreachable("LM Studio", endpoint, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return Unreachable("LM Studio", endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            return Unreachable("LM Studio", endpoint, ex.Message);
        }
    }

    private async Task<ServiceHealthReport> CheckServiceHealthEndpointAsync(
        string serviceName,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        var endpoint = CombineUrl(baseUrl, "/health");

        try
        {
            var client = _httpClientFactory.CreateClient(HttpServiceHealthCheckerExtensions.HttpClientName);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(RequestTimeout);

            using var response = await client.GetAsync(endpoint, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceHealthReport(
                    serviceName,
                    endpoint,
                    IsHealthy: false,
                    StatusLabel: "unhealthy",
                    Detail: $"HTTP {(int)response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(cts.Token);
            var isHealthy = string.Equals(payload?.Status, "healthy", StringComparison.OrdinalIgnoreCase);

            return new ServiceHealthReport(
                serviceName,
                endpoint,
                isHealthy,
                isHealthy ? "healthy" : "unhealthy",
                payload?.Model ?? payload?.VoiceModel);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unreachable(serviceName, endpoint, "Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return Unreachable(serviceName, endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            return Unreachable(serviceName, endpoint, ex.Message);
        }
    }

    private static ServiceHealthReport Unreachable(
        string serviceName,
        string endpoint,
        string detail) =>
        new(serviceName, endpoint, false, "unreachable", detail);

    private static string CombineUrl(string baseUrl, string path)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return $"{trimmed}{path}";
    }

    private sealed class HealthResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("voice_model")]
        public string? VoiceModel { get; set; }
    }
}
