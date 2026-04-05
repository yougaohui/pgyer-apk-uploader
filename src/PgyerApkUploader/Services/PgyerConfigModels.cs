using System.Text.Json.Serialization;

namespace PgyerApkUploader.Services;

public sealed class PgyerMergedConfig
{
    public string RemarkText { get; init; } = "";
    public string? SourcePath { get; init; }
    public string? InstallType { get; init; }
    public string? Password { get; init; }
}

internal sealed class PgyerConfigJson
{
    [JsonPropertyName("update_description")]
    public string? UpdateDescription { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("install_type")]
    public string? InstallType { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}

internal sealed class LegacyPgyerUploadJson
{
    [JsonPropertyName("updateDescription")]
    public string? UpdateDescription { get; set; }
}
