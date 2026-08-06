# Step 07 — Drawing tools: pen presets + eraser

## Goal
Add multiple drawing tool options: pen colors, pen sizes, and eraser.

## Deliverables
- Tool palette UI (minimal MVP)
- Pen presets and custom selection
- Eraser mode

## Tool spec
### Pen
- Configurable:
  - Color (ARGB)
  - Thickness (float)
- Optional presets:
  - Small/Medium/Large
  - Common colors

### Eraser (MVP)
- **Stroke eraser**: erase whole stroke when intersecting eraser circle.
- Eraser size configurable.

## Implementation notes (Win2D)
- Hit testing:
  - For each stroke, compute distance from pointer to polyline segments.
  - If within radius, remove the stroke.
- Performance:
  - MVP can do brute-force tests for moderate stroke counts.
  - Later optimization: spatial indexing (quad-tree) if needed.

## MVVM binding
- `PageViewModel` (or a dedicated `ToolViewModel`)
  - `ToolMode` enum: `Pen`, `Eraser`
  - `PenColor`, `PenThickness`
  - `EraserRadius`

## Acceptance criteria
- User can switch between pen and eraser.
- Eraser removes strokes reliably.
- Tool settings persist at least for the current session (per-page or global comes later).

## References
- Win2D repo: https://github.com/microsoft/Win2D
- Pointer input: https://learn.microsoft.com/windows/apps/design/input/pointer
