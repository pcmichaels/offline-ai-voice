namespace AiVoiceTest.Core.Session;

public static class TranslationLabels
{
    public const string Prefix = "Translation";

    public static string FormatLine(string languageDisplayName, string translatedText) =>
        $"{Prefix} ({languageDisplayName}): {translatedText}";
}
