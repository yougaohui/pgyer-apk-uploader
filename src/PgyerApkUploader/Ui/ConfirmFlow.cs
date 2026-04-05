namespace PgyerApkUploader.Ui;

/// <summary>
/// APK and remark confirmation without relying on non-blocking Read-Host in IDE terminals.
/// </summary>
public static class ConfirmFlow
{
    public static void ConfirmApkBeforeUpload(FileInfo apk)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("---------- APK to upload ----------");
        Console.ResetColor();
        Console.WriteLine($"  File:     {apk.FullName}");
        Console.WriteLine($"  Size:     {apk.Length / (1024.0 * 1024.0):F2} MB");
        Console.WriteLine($"  Modified: {apk.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("-----------------------------------");
        Console.ResetColor();

        if (!ChoiceYesNo())
        {
            Console.WriteLine("Cancelled.");
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Shows full remark text; user must confirm or edit before upload.
    /// </summary>
    public static string ConfirmRemarkBeforeUpload(string initialRemark, string? configSourceLabel)
    {
        while (true)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("---------- Remark (Pgyer buildUpdateDescription) ----------");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(configSourceLabel))
                Console.WriteLine($"  Source: {configSourceLabel}");
            var display = string.IsNullOrEmpty(initialRemark) ? "(empty — no update notes)" : initialRemark;
            Console.WriteLine(display);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("------------------------------------------------------------");
            Console.ResetColor();

            Console.WriteLine("Proceed with this remark?  [Y] Upload  [E] Edit  [N] Cancel");
            var key = ReadYen();
            switch (key)
            {
                case ConsoleKey.Y:
                    return initialRemark;
                case ConsoleKey.E:
                    initialRemark = PromptMultilineRemark();
                    continue;
                case ConsoleKey.N:
                    Console.WriteLine("Cancelled.");
                    Environment.Exit(0);
                    return "";
                default:
                    Console.WriteLine("Please press Y, E, or N.");
                    break;
            }
        }
    }

    private static string PromptMultilineRemark()
    {
        Console.WriteLine("Enter remark (finish with a blank line, or type a single line and press Enter):");
        var lines = new List<string>();
        while (true)
        {
            var line = Console.ReadLine();
            if (line is null)
                break;
            if (lines.Count == 0 && string.IsNullOrEmpty(line))
                return "";
            if (lines.Count > 0 && string.IsNullOrEmpty(line))
                break;
            lines.Add(line);
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Uses choice.exe on Windows when available (same idea as ApkUpload.ps1); falls back to Y/N keys.
    /// </summary>
    private static bool ChoiceYesNo()
    {
        var choiceExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "choice.exe");
        if (File.Exists(choiceExe))
        {
            try
            {
                // Keep prompt short; choice.exe /M breaks on embedded quotes — full context is printed above.
                var psi = new System.Diagnostics.ProcessStartInfo(choiceExe, "/C YN /M \"Upload this APK? \"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p is null)
                    return PromptYnFallback("Upload this APK?");
                p.WaitForExit();
                // choice: 1 = first option (Y), 2 = N, 0 = Ctrl+C
                return p.ExitCode switch
                {
                    1 => true,
                    2 => false,
                    0 => false,
                    _ => throw new InvalidOperationException($"Upload cancelled or choice failed (exit code {p.ExitCode}).")
                };
            }
            catch
            {
                return PromptYnFallback("Upload this APK?");
            }
        }

        return PromptYnFallback("Upload this APK?");
    }

    private static bool PromptYnFallback(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{message} (Y/N)");
        Console.ResetColor();
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Y) return true;
            if (k.Key == ConsoleKey.N) return false;
        }
    }

    private static ConsoleKey ReadYen()
    {
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key is ConsoleKey.Y or ConsoleKey.E or ConsoleKey.N)
                return k.Key;
        }
    }
}
