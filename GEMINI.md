# Gemini Context: PgyerApkUploader

## Project Overview
PgyerApkUploader is a standalone Windows-friendly console application written in C# (.NET 8) for uploading Android APK files to the [蒲公英 Pgyer](https://www.pgyer.com) platform using their API v2. It is designed to replace legacy PowerShell scripts (`ApkUpload.ps1`) with more robust configuration handling and an interactive confirmation workflow.

### Key Technologies
- **Runtime:** .NET 8.0
- **Language:** C#
- **API Interaction:** `HttpClient` with multipart form data.
- **JSON Processing:** `System.Text.Json` (case-insensitive, supports comments and trailing commas).
- **Target Platform:** Primarily Windows (supports single-file self-contained publishing), but compatible with any .NET 8 environment.

### Core Architecture
- **Entry Point (`Program.cs`):** Manages the high-level workflow: CLI parsing, project/APK resolution, optional Gradle build, confirmation prompts, and final upload.
- **Services:**
  - `PgyerApiClient`: Handles the `POST https://api.pgyer.com/apiv2/app/upload` request.
  - `PgyerConfigLoader`: Loads metadata (update description, install type, password) from `pgyer_config.json` or legacy `.local.json` files.
  - `CredentialStore`: Resolves the `_api_key` from environment variables (`PGYER_API_KEY`) or local files.
  - `ApkResolver` & `ApkPicker`: Handles scanning for APKs and interactive selection.
  - `ProjectLocator`: Locates the Android project root and Gradle wrapper.
- **UI (`Ui/`):**
  - `ConfirmFlow`: Implements a "Preview before Upload" logic for APK choice and remark text (Y/E/N prompts).

## Building and Running

### Build
```bash
dotnet build PgyerApkUploader.sln -c Release
```

### Run
```bash
dotnet run --project src/PgyerApkUploader/PgyerApkUploader.csproj -- [args]
```

### Publish (Single-File)
```bash
dotnet publish src/PgyerApkUploader/PgyerApkUploader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Development Conventions

### Configuration Search Order
1. **API Key:** `PGYER_API_KEY` Environment Variable -> `pgyer-api-key.local` in project root -> `pgyer-api-key.local` in exe directory.
2. **Project Config:** `pgyer_config.json` searched in: Project Root -> `%USERPROFILE%` -> Exe Directory.

### Interactive Workflow
- **Confirmation:** By default, the tool requires user confirmation for the APK file and the update description.
- **CI/Non-Interactive:** Use the `--force` flag to skip all prompts (useful for automated build pipelines).
- **Error Handling:** On Windows, if the console is not interactive and an error occurs, it attempts to show a `MessageBox` to notify the user.

### Coding Style
- **Naming:** standard .NET PascalCase for public members and types.
- **Nullability:** Nullable reference types are enabled (`<Nullable>enable</Nullable>`).
- **Dependency Management:** Minimizes external dependencies; currently uses built-in .NET 8 libraries.
