namespace PgyerApkUploader.Services;

/// <summary>
/// Matches <c>Get-GradleAssembleTask</c> in ApkUpload.ps1.
/// </summary>
public static class GradleTaskName
{
    public static string GetAssembleTask(string flavor, string buildType)
    {
        var suffix = char.ToUpperInvariant(buildType[0]) + buildType.Substring(1).ToLowerInvariant();
        if (string.Equals(flavor, "peerService", StringComparison.Ordinal))
            return $"assemblePeerService{suffix}";

        if (flavor.StartsWith("_", StringComparison.Ordinal))
            return $"assemble{flavor}{suffix}";

        var cap = flavor.Length > 0
            ? char.ToUpperInvariant(flavor[0]) + flavor.Substring(1)
            : flavor;
        return $"assemble{cap}{suffix}";
    }
}
