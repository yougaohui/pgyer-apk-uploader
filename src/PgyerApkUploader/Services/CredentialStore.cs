namespace PgyerApkUploader.Services;

public static class CredentialStore
{
    /// <summary>
    /// PGYER_API_KEY env, then project/pgyer-api-key.local, %USERPROFILE%/.pgyer-api-key.local, exe dir.
    /// </summary>
    public static string ResolveApiKey(string projectRoot, string? exeDirectory)
    {
        var env = Environment.GetEnvironmentVariable("PGYER_API_KEY");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        var candidates = new List<string>
        {
            Path.Combine(projectRoot, "pgyer-api-key.local"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pgyer-api-key.local"),
        };
        if (!string.IsNullOrEmpty(exeDirectory))
            candidates.Add(Path.Combine(exeDirectory, "pgyer-api-key.local"));

        foreach (var file in candidates)
        {
            if (!File.Exists(file))
                continue;
            var line = ReadFirstNonCommentLine(file);
            if (!string.IsNullOrWhiteSpace(line))
                return line.Trim();
        }

        throw new InvalidOperationException(
            "Pgyer API key missing. Set user environment variable PGYER_API_KEY (recommended), or create:\n" +
            $"  {Path.Combine(projectRoot, "pgyer-api-key.local")}\n" +
            $"  or {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pgyer-api-key.local")}\n" +
            "(one line, your API Key from pgyer.com Dashboard > API)");
    }

    private static string? ReadFirstNonCommentLine(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var t = line.Trim();
            if (t.Length == 0)
                continue;
            if (t[0] == '#')
                continue;
            return t;
        }

        return null;
    }
}
