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

The smoke test builds and launches `WritingHost`, then drives it through UIA: `invoke` (buttons, todo checkboxes via TogglePattern), `get-value`, `search`, `wait-for` (`-t` is in milliseconds), and captures a screenshot artifact. Controls carry `x:Name`/AutomationIds (`LoadDemoButton`, `InsertTodoButton`, `ClearInkButton`, `StatusText`) so selectors survive layout changes.

`Invoke-PenGestureTest.ps1` exercises real synthetic-pen inking (pressure-varied strokes, eraser pass, MP4 recording via `winapp ui record`, pixel-diff assertion). It self-skips unless running on a single monitor at 100% scaling — see the limitation below — and can be forced with `-Force`.

Known limitation: `winapp ui pen` / `ui touch` raw input injection mistargets on mixed-DPI multi-monitor setups (strokes land in the wrong coordinate space; `ui inspect` reports physical per-monitor pixels while injection consumes a different space). UIA patterns and mouse `click`/`hover` are DPI-safe. Run pen/touch gesture tests on a single-monitor or 100%-scaling machine (e.g. a CI VM); `winapp ui record` can capture MP4 evidence there.

The WinUI projects retain their source target frameworks and package versions: ink remains on .NET 10 / Windows App SDK 1.8; rich text and InputLatency remain on .NET 8 / Windows App SDK 1.8. The sample hosts use `WindowsPackageType=None` solely to make this research repository build unpackaged without signing or packaging output. Build with `Platform=x64` because Win2D cannot be referenced correctly as AnyCPU. UI-test artifacts (screenshots, recordings, and temporary output) are ignored.

Excluded deliberately: notebook/timeline/task models, mobile/server/Aspire, persistence and product shell code, package artifacts, and TypeScript application code.
