param([string]$Out = "shot.png", [int]$X = -1, [int]$Y = -1, [int]$W = 0, [int]$H = 0)
# Screenshot helper for testing (DPI-aware so coordinates match the real screen).
Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public static class DpiFix { [DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); }
"@
[DpiFix]::SetProcessDPIAware() | Out-Null
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
if ($W -le 0) { $W = $b.Width }; if ($H -le 0) { $H = $b.Height }
if ($X -lt 0) { $X = $b.Width - $W }; if ($Y -lt 0) { $Y = $b.Height - $H }
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($X, $Y, 0, 0, (New-Object System.Drawing.Size $W, $H))
$g.Dispose()
$bmp.Save([System.IO.Path]::GetFullPath($Out), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "screen $($b.Width)x$($b.Height) -> $Out"
