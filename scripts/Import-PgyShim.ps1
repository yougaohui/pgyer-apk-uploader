# Dot-source this file from your PowerShell profile so `pgy` works from any directory:
#   . "D:\path\to\pgyer-apk-uploader\scripts\Import-PgyShim.ps1"
# Requires user env var PGYER_APK_UPLOADER_HOME = folder containing PgyerApkUploader.exe.

function pgy {
    if (-not $env:PGYER_APK_UPLOADER_HOME) {
        Write-Error "PGYER_APK_UPLOADER_HOME is not set. Set it to the folder that contains PgyerApkUploader.exe (see README.md)."
        return
    }
    $exe = Join-Path $env:PGYER_APK_UPLOADER_HOME 'PgyerApkUploader.exe'
    if (-not (Test-Path -LiteralPath $exe)) {
        Write-Error "PgyerApkUploader.exe not found at: $exe"
        return
    }
    & $exe @args
}
