namespace AiVoiceTest.Paths;

public static class RepositoryPaths
{
    public static string FindRepositoryRoot()
    {
        foreach (var directory in EnumerateCandidateRoots())
        {
            if (Directory.Exists(Path.Combine(directory, "data")))
            {
                return directory;
            }
        }

        throw new DirectoryNotFoundException(
            "Could not locate repository root (expected a 'data' folder). " +
            "Run from the repository root or ensure data/appsettings.json exists.");
    }

    public static string GetAppSettingsPath(string repositoryRoot) =>
        Path.Combine(repositoryRoot, "data", "appsettings.json");

    private static IEnumerable<string> EnumerateCandidateRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            if (string.IsNullOrWhiteSpace(start))
            {
                continue;
            }

            var fullStart = Path.GetFullPath(start);
            var current = fullStart;

            while (seen.Add(current))
            {
                yield return current;

                var parent = Directory.GetParent(current);
                if (parent is null)
                {
                    break;
                }

                current = parent.FullName;
            }
        }
    }
}
