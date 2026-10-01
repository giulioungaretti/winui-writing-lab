# WinUI Writing Lab

A focused C#/WinUI 3 research workspace for reusable ink input/rendering and block-based rich-text editing.

## Layout

- `src/Ink.Core` — platform-neutral stroke, geometry, viewport, and input processing.
- `src/Ink.WinUI` — reusable `InkControl` WinUI renderer/control.
- `src/RichText.Core` — document model, parsers, and text renderers.
- `src/RichText.WinUI` — archived reusable rich-text controls, services, and view models.
- `tests` — migrated core tests.
- `tests/WritingHost.UITests` — WinApp CLI (`winapp`) UI smoke test driving the combined host via UI Automation.
- `samples` — small hosts for the ink and rich-text controls, plus `WritingHost`, a combined host with the rich-text editor and low-latency ink canvas side by side.
- `benchmarks/InputLatency` — Win2D pointer-to-render latency experiment.
- `research` — selected architecture/performance notes only (no TypeScript application code).
- `provenance/SESSION-STATE.md` — sources, current archive locations, revisions, scope, and migration decisions.
- `provenance/archive-lab-source-repos-report.md` — sanitized archive-location and revision record; workstation paths and operational cleanup details are deliberately excluded.

## Build and test

```powershell
dotnet build WinUI.Writing.Lab.slnx
dotnet build benchmarks\InputLatency\InputLatency.csproj -p:Platform=x64
dotnet test tests\Ink.Core.Tests\Ink.Core.Tests.csproj
dotnet test tests\RichText.Core.Tests\RichText.Core.Tests.csproj
```

## UI testing (WinApp CLI)

