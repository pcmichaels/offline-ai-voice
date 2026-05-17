using AiVoiceTest.Core.Session;

namespace AiVoiceTest.Core.Configuration;

public static class AppConfigurationValidator
{
    public static IReadOnlyList<string> Validate(
        LlmOptions llm,
        SttOptions stt,
        TtsOptions tts,
        AudioOptions audio,
        SessionOptions session,
        SelfTestOptions selfTest,
        PostTranscriptionOptions postTranscription,
        TranslationOptions translation)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(llm.BaseUrl))
        {
            errors.Add("Llm:BaseUrl is required.");
        }
        else if (!Uri.TryCreate(llm.BaseUrl, UriKind.Absolute, out _))
        {
            errors.Add("Llm:BaseUrl must be an absolute URL.");
        }

        if (string.IsNullOrWhiteSpace(llm.Model))
        {
            errors.Add("Llm:Model is required.");
        }

        ValidateHttpServiceMode(stt.Mode, stt.ServiceUrl, "Stt", errors);

        ValidateHttpServiceMode(tts.Mode, tts.ServiceUrl, "Tts", errors);

        if (audio.SampleRate <= 0)
        {
            errors.Add("Audio:SampleRate must be greater than zero.");
        }

        if (session.MaxHistoryMessages < 0)
        {
            errors.Add("Session:MaxHistoryMessages cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(selfTest.Phrase))
        {
            errors.Add("SelfTest:Phrase is required.");
        }

        if (selfTest.DurationSeconds is < 2 or > 60)
        {
            errors.Add("SelfTest:DurationSeconds must be between 2 and 60.");
        }

        if (audio.MaxRecordingSeconds < 5)
        {
            errors.Add("Audio:MaxRecordingSeconds must be at least 5.");
        }

        if (audio.SilenceDurationMs < 300)
        {
            errors.Add("Audio:SilenceDurationMs must be at least 300.");
        }

        if (!string.IsNullOrWhiteSpace(translation.DefaultTargetLanguage)
            && !TranslationLanguages.TryGetByCode(
                translation.DefaultTargetLanguage,
                out _))
        {
            errors.Add("Translation:DefaultTargetLanguage must be es, zh, or de when set.");
        }

        if (postTranscription.OfferReadback
            && !postTranscription.ReadbackUsesTts
            && !postTranscription.OfferRecordingPlayback)
        {
            errors.Add(
                "PostTranscription: enable ReadbackUsesTts and/or OfferRecordingPlayback when OfferReadback is true.");
        }

        return errors;
    }

    private static void ValidateHttpServiceMode(
        string mode,
        string serviceUrl,
        string sectionName,
        List<string> errors)
    {
        if (!string.Equals(mode, "http", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(serviceUrl))
        {
            errors.Add($"{sectionName}:ServiceUrl is required when {sectionName}:Mode is 'http'.");
            return;
        }

        if (!Uri.TryCreate(serviceUrl, UriKind.Absolute, out _))
        {
            errors.Add($"{sectionName}:ServiceUrl must be an absolute URL.");
        }
    }
}
