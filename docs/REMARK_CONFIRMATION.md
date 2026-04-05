# Why the legacy `ApkUpload.ps1` skipped remark confirmation

1. **When `pgyer_config.json` has `update_description` or `note` set,** the script uses that text and **skips** the interactive `Read-Host` for notes. The terminal prints:  
   `update_description is set — interactive remark prompt skipped`

2. **There is no second step** that prints the full remark and asks you to confirm before upload—only APK confirmation (`choice.exe`).

3. **`Read-Host` in embedded IDE terminals** can return immediately (non-blocking), so it may look like the script “never asked” for a remark even when the config path was empty.

**PgyerApkUploader** always shows the full remark text (or “empty”) and requires **Y / E / N** before upload unless `--force` is used (e.g. CI).
