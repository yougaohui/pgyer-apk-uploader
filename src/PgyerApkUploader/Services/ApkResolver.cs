namespace PgyerApkUploader.Services;

public static class ApkResolver
{
    public static string GetApkOutputDir(string projectRoot, string flavor, string buildType)
    {
        var dirName = string.Equals(buildType, "Release", StringComparison.OrdinalIgnoreCase)
            ? "release"
            : "debug";
        return Path.Combine(projectRoot, "app", "build", "outputs", "apk", flavor, dirName);
    }

    /// <summary>
    /// Newest .apk under flavor release/debug, or recursively under flavor folder.
    /// </summary>
    public static string ResolveNewestApk(string projectRoot, string flavor, string buildType)
    {
        var dir = GetApkOutputDir(projectRoot, flavor, buildType);
        var flavorRoot = Path.Combine(projectRoot, "app", "build", "outputs", "apk", flavor);

        FileInfo? apk = null;
        if (Directory.Exists(dir))
        {
            apk = new DirectoryInfo(dir)
                .EnumerateFiles("*.apk", SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
        }

        if (apk is null && Directory.Exists(flavorRoot))
        {
            apk = new DirectoryInfo(flavorRoot)
                .EnumerateFiles("*.apk", SearchOption.AllDirectories)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
        }

        if (apk is null)
        {
            const string hint = "Build the APK in Android Studio (Build > Build APK) or run with --build";
            if (!Directory.Exists(flavorRoot))
                throw new InvalidOperationException($"APK folder not found: {flavorRoot}. {hint}");
            throw new InvalidOperationException(
                $"No .apk found under flavor '{flavor}' (looked in {dir} and recursively under {flavorRoot}). {hint}");
        }

        return apk.FullName;
    }
}
