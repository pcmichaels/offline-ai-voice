namespace AiVoiceTest.Integration.Tests;

internal static class IntegrationTestGate
{
    public const string EnvironmentVariableName = "AI_VOICE_TEST_INTEGRATION";

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(EnvironmentVariableName),
            "1",
            StringComparison.Ordinal);
}
