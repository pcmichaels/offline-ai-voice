namespace AiVoiceTest.Core.Configuration;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    public string BaseUrl { get; set; } = "http://localhost:1234";

    public string Model { get; set; } = "local-model";

    public string SystemPrompt { get; set; } = "You are a helpful assistant.";

    public double Temperature { get; set; } = 0.7;

    public int MaxTokens { get; set; } = 512;
}
