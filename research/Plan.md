## Intro 
### Atrament:
repo: https://github.com/jakubfiala/atrament 

What it is:
“A small JS library for beautiful drawing and handwriting on the HTML Canvas.” It draws directly to canvas (no vector model) with smoothing and basic modes (draw / erase / fill).
Why it’s not “old cruft”:
- The repo explicitly mentions support for evergreen browsers and Safari 15+, with a v4 line — meaning it has seen updates for modern environments.
- Depends on standard canvas + Pointer Events; no legacy dependencies.
Pros for low‑latency scribbling:
- Direct bitmap drawing → very minimal overhead.
- Built‑in adaptive smoothing that still feels responsive.
- Events so you can implement undo/redo or replay later if you want.
Trade‑off:
Because it draws directly to the canvas bitmap, you don’t get a vector stroke model out of the box. Great for quick scribble pads; less ideal if you need high‑level editing.

### Ink API
What it is:
A browser API, not a library. It lets the browser/OS compositor render ink between JS event frames, which cuts the perceived latency further than JS alone can.
- Entry point: navigator.ink.requestPresenter(canvas).
- You keep Atrament/perfect-freehand/signature_pad logic, but the OS draws the “live trail” between frames.
Why it matters for you:
If you care about hard‑mode latency, you can:
- Use a tiny JS lib (perfect-freehand or Atrament) for the canonical stroke data and rendering.
- Use Ink API where available to let the OS show “live ink” while JS catches up.

How I’d architect a modern, low-latency inking stack
- Pointer handling layer (your code):
    - Capture: pointerdown/move/up with { x, y, pressure, time }.
    - Optional: caputre only pen input, no touch / mouse, if possible
    - Normalize to a common format that you can swap backends under.
- Stroke engine:
    - Option B:  Atrament → directly draw to canvas.
- Rendering:
    - Canvas2D for simplicity, 
    - Keep a small, explicit stroke model (even if Atrament is drawing directly, you can still track events for undo).
    - eventually evaluate webgl if there are tangible improvemnts we can make to latency/inking quality
- Latency enhancement:
    - If available, use Ink API for the predictive live trail.
    - Else: we don't support it  / sad.



# Starting plan
Improve on the existing react app to add a simple sketchpad concept.
Start with the simplest mvp MVP and implement it, make sure its' modular and easy to test.
Then make a series of Product requirements document (PRDs) as markdown files, using #runAgent for:
- adding infinite canvas
- adding background patterns
    - dot 
    - lines
- adding pages, tabs, notebook grouping
- adding more tools:
    - eraser
    - multipel color
    - differnt brushes/skething (pen, pencil, hiliight)
- adding undo redo
- adding selection mode


# checklist
- [x] first plan for the implemenation is made, it contains a PRD decribing the MVP and a section with the checklist to keep upaded during implemenation
- [x] implemented the MVP based on the PRD and keep the checklist updated, the checklist is fully completed
- [x] written  all the PRDs for future fetaures

## Implementation Summary

### MVP Sketchpad - COMPLETED ✓
See [PRD-MVP-Sketchpad.md](docs/PRD-MVP-Sketchpad.md)

The MVP includes:
- Full-viewport canvas with Atrament integration
- Pressure-sensitive drawing
- Ink API support for reduced latency (where available)
- Modular architecture with React hooks

### Future Feature PRDs - COMPLETED ✓
1. [PRD-Infinite-Canvas.md](docs/PRD-Infinite-Canvas.md) - Pan/zoom for unlimited drawing space
2. [PRD-Background-Patterns.md](docs/PRD-Background-Patterns.md) - Dot grid, line grid patterns
3. [PRD-Pages-Notebooks.md](docs/PRD-Pages-Notebooks.md) - Multi-page/notebook organization
4. [PRD-Drawing-Tools.md](docs/PRD-Drawing-Tools.md) - Eraser, colors, pen/pencil/highlighter
5. [PRD-Undo-Redo.md](docs/PRD-Undo-Redo.md) - Undo/redo with stroke recording
6. [PRD-Selection-Mode.md](docs/PRD-Selection-Mode.md) - Select, move, transform strokes
