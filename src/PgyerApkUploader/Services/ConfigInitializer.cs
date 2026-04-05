using System.Text.Json;

namespace PgyerApkUploader.Services;

/// <summary>
/// Creates default Pgyer config files in a project directory when missing (does not overwrite).
/// </summary>
public static class ConfigInitializer
{
    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    private const string ApiKeyTemplate = """
        # Paste your Pgyer API key on the line below (Dashboard > API).
        # Lines starting with # are ignored. Or set environment variable PGYER_API_KEY.


        """;

    /// <summary>
    /// Writes <c>pgyer_config.json</c> and/or <c>pgyer-api-key.local</c> if they do not exist.
    /// </summary>
    public static void Initialize(string projectRoot)
    {
        projectRoot = Path.GetFullPath(projectRoot);
        Console.WriteLine($"Initializing Pgyer config files under: {projectRoot}");
        Console.WriteLine();

        var configPath = Path.Combine(projectRoot, "pgyer_config.json");
        if (File.Exists(configPath))
        {
            Console.WriteLine($"Skip (already exists): {configPath}");
        }
        else
        {
            var json = new PgyerConfigJson
            {
                UpdateDescription = "",
                Note = "",
                InstallType = "",
                Password = ""
            };
            var text = JsonSerializer.Serialize(json, JsonWriteOptions);
            File.WriteAllText(configPath, text);
            Console.WriteLine($"Created: {configPath}");
        }

        var keyPath = Path.Combine(projectRoot, "pgyer-api-key.local");
        if (File.Exists(keyPath))
        {
            Console.WriteLine($"Skip (already exists): {keyPath}");
        }
        else
        {
            File.WriteAllText(keyPath, ApiKeyTemplate);
            Console.WriteLine($"Created: {keyPath}");
        }

        Console.WriteLine();
        Console.WriteLine("Edit the files above, or set PGYER_API_KEY. Then run PgyerApkUploader without --init.");
    }
}
