# Step 10 — Infinite canvas mechanics (world coordinates + pan/zoom)

## Goal
Support an “infinite canvas” experience by decoupling world coordinates from screen coordinates, and enabling panning (and optional zoom).

## Deliverables
- Viewport transform (world <-> screen)
- Panning interaction (pen barrel button or two-finger gesture; MVP: explicit pan tool)
- Optional zoom (ctrl+wheel or pinch)

## MVP decision points
- If you want strictly **pen-only drawing**, panning/zooming needs a non-drawing gesture:
  - Pan tool mode (user toggles a “hand” tool)
  - OR allow touch for pan only (if acceptable)

## Technical design (Win2D)
- Maintain:
  - `Vector2 Pan`
  - `float Zoom`
- Convert pointer positions from screen to world:
  - `world = (screen - Pan) / Zoom`
- Store stroke points in world coordinates.
- Render using a transform matrix.

## Background interaction
- Background spacing can be interpreted in world coords.
- When zooming, background scales naturally.

## Acceptance criteria
- User can pan to previously-drawn content beyond the initial viewport.
- Drawn strokes remain stable relative to world coordinates.

## References
- Win2D transformations: https://learn.microsoft.com/windows/apps/win2d/drawing-basics
- Pointer input: https://learn.microsoft.com/windows/apps/design/input/pointer
