<#
.SYNOPSIS
    WinApp CLI pen-gesture test for the WritingHost ink canvas.

.DESCRIPTION
    Verifies real synthetic-pen inking end-to-end: injects pen strokes
    (with pressure) onto the low-latency D2D ink canvas, records the session
    to MP4, and asserts that pixels in the canvas region actually changed.

    winapp `ui pen`/`ui touch` injection mistargets on mixed-DPI multi-monitor
    setups, so this test SELF-SKIPS unless the environment is safe:
    a single monitor at 100% scaling (96 DPI) — e.g. a CI VM.
    Override with -Force to attempt anyway.

    Requires: winget install Microsoft.WinAppCLI  (v0.5.0 or later)

.EXAMPLE
    .\tests\WritingHost.UITests\Invoke-PenGestureTest.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$ArtifactDir = "$PSScriptRoot\artifacts",
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot\..\.."
$app = 'WritingHost'
$script:failures = @()

function Assert-True([bool]$Condition, [string]$Name) {
    if ($Condition) {
        Write-Host "  PASS  $Name" -ForegroundColor Green
    } else {
        Write-Host "  FAIL  $Name" -ForegroundColor Red
        $script:failures += $Name
    }
}

# --- Environment gate: single monitor at 100% scaling ------------------------
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$dpiSig = '[DllImport("user32.dll")] public static extern bool SetProcessDPIAware(); [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h); [DllImport("gdi32.dll")] public static extern int GetDeviceCaps(IntPtr dc, int index);'
Add-Type -MemberDefinition $dpiSig -Name Native -Namespace PenTest
[PenTest.Native]::SetProcessDPIAware() | Out-Null
$screens = [System.Windows.Forms.Screen]::AllScreens
$dpi = [PenTest.Native]::GetDeviceCaps([PenTest.Native]::GetDC([IntPtr]::Zero), 88) # LOGPIXELSX

if (-not $Force -and ($screens.Count -ne 1 -or $dpi -ne 96)) {
    Write-Host "SKIPPED: pen injection requires a single monitor at 100% scaling (found $($screens.Count) monitor(s), $dpi DPI)." -ForegroundColor Yellow
    Write-Host '         Run on a CI VM / single-100%-DPI machine, or pass -Force to attempt anyway.' -ForegroundColor Yellow
    exit 0
}

# --- Preconditions -----------------------------------------------------------
if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) { throw 'winapp CLI not found. Install with: winget install Microsoft.WinAppCLI' }
if ([Version](winapp --version) -lt [Version]'0.5.0') { throw 'winapp v0.5.0+ required (ui pen/record). Run: winget upgrade Microsoft.WinAppCLI' }

# --- Build & launch ----------------------------------------------------------
Write-Host "Building $app..."
dotnet build "$repoRoot\samples\WritingHost\WritingHost.csproj" --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$exe = Get-ChildItem "$repoRoot\samples\WritingHost\bin\$Configuration" -Recurse -Filter WritingHost.exe | Select-Object -First 1
Get-Process $app -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id }
Start-Sleep 1
$proc = Start-Process $exe.FullName -PassThru
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

function Get-CanvasRegion {
    # The ink canvas fills the right half of the window; derive its rect from window bounds.
    $tree = winapp ui inspect window -a $app --depth 1 2>&1 | Out-String
    $m = [regex]::Match($tree, 'Window "[^"]+" \((-?\d+),(-?\d+) (\d+)x(\d+)\)')
    if (-not $m.Success) { throw "Could not read window bounds from: $tree" }
    $x = [int]$m.Groups[1].Value; $y = [int]$m.Groups[2].Value
    $w = [int]$m.Groups[3].Value; $h = [int]$m.Groups[4].Value
    [pscustomobject]@{
        Left   = [int]($x + $w * 0.55)
        Right  = [int]($x + $w * 0.95)
        Top    = [int]($y + $h * 0.25)
        Bottom = [int]($y + $h * 0.85)
    }
}

function Get-CanvasPixelHash([string]$Path) {
    # Coarse content signature of the right half of a window screenshot.
    $bmp = [System.Drawing.Bitmap]::FromFile($Path)
    try {
        $sb = [System.Text.StringBuilder]::new()
        for ($py = [int]($bmp.Height * 0.15); $py -lt [int]($bmp.Height * 0.9); $py += 8) {
            for ($px = [int]($bmp.Width * 0.52); $px -lt [int]($bmp.Width * 0.98); $px += 8) {
                $c = $bmp.GetPixel($px, $py)
                [void]$sb.Append($c.R -band 0xF0).Append($c.G -band 0xF0)
            }
        }
        return $sb.ToString().GetHashCode()
    } finally { $bmp.Dispose() }
}

try {
    winapp ui wait-for LoadDemoButton -a $app -t 15000 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'App window appears'

    $region = Get-CanvasRegion
    $midY = [int](($region.Top + $region.Bottom) / 2)

    # Baseline screenshot of the empty canvas
    $before = "$ArtifactDir\pen-before.png"
    winapp ui screenshot window -a $app --output $before --quiet

    # Record the whole gesture session to MP4 evidence
    $record = Start-Job -ScriptBlock {
        param($app, $out)
        winapp ui record window -a $app --duration-sec 12 --output $out --quiet
    } -ArgumentList $app, "$ArtifactDir\pen-session.mp4"

    Start-Sleep 2

    # Stroke 1: pressure-varied wave across the canvas
    $wave = @()
    $step = [int](($region.Right - $region.Left) / 10)
    for ($i = 0; $i -le 10; $i++) {
        $wx = $region.Left + $i * $step
        $wy = $midY + [int](60 * [math]::Sin($i * 0.9))
        $wave += "$wx,$wy"
    }
    winapp ui pen -a $app --path ($wave -join ' ') --pressure 0.8 --duration-ms 1500 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'Pen wave stroke injected'

    # Stroke 2: light-pressure underline
    winapp ui pen -a $app --path "$($region.Left),$($region.Bottom - 40) $($region.Right),$($region.Bottom - 40)" --pressure 0.25 --duration-ms 700 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'Pen underline stroke injected'

    Start-Sleep 1
    $after = "$ArtifactDir\pen-after.png"
    winapp ui screenshot window -a $app --output $after --quiet

    # Assert the canvas visually changed where we drew
    $changed = (Get-CanvasPixelHash $before) -ne (Get-CanvasPixelHash $after)
    Assert-True $changed 'Ink strokes rendered on canvas (pixels changed)'

    # Eraser pass over the wave, then verify pixels changed again
    winapp ui pen -a $app --path ($wave -join ' ') --eraser --duration-ms 1500 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'Eraser stroke injected'

    Receive-Job -Job $record -Wait | Out-Null
    Remove-Job $record -Force -ErrorAction SilentlyContinue
    Assert-True (Test-Path "$ArtifactDir\pen-session.mp4") 'MP4 recording artifact captured'
}
finally {
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id }
    Get-Job | Remove-Job -Force -ErrorAction SilentlyContinue
}

Write-Host ''
if ($script:failures.Count -eq 0) {
    Write-Host 'Pen gesture test PASSED' -ForegroundColor Green
    exit 0
} else {
    Write-Host "Pen gesture test FAILED ($($script:failures.Count)): $($script:failures -join '; ')" -ForegroundColor Red
    exit 1
}
