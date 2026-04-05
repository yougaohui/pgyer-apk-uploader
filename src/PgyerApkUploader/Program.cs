using System.Diagnostics;
using System.Runtime.InteropServices;
using PgyerApkUploader.Services;
using PgyerApkUploader.Ui;

namespace PgyerApkUploader;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Any(a => a is "-h" or "--help" or "/?"))
            {
                PrintHelp();
                return 0;
            }

            var opts = CliOptions.Parse(args);
            var exeDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var projectRoot = ProjectLocator.ResolveProjectRoot(opts.ProjectRoot);
            Directory.SetCurrentDirectory(projectRoot);
            Console.WriteLine($"Project root: {projectRoot}");

            var apiKey = CredentialStore.ResolveApiKey(projectRoot, exeDir);

            string apkPath;
            if (!string.IsNullOrEmpty(opts.ApkPath))
            {
                if (!File.Exists(opts.ApkPath))
                    throw new InvalidOperationException($"APK not found: {opts.ApkPath}");
                apkPath = Path.GetFullPath(opts.ApkPath);
                Console.WriteLine($"Using APK: {apkPath}");
            }
            else if (opts.Build)
            {
                var task = GradleTaskName.GetAssembleTask(opts.Flavor, opts.BuildType);
                Console.WriteLine($"Running: .\\gradlew.bat {task}");
                var exit = RunGradle(projectRoot, task);
                if (exit != 0)
                    return exit;
                apkPath = ApkResolver.ResolveNewestApk(projectRoot, opts.Flavor, opts.BuildType);
                Console.WriteLine($"Using APK: {apkPath}");
            }
            else
            {
                Console.WriteLine(
                    $"Scanning for newest APK (flavor={opts.Flavor}, buildType={opts.BuildType}) under app\\build\\outputs\\apk ...");
                apkPath = ApkResolver.ResolveNewestApk(projectRoot, opts.Flavor, opts.BuildType);
                Console.WriteLine($"Using APK: {apkPath}");
            }

            var apkItem = new FileInfo(apkPath);
            if (!opts.Force)
                ConfirmFlow.ConfirmApkBeforeUpload(apkItem);

            var cfg = PgyerConfigLoader.Load(projectRoot, exeDir);

            string remarkToSend;
            string? remarkSourceLabel;
            if (!string.IsNullOrWhiteSpace(opts.UpdateDescription))
            {
                remarkToSend = opts.UpdateDescription.Trim();
                remarkSourceLabel = "command line (--update-description)";
                WriteColoredLine("Using update description from command line.", ConsoleColor.DarkGray);
            }
            else if (cfg is not null && !string.IsNullOrWhiteSpace(cfg.RemarkText))
            {
                remarkToSend = cfg.RemarkText;
                remarkSourceLabel = cfg.SourcePath is not null ? $"config file: {cfg.SourcePath}" : "config file";
                WriteColoredLine(
                    "Loaded remark from pgyer_config.json / legacy file — you will preview and confirm before upload.",
                    ConsoleColor.DarkGray);
            }
            else
            {
                remarkToSend = "";
                remarkSourceLabel = "(no config text — optional notes)";
            }

            if (!opts.Force)
                remarkToSend = ConfirmFlow.ConfirmRemarkBeforeUpload(remarkToSend, remarkSourceLabel);

            Console.WriteLine("Uploading to Pgyer...");
            var client = new PgyerApiClient();
            var result = await client.UploadAsync(
                apkPath,
                apiKey,
                remarkToSend,
                cfg?.InstallType,
                cfg?.Password).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Upload OK (code=0).");
            if (!string.IsNullOrWhiteSpace(result.ShortcutUrl))
                Console.WriteLine($"Shortcut URL: {result.ShortcutUrl}");
            if (!string.IsNullOrWhiteSpace(result.QrCodeUrl))
                Console.WriteLine($"QR code URL:  {result.QrCodeUrl}");
            if (string.IsNullOrWhiteSpace(result.ShortcutUrl) && string.IsNullOrWhiteSpace(result.QrCodeUrl))
                Console.WriteLine($"Response: {result.RawJson}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            PauseAfterError(ex.Message);
            return 1;
        }
    }

    private static bool IsCiLikeEnvironment()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
            return true;
        if (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    /// <summary>
    /// Wait for a key when possible; if stdin is redirected, ReadKey fails, or there is no usable console,
    /// show a Windows message box (skipped in CI-like environments).
    /// </summary>
    private static void PauseAfterError(string message)
    {
        var readKeyOk = false;
        try
        {
            if (!Console.IsInputRedirected && !Console.IsOutputRedirected)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Press any key to exit...");
                Console.ReadKey(intercept: true);
                readKeyOk = true;
            }
        }
        catch
        {
            // No console or ReadKey not available.
        }

        if (readKeyOk)
            return;

        if (!OperatingSystem.IsWindows() || IsCiLikeEnvironment())
            return;

        try
        {
            var text = message.Length > 1500 ? message[..1500] + "…" : message;
            NativeMethods.MessageBoxW(IntPtr.Zero, text, "PgyerApkUploader", 0x00000010); // MB_OK | MB_ICONERROR
        }
        catch
        {
            // MessageBox unavailable.
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MessageBoxW(IntPtr hWnd, string? text, string? caption, uint type);
    }

    private static void WriteColoredLine(string message, ConsoleColor color)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = prev;
    }

    private static int RunGradle(string projectRoot, string task)
    {
        var gradlew = Path.Combine(projectRoot, "gradlew.bat");
        if (!File.Exists(gradlew))
            throw new InvalidOperationException($"gradlew.bat not found under {projectRoot}");

        var psi = new ProcessStartInfo
        {
            FileName = gradlew,
            Arguments = task,
            WorkingDirectory = projectRoot,
            UseShellExecute = false
        };
        using var p = Process.Start(psi);
        if (p is null)
            throw new InvalidOperationException("Failed to start gradlew.bat");
        p.WaitForExit();
        return p.ExitCode;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("""
            PgyerApkUploader — upload APK to 蒲公英 (Pgyer) API v2.

            Usage:
              PgyerApkUploader [--project PATH] [--flavor NAME] [--build-type Release|Debug]
                               [--build] [--apk PATH] [--update-description TEXT] [--force]

            Options:
              --project, -p          Android project root (folder with gradlew.bat). Default: walk up from cwd.
              --flavor, -f           Gradle flavor folder under app/build/outputs/apk (default: _365GPSCN).
              --build-type           Release or Debug (default: Release).
              --build                Run gradlew assemble before scanning for APK.
              --apk                  Upload this file; skips scan and --build.
              --update-description   Pgyer buildUpdateDescription (overrides pgyer_config.json).
              --force                Skip APK and remark confirmation (for CI).

            Environment:
              PGYER_API_KEY          API key (recommended).
              APKUPLOAD_PROJECT_ROOT Optional default project root when not using --project.

            Config files (same as legacy ApkUpload.ps1):
              pgyer_config.json      update_description, note, install_type, password
              pgyer-api-key.local    one line API key if PGYER_API_KEY is unset

            Remark flow: the full remark is always shown; press Y to upload, E to edit, N to cancel
            (unless --force). This avoids skipping confirmation when update_description is set in JSON.
            """);
    }
}