Requires the [Windows App Development CLI](https://github.com/microsoft/winappCli) v0.5.0+ (`winget install Microsoft.WinAppCLI`):

```powershell
.\tests\WritingHost.UITests\Invoke-UISmokeTest.ps1
```

### Windows Sandbox

Sandbox smoke testing requires WinApp CLI **0.7.0+**, Windows 11 **24H2+** on a supported edition, hardware virtualization, and the Windows Sandbox optional feature enabled. Enable the feature and complete any restart/client setup yourself; the scripts never change Windows settings or stop an existing Sandbox.

```powershell
.\scripts\Publish-WritingHost.ps1 -Version 0.1.0-preview.1
Expand-Archive .\artifacts\release\WritingHost-0.1.0-preview.1-win-x64.zip .\artifacts\extracted
.\tests\WritingHost.UITests\Invoke-UISmokeTest.ps1 -Sandbox `
    -PublishDirectory .\artifacts\extracted\WritingHost-0.1.0-preview.1-win-x64
```

The same assertions run against the guest PID, with `--on sandbox` on every UI command. `winapp run` evaluates the unpackaged project with `--no-build --no-restore` and an output-directory override pointing to the extracted release; the ZIP is not rebuilt or debug-packaged. WinApp manages the detached guest application's lifetime and can provision missing supported runtimes. This is a functional release smoke test, not proof that a pristine machine needs no prerequisites, and does not certify SmartScreen/browser download behavior or every Windows version. An existing Sandbox may already contain dependencies; use a fresh instance for clean-machine checks. The test closes only its own application process and leaves the Sandbox running. Keep the host session unlocked and the Sandbox client connected for screenshots. Screenshots are saved to the host's artifact directory.

See [WinApp CLI Sandbox execution](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/sandbox-execution). Sandbox is a shared guest environment, and `--clean` is not a guest reset. Do not share a runner with unrelated or untrusted workflows.

## Downloadable builds and releases

The `Windows build and release` GitHub Actions workflow builds the sample hosts, runs both core test suites, and uploads a **WritingHost-win-x64** artifact containing a versioned portable ZIP and its SHA-256 checksum. Pushing a `vMAJOR.MINOR.PATCH` tag (optionally with a prerelease suffix, such as `v0.1.0-preview.1`) publishes those assets to [GitHub Releases](https://github.com/giulioungaretti/winui-writing-lab/releases). All releases are marked as research prereleases, including signed ones. Merge the workflow before tagging a commit that contains it.

Download the **WritingHost ZIP**, not GitHub's source archive. Extract the entire archive and open `WritingHost.exe` inside its folder. Windows x64, Windows 11 22H2/build 22621 or newer is required. Both the .NET and Windows App SDK runtimes are bundled; native dependencies still need target-machine verification. No installer, Start menu registration, or automatic updater is provided. Keep all files together and extract updates into a new folder.

For a local build, use PowerShell 7 and the .NET 10 SDK:

```powershell
.\scripts\Publish-WritingHost.ps1 -Version 0.1.0-preview.1
Get-FileHash .\artifacts\release\WritingHost-0.1.0-preview.1-win-x64.zip -Algorithm SHA256
.\tests\Release.Tests\Invoke-ReleaseTests.ps1
```

Output is under `artifacts\release`; the script refuses to overwrite an existing layout or release asset. `global.json` selects a stable .NET 10 SDK. CI also installs .NET 8 for the older core tests. Portable publishing is opt-in and does not change normal development builds.

### Optional Sandbox release gate

Register a dedicated, trusted Windows 11 24H2+ x64 self-hosted runner with labels **Windows**, **X64**, and **windows-sandbox**, and install PowerShell 7. Pre-enable Sandbox and finish client setup. Run the runner in an unlocked interactive user session, not as a headless Windows service. Set the repository Actions variable `WINDOWS_SANDBOX_ENABLED=true` to enable the gate: it downloads the actual build ZIP, verifies its checksum, extracts it, runs the Sandbox smoke test, and uploads screenshots. A failed gate prevents publication. Without this variable, hosted CI still produces downloads and release notes explicitly report that Sandbox testing was not performed.

Sandbox jobs never run on pull requests, because executing untrusted PR code on a persistent self-hosted runner is unsafe. One repository-wide concurrency group serializes these tests; reserve the runner/Sandbox for this workflow. The workflow does not silently fall back to the host desktop.

### Signing

Unsigned builds are supported for research testing and labeled accordingly. Windows SmartScreen, Smart App Control, or enterprise policy may block them; do not disable security protections. A trusted signature identifies the publisher but does not guarantee immediate SmartScreen reputation. Self-signed development certificates are not public-distribution trust.

Optional [Azure Artifact Signing](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options) is wired into tag builds. Set Actions variable `ARTIFACT_SIGNING_ENABLED=true`, variables `ARTIFACT_SIGNING_ENDPOINT`, `ARTIFACT_SIGNING_ACCOUNT`, `ARTIFACT_SIGNING_PROFILE`, and secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`. Configure Azure GitHub OIDC federation for the release tag scope and grant the identity only the certificate-profile signing role. Use a **Public Trust** profile; eligibility and identity verification are handled by the service. No PFX/password is stored in the repository.

Signing targets only this repository's application binaries, preserving vendor runtime signatures, and happens before ZIP/checksum generation. The archiver requires valid trusted signatures and timestamps when signing is enabled; errors fail the release rather than silently publishing unsigned output. For another signing provider:

```powershell
.\scripts\Publish-WritingHost.ps1 -Version 0.1.0-preview.1 -PublishOnly
# Sign WritingHost.exe, WritingHost.dll, Ink.Core.dll, Ink.WinUI.dll,
# RichText.Core.dll, and RichText.WinUI.dll in the published folder.
.\scripts\Publish-WritingHost.ps1 -Version 0.1.0-preview.1 -ArchiveOnly -RequireSigned
```

MSIX/Store packaging is not introduced here. Direct MSIX distribution normally requires a trusted package signature and a matching Publisher identity. A repository license and a complete redistribution-notice policy still need an owner decision before treating these research downloads as a general-public product.

The smoke test builds and launches `WritingHost`, then drives it through UIA: `invoke` (buttons, todo checkboxes via TogglePattern), `get-value`, `search`, `wait-for` (`-t` is in milliseconds), and captures a screenshot artifact. Controls carry `x:Name`/AutomationIds (`LoadDemoButton`, `InsertTodoButton`, `ClearInkButton`, `StatusText`) so selectors survive layout changes.

`Invoke-PenGestureTest.ps1` exercises real synthetic-pen inking (pressure-varied strokes, eraser pass, MP4 recording via `winapp ui record`, pixel-diff assertion). It self-skips unless running on a single monitor at 100% scaling — see the limitation below — and can be forced with `-Force`.

Known limitation: `winapp ui pen` / `ui touch` raw input injection mistargets on mixed-DPI multi-monitor setups (strokes land in the wrong coordinate space; `ui inspect` reports physical per-monitor pixels while injection consumes a different space). UIA patterns and mouse `click`/`hover` are DPI-safe. Run pen/touch gesture tests on a single-monitor or 100%-scaling machine (e.g. a CI VM); `winapp ui record` can capture MP4 evidence there.

The WinUI projects retain their source target frameworks and package versions: ink remains on .NET 10 / Windows App SDK 1.8; rich text and InputLatency remain on .NET 8 / Windows App SDK 1.8. The sample hosts use `WindowsPackageType=None` solely to make this research repository build unpackaged without signing or packaging output. Build with `Platform=x64` because Win2D cannot be referenced correctly as AnyCPU. UI-test artifacts (screenshots, recordings, and temporary output) are ignored.

Excluded deliberately: notebook/timeline/task models, mobile/server/Aspire, persistence and product shell code, package artifacts, and TypeScript application code.
