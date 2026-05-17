namespace AiVoiceTest.Core.Services;

public interface ILlmChatService
{
    Task<ServiceHealthReport> CheckConnectivityAsync(CancellationToken cancellationToken = default);

    Task<string> SendUserMessageAsync(
        string userText,
        IProgress<string>? streamChunks = null,
        CancellationToken cancellationToken = default);
}