internal sealed class CliOptions
{
    public string? ProjectRoot { get; init; }
    public string Flavor { get; init; } = DefaultFlavor;
    public string BuildType { get; init; } = "Release";
    public bool Build { get; init; }
    public string? ApkPath { get; init; }
    public string? UpdateDescription { get; init; }
    public bool Force { get; init; }

    private const string DefaultFlavor = "_365GPSCN";

    public static CliOptions Parse(string[] args)
    {
        string? project = null;
        var flavor = DefaultFlavor;
        var buildType = "Release";
        var build = false;
        string? apk = null;
        string? updateDesc = null;
        var force = false;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--project":
                case "-p":
                    project = RequireValue(args, ref i, a);
                    break;
                case "--flavor":
                case "-f":
                    flavor = RequireValue(args, ref i, a);
                    break;
                case "--build-type":
                    buildType = RequireValue(args, ref i, a);
                    if (!buildType.Equals("Release", StringComparison.OrdinalIgnoreCase) &&
                        !buildType.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("--build-type must be Release or Debug.");
                    buildType = char.ToUpperInvariant(buildType[0]) + buildType.Substring(1).ToLowerInvariant();
                    break;
                case "--build":
                    build = true;
                    break;
                case "--apk":
                    apk = RequireValue(args, ref i, a);
                    break;
                case "--update-description":
                case "-u":
                    updateDesc = RequireValue(args, ref i, a);
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown argument: {a}. Use --help.");
            }
        }

        return new CliOptions
        {
            ProjectRoot = project,
            Flavor = flavor,
            BuildType = buildType,
            Build = build,
            ApkPath = apk,
            UpdateDescription = updateDesc,
            Force = force
        };
    }

    private static string RequireValue(string[] args, ref int i, string flag)
    {
        if (i + 1 >= args.Length)
            throw new InvalidOperationException($"Missing value for {flag}");
        return args[++i];
    }
}
