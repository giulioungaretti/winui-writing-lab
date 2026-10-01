<#
.SYNOPSIS
Checks archive validation and Sandbox smoke routing without installing tools or launching apps.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
$temporary = Join-Path $root "artifacts\release-tests-$([guid]::NewGuid())"
$publish = "$root\scripts\Publish-WritingHost.ps1"
$smoke = "$root\tests\WritingHost.UITests\Invoke-UISmokeTest.ps1"
$name = 'WritingHost-1.2.3-preview.1-win-x64'
$calls = [Collections.Generic.List[string[]]]::new()
$caseNumber = 0
$mockExitCode = 0
$mockVersion = '0.7.0'
$mockUiFailure = $false
$mockSandbox = $true

function Assert-True([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAIL: $Name" }
    Write-Host "PASS: $Name"
}

function Assert-Throws([scriptblock]$Action, [string]$Pattern) {
    $failure = $null
    try { & $Action } catch { $failure = $_ }
    Assert-True ($null -ne $failure -and "$failure" -match $Pattern) "Rejects $Pattern"
}

function New-Layout {
    $script:caseNumber++
    $output = Join-Path $temporary "case-$script:caseNumber"
    $layout = Join-Path $output $name
    foreach ($file in @(
        'WritingHost.exe', 'WritingHost.dll', 'Ink.Core.dll', 'Ink.WinUI.dll',
        'RichText.Core.dll', 'RichText.WinUI.dll', 'WritingHost.deps.json',
        'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll', 'Microsoft.UI.Xaml.dll',
        'WritingHost.pri', 'Ink.WinUI.pri', 'RichText.WinUI.pri',
        'App.xbf', 'MainWindow.xbf', 'Ink.WinUI\Controls\InkCanvas.xbf', 'Assets\demo-sketch.png'
    )) {
        $path = Join-Path $layout $file
        New-Item -ItemType Directory -Path (Split-Path -Parent $path) -Force | Out-Null
        [IO.File]::WriteAllText($path, 'test fixture')
    }
    '{"runtimeOptions":{"includedFrameworks":[{"name":"Microsoft.NETCore.App","version":"10.0.0"}]}}' |
        Set-Content -LiteralPath "$layout\WritingHost.runtimeconfig.json"
    return $output
}

function winapp {
    $command = [string[]]$args
    $calls.Add($command)
    $global:LASTEXITCODE = $mockExitCode
    if ($mockExitCode -ne 0) { return 'mock command failure' }
    if ($mockUiFailure -and $command[0] -eq 'ui') {
        $global:LASTEXITCODE = 1
        return 'mock UI failure'
    }
    if ($command[0] -eq '--version') { return $mockVersion }
    if ($command[0] -eq 'run') {
        return @{ ProcessId = 12345; Sandbox = $mockSandbox; ProcessScope = 'sandbox' } | ConvertTo-Json
    }
    if ($command[0] -eq 'target' -and $command[1] -eq 'exec') {
        return
    }
    switch ($command[1]) {
        'get-value' { return 'Demo document loaded; Ink cleared' }
        'search' { return '#demo lab sketch' }
        'inspect' { return 'chk-ab12 CheckBox [off]; chk-ab12 CheckBox [on]' }
        'screenshot' {
            $index = [Array]::IndexOf($command, '--output')
            [IO.File]::WriteAllText($command[$index + 1], 'mock screenshot')
        }
    }
}

try {
    Assert-Throws { & $publish -Version '..\escape' -ArchiveOnly } 'Cannot validate'
    Assert-Throws { & $publish -PublishOnly -ArchiveOnly } 'mutually exclusive'
    $output = New-Layout
    & $publish -Version '1.2.3-preview.1' -OutputDirectory $output -ArchiveOnly
    $zip = Join-Path $output "$name.zip"
    $expected = (Get-Content -LiteralPath "$zip.sha256").Split(' ')[0]
    Assert-True ((Get-FileHash -LiteralPath $zip).Hash.ToLowerInvariant() -eq $expected) 'ZIP checksum matches'
    $unpacked = Join-Path $temporary 'extracted'
    Expand-Archive -LiteralPath $zip -DestinationPath $unpacked
    Assert-True (Test-Path -LiteralPath "$unpacked\$name\Assets\demo-sketch.png") 'Archive retains directory structure'
    $manifest = Get-Content -LiteralPath "$unpacked\$name\release.json" -Raw | ConvertFrom-Json
    Assert-True (-not $manifest.signed -and $manifest.selfContained -and $manifest.version -eq '1.2.3-preview.1') 'Unsigned release metadata is accurate'
    Assert-Throws { & $publish -Version '1.2.3-preview.1' -OutputDirectory $output -ArchiveOnly } 'already exists'
    Assert-Throws { & $publish -Version '1.2.3-preview.1' -OutputDirectory $output } 'already exists'

    $missing = New-Layout
    Remove-Item -LiteralPath "$missing\$name\WritingHost.pri"
    Assert-Throws { & $publish -Version '1.2.3-preview.1' -OutputDirectory $missing -ArchiveOnly } 'missing WritingHost.pri'
    $dependent = New-Layout
    '{"runtimeOptions":{"framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}' |
        Set-Content -LiteralPath "$dependent\$name\WritingHost.runtimeconfig.json"
    Assert-Throws { & $publish -Version '1.2.3-preview.1' -OutputDirectory $dependent -ArchiveOnly } 'shared .NET runtime'
    $unsigned = New-Layout
    Assert-Throws { & $publish -Version '1.2.3-preview.1' -OutputDirectory $unsigned -ArchiveOnly -RequireSigned } 'Authenticode signature'
    Assert-True (-not (Test-Path -LiteralPath "$unsigned\$name.zip")) 'Signing failure creates no ZIP'

    & $smoke -Sandbox -PublishDirectory "$output\$name" -ArtifactDir "$temporary\evidence"
    $uiCalls = @($calls | Where-Object { $_[0] -eq 'ui' })
    Assert-True ($uiCalls.Count -ge 10) 'Smoke test exercises UI assertions'
    foreach ($call in $uiCalls) {
        $on = [Array]::IndexOf($call, '--on')
        $app = [Array]::IndexOf($call, '-a')
        Assert-True ($on -ge 0 -and $call[$on + 1] -eq 'sandbox' -and $app -ge 0 -and $call[$app + 1] -eq '12345') 'UI command uses guest scope and PID'
    }
    $runCalls = @($calls | Where-Object { $_[0] -eq 'run' })
    Assert-True ($runCalls.Count -eq 1 -and $runCalls[0] -contains '--no-build' -and $runCalls[0] -contains '--no-restore') 'Release test launches without rebuilding'
    Assert-True ($runCalls[0] -contains "OutDir=$output\$name\") 'Release test selects the extracted output directory'
    Assert-True (@($calls | Where-Object { $_[-1] -match 'Stop-Process -Id 12345' }).Count -eq 1) 'Cleanup stops only the test process'
    $mockUiFailure = $true
    $workflowBefore = $env:WINAPP_UI_WORKFLOW_ID
    Assert-Throws { & $smoke -Sandbox -PublishDirectory "$output\$name" -ArtifactDir "$temporary\evidence" } 'mock UI failure'
    Assert-True (@($calls | Where-Object { $_[-1] -match 'Stop-Process -Id 12345' }).Count -eq 2) 'UI failure still cleans up its process'
    Assert-True ($env:WINAPP_UI_WORKFLOW_ID -eq $workflowBefore) 'Workflow ownership is restored after failure'
    $mockUiFailure = $false
    $mockSandbox = $false
    Assert-Throws { & $smoke -Sandbox -PublishDirectory "$output\$name" -ArtifactDir "$temporary\evidence" } 'Refusing host UI fallback'
    $mockSandbox = $true
    $mockExitCode = 1
    Assert-Throws { & $smoke -Sandbox -PublishDirectory "$output\$name" -ArtifactDir "$temporary\evidence" } 'mock command failure'
    $mockExitCode = 0
    $mockVersion = '0.6.0'
    Assert-Throws { & $smoke -Sandbox -PublishDirectory "$output\$name" -ArtifactDir "$temporary\evidence" } '0.7.0'
    Write-Host 'Release script tests PASSED'
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
