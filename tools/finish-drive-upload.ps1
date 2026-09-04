# Completes the Google Drive upload by filling the file-picker dialog that Brave already has open.
# Requires the PC to be UNLOCKED (Windows blocks clipboard and synthetic keystrokes on a locked session).
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System; using System.Collections.Generic; using System.Runtime.InteropServices; using System.Text;
public class Win {
  public delegate bool EnumProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  public static IntPtr FindDialog() {
    IntPtr found = IntPtr.Zero;
    EnumWindows((h, l) => {
      if (!IsWindowVisible(h)) return true;
      var c = new StringBuilder(256); GetClassName(h, c, 256);
      if (c.ToString() == "#32770") { found = h; return false; }
      return true;
    }, IntPtr.Zero);
    return found;
  }
}
"@

$paths = '"D:\Claude Output\The Agency\sj-buddy\dist\SJ-Buddy-v1.0.zip" "D:\Claude Output\The Agency\sj-buddy\docs\SJ Buddy - Guide.pdf"'

# refuse to fire keystrokes at a locked session
try { Set-Clipboard -Value $paths } catch { Write-Host "LOCKED: the session is locked, unlock the PC first."; exit 2 }

$dlg = [Win]::FindDialog()
if ($dlg -eq [IntPtr]::Zero) { Write-Host "NO DIALOG: open Drive, click New > File upload, then run this again."; exit 1 }

[Win]::SetForegroundWindow($dlg) | Out-Null
Start-Sleep -Milliseconds 900
if ([Win]::GetForegroundWindow() -ne $dlg) { Write-Host "Could not focus the dialog. Click it once, then run this again."; exit 3 }

[System.Windows.Forms.SendKeys]::SendWait("^v")
Start-Sleep -Milliseconds 800
[System.Windows.Forms.SendKeys]::SendWait("{ENTER}")
Write-Host "Sent both file paths to the dialog. Watch Drive for the upload progress box."
