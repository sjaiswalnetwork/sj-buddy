# Publishes SJ Buddy to GitHub: creates the public repo (first time), pushes the code, and creates the v1.0 release
# with the zip and the PDF guide attached. Needs the GitHub CLI logged in:   gh auth login
param([string]$RepoName = "sj-buddy", [string]$Tag = "v1.0")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

# locate gh (PATH, or the WinGet install locations)
$gh = (Get-Command gh -ErrorAction SilentlyContinue | Select-Object -First 1).Source
if (-not $gh) { $gh = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Links\gh.exe", "$env:LOCALAPPDATA\Programs\GitHub CLI\gh.exe", "$env:ProgramFiles\GitHub CLI\gh.exe" -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $gh) { $gh = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages" -Recurse -Filter gh.exe -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName }
if (-not $gh) { throw "GitHub CLI (gh.exe) not found. Install it with:  winget install --id GitHub.cli" }
$env:Path = (Split-Path -Parent $gh) + ";" + $env:Path

& $gh auth status 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Not logged in. Run:  gh auth login   (GitHub.com, HTTPS, login with a web browser) and then run this script again." }
& $gh auth setup-git 2>&1 | Out-Null

$owner = (& $gh api user -q .login).Trim()
if (-not $owner) { throw "Could not read the GitHub username." }
$Repo = "$owner/$RepoName"

if (-not (Test-Path "$root\dist\SJ-Buddy-v1.0.zip")) { throw "dist\SJ-Buddy-v1.0.zip is missing. Run tools\package.ps1 first." }

$ErrorActionPreference = "Continue"
& $gh repo view $Repo --json name 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Creating public repository $Repo ..."
    & $gh repo create $Repo --public --source . --remote origin --push --description "SJ Buddy: desktop toon companion for Windows (Shinchan, SpongeBob, Doraemon, Jethalal, Bhide). Made by SJ, built with Claude Fable 5.1."
    if ($LASTEXITCODE -ne 0) { throw "repo create failed" }
} else {
    if (-not (git remote 2>$null | Select-String -SimpleMatch "origin")) { git remote add origin "https://github.com/$Repo.git" }
    git push -u origin main
    if ($LASTEXITCODE -ne 0) { throw "push failed" }
}

# "release not found" arrives on stderr; keep PowerShell from treating that as a terminating error.
$ErrorActionPreference = "Continue"
& $gh release view $Tag --repo $Repo 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Release $Tag already exists; uploading assets (overwriting)..."
    & $gh release upload $Tag "dist\SJ-Buddy-v1.0.zip" "docs\SJ Buddy - Guide.pdf" --repo $Repo --clobber
} else {
    & $gh release create $Tag "dist\SJ-Buddy-v1.0.zip" "docs\SJ Buddy - Guide.pdf" --repo $Repo --title "SJ Buddy v1.0" --notes-file "docs\release-notes.md" --latest
}
if ($LASTEXITCODE -ne 0) { throw "release failed" }

Write-Host ""
Write-Host "PUBLISHED"
Write-Host "  Project page : https://github.com/$Repo"
Write-Host "  Direct zip   : https://github.com/$Repo/releases/latest/download/SJ-Buddy-v1.0.zip"
Write-Host "  PDF guide    : https://github.com/$Repo/releases/latest/download/SJ.Buddy.-.Guide.pdf"
