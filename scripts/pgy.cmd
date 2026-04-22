@echo off
setlocal
if "%PGYER_APK_UPLOADER_HOME%"=="" (
  echo PGYER_APK_UPLOADER_HOME is not set. Set it to the folder that contains PgyerApkUploader.exe, 1>&2
  echo then add this scripts folder to your PATH. See README.md. 1>&2
  exit /b 1
)
"%PGYER_APK_UPLOADER_HOME%\PgyerApkUploader.exe" %*
