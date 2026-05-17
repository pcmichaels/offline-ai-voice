namespace AiVoiceTest.Core.Services;

public interface ILlmChatService
{
    Task<ServiceHealthReport> CheckConnectivityAsync(CancellationToken cancellationToken = default);

    Task<string> SendUserMessageAsync(string userText, CancellationToken cancellationToken = default);
}
