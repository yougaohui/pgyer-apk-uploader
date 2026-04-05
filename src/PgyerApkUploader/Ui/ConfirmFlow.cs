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
    /// Remark was read from pgyer_config.json (or legacy file). Show it and prompt [Y/n] — no terminal editing.
    /// </summary>
    public static void ConfirmLoadedRemarkFromConfigOrCancel(string remark, string? configSourceLabel)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("---------- Remark (from config file) ----------");
        Console.ResetColor();
        if (!string.IsNullOrEmpty(configSourceLabel))
            Console.WriteLine($"  Source: {configSourceLabel}");
        Console.WriteLine(remark);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("-----------------------------------------------");
        Console.ResetColor();

        if (!PromptYnFallback("Upload with this remark?"))
        {
            Console.WriteLine("Cancelled.");
            Environment.Exit(0);
        }
    }

    /// <summary>
    /// Same-console [Y/n] (Enter = Yes). We do not use choice.exe: hidden subprocess prompts look "stuck".
    /// </summary>
    private static bool ChoiceYesNo() => PromptYnFallback("Upload this APK?");

    private static bool PromptYnFallback(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{message} [Y/n]");
        Console.ResetColor();
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key is ConsoleKey.Y or ConsoleKey.Enter) return true;
            if (k.Key == ConsoleKey.N) return false;
        }
    }
}
