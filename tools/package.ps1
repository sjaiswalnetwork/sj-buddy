# Builds the release folder and zip:  dist\SJ-Buddy-v1.0\  and  dist\SJ-Buddy-v1.0.zip
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

Get-Process "SJ Buddy" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1

& cmd.exe /c "`"$root\build.bat`""
if (-not (Test-Path "$root\dist\SJ Buddy.exe")) { throw "build failed" }

$pkg = "$root\dist\SJ-Buddy-v1.0"
if (Test-Path $pkg) { [System.IO.Directory]::Delete($pkg, $true) }
New-Item -ItemType Directory -Force "$pkg\source" | Out-Null
Copy-Item "$root\dist\SJ Buddy.exe" $pkg
Copy-Item "$root\docs\SJ Buddy - Guide.pdf" $pkg
Copy-Item "$root\docs\README.txt" $pkg
Copy-Item "$root\src", "$root\tools", "$root\assets", "$root\build.bat" "$pkg\source" -Recurse
Copy-Item "$root\docs\guide.html" "$pkg\source\guide.html"
foreach ($f in @("$pkg\source\tools\shot.ps1", "$pkg\source\tools\SheetMain.cs", "$pkg\source\tools\package.ps1")) { if (Test-Path $f) { [System.IO.File]::Delete($f) } }

$zip = "$root\dist\SJ-Buddy-v1.0.zip"
if (Test-Path $zip) { [System.IO.File]::Delete($zip) }
Compress-Archive -Path "$pkg\*" -DestinationPath $zip -CompressionLevel Optimal

Write-Host "--- release folder ---"
Get-ChildItem $pkg -Recurse | ForEach-Object { $_.FullName.Replace("$pkg\", "") }
Write-Host "--- zip ---"
Get-Item $zip | Select-Object Name, Length | Format-Table -AutoSize
