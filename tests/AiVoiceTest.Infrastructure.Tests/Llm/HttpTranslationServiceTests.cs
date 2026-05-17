using System.Net;
using System.Text;
using AiVoiceTest.Core.Configuration;
using AiVoiceTest.Core.Session;
using AiVoiceTest.Infrastructure.Llm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AiVoiceTest.Infrastructure.Tests.Llm;

public sealed class HttpTranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_SendsOneShotRequest_ReturnsTranslation()
    {
        const string responseJson =
            """{"choices":[{"message":{"role":"assistant","content":"Hola"}}]}""";

        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });

        var services = new ServiceCollection();
        services
            .AddHttpClient(LlmServiceCollectionExtensions.HttpClientName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.Configure<LlmOptions>(o =>
        {
            o.BaseUrl = "http://localhost:1234";
            o.Model = "local-model";
            o.MaxTokens = 256;
        });

        var provider = services.BuildServiceProvider();
        var sut = new HttpTranslationService(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<IOptions<LlmOptions>>());

        var result = await sut.TranslateAsync("Hello", TranslationLanguages.SpanishCode);

        Assert.Equal("Hola", result);
        Assert.Contains("translator", handler.LastRequestBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Spanish", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("Hello", handler.LastRequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TranslateAsync_WhenUnsupportedCode_Throws()
    {
        var services = new ServiceCollection();
        services.AddHttpClient(LlmServiceCollectionExtensions.HttpClientName);
        services.Configure<LlmOptions>(o => o.BaseUrl = "http://localhost:1234");

        var provider = services.BuildServiceProvider();
        var sut = new HttpTranslationService(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<IOptions<LlmOptions>>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.TranslateAsync("Hello", "fr"));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public string LastRequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            return _responder(request);
        }
    }
}
