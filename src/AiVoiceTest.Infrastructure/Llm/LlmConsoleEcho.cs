using AiVoiceTest.Core.Configuration;

namespace AiVoiceTest.Infrastructure.Llm;

internal static class LlmConsoleEcho
{
    public static void LogRequest(
        LlmOptions options,
        string operation,
        string endpoint,
        LlmChatCompletionRequest request)
    {
        if (!options.LogRequestsToConsole)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"── LLM [{operation}] POST {endpoint}");
        Console.WriteLine($"   model: {request.Model}");
        Console.WriteLine($"   temperature: {request.Temperature}, max_tokens: {request.MaxTokens}, stream: {request.Stream}");
        Console.WriteLine("   messages:");

        foreach (var message in request.Messages)
        {
            var preview = Truncate(message.Content, 200);
            Console.WriteLine($"     [{message.Role}] {preview}");
        }

        Console.WriteLine("   (translation and assistant chat both use LM Studio /v1/chat/completions)");
    }

    public static void LogResponse(LlmOptions options, string operation, string contentPreview)
    {
        if (!options.LogRequestsToConsole)
        {
            return;
        }

        Console.WriteLine($"── LLM [{operation}] response: {Truncate(contentPreview, 300)}");
        Console.WriteLine();
    }

    public static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body)
            ? response.ReasonPhrase ?? "Unknown error"
            : Truncate(body, 800);

        throw new HttpRequestException(
            $"LM Studio [{operation}] HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {detail}");
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }
}
