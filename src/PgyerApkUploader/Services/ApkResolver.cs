namespace PgyerApkUploader.Services;

public static class ApkResolver
{
    /// <summary>
    /// All <c>*.apk</c> under <paramref name="root"/> (recursive), newest first by last write time.
    /// </summary>
    public static IReadOnlyList<FileInfo> EnumerateApksSortedNewestFirst(string root)
    {
        var rootFull = Path.GetFullPath(root);
        if (!Directory.Exists(rootFull))
            throw new InvalidOperationException($"Directory not found: {rootFull}");

        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        var list = new List<FileInfo>();
        foreach (var f in new DirectoryInfo(rootFull).EnumerateFiles("*.apk", opts))
            list.Add(f);

        return list
            .GroupBy(f => f.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .ToList();
    }
}
