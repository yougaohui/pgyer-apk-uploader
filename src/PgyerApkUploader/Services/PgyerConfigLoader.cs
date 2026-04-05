using System.Text.Json;

namespace PgyerApkUploader.Services;

public static class PgyerConfigLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Search order: project root, %USERPROFILE%, exe directory (same as ApkUpload.ps1 primary + script dir).
    /// </summary>
    public static PgyerMergedConfig? Load(string projectRoot, string? exeDirectory)
    {
        var primary = new[]
        {
            Path.Combine(projectRoot, "pgyer_config.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "pgyer_config.json"),
        };

        foreach (var path in primary.Concat(string.IsNullOrEmpty(exeDirectory) ? Array.Empty<string>() : new[] { Path.Combine(exeDirectory, "pgyer_config.json") }))
        {
            var cfg = TryLoadPrimary(path);
            if (cfg is not null)
                return cfg;
        }

        var legacy = new[]
        {
            Path.Combine(projectRoot, "pgyer-upload.local.json"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".pgyer-upload.local.json"),
        };

        foreach (var path in legacy.Concat(string.IsNullOrEmpty(exeDirectory) ? Array.Empty<string>() : new[] { Path.Combine(exeDirectory, "pgyer-upload.local.json") }))
        {
            var cfg = TryLoadLegacy(path);
            if (cfg is not null)
                return cfg;
        }

        return null;
    }

    private static PgyerMergedConfig? TryLoadPrimary(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var raw = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var obj = JsonSerializer.Deserialize<PgyerConfigJson>(raw, JsonOptions);
            if (obj is null)
                return null;

            var text = "";
            if (!string.IsNullOrWhiteSpace(obj.UpdateDescription))
                text = obj.UpdateDescription.Trim();
            if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(obj.Note))
                text = obj.Note!.Trim();

            string? installType = null;
            if (!string.IsNullOrWhiteSpace(obj.InstallType))
                installType = obj.InstallType.Trim();

            string? password = null;
            if (!string.IsNullOrWhiteSpace(obj.Password))
                password = obj.Password;

            return new PgyerMergedConfig
            {
                RemarkText = text,
                SourcePath = path,
                InstallType = installType,
                Password = password
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: could not read pgyer_config.json '{path}': {ex.Message}");
            return null;
        }
    }

    private static PgyerMergedConfig? TryLoadLegacy(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            var raw = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(raw))
                return null;
            var obj = JsonSerializer.Deserialize<LegacyPgyerUploadJson>(raw, JsonOptions);
            if (obj is null || string.IsNullOrWhiteSpace(obj.UpdateDescription))
                return null;
            return new PgyerMergedConfig
            {
                RemarkText = obj.UpdateDescription.Trim(),
                SourcePath = path,
                InstallType = null,
                Password = null
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: could not read legacy Pgyer config '{path}': {ex.Message}");
            return null;
        }
    }
}
