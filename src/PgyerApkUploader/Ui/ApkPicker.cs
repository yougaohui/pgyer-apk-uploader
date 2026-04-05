namespace PgyerApkUploader.Ui;

/// <summary>
/// When multiple APKs exist, lets the user pick by index (newest is default).
/// </summary>
public static class ApkPicker
{
    /// <param name="force">If true, returns the newest APK without prompting.</param>
    public static string SelectApk(string projectRoot, IReadOnlyList<FileInfo> apks, bool force)
    {
        if (apks.Count == 0)
            throw new InvalidOperationException(
                $"No .apk found under {projectRoot}. Build an APK or pass --apk PATH.");

        if (force)
            return apks[0].FullName;

        if (apks.Count == 1)
            return apks[0].FullName;

        if (Console.IsInputRedirected)
            throw new InvalidOperationException(
                "Multiple APKs found but stdin is redirected. Pass --apk PATH, or use --force to upload the newest.");

        Console.WriteLine();
        Console.WriteLine($"Found {apks.Count} APK file(s) under {projectRoot} (newest first):");
        for (var i = 0; i < apks.Count; i++)
        {
            var f = apks[i];
            var label = TryRelativePath(projectRoot, f.FullName);
            Console.WriteLine(
                $"  [{i + 1}] {f.LastWriteTime:yyyy-MM-dd HH:mm}  {f.Length / (1024.0 * 1024.0):F2} MB  {label}");
        }

        Console.WriteLine();
        Console.WriteLine("Enter index (1-N), or press Enter for [1]:");
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null)
                throw new InvalidOperationException("No input; cancelled.");
            if (string.IsNullOrWhiteSpace(line))
                return apks[0].FullName;
            if (int.TryParse(line.Trim(), out var idx) && idx >= 1 && idx <= apks.Count)
                return apks[idx - 1].FullName;
            Console.WriteLine($"Enter a number from 1 to {apks.Count}, or Enter for default.");
        }
    }

    private static string TryRelativePath(string root, string fullPath)
    {
        try
        {
            var rel = Path.GetRelativePath(root, fullPath);
            if (!rel.StartsWith("..", StringComparison.Ordinal))
                return rel;
        }
        catch
        {
            // ignore
        }

        return fullPath;
    }
}
