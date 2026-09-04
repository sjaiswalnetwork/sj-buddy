# Renders two 1200x1200 LinkedIn post images into docs\: linkedin-1.png (showcase) and linkedin-2.png (how it was built).
param([string]$Link = "Download link in the post")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$docs = Join-Path $root "docs"
$W = 1200; $H = 1200
$navy = [System.Drawing.Color]::FromArgb(24, 48, 110)
$blue = [System.Drawing.Color]::FromArgb(37, 99, 235)
$ink = [System.Drawing.Color]::FromArgb(38, 38, 48)
$grey = [System.Drawing.Color]::FromArgb(100, 106, 125)

function NewCanvas {
    $bmp = New-Object System.Drawing.Bitmap $W, $H
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $p1 = New-Object System.Drawing.Point 0, 0
    $p2 = New-Object System.Drawing.Point 0, $H
    $br = New-Object System.Drawing.Drawing2D.LinearGradientBrush $p1, $p2, ([System.Drawing.Color]::FromArgb(230, 239, 255)), ([System.Drawing.Color]::FromArgb(255, 255, 255))
    $g.FillRectangle($br, 0, 0, $W, $H)
    $br.Dispose()
    # subtle dots
    $dot = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40, 37, 99, 235))
    for ($x = 30; $x -lt $W; $x += 60) { for ($y = 30; $y -lt $H; $y += 60) { $g.FillEllipse($dot, $x, $y, 4, 4) } }
    $dot.Dispose()
    return @($bmp, $g)
}

function RR($x, $y, $w, $h, $r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90); $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90); $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure(); return $p
}

function Card($g, $x, $y, $w, $h, $fill, $border) {
    $sh = RR ($x + 4) ($y + 6) $w $h 22
    $sb = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(28, 0, 0, 0)); $g.FillPath($sb, $sh); $sb.Dispose(); $sh.Dispose()
    $p = RR $x $y $w $h 22
    $b = New-Object System.Drawing.SolidBrush $fill; $g.FillPath($b, $p); $b.Dispose()
    if ($border) { $pen = New-Object System.Drawing.Pen $border, 2; $g.DrawPath($pen, $p); $pen.Dispose() }
    $p.Dispose()
}

function Txt($g, $s, $x, $y, $size, $color, $bold, $align, $maxW) {
    $st = [System.Drawing.FontStyle]::Regular; if ($bold) { $st = [System.Drawing.FontStyle]::Bold }
    $f = New-Object System.Drawing.Font "Segoe UI", $size, $st, ([System.Drawing.GraphicsUnit]::Pixel)
    $b = New-Object System.Drawing.SolidBrush $color
    $sf = New-Object System.Drawing.StringFormat
    if ($align -eq "c") { $sf.Alignment = [System.Drawing.StringAlignment]::Center } elseif ($align -eq "r") { $sf.Alignment = [System.Drawing.StringAlignment]::Far }
    if ($maxW -gt 0) {
        $rx = $x; if ($align -eq "c") { $rx = $x - $maxW / 2 } elseif ($align -eq "r") { $rx = $x - $maxW }
        $rect = New-Object System.Drawing.RectangleF $rx, $y, $maxW, 800
        $g.DrawString($s, $f, $b, $rect, $sf)
    } else { $g.DrawString($s, $f, $b, [single]$x, [single]$y, $sf) }
    $f.Dispose(); $b.Dispose()
}

function Pill($g, $s, $cx, $y, $size, $bg, $fg) {
    $f = New-Object System.Drawing.Font "Segoe UI", $size, ([System.Drawing.FontStyle]::Bold), ([System.Drawing.GraphicsUnit]::Pixel)
    $sz = $g.MeasureString($s, $f)
    $w = $sz.Width + 44; $h = $sz.Height + 18
    $p = RR ($cx - $w / 2) $y $w $h ($h / 2)
    $b = New-Object System.Drawing.SolidBrush $bg; $g.FillPath($b, $p); $b.Dispose(); $p.Dispose()
    $tb = New-Object System.Drawing.SolidBrush $fg
    $g.DrawString($s, $f, $tb, [single]($cx - $sz.Width / 2), [single]($y + 9))
    $tb.Dispose(); $f.Dispose()
}

function ImgRounded($g, $img, $x, $y, $w, $h, $r) {
    $p = RR $x $y $w $h $r
    $g.SetClip($p)
    $g.DrawImage($img, (New-Object System.Drawing.Rectangle $x, $y, $w, $h))
    $g.ResetClip()
    $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(215, 222, 240)), 3
    $g.DrawPath($pen, $p); $pen.Dispose(); $p.Dispose()
}

# ------------------------------------------------------------------ image 1: showcase
$c = NewCanvas; $bmp = $c[0]; $g = $c[1]
Txt $g "SJ Buddy" 600 36 104 $navy $true "c" 0
Txt $g "Your desktop toon companion for Windows" 600 158 38 $grey $false "c" 0
Pill $g "FREE   -   WINDOWS 10 / 11   -   NO INSTALL" 600 216 26 $blue ([System.Drawing.Color]::White)

