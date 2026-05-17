namespace AiVoiceTest.Session;

internal static class SelfTestArgs
{
    private const string Flag = "--self-test";
    private const string LegacyFlag = "--mic-test";
    private const string SecondsPrefix = "--self-test-seconds=";
    private const string LegacySecondsPrefix = "--mic-test-seconds=";

    public static bool IsSelfTest(string[] args) =>
        args.Contains(Flag, StringComparer.OrdinalIgnoreCase)
        || args.Contains(LegacyFlag, StringComparer.OrdinalIgnoreCase);

    public static bool UsesLegacyMicTestAlias(string[] args) =>
        args.Contains(LegacyFlag, StringComparer.OrdinalIgnoreCase)
        || args.Any(a => a.StartsWith(LegacySecondsPrefix, StringComparison.OrdinalIgnoreCase));

    public static int? TryParseSecondsOverride(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.StartsWith(SecondsPrefix, StringComparison.OrdinalIgnoreCase)
                || arg.StartsWith(LegacySecondsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var prefix = arg.StartsWith(SecondsPrefix, StringComparison.OrdinalIgnoreCase)
                    ? SecondsPrefix
                    : LegacySecondsPrefix;
                var value = arg[prefix.Length..];
                if (int.TryParse(value, out var s) && s >= 2 && s <= 60)
                {
                    return s;
                }
            }
        }

        return null;
    }
}
