namespace PgyerApkUploader.Services;

/// <summary>
/// Finds Android Gradle project root (folder containing gradlew.bat or gradlew).
/// </summary>
public static class ProjectLocator
{
    /// <summary>
    /// Resolves project root: explicit path, then APKUPLOAD_PROJECT_ROOT, then walk up from cwd, then exe directory.
    /// </summary>
    public static string ResolveProjectRoot(string? explicitRoot, string? cwd = null)
    {
        cwd ??= Directory.GetCurrentDirectory();

        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            if (!Directory.Exists(explicitRoot))
                throw new InvalidOperationException($"Project root not found: {explicitRoot}");
            var r = Path.GetFullPath(explicitRoot);
            if (!HasGradleWrapper(r))
                throw new InvalidOperationException($"Project root must contain gradlew.bat: {r}");
            return r;
        }

        var envRoot = Environment.GetEnvironmentVariable("APKUPLOAD_PROJECT_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            if (!Directory.Exists(envRoot))
                throw new InvalidOperationException($"APKUPLOAD_PROJECT_ROOT is not a valid path: {envRoot}");
            var r = Path.GetFullPath(envRoot);
            if (!HasGradleWrapper(r))
                throw new InvalidOperationException($"APKUPLOAD_PROJECT_ROOT must contain gradlew.bat: {r}");
            return r;
        }

        var fromCwd = FindGradleRoot(cwd);
        if (fromCwd is not null)
            return fromCwd;

        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(baseDir))
        {
            var fromExe = FindGradleRoot(baseDir);
            if (fromExe is not null)
                return fromExe;
        }

        throw new InvalidOperationException(
            "Could not find Android project root (gradlew.bat).\n" +
            "  - Open a terminal with current directory inside your project, or\n" +
            "  - Set user env var APKUPLOAD_PROJECT_ROOT to the project root, or\n" +
            "  - Pass --project \"D:\\path\\to\\project\"");
    }

    public static string? FindGradleRoot(string startPath)
    {
        var p = Path.GetFullPath(startPath);
        if (File.Exists(p))
            p = Path.GetDirectoryName(p) ?? p;

        while (!string.IsNullOrEmpty(p))
        {
            if (HasGradleWrapper(p))
                return p;
            var parent = Directory.GetParent(p)?.FullName;
            if (string.IsNullOrEmpty(parent) || parent == p)
                break;
            p = parent;
        }

        return null;
    }

    private static bool HasGradleWrapper(string dir)
    {
        return File.Exists(Path.Combine(dir, "gradlew.bat")) || File.Exists(Path.Combine(dir, "gradlew"));
    }
}
