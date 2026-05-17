namespace AiVoiceTest.Core.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(
        string sourceText,
        string targetLanguageCode,
        CancellationToken cancellationToken = default);
}
