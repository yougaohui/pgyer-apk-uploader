# PgyerApkUploader

Standalone Windows-friendly uploader for [蒲公英 Pgyer](https://www.pgyer.com) API v2 (`POST https://api.pgyer.com/apiv2/app/upload`). Behavior matches the legacy `ApkUpload.ps1` in the GPS365 repo: resolve `PGYER_API_KEY`, scan `app/build/outputs/apk`, optional `gradlew` assemble, multipart upload with `buildUpdateDescription` / `buildInstallType` / `buildPassword`.

**Difference from the old script:** the full remark (`buildUpdateDescription`) is always shown before upload; you confirm with **Y**, edit with **E**, or cancel with **N** (unless `--force`). See `docs/REMARK_CONFIRMATION.md`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build and publish)

## Build

From the repository root:

```bash
dotnet build PgyerApkUploader.sln -c Release
```

## Run (framework-dependent)

From the repository root:

```bash
dotnet run --project src/PgyerApkUploader/PgyerApkUploader.csproj -- --project D:\path\to\android\project
```

Arguments: `--help` for full list (`--project`, `--flavor`, `--build-type`, `--build`, `--apk`, `--update-description`, `--force`).

## Publish: single-file self-contained (no .NET runtime on target PC)

From the repository root:

```bash
dotnet publish src/PgyerApkUploader/PgyerApkUploader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Copy `publish/PgyerApkUploader.exe` to another machine (same Windows x64), add to PATH if desired. Place optional `pgyer_config.json` or `pgyer-api-key.local` next to the exe or use `%USERPROFILE%` / project root as documented in `--help`.

### Short command `pgy` (Windows)

`PGYER_API_KEY` only supplies the API key; it does **not** add a short name for the executable.

If you used `Set-Alias` with a **relative** path (for example `.\publish\PgyerApkUploader.exe`), it only works in that folder. Use an **absolute** path, or use the shim below.

1. Set a **user** environment variable **`PGYER_APK_UPLOADER_HOME`** to the folder that contains `PgyerApkUploader.exe` (for example the repo’s `publish` folder after `dotnet publish`, or wherever you copied the exe).
2. Add this repo’s **`scripts`** folder to your user **PATH** (the folder that contains `pgy.cmd`).
3. Open a **new** terminal. You can run **`pgy`** from any directory (for example `pgy --help`).

**PowerShell** (optional): dot-source `scripts/Import-PgyShim.ps1` from your profile, or add a function that uses `$env:PGYER_APK_UPLOADER_HOME` and `@args` (not a relative-path alias).

## Configuration

Same as `pgyer_config.json.example` in the main app repo: `update_description`, `note`, `install_type`, `password`. API key: `PGYER_API_KEY` or `pgyer-api-key.local` (one line).