$sheet = [System.Drawing.Image]::FromFile((Join-Path $docs "sheet.png"))
Card $g 40 290 1120 330 ([System.Drawing.Color]::White) ([System.Drawing.Color]::FromArgb(215, 222, 240))
$names = @("Shinchan", "SpongeBob", "Doraemon", "Jethalal", "Bhide")
for ($i = 0; $i -lt 5; $i++) {
    $src = New-Object System.Drawing.Rectangle 0, ($i * 330 + 20), 230, 300
    $dx = 60 + $i * 218; $dw = 200; $dh = 261
    $g.DrawImage($sheet, (New-Object System.Drawing.Rectangle $dx, 304, $dw, $dh), $src, [System.Drawing.GraphicsUnit]::Pixel)
    Txt $g $names[$i] ($dx + $dw / 2) 572 26 $ink $true "c" 0
}
$sheet.Dispose()

$hero = [System.Drawing.Image]::FromFile((Join-Path $docs "hero.png"))
$hov = [System.Drawing.Image]::FromFile((Join-Path $docs "hover.png"))
ImgRounded $g $hero 40 656 548 330 20
ImgRounded $g $hov 612 656 548 330 20
$hero.Dispose(); $hov.Dispose()
Txt $g "Reminds you to drink water" 314 992 24 $grey $false "c" 0
Txt $g "Notices you, reacts to typing and the mouse" 886 992 24 $grey $false "c" 0

Card $g 40 1036 1120 128 $navy $null
Txt $g "Built end-to-end by Claude Fable 5.1 inside Claude Code" 600 1050 34 ([System.Drawing.Color]::White) $true "c" 0
Txt $g "~4,000 lines of C#   -   5 characters drawn in code   -   990 lines of dialogue in English, Hindi and Hinglish   -   learns your routine" 600 1102 22 ([System.Drawing.Color]::FromArgb(200, 214, 245)) $false "c" 1080
$bmp.Save((Join-Path $docs "linkedin-1.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

# ------------------------------------------------------------------ image 2: how it was built
$c = NewCanvas; $bmp = $c[0]; $g = $c[1]
Txt $g "How it was built" 600 40 84 $navy $true "c" 0
Txt $g "One prompt + 7 screenshots  ->  a finished Windows app, by Claude Fable 5.1" 600 142 32 $grey $false "c" 0

$steps = @(
    @("1", "Research", "Read up on all five characters: catchphrases, running gags, families, rivals, so every line sounds like them."),
    @("2", "Art in code", "No image files. Every character is drawn with vector shapes: eyes, mouths, arms, props like a laptop, water glass, bamboo copter, scooter."),
    @("3", "A brain", "Learns your usual start time, lunch hour, typing speed, how chatty you like it, how fast you answer reminders. Offline, rule-based."),
    @("4", "Self-testing", "Ran the app, took its own screenshots, found DPI layout bugs and fixed them. Verified the water reminder and the mouse reactions live."),
    @("5", "Antivirus fix", "McAfee flagged a tiny helper exe. Fable rebuilt the pipeline with no helper exe and removed the keyboard hook entirely."),
    @("6", "Packaged", "One 320 KB exe, a PDF guide and full source with a one-click build script. Feedback rounds turned around in minutes.")
)
for ($i = 0; $i -lt 6; $i++) {
    $col = $i % 2; $row = [math]::Floor($i / 2)
    $x = 40 + $col * 572; $y = 210 + $row * 262; $w = 548; $h = 236
    Card $g $x $y $w $h ([System.Drawing.Color]::White) ([System.Drawing.Color]::FromArgb(215, 222, 240))
    $circ = New-Object System.Drawing.SolidBrush $blue
    $g.FillEllipse($circ, $x + 24, $y + 24, 56, 56); $circ.Dispose()
    Txt $g $steps[$i][0] ($x + 52) ($y + 32) 32 ([System.Drawing.Color]::White) $true "c" 0
    Txt $g $steps[$i][1] ($x + 100) ($y + 30) 34 $navy $true "l" 0
    Txt $g $steps[$i][2] ($x + 24) ($y + 96) 24 $ink $false "l" ($w - 48)
}
Card $g 40 1006 1120 158 $navy $null
Txt $g "~2 hours from idea to shareable zip" 600 1022 40 ([System.Drawing.Color]::White) $true "c" 0
Txt $g "Free for anyone. Windows 10/11. Extract, double-click, done." 600 1078 28 ([System.Drawing.Color]::FromArgb(200, 214, 245)) $false "c" 0
Txt $g $Link 600 1120 24 ([System.Drawing.Color]::FromArgb(160, 190, 250)) $false "c" 0
$bmp.Save((Join-Path $docs "linkedin-2.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Host "wrote docs\linkedin-1.png and docs\linkedin-2.png"
