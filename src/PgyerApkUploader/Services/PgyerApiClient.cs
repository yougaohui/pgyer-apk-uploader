using System.Text.Json;
using System.Text.Json.Serialization;

namespace PgyerApkUploader.Services;

public sealed class PgyerApiClient
{
    private const string UploadUrl = "https://api.pgyer.com/apiv2/app/upload";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PgyerUploadResult> UploadAsync(
        string apkPath,
        string apiKey,
        string buildUpdateDescription,
        string? buildInstallType,
        string? buildPassword,
        CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.OpenRead(apkPath);
        var fileName = Path.GetFileName(apkPath);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(apiKey), "_api_key");
        content.Add(new StreamContent(fileStream), "file", fileName);

        if (!string.IsNullOrEmpty(buildUpdateDescription))
            content.Add(new StringContent(buildUpdateDescription), "buildUpdateDescription");
        if (!string.IsNullOrWhiteSpace(buildInstallType))
            content.Add(new StringContent(buildInstallType), "buildInstallType");
        if (!string.IsNullOrWhiteSpace(buildPassword))
            content.Add(new StringContent(buildPassword), "buildPassword");

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var response = await http.PostAsync(UploadUrl, content, cancellationToken).ConfigureAwait(false);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {raw}");

        PgyerApiEnvelope? json;
        try
        {
            json = JsonSerializer.Deserialize<PgyerApiEnvelope>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"Unexpected response (not JSON): {raw}");
        }

        if (json is null || json.Code is null)
            throw new InvalidOperationException($"Unexpected JSON shape: {raw}");

        if (json.Code.Value != 0)
        {
            var msg = json.Message ?? "";
            throw new InvalidOperationException($"Pgyer API error code={json.Code} message={msg}\nRaw: {raw}");
        }

        var shortcut = json.Data?.BuildShortcutUrl;
        if (string.IsNullOrWhiteSpace(shortcut))
            shortcut = json.BuildShortcutUrl;

        var qr = json.Data?.BuildQRCodeURL ?? json.Data?.BuildQrCodeUrl;
        if (string.IsNullOrWhiteSpace(qr))
            qr = json.BuildQRCodeURL;

        return new PgyerUploadResult(shortcut, qr, raw);
    }
}

public sealed record PgyerUploadResult(string? ShortcutUrl, string? QrCodeUrl, string RawJson);

internal sealed class PgyerApiEnvelope
{
    [JsonPropertyName("code")]
    public int? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public PgyerApiData? Data { get; set; }

    [JsonPropertyName("buildShortcutUrl")]
    public string? BuildShortcutUrl { get; set; }

    [JsonPropertyName("buildQRCodeURL")]
    public string? BuildQRCodeURL { get; set; }
}

internal sealed class PgyerApiData
{
    [JsonPropertyName("buildShortcutUrl")]
    public string? BuildShortcutUrl { get; set; }

    [JsonPropertyName("buildQRCodeURL")]
    public string? BuildQRCodeURL { get; set; }

    [JsonPropertyName("buildQrCodeURL")]
    public string? BuildQrCodeUrl { get; set; }
}
