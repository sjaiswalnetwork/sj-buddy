# Waits for the GitHub device login (started by gh auth login --web) to finish, then runs publish.ps1.
param([string]$Log = "$env:TEMP\gh-login.txt", [int]$TimeoutSec = 900)
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 5
    if (-not (Test-Path $Log)) { continue }
    $txt = Get-Content $Log -Raw -ErrorAction SilentlyContinue
    if ($txt -match "Logged in as") { break }
    if ($txt -match "expired|denied|error|failed") { Write-Host "LOGIN FAILED:"; Write-Host $txt; exit 1 }
}
$txt = Get-Content $Log -Raw -ErrorAction SilentlyContinue
if ($txt -notmatch "Logged in as") { Write-Host "TIMEOUT: login was not completed within $TimeoutSec seconds."; exit 2 }
Write-Host "Login detected. Publishing..."
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root "tools\publish.ps1")
exit $LASTEXITCODE
