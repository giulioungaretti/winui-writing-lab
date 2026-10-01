<#
.SYNOPSIS
Publishes a portable Windows x64 WritingHost ZIP and SHA-256 checksum.
.DESCRIPTION
Use -PublishOnly, sign the application's binaries, then use -ArchiveOnly
-RequireSigned to package signed output without rebuilding it.
#>
[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z]+([.-][0-9A-Za-z]+)*)?$')]
    [string]$Version = '0.0.0-dev',
    [string]$OutputDirectory = "$PSScriptRoot\..\artifacts\release",
    [switch]$PublishOnly,
    [switch]$ArchiveOnly,
    [switch]$RequireSigned
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($PublishOnly -and $ArchiveOnly) { throw 'PublishOnly and ArchiveOnly are mutually exclusive.' }
if ($RequireSigned -and $PublishOnly) { throw 'RequireSigned applies to archiving, after signing.' }
if (-not $IsWindows) { throw 'Publishing WritingHost requires Windows and PowerShell 7.' }

$root = [IO.Path]::GetFullPath($OutputDirectory)
$name = "WritingHost-$Version-win-x64"
$layout = Join-Path $root $name
$archive = Join-Path $root "$name.zip"
$checksum = "$archive.sha256"
$appBinaries = @('WritingHost.exe', 'WritingHost.dll', 'Ink.Core.dll', 'Ink.WinUI.dll', 'RichText.Core.dll', 'RichText.WinUI.dll')

if (-not $ArchiveOnly) {
    if (Test-Path -LiteralPath $layout) { throw "Output already exists: $layout. Choose a new output directory or version." }
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    dotnet publish "$PSScriptRoot\..\samples\WritingHost\WritingHost.csproj" `
        -c Release -r win-x64 --self-contained true -p:Platform=x64 `
        -p:PortableRelease=true "-p:Version=$Version" -o $layout --nologo -v quiet
    if ($LASTEXITCODE -ne 0) { throw "WritingHost publish failed with exit code $LASTEXITCODE." }
}

$requiredFiles = $appBinaries + @(
    'WritingHost.runtimeconfig.json', 'WritingHost.deps.json',
    'coreclr.dll', 'hostfxr.dll', 'hostpolicy.dll',
    'Microsoft.UI.Xaml.dll', 'WritingHost.pri', 'Ink.WinUI.pri', 'RichText.WinUI.pri',
    'App.xbf', 'MainWindow.xbf', 'Ink.WinUI\Controls\InkCanvas.xbf', 'Assets\demo-sketch.png'
)
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath (Join-Path $layout $file) -PathType Leaf)) {
        throw "Publish output is incomplete: missing $file in $layout."
    }
}
$runtime = Get-Content -LiteralPath "$layout\WritingHost.runtimeconfig.json" -Raw | ConvertFrom-Json
if ($runtime.runtimeOptions.PSObject.Properties.Name -contains 'framework' -or
    $runtime.runtimeOptions.PSObject.Properties.Name -contains 'frameworks') {
    throw 'Publish output requires a shared .NET runtime instead of being self-contained.'
}
if ($PublishOnly) {
    Write-Host "Published layout: $layout"
    return
}
foreach ($path in @($archive, $checksum)) {
    if (Test-Path -LiteralPath $path) { throw "Release asset already exists: $path. Refusing to overwrite it." }
}

$signatures = foreach ($file in $appBinaries) {
    $signature = Get-AuthenticodeSignature -LiteralPath (Join-Path $layout $file)
    if ($RequireSigned -and ($signature.Status -ne 'Valid' -or -not $signature.TimeStamperCertificate)) {
        throw "$file must have a valid, trusted, timestamped Authenticode signature (found $($signature.Status))."
    }
    [ordered]@{ file = $file; status = "$($signature.Status)"; timestamped = [bool]$signature.TimeStamperCertificate }
}
$signed = @($signatures | Where-Object { $_.status -ne 'Valid' -or -not $_.timestamped }).Count -eq 0
if (-not $signed) { Write-Warning 'Creating an unsigned research prerelease. Windows security policy may block execution.' }

[ordered]@{
    version = $Version
    architecture = 'x64'
    minimumWindowsBuild = 22621
    selfContained = $true
    signed = $signed
    signatures = @($signatures)
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "$layout\release.json" -Encoding utf8
@"
WritingHost $Version - Windows x64 research sample

Requires Windows 11 22H2 (build 22621) or newer.
Extract the entire ZIP, then open WritingHost.exe. Keep all files together.
The .NET and Windows App SDK runtimes are bundled. No developer tools are needed.
Native prerequisites must be checked on the target machine; this is not an installer.
Updates are manual: extract a newer release into a separate folder.

Trusted, timestamped application signatures: $signed
Unsigned builds are research prereleases and may be blocked by Windows security policy.
Signing does not guarantee SmartScreen reputation. Do not disable security protections.
See release.json for signature details.

Source and release documentation:
https://github.com/giulioungaretti/winui-writing-lab
"@ | Set-Content -LiteralPath "$layout\README.txt" -Encoding utf8

Compress-Archive -LiteralPath $layout -DestinationPath $archive -CompressionLevel Optimal
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $name.zip" | Set-Content -LiteralPath $checksum -Encoding ascii
Write-Host "Release ZIP: $archive"
Write-Host "SHA-256: $hash"
