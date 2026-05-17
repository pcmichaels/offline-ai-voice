namespace AiVoiceTest.Core.Session;

public static class TranslationLanguages
{
    public const string SpanishCode = "es";
    public const string MandarinCode = "zh";
    public const string GermanCode = "de";

    public static IReadOnlyList<TranslationLanguage> All { get; } =
    [
        new TranslationLanguage(SpanishCode, "Spanish", "Spanish"),
        new TranslationLanguage(MandarinCode, "Mandarin", "Mandarin Chinese (Simplified)"),
        new TranslationLanguage(GermanCode, "German", "German"),
    ];

    public static bool TryGetByCode(string? code, out TranslationLanguage? language)
    {
        language = null;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        foreach (var candidate in All)
        {
            if (!string.Equals(candidate.Code, code.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            language = candidate;
            return true;
        }

        return false;
    }
}

public readonly record struct TranslationLanguage(string Code, string DisplayName, string LlmLanguageName);
