<#
.SYNOPSIS
    WinApp CLI UI smoke test for the WritingHost sample.

.DESCRIPTION
    Drives the combined rich-text + ink host end-to-end using the Windows App
    Development CLI (winapp, v0.5+): launches the app, exercises the editor
    through UIA (invoke/get-value/wait-for), toggles an inline todo checkbox,
    and captures a screenshot artifact.

    Requires: winget install Microsoft.WinAppCLI  (v0.5.0 or later)

    Note: `winapp ui pen`/`ui touch` raw input injection is unreliable on
    mixed-DPI multi-monitor setups (strokes land in the wrong coordinate
    space). This test therefore sticks to UIA patterns and mouse SendInput,
    which are DPI-safe. Run pen gesture tests on a single-monitor/100%-DPI
    machine or a CI VM.

.EXAMPLE
    .\tests\WritingHost.UITests\Invoke-UISmokeTest.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug',
    [string]$ArtifactDir = "$PSScriptRoot\artifacts"
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

# --- Preconditions -----------------------------------------------------------
$winapp = Get-Command winapp -ErrorAction SilentlyContinue
if (-not $winapp) { throw 'winapp CLI not found. Install with: winget install Microsoft.WinAppCLI' }
$version = [Version](winapp --version)
if ($version -lt [Version]'0.5.0') { throw "winapp $version found; v0.5.0+ required. Run: winget upgrade Microsoft.WinAppCLI" }

# --- Build & launch ----------------------------------------------------------
Write-Host "Building $app..."
dotnet build "$repoRoot\samples\WritingHost\WritingHost.csproj" -c $Configuration -p:Platform=x64 --nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$exe = Get-ChildItem "$repoRoot\samples\WritingHost\bin\x64\$Configuration" -Recurse -Filter WritingHost.exe | Select-Object -First 1
if (-not $exe) { throw "WritingHost.exe not found under bin\x64\$Configuration" }

$existing = Get-Process $app -ErrorAction SilentlyContinue
if ($existing) { $existing | ForEach-Object { Stop-Process -Id $_.Id }; Start-Sleep 2 }

Write-Host "Launching $($exe.FullName)..."
$proc = Start-Process $exe.FullName -PassThru
New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null

try {
    winapp ui wait-for LoadDemoButton -a $app -t 15000 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'App window appears'

    # --- Load demo document ---------------------------------------------------
    winapp ui invoke LoadDemoButton -a $app --quiet
    winapp ui wait-for "An open todo item" -a $app -t 5000 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'Demo document renders inline todo items'

    $status = winapp ui get-value StatusText -a $app
    $statusText = ($status | Out-String).Trim()
    Assert-True ($statusText -match 'Demo document loaded') "Status reports demo loaded (got: $statusText)"

    # Inline tag chips render as UIA text elements
    $tags = winapp ui search '#demo' -a $app 2>&1 | Out-String
    Assert-True ($tags -match 'demo') 'Inline #demo tag chip is present'

    # Inline image exposes its alt text through automation
    $img = winapp ui search 'lab sketch' -a $app 2>&1 | Out-String
    Assert-True ($img -match 'lab sketch') 'Inline image (alt text) is present'

    # --- Toggle an inline todo checkbox (TogglePattern) -----------------------
    $before = (winapp ui search 'An open todo item' -a $app 2>&1 | Out-String)
    Assert-True ($before -match 'An open todo item') 'Open todo found before toggle'

    $tree = winapp ui inspect window -a $app --depth 8 2>&1 | Out-String
    $chk = [regex]::Match($tree, '(chk-[0-9a-f]+) CheckBox \[off\]').Groups[1].Value
    Assert-True ($chk -ne '') "Found unchecked checkbox slug ($chk)"

    winapp ui invoke $chk -a $app --quiet
    Start-Sleep 1
    $treeAfter = winapp ui inspect window -a $app --depth 8 2>&1 | Out-String
    Assert-True ($treeAfter -match "$chk CheckBox \[on\]") "Todo checkbox toggled to [on] via TogglePattern"

    # --- Append a todo via toolbar --------------------------------------------
    winapp ui invoke InsertTodoButton -a $app --quiet
    winapp ui wait-for 'New todo' -a $app -t 5000 --quiet
    Assert-True ($LASTEXITCODE -eq 0) 'Insert Todo appends a new todo block'

    # --- Ink toolbar reachable -------------------------------------------------
    winapp ui invoke ClearInkButton -a $app --quiet
    Start-Sleep 1
    $status = winapp ui get-value StatusText -a $app
    $statusText = ($status | Out-String).Trim()
    Assert-True ($statusText -match 'Ink cleared') "Clear Ink reachable via UIA (got: $statusText)"

    # --- Screenshot artifact ----------------------------------------------------
    winapp ui screenshot window -a $app --output "$ArtifactDir\writinghost-smoke.png" --quiet
    Assert-True (Test-Path "$ArtifactDir\writinghost-smoke.png") 'Screenshot artifact captured'
}
finally {
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id }
}

# --- Summary -----------------------------------------------------------------
Write-Host ''
if ($script:failures.Count -eq 0) {
    Write-Host 'UI smoke test PASSED' -ForegroundColor Green
    exit 0
} else {
    Write-Host "UI smoke test FAILED ($($script:failures.Count)): $($script:failures -join '; ')" -ForegroundColor Red
    exit 1
}
