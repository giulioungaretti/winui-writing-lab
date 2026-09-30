# WinUI Writing Lab

A focused C#/WinUI 3 research workspace for reusable ink input/rendering and block-based rich-text editing.

## Layout

- `src/Ink.Core` — platform-neutral stroke, geometry, viewport, and input processing.
- `src/Ink.WinUI` — reusable `InkControl` wrapping native WinAppSDK `InkCanvas` and `InkToolbar`.
- `src/RichText.Core` — document model, parsers, and text renderers.
- `src/RichText.WinUI` — archived reusable rich-text controls, services, and view models.
- `tests` — migrated core tests.
- `tests/WritingHost.UITests` — WinApp CLI (`winapp`) UI smoke test driving the combined host via UI Automation.
- `samples` — small hosts for the ink and rich-text controls, plus `WritingHost`, a combined host with the rich-text editor and native infinite ink canvas side by side.
- `benchmarks/InputLatency` — Win2D pointer-to-render latency experiment.
- `research` — selected architecture/performance notes only (no TypeScript application code).
- `provenance/SESSION-STATE.md` — sources, current archive locations, revisions, scope, and migration decisions.
- `provenance/archive-lab-source-repos-report.md` — sanitized archive-location and revision record; workstation paths and operational cleanup details are deliberately excluded.

## Build and test

```powershell
dotnet build WinUI.Writing.Lab.slnx -p:Platform=x64
dotnet build samples\WritingHost\WritingHost.csproj -p:Platform=x64
dotnet build benchmarks\InputLatency\InputLatency.csproj -p:Platform=x64
dotnet test tests\Ink.Core.Tests\Ink.Core.Tests.csproj
dotnet test tests\Ink.Native.Tests\Ink.Native.Tests.csproj
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

The ink and rich-text libraries and their sample hosts use Windows App SDK **2.5.4-experimental**.
Ink targets .NET 10 / Windows 11 (22621); rich text retains its .NET 8 target. The independent
`InputLatency` Win2D benchmark remains on .NET 8 / Windows App SDK 1.8 and is not the ink backend.
The sample hosts use `WindowsPackageType=None` and `WindowsAppSDKSelfContained=true`: their build
output includes the matching experimental Windows runtime, so no system-wide installation is needed.
The matching .NET runtime is still required. Build with `Platform=x64` (including the Win2D benchmark).
UI-test artifacts are ignored. `WritingHost.log` beside the executable records unhandled errors without
suppressing them.

## Native infinite canvas

All live and completed ink is rendered by `Microsoft.UI.Xaml.Controls.InkCanvas.InkPresenter`.
There is no Vortice renderer, swap chain, independent app render loop, custom drying, or fallback backend.
The built-in toolbar offers a ballpoint pen, native whole-stroke erasing, and a pan tool; pencil and
highlighter are deliberately omitted because the existing `INKS` format does not represent those brushes.

- Draw with a pen, or the left mouse button in the samples. Reusable controls opt into mouse drawing
  with `IsMouseInkingEnabled="True"`; touch is always reserved for navigation.
- Pan with the pan tool, right-mouse drag, pen barrel-button drag, one-finger touch, or the wheel.
  Shift+wheel pans horizontally. Touch dragging does not add ink.
- Pinch, Ctrl+wheel, or the zoom buttons zoom between 25% and 800%. Pointer/pinch zoom preserves
  the world point under the gesture; button zoom uses the viewport center. Reset returns to the origin.
- Pan has no document-edge clamp. Strokes retain world coordinates, including negative coordinates;
  the native surface stays viewport-sized. Each native stroke has an absolute point transform from
  its capture space to the current view, and its pen width is scaled separately. Navigation is
  coalesced to a composition frame and deferred while a stroke is being collected.
- Ruled/dotted paper is a separate clipped XAML layer anchored to the same world coordinates.
  Dense patterns are thinned at low zoom; paper rendering is bounded by viewport size.
- `GetStrokes`/`SetStrokes`, `Clear`, `ExportAsync`/`ImportAsync` and their existing aliases remain.
  `INKS` v1/v2 data and GUIDs stay authoritative. Native IDs and boot-relative timestamps are adapted,
  not substituted into the persisted format. The OS owns eraser hit behavior; the old custom
  `EraserRadius` setting and interpolation modes have been removed.

`Ink.Native.Tests` exercises the actual Windows ink point/stroke objects and production adapter/
serializer source without needing a XAML application: projected coordinates, pen width, eraser
selection geometry, timestamp conversion, repeated navigation, GUID preservation and v1/v2 data.
`Ink.Core.Tests` continues to cover the platform-neutral geometry and historical input processor.

This is an unsupported experimental SDK, pinned intentionally. The September 29 package includes
custom drying (`InkPresenter.ActivateCustomDrying` / `InkSynchronizer`), a custom-drying
`StrokeContainer` crash fix, and presenter-size corrections in
[microsoft/microsoft-ui-xaml#11801](https://github.com/microsoft/microsoft-ui-xaml/pull/11801),
plus the high-DPI right/bottom input cutoff fix in
[microsoft/microsoft-ui-xaml#11975](https://github.com/microsoft/microsoft-ui-xaml/pull/11975).
The app deliberately leaves drying native-owned; it does not activate custom drying or patch the SDK.
Full brush fidelity, physical pen/touch behavior, and transformed-canvas alignment at 150%/200%
scaling still require hardware/UI validation with this experimental runtime.

Excluded deliberately: notebook/timeline/task models, mobile/server/Aspire, persistence and product shell code, package artifacts, and TypeScript application code.
