<#
.SYNOPSIS
Exercises WritingHost through WinApp CLI UI Automation on the host or in Sandbox.
.DESCRIPTION
With -Sandbox, requires WinApp CLI 0.7.0 and an enabled Windows Sandbox on
Windows 11 24H2+. Never enables features, installs tools, or stops the Sandbox.
Use -PublishDirectory to test an extracted release without rebuilding it.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$ArtifactDir = "$PSScriptRoot\artifacts",
    [string]$PublishDirectory,
    [switch]$Sandbox
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$repoRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
$proc = $null
$guestPid = $null
$previousWorkflow = $env:WINAPP_UI_WORKFLOW_ID
$env:WINAPP_UI_WORKFLOW_ID = "writinghost-smoke-$([guid]::NewGuid())"
$targetArgs = @()
if ($Sandbox) { $targetArgs = @('--on', 'sandbox') }

function Invoke-WinApp {
    param([Parameter(Mandatory)][string[]]$CommandArgs)
    $output = & winapp @CommandArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "winapp $($CommandArgs -join ' ') failed ($LASTEXITCODE): $($output | Out-String)"
    }
    return $output
}

function Invoke-AppUI {
    param([Parameter(Mandatory)][string[]]$CommandArgs)
    Invoke-WinApp -CommandArgs (@('ui') + $CommandArgs + $targetArgs + @('-a', "$appPid"))
}

function Assert-True([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAIL: $Name" }
    Write-Host "PASS: $Name" -ForegroundColor Green
}

try {
    if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
        throw 'WinApp CLI not found. Install with: winget install Microsoft.WinAppCLI'
    }
    $version = [Version]((Invoke-WinApp -CommandArgs @('--version') | Out-String).Trim())
    $minimum = if ($Sandbox) { [Version]'0.7.0' } else { [Version]'0.5.0' }
    if ($version -lt $minimum) {
        throw "WinApp CLI $minimum+ required; found $version. Run: winget upgrade Microsoft.WinAppCLI"
    }
    New-Item -ItemType Directory -Path $ArtifactDir -Force | Out-Null
    $ArtifactDir = (Resolve-Path -LiteralPath $ArtifactDir).Path
    $screenshot = Join-Path $ArtifactDir "writinghost-smoke-$([guid]::NewGuid()).png"

    if ($PublishDirectory) {
        $PublishDirectory = (Resolve-Path -LiteralPath $PublishDirectory).Path
        $exe = Join-Path $PublishDirectory 'WritingHost.exe'
    } else {
        dotnet build "$repoRoot\samples\WritingHost\WritingHost.csproj" -c $Configuration `
            -r win-x64 -p:Platform=x64 --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'WritingHost build failed.' }
        $exe = "$repoRoot\samples\WritingHost\bin\x64\$Configuration\net10.0-windows10.0.22621.0\win-x64\WritingHost.exe"
    }
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw "WritingHost.exe not found: $exe" }

    if ($Sandbox) {
        # Project evaluation identifies the unpackaged app; OutDir selects the exact extracted files.
        $launch = Invoke-WinApp -CommandArgs @(
            'run', "$repoRoot\samples\WritingHost\WritingHost.csproj",
            '--on', 'sandbox', '--detach', '--json', '--no-build', '--no-restore',
            '--configuration', $Configuration, '--runtime', 'win-x64',
            '--property', "OutDir=$(Split-Path -Parent $exe)\",
            '--property', "SelfContained=$([bool]$PublishDirectory)",
            '--property', "PortableRelease=$([bool]$PublishDirectory)"
        ) | Out-String | ConvertFrom-Json
        if (-not $launch.Sandbox -or $launch.ProcessScope -ne 'sandbox') {
            throw 'WinApp did not report a Sandbox-scoped process. Refusing host UI fallback.'
        }
        $guestPid = [int]$launch.ProcessId
        $appPid = $guestPid
    } else {
        $proc = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -PassThru
        $appPid = $proc.Id
    }

    Invoke-AppUI -CommandArgs @('wait-for', 'LoadDemoButton', '-t', '15000', '--quiet') | Out-Null
    Write-Host 'PASS: App window appears' -ForegroundColor Green
    Invoke-AppUI -CommandArgs @('invoke', 'LoadDemoButton', '--quiet') | Out-Null
    Invoke-AppUI -CommandArgs @('wait-for', 'An open todo item', '-t', '5000', '--quiet') | Out-Null
    $status = Invoke-AppUI -CommandArgs @('get-value', 'StatusText') | Out-String
    Assert-True ($status -match 'Demo document loaded') 'Demo document loaded'
    $tags = Invoke-AppUI -CommandArgs @('search', '#demo') | Out-String
    Assert-True ($tags -match 'demo') 'Inline tag chip is present'
    $image = Invoke-AppUI -CommandArgs @('search', 'lab sketch') | Out-String
    Assert-True ($image -match 'lab sketch') 'Demo image is present'

    $tree = Invoke-AppUI -CommandArgs @('inspect', 'window', '--depth', '8') | Out-String
    $checkbox = [regex]::Match($tree, '(chk-[0-9a-f]+) CheckBox \[off\]').Groups[1].Value
    Assert-True ($checkbox -ne '') 'Unchecked todo checkbox found'
    Invoke-AppUI -CommandArgs @('invoke', $checkbox, '--quiet') | Out-Null
    $tree = Invoke-AppUI -CommandArgs @('inspect', 'window', '--depth', '8') | Out-String
    Assert-True ($tree -match "$checkbox CheckBox \[on\]") 'Todo checkbox toggles'

    Invoke-AppUI -CommandArgs @('invoke', 'InsertTodoButton', '--quiet') | Out-Null
    Invoke-AppUI -CommandArgs @('wait-for', 'New todo', '-t', '5000', '--quiet') | Out-Null
    Invoke-AppUI -CommandArgs @('invoke', 'ClearInkButton', '--quiet') | Out-Null
    $status = Invoke-AppUI -CommandArgs @('get-value', 'StatusText') | Out-String
    Assert-True ($status -match 'Ink cleared') 'Ink toolbar is reachable'
    Invoke-AppUI -CommandArgs @('screenshot', 'window', '--output', $screenshot, '--quiet') | Out-Null
    Assert-True (Test-Path -LiteralPath $screenshot) 'Screenshot captured'
    Write-Host 'UI smoke test PASSED' -ForegroundColor Green
}
finally {
    try {
        if ($guestPid) {
            Invoke-WinApp -CommandArgs @(
                'target', 'exec', 'sandbox', '--', 'powershell.exe', '-NoProfile', '-Command',
                "if (Get-Process -Id $guestPid -ErrorAction SilentlyContinue) { Stop-Process -Id $guestPid }"
            ) | Out-Null
        }
        if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id }
    }
    finally {
        $env:WINAPP_UI_WORKFLOW_ID = $previousWorkflow
    }
}
