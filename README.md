# PgyerApkUploader

Standalone Windows-friendly uploader for [蒲公英 Pgyer](https://www.pgyer.com) API v2 (`POST https://api.pgyer.com/apiv2/app/upload`). Behavior matches the legacy `ApkUpload.ps1` in the GPS365 repo: resolve `PGYER_API_KEY`, scan `app/build/outputs/apk`, optional `gradlew` assemble, multipart upload with `buildUpdateDescription` / `buildInstallType` / `buildPassword`.

**Difference from the old script:** the full remark (`buildUpdateDescription`) is always shown before upload; you confirm with **Y**, edit with **E**, or cancel with **N** (unless `--force`). See `docs/REMARK_CONFIRMATION.md`.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build and publish)

## Build

```bash
dotnet build pgyer-apk-uploader/PgyerApkUploader.sln -c Release
```

## Run (framework-dependent)

From the solution folder:

```bash
dotnet run --project src/PgyerApkUploader/PgyerApkUploader.csproj -- --project D:\path\to\android\project
```

Arguments: `--help` for full list (`--project`, `--flavor`, `--build-type`, `--build`, `--apk`, `--update-description`, `--force`).

## Publish: single-file self-contained (no .NET runtime on target PC)

```bash
dotnet publish src/PgyerApkUploader/PgyerApkUploader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Copy `publish/PgyerApkUploader.exe` to another machine (same Windows x64), add to PATH if desired. Place optional `pgyer_config.json` or `pgyer-api-key.local` next to the exe or use `%USERPROFILE%` / project root as documented in `--help`.

## Configuration

Same as `pgyer_config.json.example` in the main app repo: `update_description`, `note`, `install_type`, `password`. API key: `PGYER_API_KEY` or `pgyer-api-key.local` (one line).
