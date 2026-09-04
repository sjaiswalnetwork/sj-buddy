param([string]$Out = "assets\sjbuddy.ico")
# Builds the SJ Buddy icon in-process (no separate exe), using the C# in tools\MakeIcon.cs.
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Get-Content -Raw (Join-Path $here "MakeIcon.cs")
Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing
$full = [System.IO.Path]::GetFullPath($Out)
[MakeIcon]::Generate($full)
Write-Host "wrote $full"
