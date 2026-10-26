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
        IProgress<(long BytesRead, long TotalBytes)>? uploadProgress = null,
        CancellationToken cancellationToken = default)
    {
        Stream streamForContent;
        if (uploadProgress is null)
        {
            streamForContent = File.OpenRead(apkPath);
        }
        else
        {
            var inner = File.OpenRead(apkPath);
            streamForContent = new ProgressReadStream(inner, inner.Length, uploadProgress);
        }

        using (streamForContent)
        {
            if (uploadProgress is not null)
                uploadProgress.Report((0, streamForContent.Length));

            var fileName = Path.GetFileName(apkPath);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(apiKey), "_api_key");
            content.Add(new StreamContent(streamForContent), "file", fileName);

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

            var buildKey = json.Data?.BuildKey;
            if (string.IsNullOrWhiteSpace(buildKey))
                buildKey = json.BuildKey;

            var shortcut = json.Data?.BuildShortcutUrl;
            if (string.IsNullOrWhiteSpace(shortcut))
                shortcut = json.BuildShortcutUrl;

            var qr = json.Data?.BuildQRCodeURL;
            if (string.IsNullOrWhiteSpace(qr))
                qr = json.BuildQRCodeURL;

            return new PgyerUploadResult(shortcut, qr, raw, buildKey);
        }
    }
}

public sealed record PgyerUploadResult(
    string? ShortcutUrl,
    string? QrCodeUrl,
    string RawJson,
    string? BuildKey = null)
{
    public string? ShareUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ShortcutUrl))
            {
                if (ShortcutUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    ShortcutUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return ShortcutUrl;
                }

                return $"https://www.pgyer.com/{ShortcutUrl.TrimStart('/')}";
            }

            if (!string.IsNullOrWhiteSpace(BuildKey))
            {
                if (BuildKey.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    BuildKey.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildKey;
                }

                return $"https://www.pgyer.com/{BuildKey.TrimStart('/')}";
            }

            if (!string.IsNullOrWhiteSpace(QrCodeUrl) &&
                Uri.TryCreate(QrCodeUrl, UriKind.Absolute, out var qrUri))
            {
                var segments = qrUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2 && segments[^2].Equals("qrcode", StringComparison.OrdinalIgnoreCase))
                {
                    return $"{qrUri.Scheme}://{qrUri.Authority}/{segments[^1]}";
                }
            }

            return null;
        }
    }
}

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

    [JsonPropertyName("buildKey")]
    public string? BuildKey { get; set; }
}

internal sealed class PgyerApiData
{
    [JsonPropertyName("buildKey")]
    public string? BuildKey { get; set; }

    [JsonPropertyName("buildShortcutUrl")]
    public string? BuildShortcutUrl { get; set; }

    [JsonPropertyName("buildQRCodeURL")]
    public string? BuildQRCodeURL { get; set; }
}
