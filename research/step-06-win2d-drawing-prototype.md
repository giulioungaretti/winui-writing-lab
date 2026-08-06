# Step 06 — Drawing MVP: Win2D canvas + pen-only input

## Goal
Implement a working drawing surface per page using Win2D. Only pen input is accepted. Provide basic pen tool settings.

## Deliverables
- Win2D dependency integrated
- A reusable drawing control (e.g., `InkCanvasView` / `DrawingSurfaceControl`)
- Pen-only input capture
- Render strokes with configurable color and thickness

## Technology choice
**Win2D** is recommended for WinUI 3 drawing.

## Win2D package
- `Win2D.uwp` (commonly used with WinUI; verify compatibility with Windows App SDK)

## Drawing interaction spec
- Pointer events:
  - Start stroke on `PointerPressed`
  - Add points on `PointerMoved`
  - End stroke on `PointerReleased` / `PointerCanceled`
- Input filtering:
  - Accept only `PointerDeviceType.Pen`
  - Ignore mouse and touch

## Data model (in-memory)
- `Stroke`
  - `IReadOnlyList<Vector2> Points`
  - `uint ColorArgb`
  - `float Thickness`

## Rendering spec
- Use Win2D `CanvasControl`:
  - On draw, render background (blank for now)
  - Render all strokes
  - Render active in-progress stroke

## MVVM binding spec
- Treat drawing as a view-level concern but expose *tool settings* and *page ink state* via ViewModel:
  - `PageViewModel.PenColor`
  - `PageViewModel.PenThickness`
  - `PageViewModel.ToolMode` (Pen/Eraser later)

## Acceptance criteria
- Drawing works with pen input.
- Pen color and thickness can be changed (even if via simple UI in the page card).
- No drawing occurs with mouse/touch.

## References
- Win2D repo: https://github.com/microsoft/Win2D
- Win2D docs: https://learn.microsoft.com/windows/apps/win2d/
- Pointer input in WinUI: https://learn.microsoft.com/windows/apps/design/input/pointer
