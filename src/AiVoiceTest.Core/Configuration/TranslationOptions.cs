namespace AiVoiceTest.Core.Configuration;

public sealed class TranslationOptions
{
    public const string SectionName = "Translation";

    /// <summary>
    /// When set to es, zh, or de, skip the language picker and always translate.
    /// </summary>
    public string? DefaultTargetLanguage { get; set; }
}
