namespace AiVoiceTest.Session;

internal static class SessionInput
{
    /// <summary>
    /// True when the user wants to leave the current mode (voice chat exit or return to main menu).
    /// Enter alone starts recording; only an explicit quit command returns.
    /// </summary>
    public static bool IsReturnToMenu(string? input) =>
        string.Equals(input?.Trim(), "q", StringComparison.OrdinalIgnoreCase);
}
