namespace PgyerApkUploader.Services;

/// <summary>
/// Resolves a working directory for APK scan and config. Prefers an Android Gradle root when found.
/// </summary>
public static class ProjectLocator
{
    /// <summary>
    /// Explicit path or <c>APKUPLOAD_PROJECT_ROOT</c> must exist. Gradle wrapper is not required unless you use <c>--build</c>.
    /// Otherwise: walk up from <paramref name="cwd"/> / exe dir for <c>gradlew</c>; if none, use current directory.
    /// </summary>
    public static string ResolveWorkingRoot(string? explicitRoot, string? cwd = null)
    {
        cwd ??= Directory.GetCurrentDirectory();

        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            if (!Directory.Exists(explicitRoot))
                throw new InvalidOperationException($"Directory not found: {explicitRoot}");
            return Path.GetFullPath(explicitRoot);
        }

        var envRoot = Environment.GetEnvironmentVariable("APKUPLOAD_PROJECT_ROOT");
        if (!string.IsNullOrWhiteSpace(envRoot))
        {
            if (!Directory.Exists(envRoot))
                throw new InvalidOperationException($"APKUPLOAD_PROJECT_ROOT is not a valid path: {envRoot}");
            return Path.GetFullPath(envRoot);
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

        return Path.GetFullPath(cwd);
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

    public static bool HasGradleWrapper(string dir) =>
        File.Exists(Path.Combine(dir, "gradlew.bat")) || File.Exists(Path.Combine(dir, "gradlew"));
}
