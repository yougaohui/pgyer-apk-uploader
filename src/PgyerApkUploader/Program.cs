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

            var projectRoot = ProjectLocator.ResolveWorkingRoot(opts.ProjectRoot);
            Directory.SetCurrentDirectory(projectRoot);

            if (opts.Init)
            {
                ConfigInitializer.Initialize(projectRoot);
                return 0;
            }

            Console.WriteLine($"Working directory: {projectRoot}");

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
                if (!ProjectLocator.HasGradleWrapper(projectRoot))
                    throw new InvalidOperationException(
                        "--build requires gradlew.bat in the working directory. cd into your Android project root, or pass --project PATH to that folder.");

                var task = GradleTaskName.GetAssembleTask(opts.Flavor, opts.BuildType);
                Console.WriteLine($"Running: .\\gradlew.bat {task}");
                var exit = RunGradle(projectRoot, task);
                if (exit != 0)
                    return exit;
                Console.WriteLine($"Scanning recursively for *.apk under {projectRoot} ...");
                var apks = ApkResolver.EnumerateApksSortedNewestFirst(projectRoot);
                apkPath = ApkPicker.SelectApk(projectRoot, apks, opts.Force);
                Console.WriteLine($"Using APK: {apkPath}");
            }
            else
            {
                Console.WriteLine($"Scanning recursively for *.apk under {projectRoot} ...");
                var apks = ApkResolver.EnumerateApksSortedNewestFirst(projectRoot);
                apkPath = ApkPicker.SelectApk(projectRoot, apks, opts.Force);
                Console.WriteLine($"Using APK: {apkPath}");
            }

            var apkItem = new FileInfo(apkPath);
            if (!opts.Force)
                ConfirmFlow.ConfirmApkBeforeUpload(apkItem);

            var cfg = PgyerConfigLoader.Load(projectRoot, exeDir);

            string remarkToSend;
            if (!string.IsNullOrWhiteSpace(opts.UpdateDescription))
            {
                remarkToSend = opts.UpdateDescription.Trim();
                WriteColoredLine("Using update description from command line (no remark prompt).", ConsoleColor.DarkGray);
            }
            else if (cfg is not null && !string.IsNullOrWhiteSpace(cfg.RemarkText))
            {
                remarkToSend = cfg.RemarkText;
                var label = cfg.SourcePath is not null ? cfg.SourcePath : "config file";
                if (!opts.Force)
                    ConfirmFlow.ConfirmLoadedRemarkFromConfigOrCancel(remarkToSend, label);
            }
            else
            {
                remarkToSend = "";
            }

            var client = new PgyerApiClient();
            var result = await client.UploadAsync(
                apkPath,
                apiKey,
                remarkToSend,
                cfg?.InstallType,
                cfg?.Password,
                CreateUploadProgressReporter()).ConfigureAwait(false);

            Console.WriteLine();
            Console.WriteLine("Upload OK (code=0).");
            var shareUrl = result.ShareUrl;
            if (!string.IsNullOrWhiteSpace(shareUrl))
                Console.WriteLine($"Share URL:    {shareUrl}");
            if (!string.IsNullOrWhiteSpace(result.QrCodeUrl))
                Console.WriteLine($"QR code URL:  {result.QrCodeUrl}");
            if (string.IsNullOrWhiteSpace(shareUrl) && string.IsNullOrWhiteSpace(result.QrCodeUrl))
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

    private static IProgress<(long BytesRead, long TotalBytes)> CreateUploadProgressReporter()
    {
        var gate = new object();
        long lastPrintTicks = 0;
        const int throttleMs = 150;

        return new Progress<(long BytesRead, long TotalBytes)>(t =>
        {
            var total = t.TotalBytes;
            var read = t.BytesRead;
            var done = total > 0 && read >= total;
            lock (gate)
            {
                var now = Environment.TickCount64;
                if (!done && now - lastPrintTicks < throttleMs)
                    return;
                lastPrintTicks = now;

                var pct = total > 0 ? 100.0 * read / total : 0;
                var readMb = read / (1024.0 * 1024);
                var totalMb = total / (1024.0 * 1024);
                var line = $"Uploading to Pgyer... {pct:F1}% ({readMb:F1} / {totalMb:F1} MB)";

                if (Console.IsOutputRedirected)
                    Console.WriteLine(line);
                else
                    Console.Write($"\r{line}   ");
            }
        });
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
              PgyerApkUploader [--project PATH] [--init]
                               [--flavor NAME] [--build-type Release|Debug]
                               [--build] [--apk PATH] [--update-description TEXT] [--force]

            Options:
              --init                 Create pgyer_config.json and pgyer-api-key.local in the project folder if missing (no upload).
              --project, -p          Folder to scan for APKs and config files. Default: walk up from cwd for gradlew, else cwd.
              --flavor, -f           Gradle flavor for assemble task (default: _365GPSCN).
              --build-type           Release or Debug for assemble task (default: Release).
              --build                Run gradlew assemble, then scan for APKs.
              --apk                  Upload this file; skips scan and --build.
              --update-description   Pgyer buildUpdateDescription (overrides pgyer_config.json).
              --force                Skip APK and config-remark prompts; with multiple APKs, uploads newest (for CI).

            Environment:
              PGYER_API_KEY          API key (recommended).
              APKUPLOAD_PROJECT_ROOT Optional default working folder when not using --project.

            Config files (same as legacy ApkUpload.ps1):
              pgyer_config.json      update_description, note, install_type, password
              pgyer-api-key.local    one line API key if PGYER_API_KEY is unset

            APK scan: recursive *.apk under project root, sorted by modified time (newest first). If more
            than one exists, you pick the index (unless --force, which takes the newest).

            Remark: if pgyer_config.json (or legacy file) has update_description/note, it is shown once;
            press Enter or Y to upload, N to cancel. No local remark file → upload with empty description (no remark prompt).
            --update-description skips the remark prompt. --force skips APK + remark prompts.
            """);
    }
}

internal sealed class CliOptions
{
    public string? ProjectRoot { get; init; }
    public bool Init { get; init; }
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
        var init = false;
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
                case "--init":
                    init = true;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown argument: {a}. Use --help.");
            }
        }

        return new CliOptions
        {
            ProjectRoot = project,
            Init = init,
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
