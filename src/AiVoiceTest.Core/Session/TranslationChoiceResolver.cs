namespace AiVoiceTest.Core.Session;

public static class TranslationChoiceResolver
{
    public static readonly IReadOnlyList<string> PromptChoices = ["Skip", "Spanish", "Mandarin", "German"];

    public static TranslationLanguage? ResolvePromptChoice(string? choice) =>
        choice switch
        {
            "Spanish" => Find(TranslationLanguages.SpanishCode),
            "Mandarin" => Find(TranslationLanguages.MandarinCode),
            "German" => Find(TranslationLanguages.GermanCode),
            _ => null,
        };

    private static TranslationLanguage Find(string code) =>
        TranslationLanguages.All.First(l => l.Code == code);
}
