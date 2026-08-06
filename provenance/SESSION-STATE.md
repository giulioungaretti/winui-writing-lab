# Session state — writing-lab migration

Created: 2026-08-04

## Sources and revisions

| Source | Location | Revision / selection |
|---|---|---|
| Ink platform | `archive/FY26/personal/inkapp` | `1a50ece0eb962674c77f485d646052c5d38df3c4`; copied `inkapp.Core/Input`, ink primitives, `InkControl`, and core tests. |
| Rich text core | `archive/FY26/personal/RichTexteEditor` | `master` `9eeabe78b3433984c3d5e1cfb5674196431e5026`; copied core and tests. |
| Rich text UI | `archive/FY26/personal/RichTexteEditor` | `archive/wip-2026-03-13` `561351b6b7dd5e245cfdbc11fe81ec56fa980816`; recovered with `git archive`, then split reusable controls/services/view models from the sample host. |
| Input latency | `archive/FY26/personal/win2dlowlatecny` | `2022caf43140142f72c0486f6e7e92167949b3ce`; copied the Win2D experiment. |
| Ink research | `archive/FY26/local/inkapp-vscode` | selected drawing, eraser, persistence, and infinite-canvas notes. |
| Architecture research | `archive/FY26/local/ts` | selected drawing, infinite-canvas, undo/redo PRDs and migration plans; TypeScript application code excluded. |

## Migration decisions

- The imported UI types retain their existing namespaces (`InkControl`, `RichTexteEditor`) to avoid source-wide namespace churn; assembly/project names now describe the lab layout.
- `Ink.Core` excludes notebook, page, tag/search, block, and task models because they belong to excluded product scope and are not used by the ink control or migrated tests.
- `RichText.WinUI` is a library. `App` and `MainWindow` recovered from the archive are placed in `samples/RichTextHost`.
- No original repository was modified. This lab is published independently; the archived source locations above are relative to the workspace's `source` root.
- No package or target framework upgrades were made. `WindowsPackageType=None` was added to host/experiment projects to avoid packaging/signing requirements in the research workspace; build InputLatency with `Platform=x64` because Win2D cannot run as AnyCPU.
