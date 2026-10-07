using System.Text.Json.Serialization;

namespace AiVoiceTest.Infrastructure.Llm;

internal sealed class LlmChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<LlmChatMessageDto> Messages { get; set; } = [];

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

internal sealed class LlmChatMessageDto
{
    public LlmChatMessageDto(string role, string content)
    {
        Role = role;
        Content = content;
    }

    [JsonPropertyName("role")]
    public string Role { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }
}

internal sealed class LlmChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<LlmChatChoice>? Choices { get; set; }
}

internal sealed class LlmChatChoice
{
    [JsonPropertyName("message")]
    public LlmChatResponseMessage? Message { get; set; }
}

internal sealed class LlmChatResponseMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
